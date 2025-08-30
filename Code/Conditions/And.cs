using HTN.Planner;
using System;

namespace HTN.Conditions;

/// <summary>
/// AND, all conditions must succeed for this condition to be satisfied.
/// Backtracking is supported, all conditions are evaluated in order and all valid bindings 
/// are yielded when all conditions are satisfied.
/// 
/// For example with world state of (enemy alpha, enemy bravo, enemy charlie, can_attack alpha, can_attack bravo):
/// A query of AND( QUERY( enemy ?name ), QUERY( can_attack ?name ) ) will yield:
/// 1: BoundVariables with ?name = alpha
/// 2: BoundVariables with ?name = bravo
/// 3: No results for charlie, as can_attack charlie is not satisfied.
/// </summary>
public class And( params ICondition[] conditions ) : ICondition
{
	public EvaluationResult Evaluate( WorldState state, ScopeVariables vars, PlannerContext ctx, PlanDebugState debugState, Func<ScopeVariables, EvaluationResult> onResult )
	{
		return conditions.Length == 0 ? onResult( vars ) : EvaluateRecursive( state, vars, ctx, debugState, 0, onResult );
	}

	private EvaluationResult EvaluateRecursive( WorldState state, ScopeVariables vars, PlannerContext ctx, PlanDebugState debugState, int index, Func<ScopeVariables, EvaluationResult> onResult )
	{
		if ( index >= conditions.Length )
			return onResult( vars );

		using ( debugState?.PreconditionEvents.Scope( $"AND {index}" ) )
		{
			return conditions[index].Evaluate( state, vars, ctx, debugState, conditionResult =>
			{
				return EvaluateRecursive( state, conditionResult, ctx, debugState, index + 1, onResult );
			} );
		}
	}
}

