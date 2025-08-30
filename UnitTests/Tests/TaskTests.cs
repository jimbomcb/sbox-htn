using HTN.Conditions;
using HTN.Debug;
using HTN.Planner;
using HTN.Tasks;
using System;

namespace HTN.Tests;

[TestClass]
public partial class TaskTests
{
	private WorldState worldState;
	private ScopeVariables baseBindings;
	private PlanBuilder planBuilder;
	private PlanDebugState debugState;

	public static void PrintChain( PlanDebugState ctx )
	{
		var record = ctx.PreconditionEvents;
		Console.WriteLine( "Events:" );
		foreach ( var ev in record.Events )
		{
			Console.WriteLine( $"  {ev}" );
		}

		Console.WriteLine( "Visited Scopes:" );
		foreach ( var scope in record.VisitedScopes )
		{
			Console.WriteLine( $"  {scope}" );
		}
	}

	[TestInitialize]
	public void Setup()
	{
		worldState = new WorldState();
		baseBindings = new ScopeVariables();
		debugState = new PlanDebugState();
		planBuilder = new PlanBuilder(null, debugState);
		
		// Reset the task pool to avoid contamination between tests
		TaskPool.Reset();
	}

	[TestMethod]
	public void Planner_Primitive_Basic()
	{
		var planResult = planBuilder.CreatePlan(new PrimTaskA(), worldState, out var plan, baseBindings);

		Assert.AreEqual(PlanResult.Success, planResult);
		Assert.IsNotNull(plan);
		PrintChain(debugState);

		Assert.AreEqual(plan.Steps.Count, 1);
		Assert.IsInstanceOfType<PrimTaskA>(plan.Steps[0].Task);
		
		plan.Dispose();
	}

	public class CompoundNoPreconds : CompoundTaskBase
	{
		public CompoundNoPreconds()
		{
			Branches = [
				new Branch("Branch1", [], (_) => [Run<PrimTaskA>(), Run<PrimTaskB>(), Run<PrimTaskA>()]),
				new Branch("Branch2", [], (_) => [Run<PrimTaskC>()]),
			];
		}
	}

	[TestMethod]
	public void Planner_Compound_NoPreconditions()
	{
		var planResult = planBuilder.CreatePlan(new CompoundNoPreconds(), worldState, out var plan, baseBindings);

		Assert.AreEqual(PlanResult.Success, planResult);
		Assert.IsNotNull(plan);
		PrintChain(debugState);
		Assert.AreEqual(plan.Steps.Count, 3);
		Assert.IsInstanceOfType<PrimTaskA>(plan.Steps[0].Task);
		Assert.IsInstanceOfType<PrimTaskB>(plan.Steps[1].Task);
		Assert.IsInstanceOfType<PrimTaskA>(plan.Steps[2].Task);
		
		plan.Dispose();
	}

	public class CompoundPreconds : CompoundTaskBase
	{
		public CompoundPreconds()
		{
			Branches = [
				new Branch("Branch1", [new Query("enemy", "?enemy", "?bool")], (_) => [Run<PrimTaskA>(), Run<PrimTaskB>(), Run<PrimTaskA>()]),
				new Branch("Branch2", [], (_) => [Run<PrimTaskC>(), Run<PrimTaskB>()]),
			];
		}
	}

	[TestMethod]
	public void Planner_Compound_Preconditions_NoEnemy()
	{
		var planResult = planBuilder.CreatePlan(new CompoundPreconds(), worldState, out var plan, baseBindings);

		Assert.AreEqual(PlanResult.Success, planResult);
		Assert.IsNotNull(plan);
		PrintChain(debugState);
		Assert.AreEqual(plan.Steps.Count, 2);
		Assert.IsInstanceOfType<PrimTaskC>(plan.Steps[0].Task);
		Assert.IsInstanceOfType<PrimTaskB>(plan.Steps[1].Task);
		
		plan.Dispose();
	}

	[TestMethod]
	public void Planner_Compound_Preconditions_Enemy()
	{
		worldState.Add("enemy", ("alpha", false));
		worldState.Add("enemy", ("bravo", true));

		var planResult = planBuilder.CreatePlan(new CompoundPreconds(), worldState, out var plan, baseBindings);

		Assert.AreEqual(PlanResult.Success, planResult);
		Assert.IsNotNull(plan);
		PrintChain(debugState);
		Assert.AreEqual(plan.Steps.Count, 3);
		Assert.IsInstanceOfType<PrimTaskA>(plan.Steps[0].Task);
		Assert.IsInstanceOfType<PrimTaskB>(plan.Steps[1].Task);
		Assert.IsInstanceOfType<PrimTaskA>(plan.Steps[2].Task);
		
		plan.Dispose();
	}

	public class CompoundPrecondsParams : CompoundTaskBase
	{
		public CompoundPrecondsParams()
		{
			Branches = [
				new("Branch1", 
					[new Query("enemy", "?enemy", "?var")], 
					(vars) => [
						Run<StringExpandingTask>().Configure(vars.Get<string>("?enemy")), 
						Run<StringExpandingTask>().Configure(vars.Get<string>("?var"))
					]
				),
				new("Branch2", [], (_) => [Run<PrimTaskC>(), Run<PrimTaskB>()])
			];
		}
	}

	[TestMethod]
	public void Planner_Compound_Preconditions_EnemyParameter()
	{
		worldState.Add( "enemy", ("alpha", "foo") );
		worldState.Add( "enemy", ("bravo", "bar") );

		var planResult = planBuilder.CreatePlan( new CompoundPrecondsParams(), worldState, out var plan );

		Assert.AreEqual(PlanResult.Success, planResult);
		Assert.IsNotNull( plan );
		PrintChain( debugState );
		Assert.AreEqual( plan.Steps.Count, 2 );

		var (task0, task0Vars) = plan.GetStep<StringExpandingTask>(0);
		Assert.AreEqual( "alpha", task0.String );

		var (task1, task1Vars) = plan.GetStep<StringExpandingTask>(1);
		Assert.AreEqual( "foo", task1.String );
		
		plan.Dispose();
	}

