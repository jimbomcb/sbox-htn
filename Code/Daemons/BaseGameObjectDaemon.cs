using HTN.Planner;
using Sandbox;

namespace HTN.Daemons;

/// <summary>
/// Provides basic information to the world state such as the owning GameObject's location and rotation.
/// </summary>
public class BaseGameObjectDaemon( GameObject owner ) : IHTNDaemon
{
	public void ApplyWorldState( WorldState worldState )
	{
		if ( !owner.IsValid )
			return;

		worldState.Add( "position", owner.WorldPosition );
		worldState.Add( "rotation", owner.WorldRotation );
	}
}
