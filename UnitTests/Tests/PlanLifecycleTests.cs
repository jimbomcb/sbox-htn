using HTN.Debug;
using HTN.Planner;
using HTN.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace HTN.Tests;

[TestClass]
public class PlanLifecycleTests
{
	private PlanExecutor _planExecutor;

	[TestInitialize]
	public void Setup()
	{
		_planExecutor = new PlanExecutor( new PlannerContext(null), true );
	}

	[TestCleanup]
	public void Cleanup()
	{
		_planExecutor.Dispose();
		_planExecutor = null;
	}

	[TestMethod]
	public void PlanBuilder_BasicFunctionality()
	{
		var worldState = new WorldState();
		var baseBindings = new ScopeVariables();
		var debugState = new PlanDebugState();
		var planBuilder = new PlanBuilder(null, debugState);
		TaskPool.Reset();

		var planResult = planBuilder.CreatePlan(new SingleBranchTask(), worldState, out var plan, baseBindings);

		Assert.AreEqual(PlanResult.Success, planResult);
		Assert.IsNotNull(plan);
		Assert.AreEqual(1, plan.Steps.Count);
		Assert.AreEqual("Only Choice", ((StringTask)plan.Steps[0].Task).Text);
		
		plan.Dispose();
	}

	[TestMethod]
	public void PlanBuilder_EmptyWorldState()
	{
		var emptyWorldState = new WorldState();
		var baseBindings = new ScopeVariables();
		var debugState = new PlanDebugState();
		var planBuilder = new PlanBuilder(null, debugState);
		TaskPool.Reset();

		var planResult = planBuilder.CreatePlan(new EmptyChild(), emptyWorldState, out var plan, baseBindings);

		Assert.AreEqual(PlanResult.Success, planResult);
		Assert.IsNotNull(plan);
		Assert.AreEqual(1, plan.Steps.Count);
		
		plan.Dispose();
	}

	[TestMethod]
	public void PlanBuilder_FailedPlanning()
	{
		var worldState = new WorldState();
		var baseBindings = new ScopeVariables();
		var debugState = new PlanDebugState();
		var planBuilder = new PlanBuilder(null, debugState);
		TaskPool.Reset();

		var planResult = planBuilder.CreatePlan(new AlwaysFailingTask(), worldState, out var plan, baseBindings);

		Assert.AreEqual(PlanResult.Failed, planResult);
		Assert.IsNull(plan);
	}

	[TestMethod]
	public void DebugState_EventRecording()
	{
		var worldState = new WorldState();
		var baseBindings = new ScopeVariables();
		var debugState = new PlanDebugState();
		var planBuilder = new PlanBuilder(null, debugState);
		TaskPool.Reset();

		worldState.Add("enemy", ("alpha", "castle"));
		worldState.Add("weapon", ("sword"));
		worldState.Add("can_use_weapon", ("sword", "alpha"));

		var planResult = planBuilder.CreatePlan(new ParentWithProperBinding(), worldState, out var plan, baseBindings);

		Assert.AreEqual(PlanResult.Success, planResult);
		Assert.IsNotNull(plan);
		
		Assert.IsTrue(debugState.PreconditionEvents.Events.Count > 0);
		Assert.IsTrue(debugState.QueryEvaluations > 0);
		
		plan.Dispose();
	}

	[TestMethod]
	public void TaskPool_ProperResourceManagement()
	{
		var worldState = new WorldState();
		var baseBindings = new ScopeVariables();
		var debugState = new PlanDebugState();
		var planBuilder = new PlanBuilder(null, debugState);
		TaskPool.Reset();
		
		var planResult1 = planBuilder.CreatePlan(new SingleBranchTask(), worldState, out var plan1, baseBindings);
		Assert.AreEqual(PlanResult.Success, planResult1);
		
		var planResult2 = planBuilder.CreatePlan(new SingleBranchTask(), worldState, out var plan2, baseBindings);
		Assert.AreEqual(PlanResult.Success, planResult2);
		
		plan1.Dispose();
		plan2.Dispose();
	}

	[TestMethod]
	public void WorldState_ComplexDataTypes()
	{
		var worldState = new WorldState();
		var baseBindings = new ScopeVariables();
		var debugState = new PlanDebugState();
		var planBuilder = new PlanBuilder(null, debugState);
		TaskPool.Reset();

		var complexData = new { Name = "TestObject", Value = 42, Position = new { X = 1.0f, Y = 2.0f, Z = 3.0f } };
		worldState.Add("complex_object", complexData);
		
		var planResult = planBuilder.CreatePlan(new EmptyChild(), worldState, out var plan, baseBindings);
		
		Assert.AreEqual(PlanResult.Success, planResult);
		Assert.IsNotNull(plan);
		
		plan.Dispose();
	}

