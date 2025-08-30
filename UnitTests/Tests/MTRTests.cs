using HTN.Debug;
using HTN.Planner;

namespace HTN.Tests;

[TestClass]
public class MTRTests
{
	private WorldState worldState;
	private ScopeVariables baseBindings;
	private PlanBuilder planBuilder;
	private PlanDebugState debugState;

	[TestInitialize]
	public void Setup()
	{
		worldState = new WorldState();
		baseBindings = new ScopeVariables();
		debugState = new PlanDebugState();
		planBuilder = new PlanBuilder(null, debugState);
		TaskPool.Reset();
	}

	[TestMethod]
	public void MTR_EdgeCase_EmptyMTR()
	{
		var emptyMTR = new PlanMTR();
		var planResult = planBuilder.CreatePlan(new ComplexMTRTask(), worldState, out var plan, baseBindings, emptyMTR);

		Assert.AreEqual(PlanResult.Success, planResult);
		Assert.IsNotNull(plan);
		Assert.AreEqual(new PlanMTR(1), plan.MTR);
		plan.Dispose();
	}

	[TestMethod]
	public void MTR_EdgeCase_MTRTooLong()
	{
		// MTR longer than what the task can actually decompose to
		var previousMTR = new PlanMTR(9, 0, 0, 1, 2, 3);
		var planResult = planBuilder.CreatePlan(new ComplexMTRTask(), worldState, out var plan, baseBindings, previousMTR);

		Assert.AreEqual(PlanResult.Success, planResult);
		Assert.IsNotNull(plan);
		Assert.AreEqual(new PlanMTR(1), plan.MTR);
		plan.Dispose();
	}

	[TestMethod]
	public void MTR_EdgeCase_InvalidBranchIndex()
	{
		// Using branch index 5 when only 0,1 exist
		var previousMTR = new PlanMTR(5);
		var planResult = planBuilder.CreatePlan(new ComplexMTRTask(), worldState, out var plan, baseBindings, previousMTR);

		Assert.AreEqual(PlanResult.Success, planResult);
		Assert.IsNotNull(plan);
		Assert.AreEqual(new PlanMTR(1), plan.MTR);
		plan.Dispose();
	}

	[TestMethod]
	public void MTR_EdgeCase_VeryDeepTraversal()
	{
		worldState.Add("deep_condition", "true");

		var planResult = planBuilder.CreatePlan(new DeepMTRTask(), worldState, out var plan, baseBindings);

		Assert.AreEqual(PlanResult.Success, planResult);
		Assert.IsNotNull(plan);
		Assert.AreEqual("Deep Success", ((StringTask)plan.Steps[0].Task).Text);
		Assert.AreEqual(new PlanMTR(0, 0, 0, 0), plan.MTR);
		plan.Dispose();
	}

	[TestMethod]
	public void MTR_EdgeCase_VeryDeepContinuation()
	{
		worldState.Add("deep_condition", "true");

		var previousMTR = new PlanMTR(0, 0, 0, 0);
		var planResult = planBuilder.CreatePlan(new DeepMTRTask(), worldState, out var plan, baseBindings, previousMTR);

		Assert.AreEqual(PlanResult.Continue, planResult);
	}

	[TestMethod]
	public void MTR_EdgeCase_DeepBacktracking()
	{
		// No deep_condition, should backtrack through multiple levels
		var planResult = planBuilder.CreatePlan(new DeepMTRTask(), worldState, out var plan, baseBindings);

		Assert.AreEqual(PlanResult.Success, planResult);
		Assert.IsNotNull(plan);
		Assert.AreEqual("Level4 Fallback", ((StringTask)plan.Steps[0].Task).Text);

		Assert.AreEqual(new PlanMTR(0, 0, 0, 1), plan.MTR);
		plan.Dispose();
	}

	[TestMethod]
	public void MTR_EdgeCase_PartialMTRMatch()
	{
		worldState.Add("deep_condition", "true");

		// Previous plan was at [0,0,0,1], new condition allows [0,0,0,0]
		var previousMTR = new PlanMTR(0, 0, 0, 1);
		var planResult = planBuilder.CreatePlan(new DeepMTRTask(), worldState, out var plan, baseBindings, previousMTR);

		Assert.AreEqual(PlanResult.Success, planResult);
		Assert.IsNotNull(plan);
		Assert.AreEqual("Deep Success", ((StringTask)plan.Steps[0].Task).Text);
		Assert.AreEqual(new PlanMTR(0, 0, 0, 0), plan.MTR);
		plan.Dispose();
	}

	[TestMethod]
	public void MTR_EdgeCase_WideBranching()
	{
		worldState.Add("wide_4", "true");

		var planResult = planBuilder.CreatePlan(new WideMTRTask(), worldState, out var plan, baseBindings);

		Assert.AreEqual(PlanResult.Success, planResult);
		Assert.IsNotNull(plan);
		Assert.AreEqual("Wide Branch 4", ((StringTask)plan.Steps[0].Task).Text);
		Assert.AreEqual(new PlanMTR(4), plan.MTR);
		plan.Dispose();
	}

