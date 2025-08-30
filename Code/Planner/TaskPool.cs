using HTN.Tasks;
using Sandbox;
using Sandbox.Diagnostics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace HTN.Planner;

/// <summary>
/// TODO: Rework, temporary implementation of a task pool to reduce garbage collection pressure from task instantiation.
/// </summary>
public class TaskPool
{
	public static TaskPool Instance { get; set; } = new();

	private readonly Dictionary<Type, Stack<ITask>> _pools = [];
	private readonly object _lockObject = new();

	[ThreadStatic]
	private static List<ITask> _sessionTasks;
	[ThreadStatic]
	private static List<ScopeVariables> _sessionScopeVariables;
	[ThreadStatic]
	private static bool _sessionActive;
	[ThreadStatic]
	private static HashSet<ITask> _reservedTasks;
	[ThreadStatic]
	private static HashSet<ScopeVariables> _reservedVariables;
	[ThreadStatic]
	private static HashSet<ITask> _toReleaseTasks;
	[ThreadStatic]
	private static HashSet<ScopeVariables> _toReleaseVariables;

	internal static void BeginSession()
	{
		Assert.False( _sessionActive, "Active session to end on this thread" );

		_reservedTasks ??= [];
		_reservedVariables ??= [];
		_toReleaseTasks ??= [];
		_toReleaseVariables ??= [];

		_sessionTasks ??= [];
		_sessionTasks.Clear();
		_sessionScopeVariables ??= [];
		_sessionScopeVariables.Clear();

		_sessionActive = true;
	}

	internal static void EndSession()
	{
		Assert.True( _sessionActive, "No active session to end on this thread" );
		_sessionActive = false;
		
		if ( _sessionTasks?.Count > 0 )
		{
			Instance.Return( _sessionTasks );
			_sessionTasks.Clear();
		}

		if ( _sessionScopeVariables?.Count > 0 )
		{
			ReturnScopeVariables( _sessionScopeVariables );
			_sessionScopeVariables.Clear();
		}
	}

	internal static void ReserveThenRelease( List<PlanStep> toReserve )
	{
		if ( !_sessionActive )
			return;

		Assert.NotNull( _sessionTasks );

		foreach(var (task, vars) in toReserve )
		{
			_reservedTasks.Add( task );
			_reservedVariables.Add( vars );
		}

		// Find any session tasks and session scope variables that are not in the to reserve list, these get returned to the pool

		_toReleaseTasks.Clear();
		foreach( var task in _sessionTasks )
		{
			if ( !_reservedTasks.Contains( task ) )
			{
				_toReleaseTasks.Add( task );
			}
		}
		_sessionTasks.Clear();

		_toReleaseVariables.Clear();
		foreach ( var vars in _sessionScopeVariables )
		{
			if ( !_reservedVariables.Contains( vars ) )
			{
				_toReleaseVariables.Add( vars );
			}
		}
		_sessionScopeVariables.Clear();

		
		Instance.Return( _toReleaseTasks );
		ReturnScopeVariables( _toReleaseVariables );


		_reservedTasks.Clear();
		_reservedVariables.Clear();
	}

	public T Get<T>() where T : ITask, new()
	{
		lock ( _lockObject )
		{
			var stack = _pools.GetOrCreate( typeof( T ) );

			if ( !stack.TryPop( out ITask task ) )
			{
				task = new T();
				//Log.Info( $"TaskPool: Created new {typeof( T ).Name}, instance: {task.GetHashCode()}" );
			}
			//else
			//{
			//	Log.Info( $"TaskPool: Dequeued {typeof( T ).Name} from pool, instance: {task.GetHashCode()}" );
			//}

			if ( _sessionActive )
				_sessionTasks?.Add( task );

#if DEBUG
			Debug.TaskInstantiationTracker.RegisterPoolCreation(task);
#endif
			
			return (T)task;
		}
	}

	private void Return<T>( T task ) where T : ITask
	{
		if ( task == null ) return;

		lock ( _lockObject )
		{
			var stack = _pools.GetOrCreate( task.GetType() );
			
			//Log.Info($"TaskPool: Returning {typeof( T ).Name} to pool, instance: {task.GetHashCode()} - SumThis:{stack.Count} SumTotal:{_pools.Sum( x => x.Value.Count )}" );
			stack.Push( task );

			//if ( stack.Count >= 10000 )
			//{
			//	Log.Warning($"Excessive amount of {type.Name} tasks returned to pool ({stack.Count}), please make sure you are using Run<TaskType>() to instantiate from pool and not constructing at execution time");
			//}
		}
	}

