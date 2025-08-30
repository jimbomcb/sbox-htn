using HTN.Planner;

namespace HTN.Tasks;

/// <summary>
/// A single performed action in the HTN planner.
/// The planner takes compound tasks and decomposes them into a set of primitive tasks depending on the world state and evaluated preconditions.
/// </summary>
public abstract class PrimitiveTaskBase : IPrimitiveTask
{
	/// <summary>
	/// Called every planner tick when this task is active. 
	/// </summary>
	/// <param name="ctx">The context provided to the owning plan executor</param>
	/// <param name="variables">The scope variables at planning time</param>
	/// <returns>
	///	- <see cref="TaskResult.Success"/> when this task has completed, the executor will advance to the next task on the next tick.
	///	- <see cref="TaskResult.Failure"/> when this task has failed, the executor will abandon all future planned tasks and replan on the next tick.
	///	- <see cref="TaskResult.Running"/> when this task is performing an action over multiple frames, will be executed again next tick.
	/// </returns>
	public virtual TaskResult Execute( PlannerContext ctx, ScopeVariables variables ) => TaskResult.Success;

	/// <summary>
	/// Called as soon as this task is planned to run, before any execution.
	/// Underlying task might not end up getting activated if replanned before execution.
	/// If this returns false it signals that the scheduled plan should be cancelled and not executed.
	/// </summary>
	public virtual bool OnPlanned( PlannerContext ctx, ScopeVariables variables ) => true;

	/// <summary>
	/// Called as soon as the plan is finished, after all tasks have been executed. 
	/// May be called before execution if the plan is cancelled or fails and we replan.
	/// </summary>
	public virtual void OnPlanFinished( PlannerContext ctx, ScopeVariables variables ) { }

	/// <summary>
	/// Called right before the first execution (this plan) of this task.
	/// Returning false signals that this is no longer valid to perform and we should replan.
	/// </summary>
	public virtual bool OnActivate( PlannerContext ctx, ScopeVariables variables ) => true;

	/// <summary>
	/// Called after execution of this task has finished, regardless of the cause (success, failure, or cancellation).
	/// Guaranteed to be called if OnActivate was called prior.
	/// </summary>
	public virtual void OnDeactivate( PlannerContext ctx, ScopeVariables variables ) { }
}
