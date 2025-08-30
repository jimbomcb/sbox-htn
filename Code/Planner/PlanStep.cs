using HTN.Tasks;

namespace HTN.Planner;

/// <summary>
/// The planner produces a Plan of PlanSteps, each step containing a task and set of generated bindings.
/// The owning Plan is responsible for the release of the step task back into the pool via disposal.
/// </summary>
public interface IPlanStep
{
	IPrimitiveTask Task { get; }
	ScopeVariables Variables { get; }
}

public readonly record struct PlanStep( IPrimitiveTask Task, ScopeVariables Variables ) : IPlanStep
{
	IPrimitiveTask IPlanStep.Task { get => Task; }
	ScopeVariables IPlanStep.Variables { get => Variables; }
}

public readonly record struct PlanStep<T>( T Task, ScopeVariables Variables ) : IPlanStep where T : IPrimitiveTask
{
	IPrimitiveTask IPlanStep.Task { get => Task; }
	ScopeVariables IPlanStep.Variables { get => Variables; }
}