	[TestMethod]
	public void StringTask_Configuration()
	{
		var stringTask = new StringTask();
		stringTask.Configure("Test Message");
		
		Assert.AreEqual("Test Message", stringTask.Text);
		
		var result = stringTask.Execute(null, new ScopeVariables());
		Assert.AreEqual(TaskResult.Success, result);
	}

	// Test classes for lifecycle verification
	internal class LifecycleTrackingTask : PrimitiveTaskBase
	{
		private static readonly List<string> s_lifecycleEvents = new();
		private readonly string _taskId;

		public LifecycleTrackingTask(string taskId)
		{
			_taskId = taskId;
		}

		public static List<string> GetLifecycleEvents() => new(s_lifecycleEvents);
		public static void ClearLifecycleEvents() => s_lifecycleEvents.Clear();

		public override bool OnPlanned(PlannerContext ctx, ScopeVariables variables)
		{
			s_lifecycleEvents.Add($"{_taskId}_OnPlanned");
			return base.OnPlanned(ctx, variables);
		}

		public override void OnPlanFinished(PlannerContext ctx, ScopeVariables variables)
		{
			s_lifecycleEvents.Add($"{_taskId}_OnPlanFinished");
			base.OnPlanFinished(ctx, variables);
		}

		public override bool OnActivate(PlannerContext ctx, ScopeVariables variables)
		{
			s_lifecycleEvents.Add($"{_taskId}_OnActivate");
			return base.OnActivate(ctx, variables);
		}

		public override void OnDeactivate(PlannerContext ctx, ScopeVariables variables)
		{
			s_lifecycleEvents.Add($"{_taskId}_OnDeactivate");
			base.OnDeactivate(ctx, variables);
		}

		public override TaskResult Execute(PlannerContext ctx, ScopeVariables variables)
		{
			s_lifecycleEvents.Add($"{_taskId}_Execute");
			return TaskResult.Success;
		}
	}

	public class LifecycleTestCompound : CompoundTaskBase
	{
		public LifecycleTestCompound()
		{
			Branches = [
				new Branch("Default", [], (_) => [
					new LifecycleTrackingTask("Task1"),
					new LifecycleTrackingTask("Task2"),
					new LifecycleTrackingTask("Task3")
				])
			];
		}
	}

	[TestMethod]
	public void OnPlannedAndOnPlanFinished_SuccessfulPlanCompletion_ShouldCallBothForAllTasks()
	{
		// validates bugfix where OnPlanFinished was not called when plan completes successfully
		// because RunningTaskIndex == _runningPlan.Steps.Count at completion
		
		LifecycleTrackingTask.ClearLifecycleEvents();
		_planExecutor.RootTask = new LifecycleTestCompound();

		// Execute the entire plan
		_planExecutor.Tick(); // Task1 planned and executed
		_planExecutor.Tick(); // Task2 planned and executed  
		_planExecutor.Tick(); // Task3 planned and executed - plan completes

		var events = LifecycleTrackingTask.GetLifecycleEvents();

		// Verify each event was called for all tasks
		Assert.IsTrue(events.Contains("Task1_OnPlanned"), "Task1 OnPlanned should be called");
		Assert.IsTrue(events.Contains("Task2_OnPlanned"), "Task2 OnPlanned should be called");
		Assert.IsTrue(events.Contains("Task3_OnPlanned"), "Task3 OnPlanned should be called");

		Assert.IsTrue(events.Contains("Task1_OnActivate"), "Task1 OnActivate should be called");
		Assert.IsTrue(events.Contains("Task2_OnActivate"), "Task2 OnActivate should be called");
		Assert.IsTrue(events.Contains("Task3_OnActivate"), "Task3 OnActivate should be called");

		Assert.IsTrue(events.Contains("Task1_Execute"), "Task1 Execute should be called");
		Assert.IsTrue(events.Contains("Task2_Execute"), "Task2 Execute should be called");
		Assert.IsTrue(events.Contains("Task3_Execute"), "Task3 Execute should be called");

		Assert.IsTrue(events.Contains("Task1_OnDeactivate"), "Task1 OnDeactivate should be called");
		Assert.IsTrue(events.Contains("Task2_OnDeactivate"), "Task2 OnDeactivate should be called");
		Assert.IsTrue(events.Contains("Task3_OnDeactivate"), "Task3 OnDeactivate should be called");

		Assert.IsTrue(events.Contains("Task1_OnPlanFinished"), "Task1 OnPlanFinished should be called");
		Assert.IsTrue(events.Contains("Task2_OnPlanFinished"), "Task2 OnPlanFinished should be called");
		Assert.IsTrue(events.Contains("Task3_OnPlanFinished"), "Task3 OnPlanFinished should be called");

		var task1Events = events.Where(e => e.StartsWith("Task1_")).ToList();
		var expectedTask1Order = new[] { "Task1_OnPlanned", "Task1_OnActivate", "Task1_Execute", "Task1_OnDeactivate", "Task1_OnPlanFinished" };
		for (int i = 0; i < expectedTask1Order.Length; i++)
		{
			Assert.AreEqual(expectedTask1Order[i], task1Events[i], $"Task1 event {i} should be {expectedTask1Order[i]}");
		}

		// Every OnPlanned should have a corresponding OnPlanFinished
		var plannedCount = events.Count(e => e.EndsWith("_OnPlanned"));
		var planFinishedCount = events.Count(e => e.EndsWith("_OnPlanFinished"));
		Assert.AreEqual(plannedCount, planFinishedCount, "Every OnPlanned call should have a corresponding OnPlanFinished call");

		// Every OnActivate should have a corresponding OnDeactivate
		var activateCount = events.Count(e => e.EndsWith("_OnActivate"));
		var deactivateCount = events.Count(e => e.EndsWith("_OnDeactivate"));
		Assert.AreEqual(activateCount, deactivateCount, "Every OnActivate call should have a corresponding OnDeactivate call");

		Assert.AreEqual(3, plannedCount, "All 3 tasks should have OnPlanned called");
		Assert.AreEqual(3, activateCount, "All 3 tasks should have OnActivate called");
		Assert.AreEqual(3, deactivateCount, "All 3 tasks should have OnDeactivate called");
		Assert.AreEqual(3, planFinishedCount, "All 3 tasks should have OnPlanFinished called");
	}

