using HTN.Planner;
using Sandbox.Diagnostics;
using System;

namespace HTN.Tasks;

/// <summary>
/// Compound tasks contain a series of branches, each branch containing preconditions and tasks (compound or primitive).
/// Tasks should define a constructor in which they populate the Branches list, containing the preconditions and tasks to execute.
/// 
/// The planner finds the first branch with matching preconditions, and processes the list of primitive and compound tasks. 
/// Each compound task will be "decomposed" (collapsed into a list of tasks) recursively decomposing compound tasks until we are left with a 
/// chain of primitive tasks to execute.
/// 
/// See:	https://www.gameaipro.com/GameAIPro/GameAIPro_Chapter12_Exploring_HTN_Planners_through_Example.pdf#page=5
///			https://www.gameaipro.com/GameAIPro/GameAIPro_Chapter29_Hierarchical_AI_for_Multiplayer_Bots_in_Killzone_3.pdf#page=4
/// 
/// Backtracking is performed if compound tasks fail to decompose, trying alternative variables from the branch precondition queries,
/// once all alternative variables have been exhausted the next precondition-passing branch will be selected.
/// 
/// See:	https://youtu.be/b04n6JfDano?t=614
/// 
/// Shouldn't be inherited from directly, instead use the CompoundTaskBase class and build a list of Branches in the constructor,
/// acquiring tasks from the TaskPool using the <![CDATA[Run<TaskType>()]]> method.
/// </summary>
public abstract class CompoundTaskBase : ICompoundTask
{
	/// <summary>
	/// Populated in the constructor of derived compound tasks with a series of branches, each 
	/// containing a set of preconditions and tasks to execute if the preconditions pass.
	/// </summary>
	public Branch[] Branches { get; init; }

	public ReadOnlySpan<Branch> GetBranches()
	{
		Assert.NotNull( Branches );
		return Branches.AsSpan();
	}

	/// <summary>
	/// Get a task from the pool. This is the ONLY way tasks should be created during planning.
	/// Direct instantiation with 'new TaskType()' is not allowed and will be detected in DEBUG builds.
	/// For example:
	/// 
	/// <![CDATA[
	/// Branches = [
	///		new Branch("Branch1",
	///			[new Query("weapon", "?weapon"), new Query("can_use_weapon", "?weapon", "?enemy")],
	/// --->	/* AttackEnemy is instantiated via Run<TaskType>() and configured with the bound variables */
	/// --->	(vars) => [ Run<AttackEnemy>().Configure(vars.Get<Enemy>("?enemy"), vars.Get<Weapon>("?weapon")) ]
	///		)
	/// ];
	///	]]>
	/// </summary>
	protected static T Run<T>() where T : ITask, new()
	{
		var task = TaskPool.Instance.Get<T>();
#if !DEBUG
		HTN.Diagnostics.TaskInstantiationTracker.RegisterPoolCreation(task);
#endif
		return task;
	}
}
