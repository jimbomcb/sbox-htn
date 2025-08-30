using HTN.Planner;

namespace HTN.Tasks;

/// <summary>
/// <see cref="CompoundTaskBase"/>
/// </summary>
public interface ICompoundTask : ITask
{
	Branch[] Branches { get; init; }
}
