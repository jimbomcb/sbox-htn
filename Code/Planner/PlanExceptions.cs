using HTN.Conditions;
using HTN.Tasks;
using System;
using System.Collections.Generic;

namespace HTN.Planner;

/// <summary>
/// Generic planning exception thrown at a specific phase of the process
/// </summary>
public class PlanningException( string message, ITask task = null, ScopeVariables variables = null, string phase = null, Exception innerException = null ) : 
	Exception( message, innerException )
{
	public ITask Task { get; } = task;
	public ScopeVariables Variables { get; } = variables != null ? TaskPool.CloneScopeVariables( variables ) : null;
	public string Phase { get; } = phase;
}

/// <summary>
/// Exception in the [Binding(name)] attribute variable binding process.
/// </summary>
public class TaskBindingException( string message, ITask task, ScopeVariables variables, Exception innerException = null ) : 
	PlanningException( message, task, variables, "Binding", innerException )
{
}


/// <summary>
/// Exception when a specific branch of a compound task fails to decompose due to precondition failure.
/// Bubbles up into CreatePlan and wrapped in a DecompositionException
/// Why does sbox error when this uses a primary constructor?
/// </summary>
public class InnerDecompositionException :
	PlanningException
{
	public int BranchIndex { get; }
	public ICondition Condition { get; }
	public string BranchName { get; }
	public Type TaskType { get; }

	public InnerDecompositionException( int branchIndex, string branchName, Type taskType, ICondition condition, ScopeVariables variables, Exception innerException = null ) : 
		base( null, null, variables, "Precondition", innerException )
	{
		BranchIndex = branchIndex;
		Condition = condition;
		BranchName = branchName;
		TaskType = taskType;
	}
}

/// <summary>
/// Exception when a compound task has failed to decompose, the inner exception is the culprit that caused the failure,
/// the message and DecompositionExceptions list contain the chain of branch stack leading up to the exception).
/// </summary>
public class DecompositionException( string message, ITask task, ScopeVariables variables, List<InnerDecompositionException> decompositionExceptions, Exception innerException = null ) : 
	PlanningException( message, task, variables, "Precondition", innerException )
{
	public List<InnerDecompositionException> DecompositionExceptions { get; } = decompositionExceptions;
}