	[TestMethod]
	public void Planner_Compound_Preconditions_StartingBindings()
	{
		worldState.Add("enemy", ("alpha", "foo"));
		worldState.Add("enemy", ("bravo", "bar"));
	
		baseBindings.Set("?var", "bar");
	
		var planResult = planBuilder.CreatePlan(new CompoundPrecondsParams(), worldState, out var plan, baseBindings);
	
		Assert.AreEqual(PlanResult.Success, planResult);
		Assert.IsNotNull(plan);
		PrintChain(debugState);
		Assert.AreEqual(plan.Steps.Count, 2);
		var (task0, task0Vars) = plan.GetStep<StringExpandingTask>( 0 );
		Assert.AreEqual( "bravo", task0.String );
	
		var (task1, task1Vars) = plan.GetStep<StringExpandingTask>( 1 );
		Assert.AreEqual( "bar", task1.String );
		
		plan.Dispose();
	}

	[TestMethod]
	public void Planner_Compound_Preconditions_StartingBindingsNoMatch()
	{
		worldState.Add("enemy", ("alpha", "foo"));
		worldState.Add("enemy", ("bravo", "bar"));
	
		baseBindings.Set("?var", "barfoo");
		// No enemies match barfoo in the ?var parameter, so falls back to 2nd branch
	
		var planResult = planBuilder.CreatePlan(new CompoundPrecondsParams(), worldState, out var plan, baseBindings );
	
		Assert.AreEqual(PlanResult.Success, planResult);
		Assert.IsNotNull(plan);
		PrintChain(debugState);
		Assert.AreEqual(2, plan.Steps.Count);
		Assert.IsInstanceOfType<PrimTaskC>(plan.Steps[0].Task);
		Assert.IsInstanceOfType<PrimTaskB>(plan.Steps[1].Task);
		
		plan.Dispose();
	}

	public class CompoundPrecondsParamsNoFallback : CompoundTaskBase
	{
		public CompoundPrecondsParamsNoFallback()
		{
			Branches = [
				new("Branch1", 
					[new Query("enemy", "?enemy", "?var")], 
					(vars) => [
						Run<StringExpandingTask>().Configure(vars.Get<string>("?enemy")), 
						Run<StringExpandingTask>().Configure(vars.Get<string>("?var"))
					]
				),
			];
		}
	}

	[TestMethod]
	public void Planner_Compound_PreconditionsNoFallback()
	{
		worldState.Add("enemy", ("alpha", "foo"));
		worldState.Add("enemy", ("bravo", "bar"));
	
		baseBindings.Set("?var", "barfoo");
		// No enemies match barfoo in the ?var parameter, and there's no fallback branch
	
		var planResult = planBuilder.CreatePlan(new CompoundPrecondsParamsNoFallback(), worldState, out var plan, baseBindings );
																								  
		Assert.AreEqual(PlanResult.Failed, planResult);													  
		Assert.IsNull(plan);															  
	}																							  
																								  
	[TestMethod]																				  
	public void Planner_Compound_OuterInner_FirstMatching()										  
	{																							  
		worldState.Add("enemy", ("alpha", "foo"));												  
		worldState.Add( "enemy", ("bravo", "bar"));												  
		worldState.Add( "weapon", ("sword"));													  
		worldState.Add( "weapon", ("gun"));														  
		worldState.Add( "can_use_weapon", ("sword", "alpha"));									  
		worldState.Add( "can_use_weapon", ("gun", "bravo"));										  

		var planResult = planBuilder.CreatePlan(new CompoundOuter(), worldState, out var plan, baseBindings );

		Assert.AreEqual(PlanResult.Success, planResult);
		Assert.IsNotNull(plan);
		PrintChain(debugState);
		Assert.AreEqual(plan.Steps.Count, 3);
		Assert.IsInstanceOfType<PrimTaskA>(plan.Steps[0].Task);
		Assert.IsInstanceOfType<StringTask>(plan.Steps[1].Task);
		Assert.IsInstanceOfType<PrimTaskA>(plan.Steps[2].Task);

		var step1 = plan.GetStep<StringTask>(1);
		Assert.AreEqual("alpha attacking with weapon sword", step1.Task.Text);

		// MTR
		Assert.AreEqual(new PlanMTR(0, 0), plan.MTR);
		
		plan.Dispose();
	}
	[TestMethod]
	public void Planner_Compound_OuterInner_Backtrack()
	{
		// Alpha can't use sword so it backtracks	

		worldState.Add("enemy", ("alpha", "foo"));
		worldState.Add("enemy", ("bravo", "bar"));
		worldState.Add("weapon", ("sword"));
		worldState.Add("weapon", ("gun"));
		worldState.Add("can_use_weapon", ("spear", "alpha"));
		worldState.Add("can_use_weapon", ("gun", "bravo"));

		var planResult = planBuilder.CreatePlan(new CompoundOuter(), worldState, out var plan, baseBindings);

		Assert.AreEqual(PlanResult.Success, planResult);
		Assert.IsNotNull(plan);
		PrintChain(debugState);
		Assert.AreEqual(3, plan.Steps.Count);
		Assert.IsInstanceOfType<PrimTaskA>(plan.Steps[0].Task );
		Assert.IsInstanceOfType<StringTask>(plan.Steps[1].Task );
		Assert.IsInstanceOfType<PrimTaskA>(plan.Steps[2].Task );

		var step1 = plan.GetStep<StringTask>( 1 );
		Assert.AreEqual( "bravo attacking with weapon gun", step1.Task.Text );

		// MTR
		Assert.AreEqual(new PlanMTR(0, 0), plan.MTR);
		
		plan.Dispose();
	}

