using HTN.Planner;
using System;
using System.Linq;

namespace HTN.Conditions;

/// <summary>
/// Perform a query against the world state tuples, binding variables of otherwise-matching patterns to the provided <see cref="ScopeVariables"/>.
/// Supports wildcard (*) matching (matches anything, but must exist) and multi-wildcard (**) matching (matches anything remaining, regardless of remaining tuple members).
/// 
/// For example, given a world state containing: 
///		(enemy alpha Vector3(100,200,300) [ThreatDetails] false) // Where ThreatDetails is an arbitrary struct
///		(enemy beta Vector3(400,500,600) [ThreatDetails] true)
///		(enemy gamma Vector3(700,800,900) [ThreatDetails] true)
///	
/// A query of `(enemy alpha ?position ?details false)` would match only alpha, binding ?position and ?details for subsequent conditions and chosen tasks.
/// A query of `(enemy ?name ?position * true)`	would match beta and gamma, binding ?name and ?position. The * wildcard matches the [ThreatDetails] part of the tuple.
/// A query of `(enemy ?name ?position **)` would match all three enemies, binding ?name and ?position. The ** multi-wildcard matches any remaining parts of the tuple.
/// A query of `(enemy ?name ?position ?details)` would return NO matches, because all enemy entries have a fourth parameter, and the query does not include a wildcard to match it.
/// </summary>
public class Query : ICondition
{
	private readonly string _key;
	private readonly object[] _query;
	private readonly bool _hasMultiWildcard;

	public Query( string key, params object[] query )
	{
		_key = key;
		_query = query;
		_hasMultiWildcard = query.Length > 0 && query[^1] is "**";

		// Validate multi-wildcard position at construction time
		if ( query.Take( query.Length - 1 ).Any( part => part is "**" ) )
			throw new ArgumentException( "Multi-wildcard (**) can only be the last parameter in a query." );
	}

	public EvaluationResult Evaluate( WorldState state, ScopeVariables vars, PlannerContext ctx, PlanDebugState debugState, Func<ScopeVariables, EvaluationResult> onResult )
	{
		debugState?.QueryEvaluated();

		if ( !state.State.TryGetValue( _key, out var tuples ) )
		{
			return EvaluationResult.NoResults;
		}

		foreach ( var tuple in tuples )
		{
			if ( !_hasMultiWildcard && _query.Length != tuple.Length )
				continue;

			if ( _hasMultiWildcard && tuple.Length < _query.Length - 1 )
				continue;

			var newVars = TaskPool.CloneScopeVariables(vars); // TODO: Move to out the foreach loop...
			var match = true;
			var queryLength = _hasMultiWildcard ? _query.Length - 1 : _query.Length;

			for ( var i = 0; i < queryLength; i++ )
			{
				var queryPart = _query[i];
				var tuplePart = tuple[i];

				switch ( queryPart ) // split out wildcard vs ?var match vs exact match
				{
					case "*":
						continue;

					case string varName when varName.StartsWith( '?' ):
						if ( newVars.Has( varName ) )
						{
							if ( !newVars.Get<object>( varName ).Equals( tuplePart ) )
							{
								match = false;
							}
						}
						else
						{
							newVars.Set( varName, tuplePart );
						}
						break;

					default:
						if ( queryPart is string queryStr && tuplePart is string tupleStr )
						{
							if ( !string.Equals( queryStr, tupleStr, StringComparison.OrdinalIgnoreCase ) )
								match = false;
						}
						else if ( !queryPart.Equals( tuplePart ) )
						{
							match = false;
						}
						break;
				}

				if ( !match )
					break;
			}

			if ( match )
			{
				debugState?.ResultEnumerated();
				if ( onResult( newVars ) == EvaluationResult.Finished )
					return EvaluationResult.Finished;
				// inner result exhaustion, backtrack to find next match
			}
		}

		return EvaluationResult.NoResults;
	}
}
