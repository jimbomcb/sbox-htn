using HTN.Planner;
using System;

namespace HTN.Conditions;

/// <summary>
/// The ALT condition allows for the OR like evaluation of multiple branches, but allows for backtrack chaining to the 
/// next condition after we exhaust all bound variables from the first matching branch.
/// 
/// This is distinct from the OR condition which will only yield results for the first branch that succeeds.
/// 
/// OR:
/// - Tries subconditions in order until ONE succeeds
/// - Returns ONLY the very first set of variables that match the query
/// - Does not allow backtracking, if the task decomposition fails with the first result, it will not try the others.
/// - Use when: "Try these subconditions in order, take the first set of matching bound variables only"
/// 
/// ALT:
/// - Evaluates ALL subconditions in order
/// - Returns each possible set of variables from each subcondition before advancing
/// - Provides full backtracking, if task decomposition fails for every possible set of variables of the first subcondition,
///   it will try the next subcondition and yield all of its matching variables.
/// - Use when: "Try each subcondition in order, allow iterating over every BoundVariables that match each subcondition"
/// </summary>
public class Alt( params ICondition[] subconditions ) : ICondition
{
	public EvaluationResult Evaluate( WorldState state, ScopeVariables vars, PlannerContext ctx, PlanDebugState debugState, Func<ScopeVariables, EvaluationResult> onResult )
	{
		using var scope = debugState?.PreconditionEvents.Scope( "ALT" );

		foreach ( var condition in subconditions )
		{
			if ( condition.Evaluate( state, vars, ctx, debugState, result => {
				debugState?.PreconditionEvents.Event( $"Alt: Yielding result from condition {Array.IndexOf( subconditions, condition )}" );
				return onResult( result );
			} ) == EvaluationResult.Finished )
			{
				return EvaluationResult.Finished;
			}
		}

		return EvaluationResult.NoResults;
	}
}
