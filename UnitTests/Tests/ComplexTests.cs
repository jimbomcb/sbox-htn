using HTN.Debug;
using HTN.Planner;
using System;

namespace HTN.Tests;

[TestClass]
public class ComplexTests
{
	private WorldState worldState;
	private ScopeVariables baseBindings;
	private PlanBuilder planBuilder;
	private PlanDebugState debugState;

	public static void PrintChain(PlanDebugState debugState)
	{
		Console.WriteLine("Precondition Events:");
		foreach (var ev in debugState.PreconditionEvents.Events)
		{
			Console.WriteLine($"  {ev}");
		}

		Console.WriteLine("Visited Scopes:");
		foreach (var scope in debugState.PreconditionEvents.Scopes)
		{
			Console.WriteLine($"  {scope}");
		}
	}

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
	public void Planner_Compound_ThreeLevel_Success()
	{
		// Setup a scenario where all conditions are met for the deepest level
		worldState.Add("mission", ("rescue", "hard"));
		worldState.Add("agent", ("bond", "expert"));
		worldState.Add("can_handle", ("bond", "hard"));
		worldState.Add("equipment", ("laser_watch"));
		worldState.Add("compatible", ("laser_watch", "bond"));

		var planResult = planBuilder.CreatePlan(new CompoundThreeLevel(), worldState, out var plan, baseBindings);

		Assert.AreEqual(PlanResult.Success, planResult);
		Assert.IsNotNull(plan);
		PrintChain(debugState);
		Assert.AreEqual(7, plan.Steps.Count);
		Assert.IsInstanceOfType<PrimTaskA>(plan.Steps[0].Task);
		Assert.IsInstanceOfType<StringTask>(plan.Steps[1].Task);
		Assert.IsInstanceOfType<StringTask>(plan.Steps[2].Task);
		Assert.IsInstanceOfType<StringTask>(plan.Steps[3].Task);
		Assert.IsInstanceOfType<PrimTaskB>(plan.Steps[4].Task);
		Assert.IsInstanceOfType<StringTask>(plan.Steps[5].Task);
		Assert.IsInstanceOfType<PrimTaskA>(plan.Steps[6].Task);

		Assert.AreEqual("Preparing rescue mission", ExpandStringTaskWithVars(plan.GetStep<StringTask>(1)));
		Assert.AreEqual("Agent bond equipped with laser_watch", ExpandStringTaskWithVars(plan.GetStep<StringTask>(2)));
		Assert.AreEqual("Executing hard rescue mission", ExpandStringTaskWithVars(plan.GetStep<StringTask>(3)));
		Assert.AreEqual("Mission rescue completed", ExpandStringTaskWithVars(plan.GetStep<StringTask>(5)));
		
		plan.Dispose();
	}

	private static string ExpandStringTaskWithVars(PlanStep<StringTask> planStep)
	{
		var baseString = planStep.Task.Text;
		var variables = planStep.Variables;

		foreach (var entry in variables.Bindings)
		{
			if (entry.Value is string strVal)
				baseString = baseString.Replace(entry.Key, strVal);
		}

		return baseString;
	}

	[TestMethod]
	public void Planner_Compound_ThreeLevel_BacktrackToSecondLevel()
	{
		// Setup where third level fails but second level has a backup
		worldState.Add("mission", ("infiltration", "medium"));
		worldState.Add("agent", ("smith", "novice"));
		worldState.Add("can_handle", ("smith", "medium"));
		worldState.Add("backup_plan", ("stealth_approach"));
		// No equipment or incompatible equipment - third level should fail

		var planResult = planBuilder.CreatePlan(new CompoundThreeLevel(), worldState, out var plan, baseBindings);

		Assert.AreEqual(PlanResult.Success, planResult);
		Assert.IsNotNull(plan);
		PrintChain(debugState);
		Assert.AreEqual(3, plan.Steps.Count);
		Assert.IsInstanceOfType<PrimTaskA>(plan.Steps[0].Task);
		Assert.IsInstanceOfType<StringTask>(plan.Steps[1].Task);
		Assert.IsInstanceOfType<PrimTaskA>(plan.Steps[2].Task);

		Assert.AreEqual("stealth_approach", plan.Steps[1].Variables.Get<string>("?plan"));
		Assert.AreEqual("infiltration", plan.Steps[1].Variables.Get<string>("?mission"));
		
		plan.Dispose();
	}

