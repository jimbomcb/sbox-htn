using HTN.Tasks;
using Sandbox.Diagnostics;
using System;
using System.Collections.Generic;

namespace HTN.Planner;

/// <summary>
/// A produced plan containing a sequence of primitive tasks to execute, along with their associated plan-time variables.
/// Must be disposed when no longer utilized to return tasks to the task pool, usually performed by the <see cref="PlanExecutor"/> on replan.
/// </summary>
public class Plan( List<PlanStep> steps, PlanMTR mtr ) : IDisposable
{
	public List<PlanStep> Steps { get; } = steps;
	/// <summary>
	/// The Method Traversal Record (MTR) of the plan, this is provided to the next <see cref="PlanBuilder.CreatePlan"/> to allow it to skip branches of the planning tree that would yield lower priority tasks.
	/// </summary>
	public PlanMTR MTR { get; } = mtr;

	private bool _disposed = false;

	~Plan()
	{
#if DEBUG
		Log.Error( $"Plan implements IDisposable not disposed, leaking task objects. Please ensure that plan objects are correctly disposed!" );
#endif
	}

	public void Dispose()
	{
		if ( _disposed )
			return;

		TaskPool.ReturnPlanSteps( Steps );

		_disposed = true;
		GC.SuppressFinalize( this );
	}

	public PlanStep<T> GetStep<T>( int index ) where T : IPrimitiveTask
	{
		Assert.True( index >= 0 && index < Steps.Count, $"Index {index} is out of bounds for steps count {Steps.Count}" );

		var step = Steps[index];
		Assert.True( step.Task is T, $"Run is not of type {typeof( T ).Name}, it is {step.Task.GetType().Name}" );
		return new PlanStep<T>( (T)step.Task, step.Variables );
	}
}
