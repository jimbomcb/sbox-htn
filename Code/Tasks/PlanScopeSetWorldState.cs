using HTN.Planner;
using Sandbox.Diagnostics;
using System.Runtime.CompilerServices;

namespace HTN.Tasks;

/// <summary>
/// Set a world state tuple fact that exists in the temporary memory of the executor for the lifetime of the plan in which it was set.
/// (including before, during, and after this task is executed, ie even if this is the final task in a plan, the variable is scoped to the full plan and available in tasks before this).
/// </summary>
public class PlanScopeSetWorldState : PrimitiveTaskBase
{
	private int _memoryIndex = -1;
	private string _destinationKey;
	private ITuple _tuple;

	public ITask Configure( string destinationKey, ITuple tuple )
	{
		_destinationKey = destinationKey;
		_tuple = tuple;
		return this;
	}

	public override TaskResult Execute( PlannerContext ctx, ScopeVariables vars ) => TaskResult.Success;

	public override bool OnPlanned( PlannerContext ctx, ScopeVariables variables )
	{
		Assert.AreEqual( -1, _memoryIndex, "Invalid scope remember state" );
		_memoryIndex = ctx.Executor.TempMemory.Add( _destinationKey, _tuple );
		return true;
	}

	public override void OnPlanFinished( PlannerContext ctx, ScopeVariables variables )
	{
		if ( _memoryIndex >= 0 )
		{
			Assert.True( ctx.Executor.TempMemory.Remove( _memoryIndex ), "Invalud temp memory state" );
			_memoryIndex = -1;
		}
	}
}
