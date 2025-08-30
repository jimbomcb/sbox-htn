using System;
using System.Collections.Generic;
using System.Linq;
using HTN.Conditions;
using HTN.Planner;

namespace HTN.Tests;

[TestClass]
public partial class PatternMatchTests
{
	private WorldState _worldState;
	private ScopeVariables _bindings;
	private PlannerContext _ctx;
	private PlanDebugState _context = null;

	private List<ScopeVariables> EvaluateToList( ICondition condition, WorldState worldState, ScopeVariables bindings, PlannerContext ctx, PlanDebugState debugState )
	{
		var list = new List<ScopeVariables>();
		condition.Evaluate( worldState, bindings, ctx, debugState, result =>
		{
			list.Add( result );
			return EvaluationResult.NoResults;
		} );
		return list;
	}

	[TestInitialize]
	public void Setup()
	{
		_worldState = new WorldState();
		_bindings = new();
		_ctx = new(null);
		//_context = new PlanDebugState();
	}

	[TestMethod]
	public void PatternMatch_NoMatchingKey_ReturnsEmpty()
	{
		var pattern = new Query("nonexistent_key", "?_value");

		var results = EvaluateToList(pattern, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(0, results.Count);
	}

	[TestMethod]
	public void PatternMatch_SingleVariableParameter_ReturnsAllValues()
	{
		_worldState.Add("items", "apple");
		_worldState.Add("items", "banana");
		_worldState.Add("items", "cherry");
		var pattern = new Query("items", "?item");

		var results = EvaluateToList(pattern, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(3, results.Count);
		Assert.IsTrue(results.Any(r => r.Get<string>("?item") == "apple"));
		Assert.IsTrue(results.Any(r => r.Get<string>("?item") == "banana"));
		Assert.IsTrue(results.Any(r => r.Get<string>("?item") == "cherry"));
	}

	[TestMethod]
	public void PatternMatch_ExactMatch_ReturnsMatch()
	{
		_worldState.Add("color", "red");
		_worldState.Add("color", "blue");
		var pattern = new Query("color", "red");

		var results = EvaluateToList(pattern, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(1, results.Count);
		Assert.AreEqual(0, results[0].Bindings.Count);
	}

	[TestMethod]
	public void PatternMatch_ExactMatchNotFound_ReturnsEmpty()
	{
		_worldState.Add("color", "red");
		_worldState.Add("color", "blue");
		var pattern = new Query("color", "green");

		var results = EvaluateToList(pattern, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(0, results.Count);
	}

	[TestMethod]
	public void PatternMatch_MultipleParameters_WithVariables_ReturnsMatches()
	{
		_worldState.Add("likes", ("john", "pizza"));
		_worldState.Add("likes", ("mary", "sushi"));
		_worldState.Add("likes", ("bob", "pizza"));
		var pattern = new Query("likes", "?person", "?food");

		var results = EvaluateToList(pattern, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(3, results.Count);
		Assert.IsTrue(results.Any(r => r.Get<string>("?person") == "john" && r.Get<string>("?food") == "pizza"));
		Assert.IsTrue(results.Any(r => r.Get<string>("?person") == "mary" && r.Get<string>("?food") == "sushi"));
		Assert.IsTrue(results.Any(r => r.Get<string>("?person") == "bob" && r.Get<string>("?food") == "pizza"));
	}

	[TestMethod]
	public void PatternMatch_MixedVariableAndExact_ReturnsFilteredMatches()
	{
		_worldState.Add("likes", ("john", "pizza"));
		_worldState.Add("likes", ("mary", "sushi"));
		_worldState.Add("likes", ("bob", "pizza"));
		var pattern = new Query("likes", "?person", "pizza");

		var results = EvaluateToList(pattern, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(2, results.Count);
		Assert.IsTrue(results.Any(r => r.Get<string>("?person") == "john"));
		Assert.IsTrue(results.Any(r => r.Get<string>("?person") == "bob"));
		Assert.IsTrue(results.All(r => !r.Bindings.ContainsKey("?food")));
	}

	[TestMethod]
	public void PatternMatch_ExistingBindings_ConsistentMatch_ReturnsMatch()
	{
		_worldState.Add("color", "red");
		_worldState.Add("color", "blue");

		_bindings.Set("?x", "red");
		var pattern = new Query("color", "?x");

		var results = EvaluateToList(pattern, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(1, results.Count);
		Assert.AreEqual("red", results[0].Get<string>("?x"));
	}

	[TestMethod]
	public void PatternMatch_ExistingBindings_InconsistentMatch_ReturnsEmpty()
	{
		_worldState.Add("color", "red");
		_worldState.Add("color", "blue");

		_bindings.Set("?x", "green");
		var pattern = new Query("color", "?x");

		var results = EvaluateToList(pattern, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(0, results.Count);
	}

	[TestMethod]
	public void PatternMatch_ComplexTuples_ThreeParameters_ReturnsMatches()
	{
		_worldState.Add("location", ("john", "kitchen", "morning"));
		_worldState.Add("location", ("mary", "office", "afternoon"));
		_worldState.Add("location", ("bob", "kitchen", "evening"));
		var pattern = new Query("location", "?person", "kitchen", "?time");

		var results = EvaluateToList(pattern, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(2, results.Count);
		Assert.IsTrue(results.Any(r => r.Get<string>("?person") == "john" && r.Get<string>("?time") == "morning"));
		Assert.IsTrue(results.Any(r => r.Get<string>("?person") == "bob" && r.Get<string>("?time") == "evening"));
	}

	[TestMethod]
	public void PatternMatch_SameVariableMultipleTimes_EnforcesConsistency()
	{
		_worldState.Add("equals", ("apple", "apple"));
		_worldState.Add("equals", ("banana", "orange"));
		_worldState.Add("equals", ("cherry", "cherry"));
		var pattern = new Query("equals", "?x", "?x");

		var results = EvaluateToList(pattern, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(2, results.Count);
		Assert.IsTrue(results.Any(r => r.Get<string>("?x") == "apple"));
		Assert.IsTrue(results.Any(r => r.Get<string>("?x") == "cherry"));
	}

	[TestMethod]
	public void PatternMatch_EmptyWorldState_ReturnsEmpty()
	{
		var pattern = new Query("anything", "?_value");

		var results = EvaluateToList(pattern, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(0, results.Count);
	}

	[TestMethod]
	public void PatternMatch_SingleParameter_MultipleEntries_ReturnsAllMatches()
	{
		_worldState.Add("numbers", 1);
		_worldState.Add("numbers", 2);
		_worldState.Add("numbers", 3);
		var pattern = new Query("numbers", "?num");

		var results = EvaluateToList(pattern, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(3, results.Count);
		Assert.IsTrue(results.Any(r => r.Get<int>("?num") == 1));
		Assert.IsTrue(results.Any(r => r.Get<int>("?num") == 2));
		Assert.IsTrue(results.Any(r => r.Get<int>("?num") == 3));
	}

	[TestMethod]
	public void PatternMatch_MixedDataTypes_WorksCorrectly()
	{
		_worldState.Add("mixed", ("text", 42));
		_worldState.Add("mixed", ("hello", 100));
		var pattern = new Query("mixed", "?str", "?num");

		var results = EvaluateToList(pattern, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(2, results.Count);
		Assert.IsTrue(results.Any(r => r.Get<string>("?str") == "text" && r.Get<int>("?num") == 42));
		Assert.IsTrue(results.Any(r => r.Get<string>("?str") == "hello" && r.Get<int>("?num") == 100));
	}

	[TestMethod]
	public void PatternMatch_MixedDataTypes2_WorksCorrectly()
	{
		_worldState.Add("mixed", ("worldState", new WorldState()));
		_worldState.Add("mixed", ("time", DateTime.UtcNow));
		var pattern = new Query("mixed", "?key", "?val");

		var results = EvaluateToList(pattern, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(2, results.Count);
		Assert.IsTrue(results.Any(r => r.Get<string>("?key") == "worldState" && r.Get<object>("?val") is WorldState));
		Assert.IsTrue(results.Any(r => r.Get<string>("?key") == "time" && r.Get<object>("?val") is DateTime));
	}

	[TestMethod]
	public void PatternMatch_PartialParameterMatch_IndexOutOfBounds_ReturnsEmpty()
	{
		_worldState.Add("short", "single");
		var pattern = new Query("short", "?first", "?second");

		var results = EvaluateToList(pattern, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(0, results.Count);
	}

	// WILDCARD TESTS

	[TestMethod]
	public void PatternMatch_SingleWildcard_MatchesAnyValue()
	{
		_worldState.Add("location", ("john", "kitchen", "morning"));
		_worldState.Add("location", ("mary", "office", "afternoon"));
		_worldState.Add("location", ("bob", "kitchen", "evening"));
		var pattern = new Query("location", "*", "kitchen", "?time");

		var results = EvaluateToList(pattern, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(2, results.Count);
		Assert.IsTrue(results.Any(r => r.Get<string>("?time") == "morning"));
		Assert.IsTrue(results.Any(r => r.Get<string>("?time") == "evening"));
		// Verify that the wildcard parameter is not captured
		Assert.IsTrue(results.All(r => r.Bindings.Count == 1));
	}

	[TestMethod]
	public void PatternMatch_SingleWildcard_FirstParameter()
	{
		_worldState.Add("activity", ("running", "outdoor", "healthy"));
		_worldState.Add("activity", ("swimming", "indoor", "healthy"));
		_worldState.Add("activity", ("reading", "indoor", "mental"));
		var pattern = new Query("activity", "*", "?location", "healthy");

		var results = EvaluateToList(pattern, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(2, results.Count);
		Assert.IsTrue(results.Any(r => r.Get<string>("?location") == "outdoor"));
		Assert.IsTrue(results.Any(r => r.Get<string>("?location") == "indoor"));
	}

	[TestMethod]
	public void PatternMatch_SingleWildcard_MiddleParameter()
	{
		_worldState.Add("event", ("conference", "tech", "monday"));
		_worldState.Add("event", ("meeting", "business", "monday"));
		_worldState.Add("event", ("party", "social", "friday"));
		var pattern = new Query("event", "?type", "*", "monday");

		var results = EvaluateToList(pattern, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(2, results.Count);
		Assert.IsTrue(results.Any(r => r.Get<string>("?type") == "conference"));
		Assert.IsTrue(results.Any(r => r.Get<string>("?type") == "meeting"));
	}

	[TestMethod]
	public void PatternMatch_SingleWildcard_LastParameter()
	{
		_worldState.Add("item", ("sword", "weapon", "sharp"));
		_worldState.Add("item", ("potion", "consumable", "magical"));
		_worldState.Add("item", ("shield", "armor", "sturdy"));
		var pattern = new Query("item", "?name", "weapon", "*");

		var results = EvaluateToList(pattern, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(1, results.Count);
		Assert.AreEqual("sword", results[0].Get<string>("?name"));
	}

	[TestMethod]
	public void PatternMatch_MultipleSingleWildcards()
	{
		_worldState.Add("transaction", ("buy", "john", "apple", "store1", "morning"));
		_worldState.Add("transaction", ("sell", "mary", "orange", "store2", "afternoon"));
		_worldState.Add("transaction", ("buy", "bob", "banana", "store1", "evening"));
		var pattern = new Query("transaction", "buy", "*", "?item", "*", "?time");

		var results = EvaluateToList(pattern, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(2, results.Count);
		Assert.IsTrue(results.Any(r => r.Get<string>("?item") == "apple" && r.Get<string>("?time") == "morning"));
		Assert.IsTrue(results.Any(r => r.Get<string>("?item") == "banana" && r.Get<string>("?time") == "evening"));
	}

	[TestMethod]
	public void PatternMatch_MultiWildcard_MatchesRemainingParameters()
	{
		_worldState.Add("location", ("john", "kitchen", "morning", "cooking", "happy"));
		_worldState.Add("location", ("mary", "office", "afternoon"));
		_worldState.Add("location", ("bob", "garden", "evening", "relaxing"));
		var pattern = new Query("location", "?person", "**");

		var results = EvaluateToList(pattern, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(3, results.Count);
		Assert.IsTrue(results.Any(r => r.Get<string>("?person") == "john"));
		Assert.IsTrue(results.Any(r => r.Get<string>("?person") == "mary"));
		Assert.IsTrue(results.Any(r => r.Get<string>("?person") == "bob"));
		// Verify that only the captured variable is bound
		Assert.IsTrue(results.All(r => r.Bindings.Count == 1));
	}

	[TestMethod]
	public void PatternMatch_MultiWildcard_WithExactMatch()
	{
		_worldState.Add("action", ("john", "cooking", "pasta", "quickly", "skillfully"));
		_worldState.Add("action", ("mary", "cooking", "soup", "slowly"));
		_worldState.Add("action", ("bob", "running", "marathon", "fast"));
		var pattern = new Query("action", "?person", "cooking", "**");

		var results = EvaluateToList(pattern, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(2, results.Count);
		Assert.IsTrue(results.Any(r => r.Get<string>("?person") == "john"));
		Assert.IsTrue(results.Any(r => r.Get<string>("?person") == "mary"));
		Assert.IsFalse(results.Any(r => r.Get<string>("?person") == "bob"));
	}

	[TestMethod]
	public void PatternMatch_MultiWildcard_WithSingleWildcard()
	{
		_worldState.Add("complex", ("data", "type1", "value1", "extra1", "extra2"));
		_worldState.Add("complex", ("info", "type2", "value2", "extra3"));
		_worldState.Add("complex", ("meta", "type1", "value3", "extra4", "extra5", "extra6"));
		var pattern = new Query("complex", "?category", "*", "?value", "**");

		var results = EvaluateToList(pattern, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(3, results.Count);
		Assert.IsTrue(results.Any(r => r.Get<string>("?category") == "data" && r.Get<string>("?value") == "value1"));
		Assert.IsTrue(results.Any(r => r.Get<string>("?category") == "info" && r.Get<string>("?value") == "value2"));
		Assert.IsTrue(results.Any(r => r.Get<string>("?category") == "meta" && r.Get<string>("?value") == "value3"));
	}

	[TestMethod]
	public void PatternMatch_MultiWildcard_OnlyMultiWildcard()
	{
		_worldState.Add("any_length", ("one"));
		_worldState.Add("any_length", ("one", "two"));
		_worldState.Add("any_length", ("one", "two", "three", "four"));
		var pattern = new Query("any_length", "**");

		var results = EvaluateToList(pattern, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(3, results.Count);
		Assert.IsTrue(results.All(r => r.Bindings.Count == 0));
	}

	[TestMethod]
	public void PatternMatch_MultiWildcard_EmptyRemainder()
	{
		_worldState.Add("short_data", ("prefix"));
		_worldState.Add("short_data", ("other_prefix"));
		var pattern = new Query("short_data", "?prefix", "**");

		var results = EvaluateToList(pattern, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(2, results.Count);
		Assert.IsTrue(results.Any(r => r.Get<string>("?prefix") == "prefix"));
		Assert.IsTrue(results.Any(r => r.Get<string>("?prefix") == "other_prefix"));
	}

	[TestMethod]
	public void PatternMatch_MultiWildcard_InvalidPosition_ThrowsException()
	{
		Assert.ThrowsException<ArgumentException>(() =>
		{
			new Query("test", "**", "?param");
		});

		Assert.ThrowsException<ArgumentException>(() =>
		{
			new Query("test", "?param", "**", "exact");
		});
	}

	[TestMethod]
	public void PatternMatch_MultiWildcard_InsufficientParameters()
	{
		_worldState.Add("short", ("only"));
		var pattern = new Query("short", "?first", "?second", "**");

		var results = EvaluateToList(pattern, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(0, results.Count);
	}

	[TestMethod]
	public void PatternMatch_WildcardCombination_ComplexScenario()
	{
		_worldState.Add("log", ("2023-11-20", "INFO", "user123", "login", "success", "from", "192.168.1.1"));
		_worldState.Add("log", ("2023-11-20", "ERROR", "user456", "payment", "failed", "reason", "insufficient_funds"));
		_worldState.Add("log", ("2023-11-20", "INFO", "user789", "logout", "success"));
		_worldState.Add("log", ("2023-11-21", "WARN", "user123", "login", "failed", "too_many_attempts"));

		var pattern = new Query("log", "2023-11-20", "?level", "*", "?action", "**");

		var results = EvaluateToList(pattern, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(3, results.Count);
		Assert.IsTrue(results.Any(r => r.Get<string>("?level") == "INFO" && r.Get<string>("?action") == "login"));
		Assert.IsTrue(results.Any(r => r.Get<string>("?level") == "ERROR" && r.Get<string>("?action") == "payment"));
		Assert.IsTrue(results.Any(r => r.Get<string>("?level") == "INFO" && r.Get<string>("?action") == "logout"));
		Assert.IsFalse(results.Any(r => r.Bindings.ContainsKey("2023-11-21")));
	}

	[TestMethod]
	public void And_NoConditions_ReturnsCurrentBindings()
	{
		var and = new And();

		var results = EvaluateToList(and, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(1, results.Count);
		Assert.AreEqual(0, results[0].Bindings.Count);
	}

	[TestMethod]
	public void And_SingleConditionThatSucceeds_ReturnsResults()
	{
		_worldState.Add("items", "apple");
		_worldState.Add("items", "banana");
		var and = new And(new Query("items", "?item"));

		var results = EvaluateToList(and, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(2, results.Count);
		Assert.IsTrue(results.Any(r => r.Get<string>("?item") == "apple"));
		Assert.IsTrue(results.Any(r => r.Get<string>("?item") == "banana"));
	}

	[TestMethod]
	public void And_SingleConditionThatFails_ReturnsEmpty()
	{
		var and = new And(new Query("nonexistent", "?item"));

		var results = EvaluateToList(and, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(0, results.Count);
	}

	[TestMethod]
	public void And_TwoConditionsAllSucceed_ReturnsIntersection()
	{
		_worldState.Add("in_fridge", "eggs");
		_worldState.Add("in_fridge", "milk");
		_worldState.Add("in_fridge", "cheese");
		_worldState.Add("allergic_to", ("alice", "eggs"));
		_worldState.Add("allergic_to", ("bob", "milk"));

		var and = new And(
			new Query("in_fridge", "?item"),
			new Query("allergic_to", "?person", "?item")
		);

		var results = EvaluateToList(and, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(2, results.Count);
		Assert.IsTrue(results.Any(r => r.Get<string>("?item") == "eggs" && r.Get<string>("?person") == "alice"));
		Assert.IsTrue(results.Any(r => r.Get<string>("?item") == "milk" && r.Get<string>("?person") == "bob"));
	}

	[TestMethod]
	public void And_TwoConditionsSecondFails_ReturnsEmpty()
	{
		_worldState.Add("in_fridge", "eggs");
		_worldState.Add("in_fridge", "milk");

		var and = new And(
			new Query("in_fridge", "?item"),
			new Query("nonexistent", "?person", "?item")
		);

		var results = EvaluateToList(and, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(0, results.Count);
	}

	[TestMethod]
	public void And_TwoConditionsFirstFails_ReturnsEmpty()
	{
		_worldState.Add("allergic_to", ("alice", "eggs"));
		_worldState.Add("allergic_to", ("bob", "milk"));

		var and = new And(
			new Query("nonexistent", "?item"),
			new Query("allergic_to", "?person", "?item")
		);

		var results = EvaluateToList(and, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(0, results.Count);
	}

	[TestMethod]
	public void And_ThreeConditions_ReturnsComplexIntersection()
	{
		_worldState.Add("in_fridge", "eggs");
		_worldState.Add("in_fridge", "milk");
		_worldState.Add("allergic_to", ("alice", "eggs"));
		_worldState.Add("allergic_to", ("bob", "milk"));
		_worldState.Add("can_cook", ("alice", "scrambled_eggs"));
		_worldState.Add("can_cook", ("bob", "pancakes"));

		var and = new And(
			new Query("in_fridge", "?item"),
			new Query("allergic_to", "?person", "?item"),
			new Query("can_cook", "?person", "?dish")
		);

		var results = EvaluateToList(and, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(2, results.Count);
		Assert.IsTrue(results.Any(r =>
			r.Get<string>("?item") == "eggs" &&
			r.Get<string>("?person") == "alice" &&
			r.Get<string>("?dish") == "scrambled_eggs"));
		Assert.IsTrue(results.Any(r =>
			r.Get<string>("?item") == "milk" &&
			r.Get<string>("?person") == "bob" &&
			r.Get<string>("?dish") == "pancakes"));
	}

	[TestMethod]
	public void And_ExactMatchesWithVariables_FiltersCorrectly()
	{
		_worldState.Add("likes", ("john", "pizza"));
		_worldState.Add("likes", ("mary", "sushi"));
		_worldState.Add("likes", ("bob", "pizza"));
		_worldState.Add("has_money", "john");
		_worldState.Add("has_money", "bob");

		var and = new And(
			new Query("likes", "?person", "pizza"),
			new Query("has_money", "?person")
		);

		var results = EvaluateToList(and, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(2, results.Count);
		Assert.IsTrue(results.Any(r => r.Get<string>("?person") == "john"));
		Assert.IsTrue(results.Any(r => r.Get<string>("?person") == "bob"));
		Assert.IsTrue(results.All(r => !r.Bindings.ContainsKey("?food")));
	}

	[TestMethod]
	public void And_ExistingBindings_ConsistentMatch_ReturnsMatch()
	{
		_worldState.Add("color", "red");
		_worldState.Add("color", "blue");
		_worldState.Add("size", "red");
		_worldState.Add("size", "large");

		_bindings.Set("?x", "red");

		var and = new And(
			new Query("color", "?x"),
			new Query("size", "?x")
		);

		var results = EvaluateToList(and, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(1, results.Count);
		Assert.AreEqual("red", results[0].Get<string>("?x"));
	}

	[TestMethod]
	public void And_ExistingBindings_InconsistentMatch_ReturnsEmpty()
	{
		_worldState.Add("color", "red");
		_worldState.Add("color", "blue");
		_worldState.Add("size", "red");
		_worldState.Add("size", "large");

		_bindings.Set("?x", "green");

		var and = new And(
			new Query("color", "?x"),
			new Query("size", "?x")
		);

		var results = EvaluateToList(and, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(0, results.Count);
	}

	[TestMethod]
	public void And_MixedDataTypes_WorksCorrectly()
	{
		_worldState.Add("person_age", ("john", 25));
		_worldState.Add("person_age", ("mary", 30));
		_worldState.Add("min_age", 25);
		_worldState.Add("min_age", 18);

		var and = new And(
			new Query("person_age", "?person", "?age"),
			new Query("min_age", "?age")
		);

		var results = EvaluateToList(and, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(1, results.Count);
		Assert.AreEqual("john", results[0].Get<string>("?person"));
		Assert.AreEqual(25, results[0].Get<int>("?age"));
	}

	[TestMethod]
	public void And_SameVariableAcrossConditions_EnforcesConsistency()
	{
		_worldState.Add("parent", ("john", "mary"));
		_worldState.Add("parent", ("bob", "alice"));
		_worldState.Add("age", ("mary", 5));
		_worldState.Add("age", ("alice", 3));
		_worldState.Add("school_age", 5);

		var and = new And(
			new Query("parent", "?parent", "?child"),
			new Query("age", "?child", "?child_age"),
			new Query("school_age", "?child_age")
		);

		var results = EvaluateToList(and, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(1, results.Count);
		Assert.AreEqual("john", results[0].Get<string>("?parent"));
		Assert.AreEqual("mary", results[0].Get<string>("?child"));
		Assert.AreEqual(5, results[0].Get<int>("?child_age"));
	}

	[TestMethod]
	public void And_EmptyWorldState_ReturnsEmpty()
	{
		var and = new And(
			new Query("anything", "?_value"),
			new Query("something", "?other")
		);

		var results = EvaluateToList(and, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(0, results.Count);
	}

	[TestMethod]
	public void And_MultipleBindingsFirstCondition_PropagatesCorrectly()
	{
		_worldState.Add("inventory", "sword");
		_worldState.Add("inventory", "potion");
		_worldState.Add("inventory", "gold");
		_worldState.Add("usable", "sword");
		_worldState.Add("usable", "potion");

		var and = new And(
			new Query("inventory", "?item"),
			new Query("usable", "?item")
		);

		var results = EvaluateToList(and, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(2, results.Count);
		Assert.IsTrue(results.Any(r => r.Get<string>("?item") == "sword"));
		Assert.IsTrue(results.Any(r => r.Get<string>("?item") == "potion"));
		Assert.IsFalse(results.Any(r => r.Get<string>("?item") == "gold"));
	}

	[TestMethod]
	public void And_NestedVariableBindings_ComplexScenario()
	{
		_worldState.Add("location", ("john", "kitchen"));
		_worldState.Add("location", ("mary", "office"));
		_worldState.Add("location", ("bob", "kitchen"));
		_worldState.Add("available_tool", ("kitchen", "knife"));
		_worldState.Add("available_tool", ("kitchen", "pan"));
		_worldState.Add("available_tool", ("office", "computer"));
		_worldState.Add("can_use", ("john", "knife"));
		_worldState.Add("can_use", ("mary", "computer"));
		_worldState.Add("can_use", ("bob", "pan"));

		var and = new And(
			new Query("location", "?person", "?place"),
			new Query("available_tool", "?place", "?tool"),
			new Query("can_use", "?person", "?tool")
		);

		var results = EvaluateToList(and, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(3, results.Count);
		Assert.IsTrue(results.Any(r =>
			r.Get<string>("?person") == "john" &&
			r.Get<string>("?place") == "kitchen" &&
			r.Get<string>("?tool") == "knife"));
		Assert.IsTrue(results.Any(r =>
			r.Get<string>("?person") == "mary" &&
			r.Get<string>("?place") == "office" &&
			r.Get<string>("?tool") == "computer"));
		Assert.IsTrue(results.Any(r =>
			r.Get<string>("?person") == "bob" &&
			r.Get<string>("?place") == "kitchen" &&
			r.Get<string>("?tool") == "pan"));
	}

	[TestMethod]
	public void Not_Example_1()
	{
		_worldState.Add("in_fridge", "eggs");
		_worldState.Add("in_fridge", "milk");
		_worldState.Add("in_fridge", "cheese");
		_worldState.Add("allergic_to", ("alice", "eggs"));
		_worldState.Add("allergic_to", ("chris", "peanut"));
		_worldState.Add("allergic_to", ("john", "milk"));

		var and = new And(
			new Query("in_fridge", "?item"),
			new Not(new Query("allergic_to", "?person", "?item"))
		);

		var results = EvaluateToList(and, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(1, results.Count);
		Assert.IsTrue(results.Any(r => r.Get<string>("?item") == "cheese"));
	}

	[TestMethod]
	public void Not_ConditionThatFails_ReturnsCurrentBindings()
	{
		var not = new Not(new Query("nonexistent", "?_value"));

		var results = EvaluateToList(not, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(1, results.Count);
		Assert.AreEqual(0, results[0].Bindings.Count);
	}

	[TestMethod]
	public void Not_ConditionThatSucceeds_ReturnsEmpty()
	{
		_worldState.Add("color", "red");
		var not = new Not(new Query("color", "red"));

		var results = EvaluateToList(not, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(0, results.Count);
	}

	[TestMethod]
	public void Not_WithVariableBinding_ConditionSucceeds_ReturnsEmpty()
	{
		_worldState.Add("likes", ("john", "pizza"));
		_worldState.Add("likes", ("mary", "sushi"));

		_bindings.Set("?person", "john");
		var not = new Not(new Query("likes", "?person", "pizza"));

		var results = EvaluateToList(not, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(0, results.Count);
	}

	[TestMethod]
	public void Not_WithVariableBinding_ConditionFails_ReturnsBindings()
	{
		_worldState.Add("likes", ("john", "pizza"));
		_worldState.Add("likes", ("mary", "sushi"));

		_bindings.Set("?person", "bob");
		var not = new Not(new Query("likes", "?person", "pizza"));

		var results = EvaluateToList(not, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(1, results.Count);
		Assert.AreEqual("bob", results[0].Get<string>("?person"));
	}

	[TestMethod]
	public void Not_ComplexCondition_PatternWithMultipleMatches_ReturnsEmpty()
	{
		_worldState.Add("inventory", "sword");
		_worldState.Add("inventory", "potion");
		_worldState.Add("inventory", "gold");

		var not = new Not(new Query("inventory", "?item"));

		var results = EvaluateToList(not, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(0, results.Count);
	}

	[TestMethod]
	public void Not_ComplexCondition_AndThatSucceeds_ReturnsEmpty()
	{
		_worldState.Add("has_item", "john");
		_worldState.Add("can_use", "john");

		var not = new Not(new And(
			new Query("has_item", "john"),
			new Query("can_use", "john")
		));

		var results = EvaluateToList(not, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(0, results.Count);
	}

	[TestMethod]
	public void Not_ComplexCondition_AndThatFails_ReturnsBindings()
	{
		_worldState.Add("has_item", "john");

		var not = new Not(new And(
			new Query("has_item", "john"),
			new Query("can_use", "john")
		));

		var results = EvaluateToList(not, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(1, results.Count);
		Assert.AreEqual(0, results[0].Bindings.Count);
	}

	[TestMethod]
	public void Not_WithExistingBindings_PreservesBindings()
	{
		_worldState.Add("forbidden", "fire");

		_bindings.Set("?player", "alice");
		_bindings.Set("?level", 5);

		var not = new Not(new Query("forbidden", "water"));

		var results = EvaluateToList(not, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(1, results.Count);
		Assert.AreEqual("alice", results[0].Get<string>("?player"));
		Assert.AreEqual(5, results[0].Get<int>("?level"));
	}

	[TestMethod]
	public void Not_InComplexAndChain_WorksCorrectly()
	{
		_worldState.Add("person", "alice");
		_worldState.Add("person", "bob");
		_worldState.Add("person", "charlie");
		_worldState.Add("banned", "bob");
		_worldState.Add("has_access", "alice");
		_worldState.Add("has_access", "charlie");

		var and = new And(
			new Query("person", "?person"),
			new Not(new Query("banned", "?person")),
			new Query("has_access", "?person")
		);

		var results = EvaluateToList(and, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(2, results.Count);
		Assert.IsTrue(results.Any(r => r.Get<string>("?person") == "alice"));
		Assert.IsTrue(results.Any(r => r.Get<string>("?person") == "charlie"));
		Assert.IsFalse(results.Any(r => r.Get<string>("?person") == "bob"));
	}

	[TestMethod]
	public void Not_WithMixedDataTypes_WorksCorrectly()
	{
		_worldState.Add("age", ("john", 25));
		_worldState.Add("age", ("mary", 30));
		_worldState.Add("restricted_age", 25);

		var and = new And(
			new Query("age", "?person", "?age"),
			new Not(new Query("restricted_age", "?age"))
		);

		var results = EvaluateToList(and, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(1, results.Count);
		Assert.AreEqual("mary", results[0].Get<string>("?person"));
		Assert.AreEqual(30, results[0].Get<int>("?age"));
	}

	[TestMethod]
	public void Not_EmptyWorldState_WithPattern_ReturnsBindings()
	{
		var not = new Not(new Query("anything", "?_value"));

		var results = EvaluateToList(not, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(1, results.Count);
		Assert.AreEqual(0, results[0].Bindings.Count);
	}

	[TestMethod]
	public void Alt_PrefersFirstButBacktracksToOthers()
	{
		_worldState.Add("preferred_ice_cream", "vanilla");
		_worldState.Add("ice_cream", "chocolate");
		_worldState.Add("ice_cream", "strawberry");
		_worldState.Add("sold_out", "chocolate");

		var and = new And(
			new Alt(
				new Query("preferred_ice_cream", "?flavor"),
				new Query("ice_cream", "?flavor")
			),
			new Not(new Query("sold_out", "?flavor"))
		);

		var results = EvaluateToList(and, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(2, results.Count);
		Assert.IsTrue(results.Any(r => r.Get<string>("?flavor") == "vanilla"));
		Assert.IsTrue(results.Any(r => r.Get<string>("?flavor") == "strawberry"));
	}

	[TestMethod]
	public void Alt_PrefersFirstButBacktracksToOthers_FailsWithOr()
	{
		_worldState.Add("preferred_ice_cream", "vanilla");
		_worldState.Add("ice_cream", "chocolate");
		_worldState.Add("ice_cream", "strawberry");
		_worldState.Add("sold_out", "chocolate");

		var and = new And(
			new Or(
				new Query("preferred_ice_cream", "?flavor"),
				new Query("ice_cream", "?flavor")
			),
			new Not(new Query("sold_out", "?flavor"))
		);

		var results = EvaluateToList(and, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(1, results.Count);
		Assert.IsTrue(results.All(r => r.Get<string>("?flavor") == "vanilla"));
	}

	[TestMethod]
	public void Alt_PrefersFirstButBacktracksToOthers_FailsWithOr2()
	{
		_worldState.Add("preferred_ice_cream", "vanilla");
		_worldState.Add("ice_cream", "chocolate");
		_worldState.Add("ice_cream", "strawberry");
		_worldState.Add("sold_out", "vanilla");

		var and = new And(
			new Or(
				new Query("preferred_ice_cream", "?flavor"),
				new Query("ice_cream", "?flavor")
			),
			new Not(new Query("sold_out", "?flavor"))
		);

		var results = EvaluateToList(and, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(0, results.Count,
			"Should have 0 results because Or doesn't backtrack into ice_cream flavours, only tries preferred_ice_cream as it was the first matching branch.");
	}

	[TestMethod]
	public void Alt_PrefersFirstButBacktracksToOthers_MultiplePreferred()
	{
		_worldState.Add("ice_cream", "chocolate");
		_worldState.Add("sold_out", "strawberry");
		_worldState.Add("preferred_ice_cream", "mint");
		_worldState.Add("preferred_ice_cream", "vanilla");
		_worldState.Add("preferred_ice_cream", "strawberry");

		var and = new And(
			new Alt(
				new Query("preferred_ice_cream", "?flavor"),
				new Query("ice_cream", "?flavor")
			),
			new Not(new Query("sold_out", "?flavor"))
		);

		var results = EvaluateToList(and, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(3, results.Count);
		Assert.IsTrue(results.Any(r => r.Get<string>("?flavor") == "vanilla"));
		Assert.IsTrue(results.Any(r => r.Get<string>("?flavor") == "mint"));
		Assert.IsTrue(results.Any(r => r.Get<string>("?flavor") == "chocolate"));
		Assert.IsTrue(results.Last().Get<string>("?flavor") == "chocolate",
			"Chocolate should always be last due to alt ordering.");
	}

	[TestMethod]
	public void Or_QuicheAllergyOrSoldOut_ReturnsFirstMatchOnly()
	{
		_worldState.Add("has_ingredient", ("quiche", "eggs"));
		_worldState.Add("has_ingredient", ("quiche", "milk"));
		_worldState.Add("has_ingredient", ("quiche", "cheese"));
		_worldState.Add("allergic_to", ("alice", "eggs"));
		_worldState.Add("allergic_to", ("bob", "milk"));
		_worldState.Add("sold_out", "cheese");

		var and = new And(
			new Query("has_ingredient", "quiche", "?ingredient"),
			new Or(
				new Query("allergic_to", "?person", "?ingredient"),
				new Query("sold_out", "?ingredient")
			)
		);

		var results = EvaluateToList(and, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(3, results.Count);

		Assert.IsTrue(results.Any(r =>
			r.Get<string>("?ingredient") == "eggs" &&
			r.Get<string>("?person") == "alice"));

		Assert.IsTrue(results.Any(r =>
			r.Get<string>("?ingredient") == "milk" &&
			r.Get<string>("?person") == "bob"));

		Assert.IsTrue(results.Any(r =>
			r.Get<string>("?ingredient") == "cheese" &&
			!r.Has("?person")));
	}

	[TestMethod]
	public void Or_QuicheAllergyOrSoldOut_OnlySoldOutIngredients()
	{
		_worldState.Add("has_ingredient", ("quiche", "eggs"));
		_worldState.Add("has_ingredient", ("quiche", "milk"));
		_worldState.Add("has_ingredient", ("quiche", "cheese"));
		_worldState.Add("sold_out", "eggs");
		_worldState.Add("sold_out", "milk");
		_worldState.Add("sold_out", "cheese");

		var and = new And(
			new Query("has_ingredient", "quiche", "?ingredient"),
			new Or(
				new Query("allergic_to", "?person", "?ingredient"),
				new Query("sold_out", "?ingredient")
			)
		);

		var results = EvaluateToList(and, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(3, results.Count);
		Assert.IsTrue(results.Any(r => r.Get<string>("?ingredient") == "eggs"));
		Assert.IsTrue(results.Any(r => r.Get<string>("?ingredient") == "milk"));
		Assert.IsTrue(results.Any(r => r.Get<string>("?ingredient") == "cheese"));

		Assert.IsTrue(results.All(r => !r.Has("?person")));
	}

	[TestMethod]
	public void Or_QuicheAllergyOrSoldOut_NoMatches()
	{
		_worldState.Add("has_ingredient", ("quiche", "eggs"));
		_worldState.Add("has_ingredient", ("quiche", "milk"));
		_worldState.Add("has_ingredient", ("quiche", "cheese"));

		var and = new And(
			new Query("has_ingredient", "quiche", "?ingredient"),
			new Or(
				new Query("allergic_to", "?person", "?ingredient"),
				new Query("sold_out", "?ingredient")
			)
		);

		var results = EvaluateToList(and, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual(0, results.Count);
	}

	[TestMethod]
	public void AltOr_DemonstrateDifference()
	{
		_worldState.Add("preferred_ice_cream", "vanilla");
		_worldState.Add("ice_cream", "chocolate");
		_worldState.Add("ice_cream", "strawberry");
		_worldState.Add("sold_out", "vanilla");

		var or = new And(
			new Or(
				new Query("preferred_ice_cream", "?flavor"),
				new Query("ice_cream", "?flavor")
			),
			new Not(new Query("sold_out", "?flavor"))
		);

		var alt = new And(
			new Alt(
				new Query("preferred_ice_cream", "?flavor"),
				new Query("ice_cream", "?flavor")
			),
			new Not(new Query("sold_out", "?flavor"))
		);

		var resultsOr = EvaluateToList(or, _worldState, _bindings, _ctx, _context);

		var altBinds = new ScopeVariables();
		var altCtx = new PlanDebugState();
		var resultsAlt = EvaluateToList(alt, _worldState, altBinds, _ctx, altCtx);

		Assert.AreNotEqual(resultsAlt.Count, resultsOr.Count);
	}
}

