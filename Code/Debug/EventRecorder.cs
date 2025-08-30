using System;
using System.Collections.Generic;
using System.Linq;

namespace HTN.Debug;

/// <summary>
/// TODO: Rework
/// </summary>
public class EventRecorder
{
	public List<string> Events { get; } = [];
	public List<string> Scopes { get; } = [];
	public List<string> VisitedScopes { get; } = [];

	public void Event(string eventName)
	{
		if (string.IsNullOrWhiteSpace(eventName))
			throw new ArgumentException("Event name cannot be null or empty.", nameof(eventName));

		var scopePrefix = string.Join( '/', Scopes );
		Events.Add($"{scopePrefix} - {eventName}");
	}

	public void PushScope(string scopeName)
	{
		if (string.IsNullOrWhiteSpace(scopeName))
			throw new ArgumentException("Scope name cannot be null or empty.", nameof(scopeName));

		Event( $"(ENTER: {scopeName})" );
		Scopes.Add(scopeName);
		VisitedScopes.Add( string.Join( '/', Scopes ) );
	}

	public void PopScope()
	{
		if (Scopes.Count > 0)
		{
			var removingScope = Scopes.Last();
			Scopes.RemoveAt(Scopes.Count - 1);
			Event( $"(EXIT: {removingScope})" );
		}
	}

	public IDisposable Scope(string scopeName)
	{
		PushScope(scopeName);
		return new RecorderScope(this);
	}

	private class RecorderScope( EventRecorder recorder ) : IDisposable
	{
		private readonly EventRecorder _recorder = recorder;
		private bool _disposed = false;

		public void Dispose()
		{
			if (!_disposed)
			{
				_recorder.PopScope();
				_disposed = true;
			}
		}
	}
}