	// Test task that fails OnPlanned to test the edge case
	internal class FailingOnPlannedTask : PrimitiveTaskBase
	{
		private static readonly List<string> s_lifecycleEvents = new();
		private readonly string _taskId;

		public FailingOnPlannedTask(string taskId)
		{
			_taskId = taskId;
		}

		public static List<string> GetLifecycleEvents() => new(s_lifecycleEvents);
		public static void ClearLifecycleEvents() => s_lifecycleEvents.Clear();

		public override bool OnPlanned(PlannerContext ctx, ScopeVariables variables)
		{
			s_lifecycleEvents.Add($"{_taskId}_OnPlanned");
			return false; // Always fail OnPlanned
		}

		public override void OnPlanFinished(PlannerContext ctx, ScopeVariables variables)
		{
			s_lifecycleEvents.Add($"{_taskId}_OnPlanFinished");
			base.OnPlanFinished(ctx, variables);
		}

		public override TaskResult Execute(PlannerContext ctx, ScopeVariables variables)
		{
			s_lifecycleEvents.Add($"{_taskId}_Execute");
			return TaskResult.Success;
		}
	}

	public class OnPlannedFailureTestCompound : CompoundTaskBase
	{
		public OnPlannedFailureTestCompound()
		{
			Branches = [
				new Branch("Default", [], (_) => [
					new LifecycleTrackingTask("Task1"),
					new LifecycleTrackingTask("Task2"),
					new FailingOnPlannedTask("Task3"), // This task will fail OnPlanned
					new LifecycleTrackingTask("Task4")  // This task should NOT get OnPlanFinished called
				])
			];
		}
	}