	[TestMethod]
	public void Planner_Compound_OuterInnerAnd_FirstMatching()
	{
		worldState.Add("enemy", ("alpha", "foo"));
		worldState.Add("enemy", ("bravo", "bar"));
		worldState.Add("weapon", ("sword"));
		worldState.Add("weapon", ("gun"));
		worldState.Add("can_use_weapon", ("sword", "alpha"));
		worldState.Add("can_use_weapon", ("gun", "bravo"));

		var planResult = planBuilder.CreatePlan(new CompoundOuterAnd(), worldState, out var plan, baseBindings);

		Assert.AreEqual(PlanResult.Success, planResult);
		Assert.IsNotNull(plan);
		PrintChain(debugState);
		Assert.AreEqual(plan.Steps.Count, 3);
		Assert.IsInstanceOfType<PrimTaskA>(plan.Steps[0].Task );
		Assert.IsInstanceOfType<StringTask>(plan.Steps[1].Task );
		Assert.IsInstanceOfType<PrimTaskA>(plan.Steps[2].Task );

		var step1 = plan.GetStep<StringTask>(1);
		Assert.AreEqual( "alpha attacking with weapon sword", step1.Task.Text );

		// MTR
		Assert.AreEqual(new PlanMTR(0, 0), plan.MTR);
		
		plan.Dispose();
	}

	[TestMethod]
	public void Planner_Compound_OuterInnerAnd_Backtrack()
	{
		// Alpha can't use sword so it backtracks 

		worldState.Add("enemy", ("alpha", "foo"));
		worldState.Add("enemy", ("bravo", "bar"));
		worldState.Add("weapon", ("sword"));
		worldState.Add("weapon", ("gun"));
		worldState.Add("can_use_weapon", ("spear", "alpha"));
		worldState.Add("can_use_weapon", ("gun", "bravo"));

		var planResult = planBuilder.CreatePlan(new CompoundOuterAnd(), worldState, out var plan, baseBindings);

		Assert.AreEqual(PlanResult.Success, planResult);
		Assert.IsNotNull(plan);
		PrintChain(debugState);
		Assert.AreEqual(3, plan.Steps.Count);
		Assert.IsInstanceOfType<PrimTaskA>(plan.Steps[0].Task);
		Assert.IsInstanceOfType<StringTask>(plan.Steps[1].Task);
		Assert.IsInstanceOfType<PrimTaskA>(plan.Steps[2].Task);

		var step1 = plan.GetStep<StringTask>(1);
		Assert.AreEqual("bravo attacking with weapon gun", step1.Task.Text);

		// MTR
		Assert.AreEqual(new PlanMTR(0, 0), plan.MTR);
		
		plan.Dispose();
	}

	[TestMethod]
	public void Planner_Compound_OuterInner_Backtrack2Levels()
	{
		worldState.Add("enemy", ("alpha", "foo"));
		worldState.Add("enemy", ("bravo", "bar"));
		worldState.Add("weapon", ("sword"));
		worldState.Add("weapon", ("gun"));
		worldState.Add("can_use_weapon", ("spear", "alpha"));
		worldState.Add("can_use_weapon", ("harpoon", "bravo"));

		var planResult = planBuilder.CreatePlan(new CompoundOuter(), worldState, out var plan, baseBindings);

		Assert.AreEqual(PlanResult.Success, planResult);
		Assert.IsNotNull(plan);
		PrintChain(debugState);
		Assert.AreEqual(2, plan.Steps.Count);
		Assert.IsInstanceOfType<PrimTaskC>(plan.Steps[0].Task);
		Assert.IsInstanceOfType<PrimTaskB>(plan.Steps[1].Task);

		// MTR
		Assert.AreEqual(new PlanMTR(1), plan.MTR);
		
		plan.Dispose();
	}

	[TestMethod]
	public void Planner_Compound_OuterInnerAnd_Backtrack2Levels()
	{
		worldState.Add("enemy", ("alpha", "foo"));
		worldState.Add("enemy", ("bravo", "bar"));
		worldState.Add("weapon", ("sword"));
		worldState.Add("weapon", ("gun"));
		worldState.Add("can_use_weapon", ("spear", "alpha"));
		worldState.Add("can_use_weapon", ("harpoon", "bravo"));

		var planResult = planBuilder.CreatePlan(new CompoundOuterAnd(), worldState, out var plan, baseBindings);

		Assert.AreEqual(PlanResult.Success, planResult);
		Assert.IsNotNull(plan);
		PrintChain(debugState);
		Assert.AreEqual(2, plan.Steps.Count);
		Assert.IsInstanceOfType<PrimTaskC>(plan.Steps[0].Task);
		Assert.IsInstanceOfType<PrimTaskB>(plan.Steps[1].Task);

		// MTR
		Assert.AreEqual(new PlanMTR(1), plan.MTR);
		
		plan.Dispose();
	}

	[TestMethod]
	public void Planner_Compound_OuterInnerParams_Backtrack()
	{
		// Alpha can't use available weapons so it backtracks to bravo

		worldState.Add("enemy", ("alpha", "foo"));
		worldState.Add("enemy", ("bravo", "bar"));
		worldState.Add("weapon", ("sword"));
		worldState.Add("weapon", ("gun"));
		worldState.Add("can_use_weapon", ("spear", "alpha"));
		worldState.Add("can_use_weapon", ("gun", "bravo"));

		var planResult = planBuilder.CreatePlan(new CompoundOuterAutoBindParams(), worldState, out var plan, baseBindings);

		Assert.AreEqual(PlanResult.Success, planResult);
		Assert.IsNotNull(plan);
		PrintChain(debugState);
		Assert.AreEqual(3, plan.Steps.Count);
		Assert.IsInstanceOfType<PrimTaskA>(plan.Steps[0].Task);
		Assert.IsInstanceOfType<StringTask>(plan.Steps[1].Task);
		Assert.IsInstanceOfType<PrimTaskA>(plan.Steps[2].Task);

		var step1 = plan.GetStep<StringTask>(1);
		Assert.AreEqual("bravo attacking with weapon gun", step1.Task.Text);
		
		plan.Dispose();
	}

