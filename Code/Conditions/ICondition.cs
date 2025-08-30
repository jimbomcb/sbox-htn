using HTN.Planner;
using System;

namespace HTN.Conditions;

/// <summary>
/// Conditions are evaluated during the HTN planning process, they compare against the world state, bind variables, and control the flow of branch execution.
/// Branches have a sequence of conditions that must all be satisfied for the branch to be considered valid, the planner will evaluate each condition in order.
/// Conditions, usually <see cref="Query"/>, bind variables that are then available to later conditions in the same branch, as well as the tasks that are produced if the branch is selected.
/// 
/// For example: new And( new Query("enemy", "?target", "?position"), new IsInState("?target", "alert") )
/// In this example, Query binds "?target" and "?position" one by one for each matching enemy, then IsInState checks if the bound "?target" is in the "alert" state.
/// For example if the first selected enemy does not pass IsInState, the planner will backtrack and try the next matching enemy, repeating until a valid set of variables is found or all options are exhausted.
/// </summary>
public interface ICondition
{
	/// <summary>
	/// Evaluates this condition against the current world state with the given variable bindings.
	/// During planning, conditions are evaluated in sequence within a branch. When a condition evaluates successfully, it may produce variable bindings that are passed to subsequent conditions and tasks.
	/// If a condition fails, the planner will backtrack and try alternative paths.
	/// </summary>
	/// <param name="state">World state at time of planning</param>
	/// <param name="vars">Variable bindings for the current branch</param>
	/// <param name="ctx">Planning context</param>
	/// <param name="debugState">Debugging information</param>
	/// <param name="onResult">Callback for when the evaluation is complete</param>
	/// <returns></returns>
	EvaluationResult Evaluate( WorldState state, ScopeVariables vars, PlannerContext ctx, PlanDebugState debugState, Func<ScopeVariables, EvaluationResult> onResult );

	public static readonly TrueCondition AlwaysTrue = new();
}
