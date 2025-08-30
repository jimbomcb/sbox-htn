using HTN.Conditions;
using HTN.Planner;
using System;

namespace HTN.Tests;

[TestClass]
public class CompareConditionTests
{
	private WorldState worldState;
	private ScopeVariables baseBindings;
	private PlanDebugState debugState;

	[TestInitialize]
	public void Setup()
	{
		worldState = new WorldState();
		baseBindings = new ScopeVariables();
		debugState = new PlanDebugState();
		TaskPool.Reset();
	}

	[TestMethod]
	public void Compare_Equal_Success()
	{
		baseBindings.Set("?value", 5);
		var condition = new Compare("?value", Compare.Operator.Equal, 5);
		var result = condition.Evaluate(worldState, baseBindings, null, debugState, binding => EvaluationResult.Finished);
		Assert.AreEqual(EvaluationResult.Finished, result);
	}

	[TestMethod]
	public void Compare_Equal_Failure()
	{
		baseBindings.Set("?value", 5);
		var condition = new Compare("?value", Compare.Operator.Equal, 6);
		var result = condition.Evaluate(worldState, baseBindings, null, debugState, binding => EvaluationResult.Finished);
		Assert.AreEqual(EvaluationResult.NoResults, result);
	}

	[TestMethod]
	public void Compare_NotEqual_Success()
	{
		baseBindings.Set("?value", 5);
		var condition = new Compare("?value", Compare.Operator.NotEqual, 6);
		var result = condition.Evaluate(worldState, baseBindings, null, debugState, binding => EvaluationResult.Finished);
		Assert.AreEqual(EvaluationResult.Finished, result);
	}

	[TestMethod]
	public void Compare_NotEqual_Failure()
	{
		baseBindings.Set("?value", 5);
		var condition = new Compare("?value", Compare.Operator.NotEqual, 5);
		var result = condition.Evaluate(worldState, baseBindings, null, debugState, binding => EvaluationResult.Finished);
		Assert.AreEqual(EvaluationResult.NoResults, result);
	}

	[TestMethod]
	public void Compare_LessThan_Success()
	{
		baseBindings.Set("?value", 4);
		var condition = new Compare("?value", Compare.Operator.LessThan, 5);
		var result = condition.Evaluate(worldState, baseBindings, null, debugState, binding => EvaluationResult.Finished);
		Assert.AreEqual(EvaluationResult.Finished, result);
	}

	[TestMethod]
	public void Compare_LessThan_Failure()
	{
		baseBindings.Set("?value", 5);
		var condition = new Compare("?value", Compare.Operator.LessThan, 5);
		var result = condition.Evaluate(worldState, baseBindings, null, debugState, binding => EvaluationResult.Finished);
		Assert.AreEqual(EvaluationResult.NoResults, result);
	}

	[TestMethod]
	public void Compare_LessThan_FailureGreater()
	{
		baseBindings.Set("?value", 6);
		var condition = new Compare("?value", Compare.Operator.LessThan, 5);
		var result = condition.Evaluate(worldState, baseBindings, null, debugState, binding => EvaluationResult.Finished);
		Assert.AreEqual(EvaluationResult.NoResults, result);
	}

	[TestMethod]
	public void Compare_LessThanOrEqual_Success()
	{
		baseBindings.Set("?value", 5);
		var condition = new Compare("?value", Compare.Operator.LessThanOrEqual, 5);
		var result = condition.Evaluate(worldState, baseBindings, null, debugState, binding => EvaluationResult.Finished);
		Assert.AreEqual(EvaluationResult.Finished, result);
	}

	[TestMethod]
	public void Compare_LessThanOrEqual_SuccessLess()
	{
		baseBindings.Set("?value", 4);
		var condition = new Compare("?value", Compare.Operator.LessThanOrEqual, 5);
		var result = condition.Evaluate(worldState, baseBindings, null, debugState, binding => EvaluationResult.Finished);
		Assert.AreEqual(EvaluationResult.Finished, result);
	}

	[TestMethod]
	public void Compare_LessThanOrEqual_Failure()
	{
		baseBindings.Set("?value", 6);
		var condition = new Compare("?value", Compare.Operator.LessThanOrEqual, 5);
		var result = condition.Evaluate(worldState, baseBindings, null, debugState, binding => EvaluationResult.Finished);
		Assert.AreEqual(EvaluationResult.NoResults, result);
	}

	[TestMethod]
	public void Compare_GreaterThan_Success()
	{
		baseBindings.Set("?value", 6);
		var condition = new Compare("?value", Compare.Operator.GreaterThan, 5);
		var result = condition.Evaluate(worldState, baseBindings, null, debugState, binding => EvaluationResult.Finished);
		Assert.AreEqual(EvaluationResult.Finished, result);
	}

	[TestMethod]
	public void Compare_GreaterThan_Failure()
	{
		baseBindings.Set("?value", 5);
		var condition = new Compare("?value", Compare.Operator.GreaterThan, 5);
		var result = condition.Evaluate(worldState, baseBindings, null, debugState, binding => EvaluationResult.Finished);
		Assert.AreEqual(EvaluationResult.NoResults, result);
	}

