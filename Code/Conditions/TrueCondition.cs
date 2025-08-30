using HTN.Planner;
using System;

namespace HTN.Conditions;

public class TrueCondition : ICondition
{
	public EvaluationResult Evaluate( WorldState state, ScopeVariables vars, PlannerContext ctx, PlanDebugState debugState, Func<ScopeVariables, EvaluationResult> onResult )
	{
		return onResult.Invoke( vars );
	}
}