	[TestMethod]
	public void Planner_Compound_OuterInnerParams_Backtrack2Levels()
	{
		worldState.Add("enemy", ("alpha", "foo"));
		worldState.Add("enemy", ("bravo", "bar"));
		worldState.Add("weapon", ("sword"));
		worldState.Add("weapon", ("gun"));
		worldState.Add("can_use_weapon", ("spear", "alpha"));
		worldState.Add("can_use_weapon", ("harpoon", "bravo"));

		var planResult = planBuilder.CreatePlan(new CompoundOuterAutoBindParams(), worldState, out var plan, baseBindings);

		Assert.AreEqual(PlanResult.Success, planResult);
		Assert.IsNotNull(plan);
		PrintChain(debugState);
		Assert.AreEqual(2, plan.Steps.Count);
		Assert.IsInstanceOfType<PrimTaskC>(plan.Steps[0].Task);
		Assert.IsInstanceOfType<PrimTaskB>(plan.Steps[1].Task);
		
		plan.Dispose();
	}

	[TestMethod]
	public void Planner_Compound_OuterInnerParamsRename()
	{
		// Setup where alpha can use sword - first matching scenario

		worldState.Add("enemy", ("alpha", "foo"));
		worldState.Add("enemy", ("bravo", "bar"));
		worldState.Add("weapon", ("sword"));
		worldState.Add("weapon", ("gun"));
		worldState.Add("can_use_weapon", ("sword", "alpha"));
		worldState.Add("can_use_weapon", ("gun", "bravo"));

		var planResult = planBuilder.CreatePlan(new CompoundOuterAutoBindRenameParams(), worldState, out var plan, baseBindings);

		Assert.AreEqual(PlanResult.Success, planResult);
		Assert.IsNotNull(plan);
		PrintChain(debugState);
		Assert.AreEqual(3, plan.Steps.Count);
		Assert.IsInstanceOfType<PrimTaskA>(plan.Steps[0].Task);
		Assert.IsInstanceOfType<StringTask>(plan.Steps[1].Task);
		Assert.IsInstanceOfType<PrimTaskA>(plan.Steps[2].Task);

		var step1 = plan.GetStep<StringTask>(1);
		Assert.AreEqual( "alpha attacking with weapon sword", step1.Task.Text);

		plan.Dispose();
	}

	[TestMethod]
	public void Planner_Compound_OuterInnerParamsRename_WithBinding()
	{
		// Setup where binding forces bravo to be selected
	
		worldState.Add("enemy", ("alpha", "foo"));
		worldState.Add("enemy", ("bravo", "bar"));
		worldState.Add("weapon", ("sword"));
		worldState.Add("weapon", ("gun"));
		worldState.Add("can_use_weapon", ("sword", "alpha"));
		worldState.Add("can_use_weapon", ("gun", "bravo"));
	
		baseBindings.Set("?var", "bar");
		var planResult = planBuilder.CreatePlan(new CompoundOuterAutoBindRenameParams(), worldState, out var plan, baseBindings );
	
		Assert.AreEqual(PlanResult.Success, planResult);
		Assert.IsNotNull(plan);
		PrintChain(debugState);
		Assert.AreEqual(3, plan.Steps.Count);
		Assert.IsInstanceOfType<PrimTaskA>(plan.Steps[0].Task);
		Assert.IsInstanceOfType<StringTask>(plan.Steps[1].Task);
		Assert.IsInstanceOfType<PrimTaskA>(plan.Steps[2].Task);
	
		var step1 = plan.GetStep<StringTask>(1);
		Assert.AreEqual( "bravo attacking with weapon gun", step1.Task.Text);
	
		// todo: after static task change revisit these
		plan.Dispose();
	}
	
	[TestMethod]
	public void Planner_Compound_OuterInnerParamsRename_WithBindingEnemy()
	{
		worldState.Add("enemy", ("alpha", "foo"));
		worldState.Add("enemy", ("bravo", "bar"));
		worldState.Add("weapon", ("sword"));
		worldState.Add("weapon", ("gun"));
		worldState.Add("can_use_weapon", ("sword", "alpha"));
		worldState.Add("can_use_weapon", ("gun", "bravo"));
	
		baseBindings.Set("?enemy", "alpha");
		var planResult = planBuilder.CreatePlan(new CompoundOuterAutoBindRenameParams(), worldState, out var plan, baseBindings );
	
		Assert.AreEqual(PlanResult.Success, planResult);
		Assert.IsNotNull(plan);
		PrintChain(debugState);
		Assert.AreEqual(3, plan.Steps.Count);
		Assert.IsInstanceOfType<PrimTaskA>( plan.Steps[0].Task );
		Assert.IsInstanceOfType<StringTask>( plan.Steps[1].Task );
		Assert.IsInstanceOfType<PrimTaskA>( plan.Steps[2].Task );
	
		var step1 = plan.GetStep<StringTask>( 1 );
		Assert.AreEqual( "alpha attacking with weapon sword", step1.Task.Text );
	
		plan.Dispose();
	}
	
