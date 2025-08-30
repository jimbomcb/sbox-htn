using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace HTN.Planner;

/// <summary>
/// The planner WorldState is a collection of (key param1 param2 ...) tuples.
/// Multiple sets of parameters can be associated with a single key, allowing complex querying.
/// 
/// These values are queried against inside branch preconditions, allowing the planner to make
/// decisions based on the current state of the world. Variables from the world state are captured 
/// during querying in sets of BoundVariables.
/// 
/// Arbitrary types are supported, but they should at least have a hash code and equality comparison.
/// 
/// For example, a world state might contain:
///		(enemy alpha Vector3(100,200,300) [ThreatDetails]) // Where ThreatDetails is an arbitrary struct	
/// And a query might look like:
///		(enemy alpha ?position ?details)
/// Allowing compound task branches to directly reference the matching complex ThreatDetails type via:
/// <![CDATA[
///		( vars ) => [ ... vars.Get<Vector3>( "?position" ), vars.Get<ThreatDetails>( "?details" ) ... ]
///	]]>
///	TODO rewrite
/// </summary>
public class WorldState
{
	public WorldState() { }

	public WorldState(WorldState copy)
	{
		State = new Dictionary<string, HashSet<ITuple>>(copy.State, StringComparer.OrdinalIgnoreCase);
	}

	public Dictionary<string, HashSet<ITuple>> State { get; } = [];

	public void Add( string key, ITuple tuple )
	{
		// todo: check tuple elements for equality/hash code? necessary?
		if ( !State.TryGetValue( key, out var value ) )
			State[key] = [tuple];
		else
			value.Add( tuple );
	}

	public void Add( string key, object singleTupleValue )
	{
		if ( !State.TryGetValue( key, out var value ) )
			State[key] = [ValueTuple.Create( singleTupleValue )];
		else
			value.Add( ValueTuple.Create( singleTupleValue ) );
	}

	public void Clear()
	{
		foreach ( var hashSet in State.Values )
		{
			hashSet.Clear();
		}
	}

}
