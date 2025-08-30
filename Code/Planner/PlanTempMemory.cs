using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace HTN.Planner;

/// <summary>
/// Tasks can submit temporary world state facts, and clean them up when done.
/// See: <see cref="Tasks.PlanScopeSetWorldState"/>
/// </summary>
public class PlanTempMemory
{
	// TODO: Hit enough to warrant more performant implementation?

	public record struct Entry(string Key, ITuple Value, int Index = -1);
	public IReadOnlyCollection<Entry> Entries => _entries.AsReadOnly();
	private readonly List<Entry> _entries = [];

	public int Add( string key, ITuple value )
	{
		var nextIndex = NextIndex();
		_entries.Add( new( key, value, nextIndex ) );
		return nextIndex;
	}

	public bool Remove( int index )
	{
		var entry = _entries.FirstOrDefault( e => e.Index == index );
		if ( entry.Index < 0 ) return false;
		_entries.Remove( entry );
		return true;
	}

	private int NextIndex()
	{
		if ( _entries.Count == 0 ) return 0;
		return _entries.Max( e => e.Index ) + 1;
	}
}