	[TestMethod]
	public void Planner_Compound_OuterInnerParamsRenameB_WithBindingEnemy()
	{
		worldState.Add("enemy", ("alpha", "foo"));
		worldState.Add("enemy", ("bravo", "bar"));
		worldState.Add("weapon", ("sword"));
		worldState.Add("weapon", ("gun"));
		worldState.Add("can_use_weapon", ("sword", "alpha"));
		worldState.Add("can_use_weapon", ("gun", "bravo"));
	
		baseBindings.Set("?enemy", "bravo");
		var planResult = planBuilder.CreatePlan(new CompoundOuterAutoBindRenameParams(), worldState, out var plan, baseBindings );
	
		Assert.AreEqual(PlanResult.Success, planResult);
		Assert.IsNotNull(plan);
		PrintChain(debugState);
		Assert.AreEqual( 3, plan.Steps.Count );
		Assert.IsInstanceOfType<PrimTaskA>( plan.Steps[0].Task );
		Assert.IsInstanceOfType<StringTask>( plan.Steps[1].Task );
		Assert.IsInstanceOfType<PrimTaskA>( plan.Steps[2].Task );
	
		var step1 = plan.GetStep<StringTask>( 1 );
		Assert.AreEqual( "bravo attacking with weapon gun", step1.Task.Text );
		
		plan.Dispose();
	}

	[TestMethod]
	public void Planner_Compound_OuterInnerParamsRename_Backtrack()
	{
		// Alpha can't use available weapons so it backtracks to bravo

		worldState.Add("enemy", ("alpha", "foo"));
		worldState.Add("enemy", ("bravo", "bar"));
		worldState.Add("weapon", ("sword"));
		worldState.Add("weapon", ("gun"));
		worldState.Add("can_use_weapon", ("spear", "alpha"));
		worldState.Add("can_use_weapon", ("gun", "bravo"));

		var planResult = planBuilder.CreatePlan(new CompoundOuterAutoBindRenameParams(), worldState, out var plan, baseBindings);

		Assert.AreEqual(PlanResult.Success, planResult);
		Assert.IsNotNull(plan);
		PrintChain(debugState);
		Assert.AreEqual( 3, plan.Steps.Count );
		Assert.IsInstanceOfType<PrimTaskA>( plan.Steps[0].Task );
		Assert.IsInstanceOfType<StringTask>( plan.Steps[1].Task );
		Assert.IsInstanceOfType<PrimTaskA>( plan.Steps[2].Task );

		var step1 = plan.GetStep<StringTask>( 1 );
		Assert.AreEqual( "bravo attacking with weapon gun", step1.Task.Text );
		
		plan.Dispose();
	}

	[TestMethod]
	public void Planner_Compound_OuterInnerParamsRename_Backtrack2Levels()
	{
		worldState.Add("enemy", ("alpha", "foo"));
		worldState.Add("enemy", ("bravo", "bar"));
		worldState.Add("weapon", ("sword"));
		worldState.Add("weapon", ("gun"));
		worldState.Add("can_use_weapon", ("spear", "alpha"));
		worldState.Add("can_use_weapon", ("harpoon", "bravo"));

		var planResult = planBuilder.CreatePlan(new CompoundOuterAutoBindRenameParams(), worldState, out var plan, baseBindings);

		Assert.AreEqual(PlanResult.Success, planResult);
		Assert.IsNotNull(plan);
		PrintChain(debugState);
		Assert.AreEqual( 2, plan.Steps.Count );
		Assert.IsInstanceOfType<PrimTaskC>( plan.Steps[0].Task );
		Assert.IsInstanceOfType<PrimTaskB>( plan.Steps[1].Task );
		
		plan.Dispose();
	}

	[TestMethod]
	public void Planner_Compound_ComplexTypes_Numbers()
	{
		worldState.Add("comp", ("alpha", new TestPrimitive()
		{
			Test = "testprimalpha",
			Bool = true,
			TestPos = new Vector3(1, 2, 3)
		}));
		worldState.Add("comp", ("bravo", new TestPrimitive()
		{
			Test = "testprimbravo",
			Bool = false,
			TestPos = new Vector3(999, 888, 777)
		}));

		var planResult = planBuilder.CreatePlan(new CompoundComplex(), worldState, out var plan, baseBindings);

		Assert.AreEqual(PlanResult.Success, planResult);
		Assert.IsNotNull(plan);
		PrintChain(debugState);
		Assert.AreEqual(plan.Steps.Count, 1);
		var step = plan.GetStep<CompoundPrimitive>(0);
		Assert.AreEqual( "alpha", step.Task.Name );

		var primitive = step.Task.Input;
		Assert.AreEqual( "testprimalpha", primitive.Test );
		Assert.AreEqual( new Vector3( 1, 2, 3 ), primitive.TestPos );
		Assert.IsTrue( primitive.Bool );
		
		plan.Dispose();
	}

	[TestMethod]
	public void Planner_Compound_ComplexTypes_Specific()
	{
		worldState.Add("comp", ("alpha", new TestPrimitive("testprimalpha", true, new Vector3(1, 2, 3))));
		worldState.Add("comp", ("bravo", new TestPrimitive("testprimbravo", false, new Vector3(999, 888, 777))));

		baseBindings.Set("?name", "alpha");

		var planResult = planBuilder.CreatePlan(new CompoundComplex(), worldState, out var plan, baseBindings );

		Assert.AreEqual(PlanResult.Success, planResult);
		Assert.IsNotNull(plan);
		PrintChain(debugState);
		Assert.AreEqual(plan.Steps.Count, 1);
		var step = plan.GetStep<CompoundPrimitive>( 0 );
		Assert.AreEqual( "alpha", step.Task.Name );

		var primitive = step.Task.Input;
		Assert.AreEqual( "testprimalpha", primitive.Test );
		Assert.AreEqual( new Vector3( 1, 2, 3 ), primitive.TestPos );
		Assert.IsTrue( primitive.Bool );
		
		plan.Dispose();
	}

