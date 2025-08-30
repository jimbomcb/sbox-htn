using HTN.Daemons;
using HTN.Tasks;
using Sandbox.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HTN.Planner;

/// <summary>
/// Owned by AI agents, the PlanExecutor contains a series of daemons and a root task.
/// The PlanExecutor Tick is called at a game-defined interval, during which it evaluates the daemons to build a world state, 
/// and then utilizes the <see cref="PlanBuilder"/> to decompose our root task into an actionable sequence of primitive tasks.
/// </summary>
public class PlanExecutor : IDisposable
{
	public PlanExecutor( PlannerContext context, bool debug = false )
	{
		_executionContext = context;
		_executionContext.Executor = this;

		_planBuilder = new( _executionContext, _debugState ); // todo: debugger state expose
	}

	/// <summary>
	/// List of daemons evaluated during planning to build the current world state.
	/// </summary>
	public List<IHTNDaemon> Daemons { get; } = [];

	/// <summary>
	/// The root task we will decompose into primitive tasks for execution.
	/// </summary>
	public ITask RootTask { get; set; }

	/// <summary>
	/// Index of the task currently being executed, -1 when we have no plan or between completion of one plan and creation of a new plan.
	/// </summary>
	public int RunningTaskIndex { get; private set; } = -1;

	/// <summary>
	/// Temporary world state fact memory, populated by tasks like PlanScopeSetWorldState
	/// </summary>
	public PlanTempMemory TempMemory { get; init; } = new();

	private readonly PlanBuilder _planBuilder;
	private readonly PlannerContext _executionContext;
	private PlanDebugState _debugState = null;
	private readonly WorldState _worldState = new();
	private Plan _runningPlan;
	private bool _activatedTask = false;
	private bool _disposed = false;

	~PlanExecutor()
	{
		Log.Warning( "PlanExectuor implements IDisposable but not was not disposed, please correctly handle disposing so tasks are pooled correctly." );
	}

	public void Dispose()
	{
		if ( _disposed ) return;
		_disposed = true;
		_runningPlan?.Dispose();
		GC.SuppressFinalize( this );
	}

	/// <summary>
	/// Run a planner tick:
	/// - Build a world state from attached daemons
	/// - Generate a plan based on the current world state and root task (or maintain the ongoing plan if no higher-prio tasks exist)
	/// - Execute each ongoing plan step
	/// </summary>
	public void Tick()
	{
		Assert.True( RootTask != null, "Root task must be set before executing the plan." );
		if ( !MaintainPlan() )
		{
			// No plan generated, abort any ongoing tasks
			ClearPlan();
			return;
		}

		if ( _runningPlan == null || _runningPlan.Steps.Count == 0 )
			return;

		Assert.True( RunningTaskIndex >= 0 && RunningTaskIndex < _runningPlan.Steps.Count );

		if ( !_activatedTask )
		{
			_runningPlan.Steps[RunningTaskIndex].Task.OnActivate( _executionContext, _runningPlan.Steps[RunningTaskIndex].Variables );
			_activatedTask = true;
		}

		try
		{
			switch ( _runningPlan.Steps[RunningTaskIndex].Task.Execute( _executionContext, _runningPlan.Steps[RunningTaskIndex].Variables ) )
			{
				case TaskResult.Running:
					break;

				case TaskResult.Failure:
					Log.Info( $"Task {RunningTaskIndex}/{_runningPlan.Steps.Count} ({_runningPlan.Steps[RunningTaskIndex].GetType().Name}) execution failed, replanning." );
					ClearPlan();
					break;

				case TaskResult.Success:
					if ( _activatedTask )
					{
						_runningPlan.Steps[RunningTaskIndex].Task.OnDeactivate( _executionContext, _runningPlan.Steps[RunningTaskIndex].Variables );
						_activatedTask = false;
					}

					if ( ++RunningTaskIndex >= _runningPlan.Steps.Count )
						ClearPlan(); // Exhausted plan

					break;

				default:
					throw new InvalidOperationException( "Unexpected task state during execution." );
			}
		}
		catch ( Exception ex )
		{
			Log.Error( ex, $"Plan execution failed while performing task {_runningPlan.Steps[RunningTaskIndex]}, aborting plan." );
			ClearPlan();
		}
	}

	private void SetPlan( Plan plan )
	{
		ClearPlan();
		_runningPlan = plan;
		RunningTaskIndex = 0;

		if ( _runningPlan == null ) 
			return;

		int plannedIdx = -1;
		for ( int i = 0; i < _runningPlan.Steps.Count; i++ )
		{
			var step = _runningPlan.Steps[i];
			if ( !step.Task.OnPlanned( _executionContext, step.Variables ) )
			{
				Log.Error( $"Rejecting attempted plan, OnPlanned signaled failure for task {step.Task}" );
				
				// push out finishing for anything that just got planned prior to the failing OnPlanned
				for( int cancelIdx = 0; cancelIdx <= plannedIdx; cancelIdx++ )
					_runningPlan.Steps[cancelIdx].Task.OnPlanFinished( _executionContext, _runningPlan.Steps[cancelIdx].Variables );

				_runningPlan.Dispose();
				_runningPlan = null;
				RunningTaskIndex = -1;
				return;
			}

			plannedIdx = i;
		}

		//Log.Info($"Running plan with steps: {string.Join( ", ", _runningPlan.Steps.Select( s => s.Task.GetType().Name ) )}" );
	}

	private void ClearPlan()
	{
		if ( _runningPlan == null ) 
			return;

		if ( _activatedTask )
		{
			_runningPlan.Steps[RunningTaskIndex].Task.OnDeactivate( _executionContext, _runningPlan.Steps[RunningTaskIndex].Variables );
			_activatedTask = false;
		}

		foreach ( var step in _runningPlan.Steps )
			step.Task.OnPlanFinished( _executionContext, step.Variables );

		_runningPlan.Dispose();
		_runningPlan = null;
		RunningTaskIndex = -1;
	}

	private bool MaintainPlan()
	{
		_worldState.Clear();

		foreach ( var daemon in Daemons )
			daemon.ApplyWorldState( _worldState );

		foreach ( var entry in TempMemory.Entries )
			_worldState.Add( entry.Key, entry.Value );

		switch ( _planBuilder.CreatePlan( RootTask, _worldState, out var newPlan, previousMTR: _runningPlan?.MTR ) )
		{
			case PlanResult.Success:
				if ( newPlan.Steps.FirstOrDefault().Task is ContinuationTask )
				{
					Assert.AreEqual( 1, newPlan.Steps.Count, "Continuation tasks must be the sole task in the produced plan" );
					newPlan.Dispose();
					return true;
				}

				SetPlan( newPlan );
				return true;

			case PlanResult.Failed:
				return false;

			case PlanResult.Continue:
				return true;
		}

		return true;
	}
}
