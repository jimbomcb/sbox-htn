using HTN.Conditions;
using HTN.Tasks;
using System;

namespace HTN.Planner;

/// <summary>
/// Each compound task consists of an ordered list of branches, the planner will find the first precondition-passing branch
/// and try to build a plan from the task list.
/// If preconditions match then it picks that set of primitive and compound tasks, the planner will recursively decompose the compound tasks 
/// until it is only left with a sequence of primitive tasks.
/// For more basics, see https://www.gameaipro.com/GameAIPro/GameAIPro_Chapter29_Hierarchical_AI_for_Multiplayer_Bots_in_Killzone_3.pdf
/// </summary>
public readonly record struct Branch( string Name, ICondition Precondition, Func<ScopeVariables, ITask[]> TaskFactory )
{
	public Branch( string Name, ICondition[] preconditions, Func<ScopeVariables, ITask[]> taskFactory )
		: this( Name, new And( preconditions ), taskFactory )
	{
	}
}
