using HTN.Planner;
using System;

namespace HTN.Conditions;

/// <summary>
/// Logical NOT, the condition is satisfied if the inner condition fails.
/// By its nature will never produce any bindings.
/// </summary>
public class Not( ICondition condition ) : ICondition
{
	public EvaluationResult Evaluate( WorldState state, ScopeVariables vars, PlannerContext ctx, PlanDebugState debugState, Func<ScopeVariables, EvaluationResult> onResult )
	{
		var hasResult = false;
		condition.Evaluate( state, vars, ctx, debugState, result =>
		{
			hasResult = true;
			return EvaluationResult.Finished; // Finished on the first result
		} );

		if ( !hasResult )
			return onResult( vars );

		return EvaluationResult.NoResults;
	}
}
