using HTN.Conditions;
using HTN.Debug;
using HTN.Planner;
using HTN.Tasks;

namespace HTN.Tests;

[TestClass]
public class BranchSelectionTests
{
	private WorldState _worldState;
	private ScopeVariables _baseBindings;
	private PlanBuilder _planBuilder;
	private PlanDebugState _debugState;

	[TestInitialize]
	public void Setup()
	{
		_worldState = new WorldState();
		_baseBindings = new ScopeVariables();
		_debugState = new PlanDebugState();
		_planBuilder = new PlanBuilder(null, _debugState);
		
		// Reset the task pool to avoid contamination between tests
		TaskPool.Reset();
	}

	[TestMethod]
	public void BranchSelection_FirstMatchingBranchSelected_NotLast()
	{
		_worldState.Add("action_available", "low_priority");
		_worldState.Add("action_available", "medium_priority");
		_worldState.Add("action_available", "high_priority");
		_worldState.Add("action_available", "fallback");

		var task = new MultiBranchTask();
		var planResult = _planBuilder.CreatePlan(task, _worldState, out var plan, _baseBindings);

		Assert.AreEqual(PlanResult.Success, planResult);
		Assert.IsNotNull(plan);
		Assert.AreEqual(1, plan.Steps.Count);
		
		var step = plan.GetStep<StringTask>(0);
		
		// should select the FIRST matching branch (high priority), not the last (fallback)
		Assert.AreEqual("HIGH_PRIORITY_TASK", step.Task.Text, 
			"Expected first matching branch to be selected, but got: " + step.Task.Text);
		
		plan.Dispose();
	}

	[TestMethod]
	public void BranchSelection_MultipleMatches_SelectsFirst()
	{
		// Setup world state where multiple branches could match
		_worldState.Add("can_do", "task1");
		_worldState.Add("can_do", "task2");
		_worldState.Add("can_do", "task3");

		var task = new OrderedBranchTask();
		var planResult = _planBuilder.CreatePlan(task, _worldState, out var plan, _baseBindings);

		Assert.AreEqual(PlanResult.Success, planResult);
		Assert.IsNotNull(plan);
		Assert.AreEqual(1, plan.Steps.Count);
		
		var step = plan.GetStep<StringTask>(0);
		
		// Should select task1 since it's the first branch, even though task2 and task3 also match
		Assert.AreEqual("TASK1", step.Task.Text, 
			"Expected first matching branch (task1) to be selected");
		
		plan.Dispose();
	}

	[TestMethod]
	public void BranchSelection_CitizenTaskLike_WanderIsLastResort()
	{
		// Start with empty world state (no specific actions available)
		var task = new CitizenTaskLike();
		var planResult = _planBuilder.CreatePlan(task, _worldState, out var plan, _baseBindings);

		Assert.AreEqual(PlanResult.Success, planResult);
		Assert.IsNotNull(plan);
		Assert.AreEqual(1, plan.Steps.Count);
		
		var step = plan.GetStep<StringTask>(0);
		
		Assert.AreEqual("WANDER", step.Task.Text, 
			"Expected wander to be selected when no other actions are available");
		
		plan.Dispose();

		_worldState.Add("has_work", "true");
		
		var planResult2 = _planBuilder.CreatePlan(task, _worldState, out var plan2, _baseBindings);

		Assert.AreEqual(PlanResult.Success, planResult2);
		Assert.IsNotNull(plan2);
		Assert.AreEqual(1, plan2.Steps.Count);
		
		var step2 = plan2.GetStep<StringTask>(0);
		
		Assert.AreEqual("WORK", step2.Task.Text, 
			"Expected work to be selected over wander when available");
		
		plan2.Dispose();
	}

	[TestMethod]
	public void BranchSelection_MultipleConditionResults_SelectsFirstValid()
	{
		// Add multiple matching items for the same query
		_worldState.Add("enemy", ("goblin", "weak"));
		_worldState.Add("enemy", ("orc", "strong"));
		_worldState.Add("enemy", ("dragon", "legendary"));

		var task = new EnemyHandlerTask();
		var planResult = _planBuilder.CreatePlan(task, _worldState, out var plan, _baseBindings);

		Assert.AreEqual(PlanResult.Success, planResult);
		Assert.IsNotNull(plan);
		Assert.AreEqual(1, plan.Steps.Count);
		
		var step = plan.GetStep<StringTask>(0);
		
		Assert.IsTrue(step.Task.Text.StartsWith("FIGHT_"), 
			"Expected to select fight action for first enemy");
		
		plan.Dispose();
	}

	public class MultiBranchTask : CompoundTaskBase
	{
		public MultiBranchTask()
		{
			Branches = [
				new Branch("HighPriority", [
					new Query("action_available", "high_priority")
				], (_) => [
					Run<StringTask>().Configure("HIGH_PRIORITY_TASK")
				]),

				new Branch("MediumPriority", [
					new Query("action_available", "medium_priority")
				], (_) => [
					Run<StringTask>().Configure("MEDIUM_PRIORITY_TASK")
				]),

				new Branch("LowPriority", [
					new Query("action_available", "low_priority")
				], (_) => [
					Run<StringTask>().Configure("LOW_PRIORITY_TASK")
				]),

				new Branch("Fallback", [
					new Query("action_available", "fallback")
				], (_) => [
					Run<StringTask>().Configure("FALLBACK_TASK")
				])
			];
		}
	}

	public class OrderedBranchTask : CompoundTaskBase
	{
		public OrderedBranchTask()
		{
			Branches = [
				new Branch("Task1", [
					new Query("can_do", "task1")
				], (_) => [
					Run<StringTask>().Configure("TASK1")
				]),

				new Branch("Task2", [
					new Query("can_do", "task2")
				], (_) => [
					Run<StringTask>().Configure("TASK2")
				]),

				new Branch("Task3", [
					new Query("can_do", "task3")
				], (_) => [
					Run<StringTask>().Configure("TASK3")
				])
			];
		}
	}

	public class CitizenTaskLike : CompoundTaskBase
	{
		public CitizenTaskLike()
		{
			Branches = [
				new Branch("Work", [
					new Query("has_work", "true")
				], (_) => [
					Run<StringTask>().Configure("WORK")
				]),

				new Branch("Rest", [
					new Query("is_tired", "true")
				], (_) => [
					Run<StringTask>().Configure("REST")
				]),

				new Branch("Eat", [
					new Query("is_hungry", "true")
				], (_) => [
					Run<StringTask>().Configure("EAT")
				]),

				// Wander should be the fallback - always succeeds but should be last
				new Branch("Wander", [
					ICondition.AlwaysTrue
				], (_) => [
					Run<StringTask>().Configure("WANDER")
				])
			];
		}
	}

	public class EnemyHandlerTask : CompoundTaskBase
	{
		public EnemyHandlerTask()
		{
			Branches = [
				new Branch("FightEnemy", [
					new Query("enemy", "?enemy_name", "?enemy_type")
				], (vars) => [
					Run<StringTask>().Configure($"FIGHT_{vars.Get<string>("?enemy_name")}")
				]),

				new Branch("NoEnemies", [
					ICondition.AlwaysTrue
				], (_) => [
					Run<StringTask>().Configure("PATROL")
				])
			];
		}
	}
}