	[TestMethod]
	public void MTR_EdgeCase_WideBranchProgression()
	{
		worldState.Add("wide_4", "true");
		worldState.Add("wide_2", "true");

		// Previous was branch 4, should find higher priority branch 2
		var previousMTR = new PlanMTR(4);
		var planResult = planBuilder.CreatePlan(new WideMTRTask(), worldState, out var plan, baseBindings, previousMTR);

		Assert.AreEqual(PlanResult.Success, planResult);
		Assert.IsNotNull(plan);
		Assert.AreEqual("Wide Branch 2", ((StringTask)plan.Steps[0].Task).Text);
		Assert.AreEqual(new PlanMTR(2), plan.MTR);
		plan.Dispose();
	}

	[TestMethod]
	public void MTR_EdgeCase_SingleBranch()
	{
		var planResult = planBuilder.CreatePlan(new SingleBranchTask(), worldState, out var plan, baseBindings);

		Assert.AreEqual(PlanResult.Success, planResult);
		Assert.IsNotNull(plan);
		Assert.AreEqual(new PlanMTR(0), plan.MTR);
		plan.Dispose();
	}

	[TestMethod]
	public void MTR_EdgeCase_SingleBranchWithHighMTR()
	{
		var previousMTR = new PlanMTR(5);
		var planResult = planBuilder.CreatePlan(new SingleBranchTask(), worldState, out var plan, baseBindings, previousMTR);

		Assert.AreEqual(PlanResult.Success, planResult);
		Assert.IsNotNull(plan);
		Assert.AreEqual(new PlanMTR(0), plan.MTR);
		plan.Dispose();
	}

	[TestMethod]
	public void MTR_EdgeCase_SingleBranchExactMatch()
	{
		var previousMTR = new PlanMTR(0);
		var planResult = planBuilder.CreatePlan(new SingleBranchTask(), worldState, out var plan, baseBindings, previousMTR);

		Assert.AreEqual(PlanResult.Continue, planResult);
	}

	[TestMethod]
	public void MTR_EdgeCase_AlwaysFailingTask()
	{
		var planResult = planBuilder.CreatePlan(new AlwaysFailingTask(), worldState, out var plan, baseBindings);

		Assert.AreEqual(PlanResult.Failed, planResult);
		Assert.IsNull(plan);
	}

	[TestMethod]
	public void MTR_EdgeCase_MixedDepthComparison()
	{
		// Deep branch without conditions should actually complete the deep decomposition 
		// and fall back to the deepest level, not to the shallow branch
		var planResult = planBuilder.CreatePlan(new MixedDepthTask(), worldState, out var plan, baseBindings);

		Assert.AreEqual(PlanResult.Success, planResult);
		Assert.IsNotNull(plan);

		// Should get "Level4 Fallback" because the deep branch tries all levels and falls back at the deepest level
		Assert.AreEqual("Level4 Fallback", ((StringTask)plan.Steps[0].Task).Text);
		Assert.AreEqual(new PlanMTR(0, 0, 0, 1), plan.MTR);
		plan.Dispose();
	}

	[TestMethod]
	public void MTR_EdgeCase_MismatchedDepthContinuation()
	{
		// Previous MTR was from a deep decomposition that ultimately succeeded with [0,0,0,1]
		// Now the same world state should produce the same plan, making it a continuation
		var previousMTR = new PlanMTR(0, 0, 0, 1);
		var planResult = planBuilder.CreatePlan(new MixedDepthTask(), worldState, out var plan, baseBindings, previousMTR);

		// Since world state hasn't changed, it should be a continuation of the same plan
		Assert.AreEqual(PlanResult.Continue, planResult);
	}

	[TestMethod]
	public void MTR_EdgeCase_DeepToShallowFallback()
	{
		// Test without the condition - should use shallow branch
		var planResult = planBuilder.CreatePlan(new ConditionalDepthTask(), worldState, out var plan, baseBindings);

		Assert.AreEqual(PlanResult.Success, planResult);
		Assert.IsNotNull(plan);
		Assert.AreEqual("Shallow Fallback", ((StringTask)plan.Steps[0].Task).Text);
		Assert.AreEqual(new PlanMTR(1), plan.MTR);

		// Now test with a previous deep MTR - since deep branch is unavailable, should still use shallow
		var previousDeepMTR = new PlanMTR(0, 0, 0, 1);  // Some deep MTR from when condition was true
		var planResult2 = planBuilder.CreatePlan(new ConditionalDepthTask(), worldState, out var plan2, baseBindings, previousDeepMTR);

		Assert.AreEqual(PlanResult.Success, planResult2);
		Assert.IsNotNull(plan2);
		Assert.AreEqual("Shallow Fallback", ((StringTask)plan2.Steps[0].Task).Text);
		Assert.AreEqual(new PlanMTR(1), plan2.MTR);
		
		plan.Dispose();
		plan2.Dispose();
	}
}
