using Sandbox.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HTN.Planner;

/// <summary>
/// The Method Traversal PreconditionEvents (MTR) is a record of the methods traversed during the planning process.
/// See https://www.gameaipro.com/GameAIPro/GameAIPro_Chapter12_Exploring_HTN_Planners_through_Example.pdf page 161
/// We use the MTR to priority-filter during new task planning, cutting branches of the tree traversal that would yield lower
/// priority tasks than the current running task MTR.
/// </summary>
public class PlanMTR : IEquatable<PlanMTR>
{
	public List<uint> Branch { get; } = [];

	public PlanMTR()
	{
		Branch.EnsureCapacity( 32 );
	}

	public PlanMTR( params uint[] initialBranches )
	{
		if ( initialBranches is null || initialBranches.Length == 0 )
			throw new ArgumentException( "Initial branches cannot be null or empty.", nameof( initialBranches ) );
		Branch.EnsureCapacity( 32 );
		Branch.AddRange( initialBranches );
	}

	public void PushBranch() => Branch.Add( 0 );
	public void PopBranch()
	{
		Assert.True( Branch.Count > 0, "Cannot pop branch from empty MTR." );
		Branch.RemoveAt( Branch.Count - 1 );
	}

	public override string ToString() => $"MTR:{string.Join( ",", Branch )}";
	public override bool Equals( object obj ) => Equals( obj as PlanMTR );
	public bool Equals( PlanMTR other ) => other is not null && Branch.SequenceEqual( other.Branch );
	public override int GetHashCode() => HashCode.Combine( Branch );
	public static bool operator ==( PlanMTR left, PlanMTR right ) => EqualityComparer<PlanMTR>.Default.Equals( left, right );
	public static bool operator !=( PlanMTR left, PlanMTR right ) => !(left == right);
}