	[TestMethod]
	public void Compare_GreaterThan_FailureLess()
	{
		baseBindings.Set("?value", 4);
		var condition = new Compare("?value", Compare.Operator.GreaterThan, 5);
		var result = condition.Evaluate(worldState, baseBindings, null, debugState, binding => EvaluationResult.Finished);
		Assert.AreEqual(EvaluationResult.NoResults, result);
	}

	[TestMethod]
	public void Compare_GreaterThanOrEqual_Success()
	{
		baseBindings.Set("?value", 5);
		var condition = new Compare("?value", Compare.Operator.GreaterThanOrEqual, 5);
		var result = condition.Evaluate(worldState, baseBindings, null, debugState, binding => EvaluationResult.Finished);
		Assert.AreEqual(EvaluationResult.Finished, result);
	}

	[TestMethod]
	public void Compare_GreaterThanOrEqual_SuccessGreater()
	{
		baseBindings.Set("?value", 6);
		var condition = new Compare("?value", Compare.Operator.GreaterThanOrEqual, 5);
		var result = condition.Evaluate(worldState, baseBindings, null, debugState, binding => EvaluationResult.Finished);
		Assert.AreEqual(EvaluationResult.Finished, result);
	}

	[TestMethod]
	public void Compare_GreaterThanOrEqual_Failure()
	{
		baseBindings.Set("?value", 4);
		var condition = new Compare("?value", Compare.Operator.GreaterThanOrEqual, 5);
		var result = condition.Evaluate(worldState, baseBindings, null, debugState, binding => EvaluationResult.Finished);
		Assert.AreEqual(EvaluationResult.NoResults, result);
	}

	[TestMethod]
	public void Compare_String_Equal_Success()
	{
		baseBindings.Set("?value", "hello");
		var condition = new Compare("?value", Compare.Operator.Equal, "hello");
		var result = condition.Evaluate(worldState, baseBindings, null, debugState, binding => EvaluationResult.Finished);
		Assert.AreEqual(EvaluationResult.Finished, result);
	}

	[TestMethod]
	public void Compare_String_Equal_Failure()
	{
		baseBindings.Set("?value", "hello");
		var condition = new Compare("?value", Compare.Operator.Equal, "world");
		var result = condition.Evaluate(worldState, baseBindings, null, debugState, binding => EvaluationResult.Finished);
		Assert.AreEqual(EvaluationResult.NoResults, result);
	}

	[TestMethod]
	public void Compare_String_LessThan_Success()
	{
		baseBindings.Set("?value", "apple");
		var condition = new Compare("?value", Compare.Operator.LessThan, "banana");
		var result = condition.Evaluate(worldState, baseBindings, null, debugState, binding => EvaluationResult.Finished);
		Assert.AreEqual(EvaluationResult.Finished, result);
	}

	[TestMethod]
	public void Compare_String_GreaterThan_Success()
	{
		baseBindings.Set("?value", "zebra");
		var condition = new Compare("?value", Compare.Operator.GreaterThan, "apple");
		var result = condition.Evaluate(worldState, baseBindings, null, debugState, binding => EvaluationResult.Finished);
		Assert.AreEqual(EvaluationResult.Finished, result);
	}

	[TestMethod]
	public void Compare_Float_LessThan_Success()
	{
		baseBindings.Set("?value", 3.14f);
		var condition = new Compare("?value", Compare.Operator.LessThan, 3.15f);
		var result = condition.Evaluate(worldState, baseBindings, null, debugState, binding => EvaluationResult.Finished);
		Assert.AreEqual(EvaluationResult.Finished, result);
	}

	[TestMethod]
	public void Compare_Double_GreaterThanOrEqual_Success()
	{
		baseBindings.Set("?value", 2.718);
		var condition = new Compare("?value", Compare.Operator.GreaterThanOrEqual, 2.718);
		var result = condition.Evaluate(worldState, baseBindings, null, debugState, binding => EvaluationResult.Finished);
		Assert.AreEqual(EvaluationResult.Finished, result);
	}

	[TestMethod]
	public void Compare_MixedNumeric_IntToDouble_Success()
	{
		baseBindings.Set("?value", 5);
		var condition = new Compare("?value", Compare.Operator.LessThan, 5.5);
		var result = condition.Evaluate(worldState, baseBindings, null, debugState, binding => EvaluationResult.Finished);
		Assert.AreEqual(EvaluationResult.Finished, result);
	}

	[TestMethod]
	public void Compare_MixedNumeric_FloatToInt_Success()
	{
		baseBindings.Set("?value", 4.9f);
		var condition = new Compare("?value", Compare.Operator.LessThan, 5);
		var result = condition.Evaluate(worldState, baseBindings, null, debugState, binding => EvaluationResult.Finished);
		Assert.AreEqual(EvaluationResult.Finished, result);
	}