	private void Return<T>( IEnumerable<T> tasks ) where T : class, ITask
	{
		foreach ( var task in tasks )
		{
			Return( task );
		}
	}

	// Scope variable pooling:
	private Stack<ScopeVariables> _scopeVariablePool = [];
	private ScopeVariables NewScopeVariablesInternal()
	{
		lock ( _lockObject )
		{
			if ( !_scopeVariablePool.TryPop( out ScopeVariables vars ) )
			{
				vars = new ScopeVariables();
			}

			if ( _sessionActive )
				_sessionScopeVariables?.Add( vars );

			return vars.Reset();
		}
	}

	private void ReturnScopeVariablesInternal( IEnumerable<ScopeVariables> vars )
	{
#if DEBUG
		Assert.NotNull( vars, "Cannot return null ScopeVariables to pool" );
		//Assert.False( _scopeVariablePool.Contains( vars ), "Duplicate release of ScopeVariables to pool detected" );
#endif
		lock ( _lockObject )
		{
			foreach ( var entry in vars )
			{
				_scopeVariablePool.Push( entry );
			}
		}
	}

	public static void ReturnScopeVariables( IEnumerable<ScopeVariables> vars )
	{
		Instance.ReturnScopeVariablesInternal( vars );
	}

	private void ReturnPlanStepsInternal(IEnumerable<PlanStep> steps)
	{
		_toReleaseTasks.Clear();
		_toReleaseVariables.Clear();

		foreach ( var step in steps )
		{
			_toReleaseTasks.Add( step.Task );
			_toReleaseVariables.Add( step.Variables );
		}


		Return( _toReleaseTasks );
		ReturnScopeVariablesInternal( _toReleaseVariables );
	}

	public static void ReturnPlanSteps( IEnumerable<PlanStep> steps )
	{
		Instance.ReturnPlanStepsInternal( steps );
	}

	public static ScopeVariables NewScopeVariables()
	{
		return Instance.NewScopeVariablesInternal();
	}

	public static ScopeVariables CloneScopeVariables( ScopeVariables copyFrom )
	{
		return NewScopeVariables().CopyFrom( copyFrom );
	}

	private void ResetInternal()
	{
		lock ( _lockObject )
		{
			Log.Info( "TaskPool: Reset - clearing all pools" );
			_pools.Clear();
		}

		_scopeVariablePool.Clear();
		_taskBindingCache.Clear();
	}

	public static void Reset()
	{
		Instance.ResetInternal();
	}

	private readonly ConcurrentDictionary<Type, (PropertyDescription prop, BindingAttribute attr)[]> _taskBindingCache = [];
	private void PerformTaskBindingInternal( ITask task, ScopeVariables srcVars, ScopeVariables destVars )
	{
		try
		{
			var taskType = task.GetType();
			var attributes = _taskBindingCache.GetOrAdd( taskType, type =>
			{
				// NOTE: because of task library fuckery, we can't trust that this gives the same results
				// in unit tests versus actual execution.
				// This is because the TypeLibrary, unlike reflection, will act like classes defined
				// within the unit test (ie every test class) doesn't exist.
				// I opened an issue at https://github.com/Facepunch/sbox-issues/issues/8811 but 
				// it sits with a sole thumbs-up.

				var gatheredItems = new List<(PropertyDescription info, BindingAttribute attr)>();
				var taskType = TypeLibrary.GetType( type );
				if ( taskType == null ) return [];

				foreach ( var property in TypeLibrary.GetType( type ).Properties )
				{
					var bindAttr = property.GetCustomAttribute<BindingAttribute>();
					if ( bindAttr != null && !string.IsNullOrEmpty( bindAttr.Name ) )
					{
						gatheredItems.Add( (property, bindAttr) );
					}
				}
				return gatheredItems.ToArray();
			} );

			foreach ( var (prop, attr) in attributes )
			{
				destVars.Set( attr.Name, prop.GetValue( task ) );
			}
		}
		catch ( Exception ex )
		{
			throw new TaskBindingException( $"Failed to perform binding for task '{task?.GetType().Name}'", task, srcVars, ex );
		}
	}

	public static void PerformTaskBinding( ITask task, ScopeVariables srcVars, ScopeVariables destVars )
	{
		Instance.PerformTaskBindingInternal( task, srcVars, destVars );
	}

}
