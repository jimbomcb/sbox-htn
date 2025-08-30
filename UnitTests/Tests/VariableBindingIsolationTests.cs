using HTN.Debug;
using HTN.Planner;
using System;

namespace HTN.Tests;

[TestClass]
public class VariableBindingIsolationTests
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
	public void VariableBinding_ParentVariablesShouldNotLeakToChild()
	{
		worldState.Add("enemy", ("alpha", "castle"));
		worldState.Add("weapon", ("sword"));
		worldState.Add("can_use_weapon", ("sword", "alpha"));

		// This should fail during planning because the child task attempts to access ?location which isn't bound
		Assert.ThrowsException<DecompositionException>(() =>
		{
			planBuilder.CreatePlan(new ParentWithLeakyChild(), worldState, out var plan, baseBindings);
		});
	}

	[TestMethod]
	public void VariableBinding_ChildShouldOnlyAccessBoundVariables()
	{
		worldState.Add("character", ("player1", "mage", "fireball"));
		worldState.Add("spell", ("fireball"));
		worldState.Add("can_cast", ("fireball", "player1"));

		// This should fail because child attempts to access ?type and ?spell from parent without binding
		Assert.ThrowsException<DecompositionException>(() =>
		{
			planBuilder.CreatePlan(new ParentWithChildTryingInvalidAccess(), worldState, out var plan, baseBindings);
		});
	}

	[TestMethod]
	public void VariableBinding_SiblingTasksShouldNotShareVariables()
	{
		worldState.Add("item", ("key", "door1"));
		worldState.Add("door", ("door1"));
		worldState.Add("can_open", ("door1", "key"));

		// Second child should fail when trying to access ?item from first child's scope
		Assert.ThrowsException<DecompositionException>(() =>
		{
			planBuilder.CreatePlan(new ParentWithSiblingLeakage(), worldState, out var plan, baseBindings);
		});
	}

	[TestMethod] 
	public void VariableBinding_ChildAccessingUnboundVariableShouldFail()
	{
		worldState.Add("target", ("enemy1", "weak", "sword"));
		worldState.Add("weapon", ("sword"));  
		worldState.Add("effectiveness", ("sword", "enemy1"));

		// Child task attempts to use ?weakness which should not be available
		Assert.ThrowsException<DecompositionException>(() =>
		{
			planBuilder.CreatePlan(new BuggyVariableLeakageTask(), worldState, out var plan, baseBindings);
		});
	}

	[TestMethod]
	public void VariableBinding_ValidBindingWorksCorrectly()
	{
		// This test verifies that proper binding still works
		worldState.Add("enemy", ("alpha", "castle"));
		worldState.Add("weapon", ("sword"));
		worldState.Add("can_use_weapon", ("sword", "alpha"));

		var planResult = planBuilder.CreatePlan(new ParentWithProperBinding(), worldState, out var plan, baseBindings);

		Assert.AreEqual(PlanResult.Success, planResult);
		Assert.IsNotNull(plan);
		Assert.AreEqual(3, plan.Steps.Count);

		var step1 = plan.GetStep<StringTask>(1);
		Assert.AreEqual("alpha attacking with weapon sword", step1.Task.Text);
		
		plan.Dispose();
	}

	[TestMethod]
	public void VariableBinding_EmptyChildScopeWorksCorrectly()
	{
		// Child with no bindings should still work if it doesn't try to access variables
		var planResult = planBuilder.CreatePlan(new ParentWithEmptyChild(), worldState, out var plan, baseBindings);

		Assert.AreEqual(PlanResult.Success, planResult);
		Assert.IsNotNull(plan);
		Assert.AreEqual(3, plan.Steps.Count);

		var step1 = plan.GetStep<StringTask>(1);
		Assert.AreEqual("empty child executed", step1.Task.Text);
		
		plan.Dispose();
	}
}
