using HTN.Conditions;
using HTN.Tasks;
using Sandbox.Diagnostics;
using System;
using System.Collections.Generic;

namespace HTN.Planner;

/// <summary>
/// The PlanBuilder is responsible for taking a root task and decomposing it into a flat list of primitive tasks that can be executed.
/// It takes a game-specific planner context that will be supplied to both planning and planned task execution.
/// CreatePlan is called at the desired agent thinking interval, returning either:
/// - <see cref="PlanResult.Success"/> with a valid Plan.
/// - <see cref="PlanResult.Continue"/> signaling that there is no better plan, and we should continue with whatever we are currently running.
/// - <see cref="PlanResult.Failed"/> indicating that no valid plan could be found.
/// If being used manually and not within a PlanExecutor, the responsible code must make sure to dispose of the generated plan.
/// </summary>
public class PlanBuilder( PlannerContext plannerCtx, PlanDebugState planDebugger = null )
{
	private enum DecomposeResult
	{
		Success,
		Exhausted,
		LowerPrio
	}

	private readonly List<PlanStep> _generatedPlanSteps = [];
	private ScopeVariables _rootScopeVars;
	private bool _planning = false;
	private ITask _rootTask = null;

	/// <summary>
	/// Given a root task, the world state, optional root scope variables, and an optional previous MTR from a previous plan,
	/// attempts to create a new plan by decomposing the root task into a series primitive tasks.
	/// </summary>
	/// <param name="rootTask">Root task to decompose, usually a <see cref="CompoundTaskBase"/></param>
	/// <param name="worldState">World state at time of planning</param>
	/// <param name="plan">If we return <see cref="PlanResult.Success"/>, the generated <see cref="Plan"/>.</param>
	/// <param name="vars">List of default variables to provide to the first query evaluation - TODO: Remove?</param>
	/// <param name="previousMTR">The <see cref="PlanMTR"/> of the last generated plan, used to then return <see cref="PlanResult.Continue"/> if no higher priority branch is found.</param>
	/// <returns>Success, Continue or Failed</returns>
	/// <exception cref="DecompositionException"></exception>
	/// <exception cref="PlanningException"></exception>
	public PlanResult CreatePlan( ITask rootTask, WorldState worldState, out Plan plan, ScopeVariables vars = null, PlanMTR previousMTR = null )
	{
		ArgumentNullException.ThrowIfNull( rootTask );
		ArgumentNullException.ThrowIfNull( worldState );
		Assert.False( _planning, "Concurrent calls to CreatePlan? not supported..." );

		_planning = true;
		planDebugger?.Reset();
		plan = null;
		var currentMTR = new PlanMTR();
		_rootTask = rootTask;

		TaskPool.BeginSession();
		try
		{
			_rootScopeVars = vars ?? TaskPool.NewScopeVariables();
			_generatedPlanSteps.Clear();

			switch ( rootTask )
			{
				case IPrimitiveTask primitiveTask:
					_generatedPlanSteps.Add( new(primitiveTask, _rootScopeVars ) );
					break;
				case ICompoundTask compoundTask:
					switch ( DecomposeCompound( compoundTask, worldState, currentMTR, previousMTR ) )
					{
						case DecomposeResult.LowerPrio: return PlanResult.Continue;
						case DecomposeResult.Exhausted: return PlanResult.Failed;
					}
					break;
				default:
					throw new PlanningException( $"Root task must be either a primitive or compound task, got: {rootTask?.GetType().Name ?? "null"}", rootTask );
			}

			if ( _generatedPlanSteps.Count == 0 )
				return PlanResult.Failed; // Taskless plans are not valid...

			if ( planDebugger != null )
			{
				if ( planDebugger.PreconditionEvents.Scopes.Count != 0 )
				{
					var remainingScopes = string.Join( " > ", planDebugger.PreconditionEvents.Scopes );
					throw new PlanningException(
						$"Planner finished with an unbalanced scope stack. " +
						$"Remaining scopes: [{remainingScopes}]. " +
						$"There's a mismatch in scope pushing/popping.",
						rootTask, _rootScopeVars, "ScopeValidation" );
				}

				planDebugger.WorldState = new( worldState );
			}

			TaskPool.ReserveThenRelease( _generatedPlanSteps ); // commit the tasks that are being sent out in the plan, rest are released in TaskPool.EndSession below
			plan = new( [.. _generatedPlanSteps], currentMTR );
			return PlanResult.Success;
		}
		catch ( InnerDecompositionException innerException )
		{
			// walk the chain of decomposition errors and compile into single decomosition error describing chain of branches
			Exception rootException = innerException;
			var branchChain = new List<string>(8) { "Root" };
			var exceptionChain = new List<InnerDecompositionException>(8);
			for (var branchException = innerException; branchException != null; branchException = branchException.InnerException as InnerDecompositionException)
			{
				rootException = branchException.InnerException ?? rootException;
				branchChain.Add(branchException.BranchName ?? "Unnamed");
				exceptionChain.Add(branchException);
			}
			var lowestScopeVars = exceptionChain[^1].Variables;
			throw new DecompositionException( $"Decomposition failed in {string.Join( " -> ", branchChain )}: {rootException?.Message}", rootTask, lowestScopeVars, exceptionChain, rootException );
		}
		catch ( PlanningException )
		{
			throw; // bubbled up directly
		}
		catch ( Exception ex )
		{
			throw new PlanningException( $"Unexpected error during plan creation for task '{rootTask?.GetType().Name}'", rootTask, _rootScopeVars, "Planning", ex ); // panic panic panic
		}
		finally
		{
			TaskPool.EndSession(); // Release any uncommited tasks back to the pool
			_planning = false;
		}
	}

