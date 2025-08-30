using HTN.Planner;
using System;

namespace HTN.Conditions;

/// <summary>
/// Compare two variables, or a variable and a value for equality, inequality, or ordering.
/// 
/// Example:
/// new Branch( "WorkNode", [
///		new Compare("?node_state", Compare.Operator.Equal, ProductionNodeState.Working),
///		new Compare("?health", Compare.Operator.GreaterThan, 50),
///	], (vars) => ... )
/// </summary>
public class Compare : ICondition
{
	public enum Operator
	{
		Equal,
		NotEqual,
		LessThan,
		LessThanOrEqual,
		GreaterThan,
		GreaterThanOrEqual
	}
	private readonly string _varNameA;
	private readonly string _varNameB;
	private readonly object _valueB;
	private readonly Operator _op;
	private readonly bool _compareWithValue;

	public Compare( string varNameA, string varNameB, Operator op )
	{
		_varNameA = varNameA;
		_varNameB = varNameB;
		_op = op;
		_compareWithValue = false;
	}

	public Compare( string varName, Operator op, object value )
	{
		_varNameA = varName;
		_valueB = value;
		_op = op;
		_compareWithValue = true;
	}

	public EvaluationResult Evaluate( WorldState state, ScopeVariables vars, PlannerContext ctx, PlanDebugState debugState, Func<ScopeVariables, EvaluationResult> onResult )
	{
		if ( !vars.Has( _varNameA ) )
			return EvaluationResult.NoResults;

		var a = vars.Bindings[_varNameA];
		var b = _compareWithValue ? _valueB : (_varNameB != null && vars.Has( _varNameB ) ? vars.Bindings[_varNameB] : throw new InvalidOperationException( $"Variable '{_varNameB}' not found in bindings." ));
		
		var result = _op switch
		{
			Operator.Equal => Equals( a, b ),
			Operator.NotEqual => !Equals( a, b ),
			Operator.LessThan => CompareValues( a, b ) < 0,
			Operator.LessThanOrEqual => CompareValues( a, b ) <= 0,
			Operator.GreaterThan => CompareValues( a, b ) > 0,
			Operator.GreaterThanOrEqual => CompareValues( a, b ) >= 0,
			_ => throw new ArgumentOutOfRangeException()
		};

		if ( result )
			return onResult( vars );

		return EvaluationResult.NoResults;
	}

	private static int CompareValues( object a, object b )
	{
		if ( a == null && b == null ) return 0;
		if ( a == null ) return -1;
		if ( b == null ) return 1;

		if ( a is IComparable comparableA && a.GetType() == b.GetType() )
			return comparableA.CompareTo( b );

		if ( IsNumeric( a ) && IsNumeric( b ) )
		{
			var doubleA = Convert.ToDouble( a );
			var doubleB = Convert.ToDouble( b );
			return doubleA.CompareTo( doubleB );
		}

		return string.Compare( a.ToString(), b.ToString(), StringComparison.Ordinal );
	}

	private static bool IsNumeric( object value )
	{
		return value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal;
	}
}