	[TestMethod]
	public void Compare_VariableToVariable_Equal_Success()
	{
		baseBindings.Set("?valueA", 5);
		baseBindings.Set("?valueB", 5);
		var condition = new Compare("?valueA", "?valueB", Compare.Operator.Equal);
		var result = condition.Evaluate(worldState, baseBindings, null, debugState, binding => EvaluationResult.Finished);
		Assert.AreEqual(EvaluationResult.Finished, result);
	}

	[TestMethod]
	public void Compare_VariableToVariable_Equal_Failure()
	{
		baseBindings.Set("?valueA", 5);
		baseBindings.Set("?valueB", 6);
		var condition = new Compare("?valueA", "?valueB", Compare.Operator.Equal);
		var result = condition.Evaluate(worldState, baseBindings, null, debugState, binding => EvaluationResult.Finished);
		Assert.AreEqual(EvaluationResult.NoResults, result);
	}

	[TestMethod]
	public void Compare_VariableToVariable_LessThan_Success()
	{
		baseBindings.Set("?valueA", 3);
		baseBindings.Set("?valueB", 7);
		var condition = new Compare("?valueA", "?valueB", Compare.Operator.LessThan);
		var result = condition.Evaluate(worldState, baseBindings, null, debugState, binding => EvaluationResult.Finished);
		Assert.AreEqual(EvaluationResult.Finished, result);
	}

	[TestMethod]
	public void Compare_VariableToVariable_GreaterThan_Success()
	{
		baseBindings.Set("?valueA", 10);
		baseBindings.Set("?valueB", 5);
		var condition = new Compare("?valueA", "?valueB", Compare.Operator.GreaterThan);
		var result = condition.Evaluate(worldState, baseBindings, null, debugState, binding => EvaluationResult.Finished);
		Assert.AreEqual(EvaluationResult.Finished, result);
	}

	[TestMethod]
	public void Compare_VariableNotFound()
	{
		var condition = new Compare("?nonexistent", Compare.Operator.Equal, 5);
		var result = condition.Evaluate(worldState, baseBindings, null, debugState, binding => EvaluationResult.Finished);
		Assert.AreEqual(EvaluationResult.NoResults, result);
	}

	[TestMethod]
	public void Compare_SecondVariableNotFound()
	{
		baseBindings.Set("?valueA", 5);
		var condition = new Compare("?valueA", "?nonexistent", Compare.Operator.Equal);
		Assert.ThrowsException<InvalidOperationException>(() =>
		{
			condition.Evaluate(worldState, baseBindings, null, debugState, binding => EvaluationResult.Finished);
		});
	}

	[TestMethod]
	public void Compare_NullValues_Equal_Success()
	{
		baseBindings.Set("?valueA", null);
		baseBindings.Set("?valueB", null);
		var condition = new Compare("?valueA", "?valueB", Compare.Operator.Equal);
		var result = condition.Evaluate(worldState, baseBindings, null, debugState, binding => EvaluationResult.Finished);
		Assert.AreEqual(EvaluationResult.Finished, result);
	}

	[TestMethod]
	public void Compare_NullToValue_NotEqual_Success()
	{
		baseBindings.Set("?value", null);
		var condition = new Compare("?value", Compare.Operator.NotEqual, 5);
		var result = condition.Evaluate(worldState, baseBindings, null, debugState, binding => EvaluationResult.Finished);
		Assert.AreEqual(EvaluationResult.Finished, result);
	}

	[TestMethod]
	public void Compare_NullComparison_LessThan()
	{
		baseBindings.Set("?valueA", null);
		baseBindings.Set("?valueB", 5);
		var condition = new Compare("?valueA", "?valueB", Compare.Operator.LessThan);
		var result = condition.Evaluate(worldState, baseBindings, null, debugState, binding => EvaluationResult.Finished);
		Assert.AreEqual(EvaluationResult.Finished, result); // null < 5 should be true
	}

	[TestMethod]
	public void Compare_ZeroComparison()
	{
		baseBindings.Set("?value", 0);
		var condition = new Compare("?value", Compare.Operator.Equal, 0);
		var result = condition.Evaluate(worldState, baseBindings, null, debugState, binding => EvaluationResult.Finished);
		Assert.AreEqual(EvaluationResult.Finished, result);
	}

	[TestMethod]
	public void Compare_NegativeNumbers()
	{
		baseBindings.Set("?value", -5);
		var condition = new Compare("?value", Compare.Operator.LessThan, 0);
		var result = condition.Evaluate(worldState, baseBindings, null, debugState, binding => EvaluationResult.Finished);
		Assert.AreEqual(EvaluationResult.Finished, result);
	}

	[TestMethod]
	public void Compare_EmptyString()
	{
		baseBindings.Set("?value", "");
		var condition = new Compare("?value", Compare.Operator.Equal, "");
		var result = condition.Evaluate(worldState, baseBindings, null, debugState, binding => EvaluationResult.Finished);
		Assert.AreEqual(EvaluationResult.Finished, result);
	}
}