	private DecomposeResult DecomposeCompound( ICompoundTask compoundTask, WorldState worldState, PlanMTR mtr, PlanMTR previousMTR )
	{
		var variables = compoundTask == _rootTask ? _rootScopeVars : TaskPool.NewScopeVariables();
		TaskPool.PerformTaskBinding( compoundTask, variables, variables );

		var initTaskCnt = _generatedPlanSteps.Count;
		uint attemptingBranch = 0;
		mtr.PushBranch();

		var branches = compoundTask.Branches;
		for ( int i = 0; i < branches.Length; i++ )
		{
			if ( previousMTR != null && mtr == previousMTR )
				return DecomposeResult.LowerPrio;

			var branch = branches[i];
			try
			{
				var precondition = branch.Precondition ?? ICondition.AlwaysTrue;
				var result = DecomposeResult.Exhausted;

				var conditionResult = precondition.Evaluate( worldState, variables, plannerCtx, planDebugger, binding =>
				{
					var tasks = branch.TaskFactory( binding );
					if ( tasks == null || tasks.Length == 0 )
						return EvaluationResult.NoResults; // nothing to perform, attempt next branch

#if DEBUG
					Debug.TaskInstantiationTracker.ValidatePoolUsage( tasks, $"Branch '{branch.Name}' in compound task '{compoundTask.GetType().Name}'" );
#endif

					var decomposeResult = DecomposeTaskList( tasks, worldState, binding, mtr, previousMTR );
					if ( decomposeResult == DecomposeResult.Exhausted )
					{
						if ( _generatedPlanSteps.Count > initTaskCnt )
							_generatedPlanSteps.RemoveRange( initTaskCnt, _generatedPlanSteps.Count - initTaskCnt );

						return EvaluationResult.NoResults; // exhausted branch, backtrack pull out any committed tasks and continue with any other matching bindings
					}
					else
					{
						result = decomposeResult;
						return EvaluationResult.Finished; // success or exhausted higher-than-existing prio branches, stop eval
					}
				} );

				if ( conditionResult == EvaluationResult.Finished ) // bubble up the underlying result once matching
					return result;
			}
			catch ( Exception ex )
			{
				throw new InnerDecompositionException( i, branch.Name, compoundTask.GetType(), branch.Precondition ?? ICondition.AlwaysTrue, variables, ex );
			}

			mtr.Branch[^1] = ++attemptingBranch;
		}

		mtr.PopBranch();
		return DecomposeResult.Exhausted;
	}

	private DecomposeResult DecomposeTaskList( IEnumerable<ITask> tasks, WorldState worldState, ScopeVariables boundVariables, PlanMTR mtr, PlanMTR previousMTR )
	{
		static DecomposeResult ProcessPrimitiveTask( IPrimitiveTask primitiveTask, ScopeVariables boundVariables, List<PlanStep> tasksToExecute )
		{
			var primitiveTaskVars = TaskPool.CloneScopeVariables( boundVariables );
			TaskPool.PerformTaskBinding( primitiveTask, boundVariables, primitiveTaskVars );
			tasksToExecute.Add( new(primitiveTask, primitiveTaskVars ) );
			return DecomposeResult.Success;
		}

		foreach ( var task in tasks )
		{
			try
			{
				var result = task switch
				{
					IPrimitiveTask primitiveTask => ProcessPrimitiveTask( primitiveTask, boundVariables, _generatedPlanSteps ),
					ICompoundTask subCompoundTask => DecomposeCompound( subCompoundTask, worldState, mtr, previousMTR ),
					_ => throw new PlanningException( $"Unknown task type: {task.GetType().Name}", task, boundVariables, "TaskListDecomposition" )
				};

				if ( result != DecomposeResult.Success ) // lower prio or exhausted a compound without valid results
					return result;
			}
			catch ( PlanningException )
			{
				throw;
			}
			catch ( Exception ex )
			{
				throw new PlanningException( $"Unexpected error decomposing task '{task?.GetType()?.Name}' in task list", task, boundVariables, "TaskListDecomposition", ex );
			}
		}

		return DecomposeResult.Success;
	}

}
