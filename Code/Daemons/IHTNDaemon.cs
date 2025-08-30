using HTN.Planner;

namespace HTN.Daemons;

/// <summary>
/// HTN daemons provide a world state for the planner through a series of tuples.
/// This includes things like perception, messaging, orders, etc.
/// </summary>
public interface IHTNDaemon
{
	public void ApplyWorldState( WorldState worldState );
}