	[TestMethod]
	public void OnPlannedFailure_ShouldOnlyCallOnPlanFinishedForSuccessfullyPlannedTasks()
	{
		// Test that when OnPlanned fails for a task, only tasks that successfully had OnPlanned called
		// should get OnPlanFinished called
		
		LifecycleTrackingTask.ClearLifecycleEvents();
		FailingOnPlannedTask.ClearLifecycleEvents();
		
		_planExecutor.RootTask = new OnPlannedFailureTestCompound();

		// This should fail to set the plan due to Task3 failing OnPlanned
		_planExecutor.Tick(); 

		var lifecycleEvents = LifecycleTrackingTask.GetLifecycleEvents();
		var failingEvents = FailingOnPlannedTask.GetLifecycleEvents();

		// Verify OnPlanned was called for Task1, Task2, and Task3
		Assert.IsTrue(lifecycleEvents.Contains("Task1_OnPlanned"), "Task1 OnPlanned should be called");
		Assert.IsTrue(lifecycleEvents.Contains("Task2_OnPlanned"), "Task2 OnPlanned should be called");
		Assert.IsTrue(failingEvents.Contains("Task3_OnPlanned"), "Task3 OnPlanned should be called");

		// Task4 should NOT have OnPlanned called because Task3 failed
		Assert.IsFalse(lifecycleEvents.Contains("Task4_OnPlanned"), "Task4 OnPlanned should NOT be called when Task3 fails");

		// Only Task1 and Task2 should get OnPlanFinished called (not Task3 which failed, not Task4 which wasn't reached)
		Assert.IsTrue(lifecycleEvents.Contains("Task1_OnPlanFinished"), "Task1 OnPlanFinished should be called");
		Assert.IsTrue(lifecycleEvents.Contains("Task2_OnPlanFinished"), "Task2 OnPlanFinished should be called");
		Assert.IsFalse(failingEvents.Contains("Task3_OnPlanFinished"), "Task3 OnPlanFinished should NOT be called when OnPlanned fails");
		Assert.IsFalse(lifecycleEvents.Contains("Task4_OnPlanFinished"), "Task4 OnPlanFinished should NOT be called when it wasn't planned");

		// Every successful OnPlanned should have a corresponding OnPlanFinished
		var allEvents = lifecycleEvents.Concat(failingEvents).ToList();
		var plannedCount = allEvents.Count(e => e.EndsWith("_OnPlanned"));
		var planFinishedCount = allEvents.Count(e => e.EndsWith("_OnPlanFinished"));
		
		Assert.AreEqual(3, plannedCount, "Should have 3 OnPlanned calls (Task1, Task2, Task3)");
		Assert.AreEqual(2, planFinishedCount, "Should have 2 OnPlanFinished calls (Task1, Task2)");

		var activateCount = allEvents.Count(e => e.EndsWith("_OnActivate"));
		var deactivateCount = allEvents.Count(e => e.EndsWith("_OnDeactivate"));
		Assert.AreEqual(0, activateCount, "No tasks should be activated when plan fails during OnPlanned");
		Assert.AreEqual(0, deactivateCount, "No tasks should be deactivated when none were activated");
	}

	// Test task that fails during execution to test activation/deactivation edge cases
	internal class FailingExecutionTask : PrimitiveTaskBase
	{
		private static readonly List<string> s_lifecycleEvents = new();
		private readonly string _taskId;

		public FailingExecutionTask(string taskId)
		{
			_taskId = taskId;
		}

		public static List<string> GetLifecycleEvents() => new(s_lifecycleEvents);
		public static void ClearLifecycleEvents() => s_lifecycleEvents.Clear();

		public override bool OnPlanned(PlannerContext ctx, ScopeVariables variables)
		{
			s_lifecycleEvents.Add($"{_taskId}_OnPlanned");
			return base.OnPlanned(ctx, variables);
		}

		public override void OnPlanFinished(PlannerContext ctx, ScopeVariables variables)
		{
			s_lifecycleEvents.Add($"{_taskId}_OnPlanFinished");
			base.OnPlanFinished(ctx, variables);
		}

		public override bool OnActivate(PlannerContext ctx, ScopeVariables variables)
		{
			s_lifecycleEvents.Add($"{_taskId}_OnActivate");
			return base.OnActivate(ctx, variables);
		}

		public override void OnDeactivate(PlannerContext ctx, ScopeVariables variables)
		{
			s_lifecycleEvents.Add($"{_taskId}_OnDeactivate");
			base.OnDeactivate(ctx, variables);
		}

		public override TaskResult Execute(PlannerContext ctx, ScopeVariables variables)
		{
			s_lifecycleEvents.Add($"{_taskId}_Execute");
			return TaskResult.Failure; // Always fail execution
		}
	}

	public class ExecutionFailureTestCompound : CompoundTaskBase
	{
		public ExecutionFailureTestCompound()
		{
			Branches = [
				new Branch("Default", [], (_) => [
					new LifecycleTrackingTask("Task1"),
					new FailingExecutionTask("Task2"), // This task will fail during execution
					new LifecycleTrackingTask("Task3")  // This task should NOT get executed
				])
			];
		}
	}