	[TestMethod]
	public void Planner_Compound_ComplexTypes_Bravo()
	{
		worldState.Add("comp", ("alpha", new TestPrimitive("testprimalpha", true, new Vector3(1, 2, 3))));
		worldState.Add("comp", ("bravo", new TestPrimitive("testprimbravo", false, new Vector3(999, 888, 777))));

		baseBindings.Set("?name", "bravo");

		var planResult = planBuilder.CreatePlan(new CompoundComplex(), worldState, out var plan, baseBindings);

		Assert.AreEqual(PlanResult.Success, planResult);
		Assert.IsNotNull(plan);
		PrintChain(debugState);
		Assert.AreEqual(plan.Steps.Count, 1);

		var step = plan.GetStep<CompoundPrimitive>(0);
		Assert.AreEqual("bravo", step.Task.Name);
		var primitive = step.Task.Input;
		Assert.AreEqual("testprimbravo", primitive.Test);
		Assert.AreEqual(new Vector3(999, 888, 777), primitive.TestPos);
		Assert.IsFalse(primitive.Bool);
		
		plan.Dispose();
	}

	[TestMethod]
	public void Planner_Compound_ComplexTypes_Equality()
	{
		var primAlpha = new TestPrimitive("testprimalpha", true, new Vector3(1, 2, 3));
		var primBravo = new TestPrimitive("testprimbravo", false, new Vector3(999, 888, 777));

		worldState.Add("comp", ("alpha", primAlpha));
		worldState.Add("comp", ("bravo", primBravo));

		baseBindings.Set("?prim", primBravo);

		var planResult = planBuilder.CreatePlan(new CompoundComplex(), worldState, out var plan, baseBindings);

		Assert.AreEqual(PlanResult.Success, planResult);
		Assert.IsNotNull(plan);
		PrintChain(debugState);
		Assert.AreEqual(plan.Steps.Count, 1);

		var step = plan.GetStep<CompoundPrimitive>( 0 );
		Assert.AreEqual( "bravo", step.Task.Name );
		var primitive = step.Task.Input;
		Assert.AreEqual( "testprimbravo", primitive.Test );
		Assert.AreEqual( new Vector3( 999, 888, 777 ), primitive.TestPos );
		Assert.IsFalse( primitive.Bool );
		
		plan.Dispose();
	}

	[TestMethod]
	public void TestBacktracking()
	{
		worldState.Add("can_do_task2", "true");

		var task = new BacktrackTask();
		var planResult = planBuilder.CreatePlan(task, worldState, out var plan, baseBindings);

		Assert.AreEqual(PlanResult.Success, planResult);
		Assert.IsNotNull(plan);
		Assert.AreEqual(2, plan.Steps.Count);
		Assert.IsInstanceOfType<StringTask>(plan.Steps[0].Task);
		Assert.IsInstanceOfType<StringTask>(plan.Steps[1].Task );
		Assert.AreEqual("?task1", ((StringTask)plan.Steps[0].Task).Text);
		Assert.AreEqual("?task2", ((StringTask)plan.Steps[1].Task).Text);
		
		plan.Dispose();
	}

	[TestMethod]
	public void TestBacktracking_NoPlan()
	{
		worldState.Add("can_do_task2", "false");

		var task = new BacktrackTask();
		var planResult = planBuilder.CreatePlan(task, worldState, out var plan, baseBindings);

		Assert.AreEqual(PlanResult.Failed, planResult);
		Assert.IsNull(plan);
	}

	public class BacktrackTask : CompoundTaskBase
	{
		public BacktrackTask()
		{
			Branches = [
				new Branch("Branch1", [], (_) => [Run<StringTask>().Configure("?task1"), Run<FailingSubTask>()]),
				new Branch("Branch2", [new Query("can_do_task2", "true")], (_) => [Run<StringTask>().Configure("?task1"), Run<StringTask>().Configure("?task2")])
			];
		}
	}

	public class FailingSubTask : CompoundTaskBase
	{
		public FailingSubTask()
		{
			// This task has no branches, so it will always fail to decompose.
			Branches = [];
		}
	}

	public class BacktrackTask2 : CompoundTaskBase
	{
		public BacktrackTask2()
		{
			Branches = [
				new Branch("Branch1", [], (_) => [Run<StringTask>().Configure("?outerTask1"), Run<PotentiallyFailingSubTask>(), Run<StringTask>().Configure("?outerTask2")]),
				new Branch("Branch2", [new Query("can_do_task2", "true")], (_) => [Run<StringTask>().Configure("?task1"), Run<StringTask>().Configure("?task2")])
			];
		}
	}

	public class PotentiallyFailingSubTask : CompoundTaskBase
	{
		public PotentiallyFailingSubTask()
		{
			Branches = [
				new Branch("Branch1", [new Query("can_do_subtask", "true")], (_) => [Run<StringTask>().Configure("?subTask1"), Run<StringTask>().Configure("?subTask2")])
			];
		}
	}

	[TestMethod]
	public void TestBacktracking2_Subtask()
	{
		worldState.Add( "can_do_subtask", "true" );

		var task = new BacktrackTask2();
		var planResult = planBuilder.CreatePlan( task, worldState, out var plan, baseBindings );

		Assert.AreEqual(PlanResult.Success, planResult);
		Assert.IsNotNull( plan );
		PrintChain(debugState);
		Assert.AreEqual( 4, plan.Steps.Count );
		Assert.IsInstanceOfType<StringTask>( plan.Steps[0].Task );
		Assert.IsInstanceOfType<StringTask>( plan.Steps[1].Task );
		Assert.IsInstanceOfType<StringTask>( plan.Steps[2].Task );
		Assert.IsInstanceOfType<StringTask>( plan.Steps[3].Task );

		var step0 = plan.GetStep<StringTask>( 0 );
		Assert.AreEqual( "?outerTask1", step0.Task.Text );
		var step1 = plan.GetStep<StringTask>( 1 );
		Assert.AreEqual( "?subTask1", step1.Task.Text );

		var step2 = plan.GetStep<StringTask>( 2 );

		Assert.AreEqual( "?subTask2", step2.Task.Text );

		var step3 = plan.GetStep<StringTask>( 3 );

		Assert.AreEqual( "?outerTask2", step3.Task.Text );


		// MTR
		Assert.AreEqual( new PlanMTR( 0, 0 ), plan.MTR );
		
		plan.Dispose();
	}