	[TestMethod]
	public void Planner_Compound_ThreeLevel_BacktrackToFirstLevel()
	{
		// Setup where both second and third levels fail, falls back to first level fallback
		worldState.Add("mission", ("impossible", "extreme"));
		// No agents can handle extreme difficulty, no backup plans available

		var planResult = planBuilder.CreatePlan(new CompoundThreeLevel(), worldState, out var plan, baseBindings);

		Assert.AreEqual(PlanResult.Success, planResult);
		Assert.IsNotNull(plan);
		PrintChain(debugState);
		Assert.AreEqual(1, plan.Steps.Count);
		Assert.IsInstanceOfType<PrimTaskC>(plan.Steps[0].Task);
		
		plan.Dispose();
	}

	[TestMethod]
	public void Planner_Compound_ThreeLevel_ThirdLevelImprovisation()
	{
		// Setup where third level uses improvisation branch
		worldState.Add("mission", ("sabotage", "easy"));
		worldState.Add("agent", ("jones", "expert"));
		worldState.Add("can_handle", ("jones", "easy"));
		worldState.Add("improvise", ("paperclip"));
		// No proper equipment, but improvisation is available

		var planResult = planBuilder.CreatePlan(new CompoundThreeLevel(), worldState, out var plan, baseBindings);

		Assert.AreEqual(PlanResult.Success, planResult);
		Assert.IsNotNull(plan);
		PrintChain(debugState);
		Assert.AreEqual(5, plan.Steps.Count); // PrimTaskA + Preparing + Improvising + Completed + PrimTaskA = 5
		Assert.IsInstanceOfType<PrimTaskA>(plan.Steps[0].Task);
		Assert.IsInstanceOfType<StringTask>(plan.Steps[1].Task);
		Assert.IsInstanceOfType<StringTask>(plan.Steps[2].Task);
		Assert.IsInstanceOfType<StringTask>(plan.Steps[3].Task);
		Assert.IsInstanceOfType<PrimTaskA>(plan.Steps[4].Task);

		Assert.AreEqual("Preparing sabotage mission", ExpandStringTaskWithVars(plan.GetStep<StringTask>(1)));
		Assert.AreEqual("Agent jones improvising with paperclip for sabotage", ExpandStringTaskWithVars(plan.GetStep<StringTask>(2)));
		Assert.AreEqual("Mission sabotage completed", ExpandStringTaskWithVars(plan.GetStep<StringTask>(3)));
		
		plan.Dispose();
	}

	[TestMethod]
	public void Planner_Compound_ThreeLevel_WithPresetBindings()
	{
		// Test with some bindings already set
		worldState.Add("mission", ("extraction", "hard"));
		worldState.Add("mission", ("rescue", "medium"));
		worldState.Add("agent", ("alpha", "expert"));
		worldState.Add("agent", ("beta", "novice"));
		worldState.Add("can_handle", ("alpha", "hard"));
		worldState.Add("can_handle", ("beta", "medium"));
		worldState.Add("equipment", ("grappling_hook"));
		worldState.Add("compatible", ("grappling_hook", "beta"));

		// Force it to choose the medium difficulty mission by binding
		baseBindings.Set("?difficulty", "medium");

		var planResult = planBuilder.CreatePlan(new CompoundThreeLevel(), worldState, out var plan, baseBindings);

		Assert.AreEqual(PlanResult.Success, planResult);
		Assert.IsNotNull(plan);
		PrintChain(debugState);
		Assert.AreEqual(7, plan.Steps.Count);

		// Should use beta agent for medium difficulty
		Assert.AreEqual("Preparing rescue mission", ExpandStringTaskWithVars(plan.GetStep<StringTask>(1)));
		Assert.AreEqual("Agent beta equipped with grappling_hook", ExpandStringTaskWithVars(plan.GetStep<StringTask>(2)));
		Assert.AreEqual("Executing medium rescue mission", ExpandStringTaskWithVars(plan.GetStep<StringTask>(3)));
		Assert.AreEqual("Mission rescue completed", ExpandStringTaskWithVars(plan.GetStep<StringTask>(5)));
		
		plan.Dispose();
	}
}
