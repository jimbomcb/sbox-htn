using HTN.Planner;

namespace HTN.Tasks;

public enum TaskResult
{
	Success,
	Failure,
	Running
}

/// <summary>
/// <see cref="PrimitiveTaskBase"/>
/// </summary>
public interface IPrimitiveTask : ITask
{
	public abstract TaskResult Execute( PlannerContext ctx, ScopeVariables variables );

	bool OnPlanned( PlannerContext ctx, ScopeVariables variables );

	void OnPlanFinished( PlannerContext ctx, ScopeVariables variables );

	bool OnActivate( PlannerContext ctx, ScopeVariables variables );

	void OnDeactivate( PlannerContext ctx, ScopeVariables variables );
}
