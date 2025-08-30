using HTN.Planner;
using System;

namespace HTN.Conditions;

/// <summary>
/// Logical OR, tries conditions in order and returns results from the first condition that succeeds.
/// Does not backtrack - once a condition produces results, subsequent conditions are not evaluated.
/// 
/// This is distinct from ALT which evaluates ALL conditions and allows full backtracking.
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
public class Or( params ICondition[] conditions ) : ICondition
{
	public EvaluationResult Evaluate( WorldState state, ScopeVariables vars, PlannerContext ctx, PlanDebugState debugState, Func<ScopeVariables, EvaluationResult> onResult )
	{
		for ( var i = 0; i < conditions.Length; i++ )
		{
			using ( debugState?.PreconditionEvents.Scope( $"OR {i}" ) )
			{
				var foundAnyResult = false;
				var result = conditions[i].Evaluate( state, vars, ctx, debugState, result =>
				{
					foundAnyResult = true;
					return onResult( result );
				} );

				if ( foundAnyResult )
					return result;
			}
		}

		return EvaluationResult.NoResults;
	}
}