	[TestMethod]
	public void ExecutionFailure_ShouldCallOnDeactivateAndOnPlanFinishedForAllPlannedTasks()
	{
		// Test that when a task fails during execution, OnDeactivate is called for the failing task
		// and OnPlanFinished is called for all planned tasks
		
		LifecycleTrackingTask.ClearLifecycleEvents();
		FailingExecutionTask.ClearLifecycleEvents();
		
		_planExecutor.RootTask = new ExecutionFailureTestCompound();

		// Execute until Task2 fails
		_planExecutor.Tick(); // Task1 executes successfully
		_planExecutor.Tick(); // Task2 fails during execution, causing plan to be cleared

		var lifecycleEvents = LifecycleTrackingTask.GetLifecycleEvents();
		var failingEvents = FailingExecutionTask.GetLifecycleEvents();

		Assert.IsTrue(lifecycleEvents.Contains("Task1_OnPlanned"), "Task1 OnPlanned should be called");
		Assert.IsTrue(failingEvents.Contains("Task2_OnPlanned"), "Task2 OnPlanned should be called");
		Assert.IsTrue(lifecycleEvents.Contains("Task3_OnPlanned"), "Task3 OnPlanned should be called");

		Assert.IsTrue(lifecycleEvents.Contains("Task1_OnActivate"), "Task1 OnActivate should be called");
		Assert.IsTrue(lifecycleEvents.Contains("Task1_Execute"), "Task1 Execute should be called");
		Assert.IsTrue(lifecycleEvents.Contains("Task1_OnDeactivate"), "Task1 OnDeactivate should be called");

		Assert.IsTrue(failingEvents.Contains("Task2_OnActivate"), "Task2 OnActivate should be called");
		Assert.IsTrue(failingEvents.Contains("Task2_Execute"), "Task2 Execute should be called");
		Assert.IsTrue(failingEvents.Contains("Task2_OnDeactivate"), "Task2 OnDeactivate should be called after execution failure");

		Assert.IsFalse(lifecycleEvents.Contains("Task3_OnActivate"), "Task3 OnActivate should NOT be called when Task2 fails");
		Assert.IsFalse(lifecycleEvents.Contains("Task3_Execute"), "Task3 Execute should NOT be called when Task2 fails");
		Assert.IsFalse(lifecycleEvents.Contains("Task3_OnDeactivate"), "Task3 OnDeactivate should NOT be called when not activated");

		Assert.IsTrue(lifecycleEvents.Contains("Task1_OnPlanFinished"), "Task1 OnPlanFinished should be called");
		Assert.IsTrue(failingEvents.Contains("Task2_OnPlanFinished"), "Task2 OnPlanFinished should be called");
		Assert.IsTrue(lifecycleEvents.Contains("Task3_OnPlanFinished"), "Task3 OnPlanFinished should be called for all planned tasks");

		// Verify proper call order for Task1 (successful execution)
		var task1Events = lifecycleEvents.Where(e => e.StartsWith("Task1_")).ToList();
		var expectedTask1Order = new[] { "Task1_OnPlanned", "Task1_OnActivate", "Task1_Execute", "Task1_OnDeactivate", "Task1_OnPlanFinished" };
		for (int i = 0; i < expectedTask1Order.Length; i++)
		{
			Assert.AreEqual(expectedTask1Order[i], task1Events[i], $"Task1 event {i} should be {expectedTask1Order[i]}");
		}

		// Verify proper call order for Task2 (failed execution)
		var task2Events = failingEvents.Where(e => e.StartsWith("Task2_")).ToList();
		var expectedTask2Order = new[] { "Task2_OnPlanned", "Task2_OnActivate", "Task2_Execute", "Task2_OnDeactivate", "Task2_OnPlanFinished" };
		for (int i = 0; i < expectedTask2Order.Length; i++)
		{
			Assert.AreEqual(expectedTask2Order[i], task2Events[i], $"Task2 event {i} should be {expectedTask2Order[i]}");
		}

		// Verify lifecycle method call counts
		var allEvents = lifecycleEvents.Concat(failingEvents).ToList();
		var plannedCount = allEvents.Count(e => e.EndsWith("_OnPlanned"));
		var planFinishedCount = allEvents.Count(e => e.EndsWith("_OnPlanFinished"));
		var activateCount = allEvents.Count(e => e.EndsWith("_OnActivate"));
		var deactivateCount = allEvents.Count(e => e.EndsWith("_OnDeactivate"));

		Assert.AreEqual(3, plannedCount, "All 3 tasks should have OnPlanned called");
		Assert.AreEqual(3, planFinishedCount, "All 3 tasks should have OnPlanFinished called");
		Assert.AreEqual(2, activateCount, "Only 2 tasks should have OnActivate called (Task1, Task2)");
		Assert.AreEqual(2, deactivateCount, "Only 2 tasks should have OnDeactivate called (Task1, Task2)");
	}
}
