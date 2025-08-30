using Sandbox;

namespace HTN.Planner;

/// <summary>
/// Context in which the plan exector is running, supplied to the execution of each primitive task.
/// Usually extended by the game project and supplied to <see cref="PlanExecutor.PlanExecutor"/> for additional game-specific contextual data, ie a typed owner component.
/// </summary>
public class PlannerContext( GameObject owner )
{
	public Scene Scene { get; init; } = owner?.Scene;
	public GameObject Owner { get; init; } = owner;
	public PlanExecutor Executor { get; set; } = null;
}