	[TestMethod]
	public void TestBacktracking2_NoSubtask()
	{
		worldState.Add("can_do_subtask", "false");
		worldState.Add("can_do_task2", "true");

		var task = new BacktrackTask2();
		var planResult = planBuilder.CreatePlan(task, worldState, out var plan, baseBindings);

		Assert.AreEqual(PlanResult.Success, planResult);
		Assert.IsNotNull(plan);
		Assert.AreEqual(2, plan.Steps.Count);
		Assert.IsInstanceOfType<StringTask>(plan.Steps[0].Task);
		Assert.IsInstanceOfType<StringTask>(plan.Steps[1].Task);

		var step0 = plan.GetStep<StringTask>(0);
		Assert.AreEqual("?task1", step0.Task.Text);
		var step1 = plan.GetStep<StringTask>(1);
		Assert.AreEqual("?task2", step1.Task.Text);


		// MTR
		Assert.AreEqual(new PlanMTR(1), plan.MTR);
		
		plan.Dispose();
	}

	[TestMethod]
	public void TestBacktracking2_None()
	{
		var task = new BacktrackTask2();
		var planResult = planBuilder.CreatePlan(task, worldState, out var plan, baseBindings);

		Assert.AreEqual(PlanResult.Failed, planResult);
		Assert.IsNull(plan);
	}

	[TestMethod]
	public void MTR_CullsTreeBranches_0()
	{
		var incomingMTR = new PlanMTR(0u);
		var planResult = planBuilder.CreatePlan(new CompoundPreconds(), worldState, out var plan, previousMTR: incomingMTR);
		Assert.AreEqual(PlanResult.Continue, planResult, "Should have lower priority plan with an MTR of 0");
		Assert.IsNull(plan);
	}

	[TestMethod]
	public void MTR_CullsTreeBranches_0_Precond()
	{
		worldState.Add("enemy", ("alpha", false));

		var incomingMTR = new PlanMTR(0u);
		var planResult = planBuilder.CreatePlan(new CompoundPreconds(), worldState, out var plan, previousMTR: incomingMTR);
		Assert.AreEqual(PlanResult.Continue, planResult, "Should have lower priority plan with an MTR of 0");
		Assert.IsNull(plan);
	}

	[TestMethod]
	public void MTR_CullsTreeBranches_1()
	{
		var incomingMTR = new PlanMTR(1u);
		var planResult = planBuilder.CreatePlan(new CompoundPreconds(), worldState, out var plan, previousMTR: incomingMTR);
		Assert.AreEqual(PlanResult.Continue, planResult);
		Assert.IsNull(plan);
	}

	[TestMethod]
	public void MTR_CullsTreeBranches_1_Precond()
	{
		worldState.Add("enemy", ("alpha", false));
		var incomingMTR = new PlanMTR(1u);
		var planResult = planBuilder.CreatePlan(new CompoundPreconds(), worldState, out var plan, previousMTR: incomingMTR);

		Assert.AreEqual(PlanResult.Success, planResult, "Should generate a plan on branch 0");
		Assert.IsNotNull(plan);
		Assert.AreEqual(plan.Steps.Count, 3);
		Assert.IsInstanceOfType<PrimTaskA>( plan.Steps[0].Task );
		Assert.IsInstanceOfType<PrimTaskB>( plan.Steps[1].Task );
		Assert.IsInstanceOfType<PrimTaskA>( plan.Steps[2].Task );
		
		plan.Dispose();
	}

	public class ComplexMTRTask : CompoundTaskBase
	{
		public ComplexMTRTask()
		{
			Branches = [
				new Branch("Branch1_Deep", [], (_) => [Run<ComplexMTR_SubTaskA>(), Run<ComplexMTR_SubTaskB>()]),
				new Branch("Branch2_Shallow", [], (_) => [Run<StringTask>().Configure("?shallowPlan")])
			];
		}
	}

	public class ComplexMTR_SubTaskA : CompoundTaskBase
	{
		public ComplexMTR_SubTaskA()
		{
			Branches = [
				new Branch("SubA_Branch1", [new Query("sub_a_1", "true")], (_) => [Run<StringTask>().Configure("?subA1")]),
				new Branch("SubA_Branch2", [new Query("sub_a_2", "true")], (_) => [Run<StringTask>().Configure("?subA2")])
			];
		}
	}

	public class ComplexMTR_SubTaskB : CompoundTaskBase
	{
		public ComplexMTR_SubTaskB()
		{
			Branches = [
				new Branch("SubB_Branch1", [new Query("sub_b_1", "true")], (_) => [Run<StringTask>().Configure("?subB1")]),
				new Branch("SubB_Branch2", [new Query("sub_b_2", "true")], (_) => [Run<StringTask>().Configure("?subB2")])
			];
		}
	}


	[TestMethod]
	public void MTR_Complex_MTRBranch_1()
	{
		var planResult = planBuilder.CreatePlan(new ComplexMTRTask(), worldState, out var plan, baseBindings);

		Assert.AreEqual(PlanResult.Success, planResult);
		Assert.IsNotNull(plan);
		Assert.AreEqual(1, plan.Steps.Count);

		var step0 = plan.GetStep<StringTask>(0);
		Assert.IsInstanceOfType<StringTask>( step0.Task );
		Assert.AreEqual("?shallowPlan", step0.Task.Text);

		Assert.AreEqual(new PlanMTR(1), plan.MTR);
		
		plan.Dispose();
	}

