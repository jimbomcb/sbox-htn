using HTN.Planner;
using System;

namespace HTN.Conditions;

/// <summary>
/// The ForAll condition is satisfied if the subCondition is true for all possible bindings from the Query.
/// </summary>
public class ForAll( Query query, ICondition subCondition ) : ICondition
{
	public EvaluationResult Evaluate( WorldState state, ScopeVariables vars, PlannerContext ctx, PlanDebugState debugState, Func<ScopeVariables, EvaluationResult> onResult )
	{
		var allSubConditionsMet = true;
		query.Evaluate( state, vars, ctx, debugState, queryResult =>
		{
			var subConditionMet = false;
			subCondition.Evaluate( state, queryResult, ctx, debugState, subQueryResult =>
			{
				subConditionMet = true;
				return EvaluationResult.Finished; // We only need one success to confirm this path
			} );

			if ( !subConditionMet )
			{
				allSubConditionsMet = false;
				return EvaluationResult.Finished; // One failure means the whole ForAll fails
			}

			return EvaluationResult.NoResults;
		} );

		if ( allSubConditionsMet )
			return onResult( vars );

		return EvaluationResult.NoResults;
	}
}
