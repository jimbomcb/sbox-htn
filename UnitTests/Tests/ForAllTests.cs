using System.Collections.Generic;
using HTN.Conditions;
using HTN.Planner;

namespace HTN.Tests;

[TestClass]
public partial class ForAllTests
{
	private WorldState _worldState;
	private ScopeVariables _bindings;
	private PlanDebugState _context = null;
	private PlannerContext _ctx = null;

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
	public void ForAll_NoForEachBindings_ReturnsSuccess()
	{
		var forAll = new ForAll(
			new Query( "person", "?person" ),
			new Not( new Query( "hungry", "?person" ) )
		);

		var results = EvaluateToList(forAll, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual( 1, results.Count, "ForAll over empty set should return success (vacuous truth)" );
	}

	[TestMethod]
	public void ForAll_AllPeopleAreFed_ReturnsSuccess()
	{
		_worldState.Add( "person", "alice" );
		_worldState.Add( "person", "bob" );
		_worldState.Add( "person", "charlie" );

		var forAll = new ForAll(
			new Query( "person", "?person" ),
			new Not( new Query( "hungry", "?person" ) )
		);

		var results = EvaluateToList(forAll, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual( 1, results.Count );
		Assert.AreEqual( 0, results[0].Bindings.Count );
	}

	[TestMethod]
	public void ForAll_SomePeopleAreHungry_ReturnsFailure()
	{
		_worldState.Add( "person", "alice" );
		_worldState.Add( "person", "bob" );
		_worldState.Add( "person", "charlie" );
		_worldState.Add( "hungry", "bob" );

		var forAll = new ForAll(
			new Query( "person", "?person" ),
			new Not( new Query( "hungry", "?person" ) )
		);

		var results = EvaluateToList(forAll, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual( 0, results.Count );
	}

	[TestMethod]
	public void ForAll_AllIngredientsInStock_ReturnsSuccess()
	{
		_worldState.Add( "has_ingredient", ("cake", "eggs") );
		_worldState.Add( "has_ingredient", ("cake", "milk") );
		_worldState.Add( "in_stock", "eggs" );
		_worldState.Add( "in_stock", "milk" );

		var forAll = new ForAll(
			new Query( "has_ingredient", "cake", "?ingredient" ),
			new Query( "in_stock", "?ingredient" )
		);

		var results = EvaluateToList(forAll, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual( 1, results.Count );
		Assert.AreEqual( 0, results[0].Bindings.Count );
	}

	[TestMethod]
	public void ForAll_SomeIngredientsOutOfStock_ReturnsFailure()
	{
		_worldState.Add( "has_ingredient", ("cake", "eggs") );
		_worldState.Add( "has_ingredient", ("cake", "milk") );
		_worldState.Add( "in_stock", "eggs" );

		var forAll = new ForAll(
			new Query( "has_ingredient", "cake", "?ingredient" ),
			new Query( "in_stock", "?ingredient" )
		);

		var results = EvaluateToList(forAll, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual( 0, results.Count );
	}

	[TestMethod]
	public void ForAll_AllFridgeItemsNotExpired_ReturnsSuccess()
	{
		_worldState.Add( "in_fridge", "milk" );
		_worldState.Add( "in_fridge", "cheese" );
		_worldState.Add( "in_fridge", "eggs" );
		_worldState.Add( "expired", "bread" );

		var forAll = new ForAll(
			new Query( "in_fridge", "?item" ),
			new Not( new Query( "expired", "?item" ) )
		);

		var results = EvaluateToList(forAll, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual( 1, results.Count );
		Assert.AreEqual( 0, results[0].Bindings.Count );
	}

	[TestMethod]
	public void ForAll_SomeFridgeItemsExpired_ReturnsFailure()
	{
		_worldState.Add( "in_fridge", "milk" );
		_worldState.Add( "in_fridge", "cheese" );
		_worldState.Add( "in_fridge", "eggs" );
		_worldState.Add( "expired", "milk" );

		var forAll = new ForAll(
			new Query( "in_fridge", "?item" ),
			new Not( new Query( "expired", "?item" ) )
		);

		var results = EvaluateToList(forAll, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual( 0, results.Count );
	}

	[TestMethod]
	public void ForAll_WithExistingBindings_ConsistentBinding_ReturnsSuccess()
	{
		_worldState.Add( "student_in_class", ("alice", "math") );
		_worldState.Add( "student_in_class", ("bob", "math") );
		_worldState.Add( "student_in_class", ("charlie", "science") );
		_worldState.Add( "passed", ("alice", "math") );
		_worldState.Add( "passed", ("bob", "math") );
		_worldState.Add( "passed", ("charlie", "science") );

		_bindings.Set( "?class", "math" );

		var forAll = new ForAll(
			new Query( "student_in_class", "?student", "?class" ),
			new Query( "passed", "?student", "?class" )
		);

		var results = EvaluateToList(forAll, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual( 1, results.Count );
		Assert.AreEqual( "math", results[0].Get<string>( "?class" ) );
	}

	[TestMethod]
	public void ForAll_WithExistingBindings_InconsistentBinding_ReturnsFailure()
	{
		_worldState.Add( "student_in_class", ("alice", "math") );
		_worldState.Add( "student_in_class", ("bob", "math") );
		_worldState.Add( "student_in_class", ("charlie", "science") );
		_worldState.Add( "passed", ("alice", "math") );
		_worldState.Add( "passed", ("bob", "math") );

		_bindings.Set( "?class", "science" );

		var forAll = new ForAll(
			new Query( "student_in_class", "?student", "?class" ),
			new Query( "passed", "?student", "?class" )
		);

		var results = EvaluateToList(forAll, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual( 0, results.Count );
	}

	[TestMethod]
	public void ForAll_ComplexMustSatisfyCondition_AndCondition_ReturnsSuccess()
	{
		_worldState.Add( "employee", "alice" );
		_worldState.Add( "employee", "bob" );
		_worldState.Add( "has_skill", ("alice", "programming") );
		_worldState.Add( "has_skill", ("bob", "programming") );
		_worldState.Add( "certified", "alice" );
		_worldState.Add( "certified", "bob" );

		var forAll = new ForAll(
			new Query( "employee", "?person" ),
			new And(
				new Query( "has_skill", "?person", "programming" ),
				new Query( "certified", "?person" )
			)
		);

		var results = EvaluateToList(forAll, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual( 1, results.Count );
		Assert.AreEqual( 0, results[0].Bindings.Count );
	}

	[TestMethod]
	public void ForAll_ComplexMustSatisfyCondition_AndCondition_ReturnsFailure()
	{
		_worldState.Add( "employee", "alice" );
		_worldState.Add( "employee", "bob" );
		_worldState.Add( "has_skill", ("alice", "programming") );
		_worldState.Add( "has_skill", ("bob", "programming") );
		_worldState.Add( "certified", "alice" );

		var forAll = new ForAll(
			new Query( "employee", "?person" ),
			new And(
				new Query( "has_skill", "?person", "programming" ),
				new Query( "certified", "?person" )
			)
		);

		var results = EvaluateToList(forAll, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual( 0, results.Count );
	}

	[TestMethod]
	public void ForAll_NestedInAndCondition_ReturnsCorrectResult()
	{
		_worldState.Add( "dish", "pasta" );
		_worldState.Add( "dish", "salad" );
		_worldState.Add( "needs_ingredient", ("pasta", "noodles") );
		_worldState.Add( "needs_ingredient", ("pasta", "sauce") );
		_worldState.Add( "needs_ingredient", ("salad", "lettuce") );
		_worldState.Add( "needs_ingredient", ("salad", "tomato") );
		_worldState.Add( "available", "noodles" );
		_worldState.Add( "available", "sauce" );
		_worldState.Add( "available", "lettuce" );

		var and = new And(
			new Query( "dish", "?dish" ),
			new ForAll(
				new Query( "needs_ingredient", "?dish", "?ingredient" ),
				new Query( "available", "?ingredient" )
			)
		);

		var results = EvaluateToList(and, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual( 1, results.Count );
		Assert.AreEqual( "pasta", results[0].Get<string>( "?dish" ) );
	}

	[TestMethod]
	public void ForAll_EmptyWorldState_ReturnsSuccess()
	{
		var forAll = new ForAll(
			new Query( "person", "?person" ),
			new Query( "happy", "?person" )
		);

		var results = EvaluateToList(forAll, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual( 1, results.Count, "ForAll over empty set should return success (vacuous truth)" );
	}

	[TestMethod]
	public void ForAll_PreservesOriginalBindings_OnSuccess()
	{
		_worldState.Add( "employee", "alice" );
		_worldState.Add( "employee", "bob" );
		_worldState.Add( "skilled", "alice" );
		_worldState.Add( "skilled", "bob" );

		_bindings.Set( "?company", "TechCorp" );
		_bindings.Set( "?year", 2024 );

		var forAll = new ForAll(
			new Query( "employee", "?person" ),
			new Query( "skilled", "?person" )
		);

		var results = EvaluateToList(forAll, _worldState, _bindings, _ctx, _context);

		Assert.AreEqual( 1, results.Count );
		Assert.AreEqual( "TechCorp", results[0].Get<string>( "?company" ) );
		Assert.AreEqual( 2024, results[0].Get<int>( "?year" ) );
		Assert.IsFalse( results[0].Has( "?person" ) );
	}
}