	[TestMethod]
	public void MTR_Complex_MTRBranch_1_Continuation()
	{
		// The same as the initial plan but with its MTR, it should only be continuation
		var previousMTR = new PlanMTR(1u);
		var planResult = planBuilder.CreatePlan(new ComplexMTRTask(), worldState, out var plan, previousMTR: previousMTR);

		Assert.AreEqual(PlanResult.Continue, planResult);
	}

	[TestMethod]
	public void MTR_Complex_MTRBranch_2()
	{
		// Taking branch A2 > B2 should result in 0,1,1 even with the previous (1) MTR
		worldState.Add("sub_a_2", "true");
		worldState.Add("sub_b_2", "true");

		var previousMTR = new PlanMTR(1u);
		var planResult = planBuilder.CreatePlan(new ComplexMTRTask(), worldState, out var plan, previousMTR: previousMTR);

		Assert.AreEqual(PlanResult.Success, planResult);
		Assert.IsNotNull(plan);
		Assert.AreEqual(new PlanMTR(0, 1, 1), plan.MTR);
		
		plan.Dispose();
	}

	[TestMethod]
	public void MTR_Complex_MTRBranch_2_Continuation()
	{
		// Same world state with the 0,1,1 MTR should continuation rather than exploring lower priority branches
		worldState.Add("sub_a_2", "true");
		worldState.Add("sub_b_2", "true");

		var previousMTR = new PlanMTR(0u, 1u, 1u);
		var planResult = planBuilder.CreatePlan(new ComplexMTRTask(), worldState, out var plan, previousMTR: previousMTR);

		Assert.AreEqual(PlanResult.Continue, planResult);
	}

	[TestMethod]
	public void MTR_Complex_MTRBranch_3()
	{
		// Branch A2 B1 being true offers a higher priority task, 0,1,0 compared to the prior 0,1,1
		worldState.Add("sub_a_2", "true");
		worldState.Add("sub_b_1", "true");

		var previousMTR = new PlanMTR(0u, 1u, 1u);
		var planResult = planBuilder.CreatePlan(new ComplexMTRTask(), worldState, out var plan, previousMTR: previousMTR);

		Assert.AreEqual(PlanResult.Success, planResult);
		Assert.IsNotNull(plan);
		Assert.AreEqual(new PlanMTR(0, 1, 0), plan.MTR);
		
		plan.Dispose();
	}

	[TestMethod]
	public void MTR_Complex_MTRBranch_3_Continuation()
	{
		// Same world state with the 0,1,0 MTR should continuation rather than exploring lower priority branches
		worldState.Add("sub_a_2", "true");
		worldState.Add("sub_b_1", "true");

		var previousMTR = new PlanMTR(0u, 1u, 0u);
		var planResult = planBuilder.CreatePlan(new ComplexMTRTask(), worldState, out var plan, previousMTR: previousMTR);

		Assert.AreEqual(PlanResult.Continue, planResult);
	}

	[TestMethod]
	public void MTR_Complex_MTRBranch_4()
	{
		// Branch A1 B2 being true offers a higher priority task, 0,0,1 compared to the prior 0,1,0
		worldState.Add("sub_a_1", "true");
		worldState.Add("sub_b_2", "true");

		var previousMTR = new PlanMTR(0u, 1u, 0u);
		var planResult = planBuilder.CreatePlan(new ComplexMTRTask(), worldState, out var plan, previousMTR: previousMTR);

		Assert.AreEqual(PlanResult.Success, planResult);
		Assert.IsNotNull(plan);
		Assert.AreEqual(new PlanMTR(0, 0, 1), plan.MTR);
		
		plan.Dispose();
	}
	[TestMethod]
	public void MTR_Complex_MTRBranch_4_Continuation()
	{
		// Same world state with the 0,0,1 MTR should continuation rather than exploring lower priority branches
		worldState.Add("sub_a_1", "true");
		worldState.Add("sub_b_2", "true");

		var previousMTR = new PlanMTR(0u, 0u, 1u);
		var planResult = planBuilder.CreatePlan(new ComplexMTRTask(), worldState, out var plan, previousMTR: previousMTR);

		Assert.AreEqual(PlanResult.Continue, planResult);
	}

	[TestMethod]
	public void MTR_Complex_MTRBranch_5()
	{
		// Branch A1 B1 being true offers a higher priority task, 0,0,0 compared to the prior 0,0,1
		worldState.Add("sub_a_1", "true");
		worldState.Add("sub_b_1", "true");

		var previousMTR = new PlanMTR(0u, 0u, 1u);
		var planResult = planBuilder.CreatePlan(new ComplexMTRTask(), worldState, out var plan, previousMTR: previousMTR);

		Assert.AreEqual(PlanResult.Success, planResult);
		Assert.IsNotNull(plan);
		Assert.AreEqual(new(0, 0, 0), plan.MTR);
		
		plan.Dispose();
	}

	[TestMethod]
	public void MTR_Complex_MTRBranch_5_Continuation()
	{
		// Same world state with the 0,0,0 MTR should continuation rather than exploring lower priority branches
		worldState.Add("sub_a_1", "true");
		worldState.Add("sub_b_1", "true");

		var previousMTR = new PlanMTR(0u, 0u, 0u);
		var planResult = planBuilder.CreatePlan(new ComplexMTRTask(), worldState, out var plan, previousMTR: previousMTR);

		Assert.AreEqual(PlanResult.Continue, planResult);
	}
}

