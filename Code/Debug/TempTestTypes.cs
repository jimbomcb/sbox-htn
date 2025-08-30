using HTN.Conditions;
using HTN.Planner;
using HTN.Tasks;

namespace HTN.Debug;

// Due to the internal workings of the TypeLibrary, we are currently unable to access
// unit test defined types from within unit tests. This obviously makes testing 
// certain functionality difficult.
//
// https://github.com/Facepunch/sbox-issues/issues/8811
//
// For now they're living in here.

public class StringExpandingTask : PrimitiveTaskBase
{
	public string String { get; private set; }

	public StringExpandingTask()
	{
	}

	public ITask Configure( string inString )
	{
		String = inString;
		return this;
	}

	public override TaskResult Execute( PlannerContext ctx, ScopeVariables variables ) => TaskResult.Success;
}

// Run type for tests that need fixed text values with binding support
public class StringTask : PrimitiveTaskBase
{
	public string Text { get; private set; }
	private static int _instanceCounter = 0;
	private readonly int _instanceId;

	public StringTask()
	{
		_instanceId = System.Threading.Interlocked.Increment(ref _instanceCounter);
		//Log.Info($"StringTask#{_instanceId} constructor called");
	}

	public ITask Configure( string text )
	{
		//Log.Info($"StringTask#{_instanceId} configured with: {text} (was: {Text})");
		Text = text;
		return this;
	}

	public override TaskResult Execute( PlannerContext ctx, ScopeVariables variables ) => TaskResult.Success;
	
	public override void OnPlanFinished( PlannerContext ctx, ScopeVariables variables )
	{
		//Log.Info($"StringTask#{_instanceId} plan finished, resetting from: {Text}");
	}
}

public class CompoundOuterAutoBindParams : CompoundTaskBase
{
	public CompoundOuterAutoBindParams()
	{
		Branches = [
			new Branch("Branch1", [new Query("enemy", "?enemy", "?var")], (vars) => [
				Run<PrimTaskA>(), 
				Run<CompoundInnerAndParamBinding>().Configure(vars.Get<string>("?enemy")),
				Run<PrimTaskA>()
			]),
			new Branch("Branch2", [], (_) => [Run<PrimTaskC>(), Run<PrimTaskB>()])
		];
	}
}

public class CompoundInnerAndParamBinding : CompoundTaskBase
{
	[Binding( "?enemy" )]
	public string EnemyVar { get; private set; }

	public CompoundInnerAndParamBinding()
	{
		Branches = [
			new Branch("Branch1", 
			[new Query("weapon", "?weapon"), new Query("can_use_weapon", "?weapon", "?enemy")], 
			(vars) => [ Run<StringTask>().Configure( $"{vars.Get<string>( "?enemy" )} attacking with weapon {vars.Get<string>( "?weapon" )}" )]
			)
		];
	}

	public ITask Configure( string enemyVar )
	{
		EnemyVar = enemyVar;
		return this;
	}
}

public class CompoundOuter : CompoundTaskBase
{
	public CompoundOuter()
	{
		Branches = [
			new Branch("Branch1", 
				[new Query("enemy", "?enemy", "?var")],
				(vars) => [Run<PrimTaskA>(), Run<CompoundInner>().Configure(vars.Get<string>("?enemy")), Run<PrimTaskA>()]
			),
			new Branch("Branch2", [], (_) => [Run<PrimTaskC>(), Run<PrimTaskB>()])
		];
	}
}

public class CompoundInner : CompoundTaskBase
{
	[Binding( "?enemy" )]
	public string EnemyVar { get; private set; }

	public CompoundInner()
	{
		Branches = [
			new Branch("Branch1", 
				[new Query("weapon", "?weapon"), new Query("can_use_weapon", "?weapon", "?enemy")],
				(vars) => [ Run<StringTask>().Configure( $"{vars.Get<string>( "?enemy" )} attacking with weapon {vars.Get<string>( "?weapon" )}" )]
			)
		];
	}
	public ITask Configure( string enemyVar )
	{
		EnemyVar = enemyVar;
		return this;
	}
}

public class CompoundOuterAnd : CompoundTaskBase
{
	public CompoundOuterAnd()
	{
		Branches = [
			new Branch("Branch1", [new Query("enemy", "?enemy", "?var")], (vars) => 
			[Run<PrimTaskA>(), Run<CompoundInnerAnd>().Configure(vars.Get<string>("?enemy")), Run<PrimTaskA>()]),
			new Branch("Branch2", [], (vars) => [Run<PrimTaskC>(), Run<PrimTaskB>()])
		];
	}
}

public class CompoundInnerAnd : CompoundTaskBase
{
	[Binding( "?enemy" )]
	public string EnemyVar { get; private set; }

	public CompoundInnerAnd()
	{
		Branches = [
			new Branch("Branch1", [new And(new Query("weapon", "?weapon"), new Query("can_use_weapon", "?weapon", "?enemy"))],
			(vars) => [Run < StringTask >().Configure($"{vars.Get < string >("?enemy")} attacking with weapon {vars.Get < string >("?weapon")}")])
		];
	}
	public ITask Configure( string enemyVar )
	{
		EnemyVar = enemyVar;
		return this;
	}
}

public class CompoundOuterAutoBindRenameParams : CompoundTaskBase
{
	public CompoundOuterAutoBindRenameParams()
	{
		Branches = [
			new Branch("Branch1",
				[new Query("enemy", "?enemy", "?var")],
				(vars) => [Run<PrimTaskA>(), Run<CompoundInnerAndParamRenameBinding>().Configure( vars.Get<string>("?enemy") ), Run<PrimTaskA>()]),

			new Branch("Branch2", [], (_) => [Run<PrimTaskC>(), Run<PrimTaskB>()])
		];
	}
}

public class CompoundInnerAndParamRenameBinding : CompoundTaskBase
{
	[Binding( "?enemy_inner" )]
	public string EnemyVar { get; private set; }

	public CompoundInnerAndParamRenameBinding()
	{
		Branches = [
			new Branch("Branch1", [new Query("weapon", "?weapon"), new Query("can_use_weapon", "?weapon", "?enemy_inner" )], 
			(vars) => [Run<StringTask>().Configure( $"{vars.Get<string>("?enemy_inner")} attacking with weapon {vars.Get<string>("?weapon")}" )])
		];
	}
	public ITask Configure( string enemyVar )
	{
		EnemyVar = enemyVar;
		return this;
	}
}

public class CompoundComplex : CompoundTaskBase
{
	public CompoundComplex()
	{
		Branches = [
			new Branch("Branch1",
				[new Query("comp", "?name", "?prim")],
				(vars) => [Run<CompoundPrimitive>().Configure(vars.Get<string>("?name"), vars.Get<TestPrimitive>( "?prim" ))]
			)
		];
	}
}

public record struct TestPrimitive( string Test, bool Bool, Vector3 TestPos );
public class PrimTaskA : PrimitiveTaskBase
{
}
public class PrimTaskB : PrimitiveTaskBase
{
}
public class PrimTaskC : PrimitiveTaskBase
{
};

public class CompoundPrimitive : PrimitiveTaskBase
{
	public string Name;
	public TestPrimitive Input;

	public ITask Configure( string name, TestPrimitive input )
	{
		Name = name;
		Input = input;
		return this;
	}
}

// Variable binding isolation test classes
// Test task that has a child attempting to access parent's unbound variable
public class ParentWithLeakyChild : CompoundTaskBase
{
	public ParentWithLeakyChild()
	{
		Branches = [
			new Branch("Branch1", 
				[new Query("enemy", "?enemy", "?location")], 
				(vars) => [
					Run<PrimTaskA>(),
					Run<ChildTryingToAccessLocation>().Configure(vars.Get<string>("?enemy")),
					Run<PrimTaskA>()
				])
		];
	}
}

public class ChildTryingToAccessLocation : CompoundTaskBase
{
	[Binding("?enemy")]
	public string EnemyVar { get; private set; }

	public ChildTryingToAccessLocation()
	{
		Branches = [
			new Branch("Branch1",
				[new Query("weapon", "?weapon"), new Query("can_use_weapon", "?weapon", "?enemy")],
				(vars) => [
					// This should FAIL - trying to access ?location from parent scope
					Run<StringTask>().Configure($"{vars.Get<string>("?enemy")} at {vars.Get<string>("?location")} attacking with {vars.Get<string>("?weapon")}")
				])
		];
	}

	public ITask Configure(string enemyVar)
	{
		EnemyVar = enemyVar;
		return this;
	}
}

// Test task where child tries to access multiple unbound parent variables
public class ParentWithChildTryingInvalidAccess : CompoundTaskBase
{
	public ParentWithChildTryingInvalidAccess()
	{
		Branches = [
			new Branch("Branch1",
				[new Query("character", "?character", "?type", "?spell")],
				(vars) => [
					Run<PrimTaskA>(),
					Run<ChildTryingInvalidAccess>().Configure(vars.Get<string>("?character")),
					Run<PrimTaskA>()
				])
		];
	}
}

public class ChildTryingInvalidAccess : CompoundTaskBase
{
	[Binding("?character")]
	public string CharacterVar { get; private set; }

	public ChildTryingInvalidAccess()
	{
		Branches = [
			new Branch("Branch1",
				[new Query("spell", "?spell"), new Query("can_cast", "?spell", "?character")],
				(vars) => [
					// This should FAIL - trying to access ?type from parent scope
					Run<StringTask>().Configure($"{vars.Get<string>("?character")} of type {vars.Get<string>("?type")} casts {vars.Get<string>("?spell")}")
				])
		];
	}

	public ITask Configure(string characterVar)
	{
		CharacterVar = characterVar;
		return this;
	}
}

// Test task where sibling tries to access variables from another sibling
public class ParentWithSiblingLeakage : CompoundTaskBase
{
	public ParentWithSiblingLeakage()
	{
		Branches = [
			new Branch("Branch1",
				[new Query("item", "?item", "?target")],
				(vars) => [
					Run<PrimTaskA>(),
					Run<FirstChildWithBinding>().Configure(vars.Get<string>("?item")),
					Run<SecondChildTryingToAccessSiblingVars>(),
					Run<PrimTaskA>()
				])
		];
	}
}

public class FirstChildWithBinding : CompoundTaskBase
{
	[Binding("?item")]
	public string ItemVar { get; private set; }

	public FirstChildWithBinding()
	{
		Branches = [
			new Branch("Branch1",
				[new Query("door", "?door"), new Query("can_open", "?door", "?item")],
				(vars) => [
					Run<StringTask>().Configure($"using {vars.Get<string>("?item")} on {vars.Get<string>("?door")}")
				])
		];
	}

	public ITask Configure(string itemVar)
	{
		ItemVar = itemVar;
		return this;
	}
}

public class SecondChildTryingToAccessSiblingVars : CompoundTaskBase
{
	public SecondChildTryingToAccessSiblingVars()
	{
		Branches = [
			new Branch("Branch1",
				[], // No preconditions
				(vars) => [
					// This should FAIL - trying to access ?item from sibling scope
					Run<StringTask>().Configure($"second child using {vars.Get<string>("?item")}")
				])
		];
	}
}

// Test task that demonstrates the bug by checking for variables that shouldn't exist
public class BuggyVariableLeakageTask : CompoundTaskBase
{
	public BuggyVariableLeakageTask()
	{
		Branches = [
			new Branch("Branch1",
				[new Query("target", "?target", "?weakness", "?weapon")],
				(vars) => [
					Run<PrimTaskA>(),
					Run<ChildThatTriesToAccessWeakness>().Configure(vars.Get<string>("?target")),
					Run<PrimTaskA>()
				])
		];
	}
}

public class ChildThatTriesToAccessWeakness : CompoundTaskBase
{
	[Binding("?target")]
	public string TargetVar { get; private set; }

	public ChildThatTriesToAccessWeakness()
	{
		Branches = [
			new Branch("Branch1",
				[new Query("weapon", "?weapon"), new Query("effectiveness", "?weapon", "?target")],
				(vars) => [
					// This should FAIL - trying to access ?weakness from parent
					Run<StringTask>().Configure($"{vars.Get<string>("?target")} attacked with {vars.Get<string>("?weapon")} exploiting {vars.Get<string>("?weakness")}")
				])
		];
	}

	public ITask Configure(string targetVar)
	{
		TargetVar = targetVar;
		return this;
	}
}

// Valid test case - proper binding should work
public class ParentWithProperBinding : CompoundTaskBase
{
	public ParentWithProperBinding()
	{
		Branches = [
			new Branch("Branch1", 
				[new Query("enemy", "?enemy", "?location")], 
				(vars) => [
					Run<PrimTaskA>(),
					Run<ChildWithProperBinding>().Configure(vars.Get<string>("?enemy")),
					Run<PrimTaskA>()
				])
		];
	}
}

public class ChildWithProperBinding : CompoundTaskBase
{
	[Binding("?enemy")]
	public string EnemyVar { get; private set; }

	public ChildWithProperBinding()
	{
		Branches = [
			new Branch("Branch1",
				[new Query("weapon", "?weapon"), new Query("can_use_weapon", "?weapon", "?enemy")],
				(vars) => [
					// This should work - only accessing bound ?enemy and local ?weapon
					Run<StringTask>().Configure($"{vars.Get<string>("?enemy")} attacking with weapon {vars.Get<string>("?weapon")}")
				])
		];
	}

	public ITask Configure(string enemyVar)
	{
		EnemyVar = enemyVar;
		return this;
	}
}

// Test case for child with no bindings
public class ParentWithEmptyChild : CompoundTaskBase
{
	public ParentWithEmptyChild()
	{
		Branches = [
			new Branch("Branch1",
				[], // No preconditions
				(vars) => [
					Run<PrimTaskA>(),
					Run<EmptyChild>(),
					Run<PrimTaskA>()
				])
		];
	}
}

public class EmptyChild : CompoundTaskBase
{
	public EmptyChild()
	{
		Branches = [
			new Branch("Branch1",
				[], // No preconditions
				(vars) => [
					// This should work - not accessing any variables
					Run<StringTask>().Configure("empty child executed")
				])
		];
	}
}

// Debug test classes
public class DebugParentWithChild : CompoundTaskBase
{
	public DebugParentWithChild()
	{
		Branches = [
			new Branch("Branch1", 
				[new Query("enemy", "?enemy", "?location")], 
				(vars) => [
					Run<PrimTaskA>(),
					Run<DebugChildTryingToAccessParentVar>().Configure(vars.Get<string>("?enemy")),
					Run<PrimTaskA>()
				])
		];
	}
}

public class DebugChildTryingToAccessParentVar : CompoundTaskBase
{
	[Binding("?enemy")]
	public string EnemyVar { get; private set; }

	public DebugChildTryingToAccessParentVar()
	{
		Branches = [
			new Branch("Branch1",
				[new Query("weapon", "?weapon"), new Query("can_use_weapon", "?weapon", "?enemy")],
				(vars) => [
					// This attempts to access ?location which should NOT be available in child scope
					Run<StringTask>().Configure($"Debug: enemy={vars.Get<string>("?enemy")} weapon={vars.Get<string>("?weapon")} location={vars.Get<string>("?location")}")
				])
		];
	}

	public ITask Configure(string enemyVar)
	{
		EnemyVar = enemyVar;
		return this;
	}
}

// MTR test task definitions
public class ComplexMTRTask : CompoundTaskBase
{
	public ComplexMTRTask()
	{
		Branches = [
			new Branch("Branch1_Deep", [], (vars) => [Run<ComplexMTR_SubTaskA>(), Run<ComplexMTR_SubTaskB>()]),
			new Branch("Branch2_Shallow", [], (vars) => [Run<StringTask>().Configure("shallow plan")])
		];
	}
}

public class ComplexMTR_SubTaskA : CompoundTaskBase
{
	public ComplexMTR_SubTaskA()
	{
		Branches = [
			new Branch("SubA_Branch1", [new Query("sub_a_1", "true")], (vars) => [Run<StringTask>().Configure("subA1")]),
			new Branch("SubA_Branch2", [new Query("sub_a_2", "true")], (vars) => [Run<StringTask>().Configure("subA2")])
		];
	}
}

public class ComplexMTR_SubTaskB : CompoundTaskBase
{
	public ComplexMTR_SubTaskB()
	{
		Branches = [
			new Branch("SubB_Branch1", [new Query("sub_b_1", "true")], (vars) => [Run<StringTask>().Configure("subB1")]),
			new Branch("SubB_Branch2", [new Query("sub_b_2", "true")], (vars) => [Run<StringTask>().Configure("subB2")])
		];
	}
}

public class DeepMTRTask : CompoundTaskBase
{
	public DeepMTRTask()
	{
		Branches = [
			new Branch("Deep1", [], (vars) => [Run<DeepMTRSubTask1>()]),
			new Branch("Deep2", [], (vars) => [Run<StringTask>().Configure("Deep Fallback")])
		];
	}
}

public class DeepMTRSubTask1 : CompoundTaskBase
{
	public DeepMTRSubTask1()
	{
		Branches = [
			new Branch("Level2_1", [], (vars) => [Run<DeepMTRSubTask2>()]),
			new Branch("Level2_2", [], (vars) => [Run<StringTask>().Configure("Level2 Fallback")])
		];
	}
}

public class DeepMTRSubTask2 : CompoundTaskBase
{
	public DeepMTRSubTask2()
	{
		Branches = [
			new Branch("Level3_1", [], (vars) => [Run<DeepMTRSubTask3>()]),
			new Branch("Level3_2", [], (vars) => [Run<StringTask>().Configure("Level3 Fallback")])
		];
	}
}

public class DeepMTRSubTask3 : CompoundTaskBase
{
	public DeepMTRSubTask3()
	{
		Branches = [
			new Branch("Level4_1", [new Query("deep_condition", "true")], (vars) => [Run<StringTask>().Configure("Deep Success")]),
			new Branch("Level4_2", [], (vars) => [Run<StringTask>().Configure("Level4 Fallback")])
		];
	}
}

public class ConditionalDepthTask : CompoundTaskBase
{
	public ConditionalDepthTask()
	{
		Branches = [
			new Branch("ConditionalDeepBranch", [new Query("allow_deep_planning", "true")], (vars) => [Run<DeepMTRSubTask1>()]),
			new Branch("ShallowFallback", [], (vars) => [Run<StringTask>().Configure("Shallow Fallback")])
		];
	}
}

public class WideMTRTask : CompoundTaskBase
{
	public WideMTRTask()
	{
		Branches = [
			new Branch("Branch0", [new Query("wide_0", "true")], (vars) => [Run<StringTask>().Configure("Wide Branch 0")]),
			new Branch("Branch1", [new Query("wide_1", "true")], (vars) => [Run<StringTask>().Configure("Wide Branch 1")]),
			new Branch("Branch2", [new Query("wide_2", "true")], (vars) => [Run<StringTask>().Configure("Wide Branch 2")]),
			new Branch("Branch3", [new Query("wide_3", "true")], (vars) => [Run<StringTask>().Configure("Wide Branch 3")]),
			new Branch("Branch4", [new Query("wide_4", "true")], (vars) => [Run<StringTask>().Configure("Wide Branch 4")]),
			new Branch("Branch5", [], (vars) => [Run<StringTask>().Configure("Wide Fallback")])
		];
	}
}

public class AlwaysFailingTask : CompoundTaskBase
{
	public AlwaysFailingTask()
	{
		Branches = [
			new Branch("FailBranch1", [new Query("impossible_condition", "true")], (vars) => [Run<StringTask>().Configure("Never Reached")]),
			new Branch("FailBranch2", [new Query("another_impossible", "true")], (vars) => [Run<StringTask>().Configure("Also Never Reached")])
		];
	}
}

public class SingleBranchTask : CompoundTaskBase
{
	public SingleBranchTask()
	{
		Branches = [
			new Branch("OnlyBranch", [], (vars) => [Run<StringTask>().Configure("Only Choice")])
		];
	}
}

public class MixedDepthTask : CompoundTaskBase
{
	public MixedDepthTask()
	{
		Branches = [
			new Branch("DeepBranch", [], (vars) => [Run<DeepMTRSubTask1>()]),  // Goes 3 levels deep
			new Branch("ShallowBranch", [], (vars) => [Run<StringTask>().Configure("Shallow")])  // Only 1 level
		];
	}
}

/// <summary>
/// Compound three-level planning tests and related classes
/// </summary>
public class CompoundThreeLevel : CompoundTaskBase
{
	public CompoundThreeLevel()
	{
		Branches = [
			new Branch("Branch1", [new Query("mission", "?mission", "?difficulty")], (vars) => [
				Run<PrimTaskA>(),
				Run<CompoundSecondLevel>().Configure(vars.Get<string>("?mission"), vars.Get<string>("?difficulty")),
				Run<PrimTaskA>()
			]),
			new Branch("Branch2", [], (vars) => [Run<PrimTaskC>()])
		];
	}
}

public class CompoundSecondLevel : CompoundTaskBase
{
	[Binding("?mission")]
	public string Mission { get; private set; }

	[Binding("?difficulty")]
	public string Difficulty { get; private set; }

	public CompoundSecondLevel()
	{
		Branches = [
			new Branch("Branch1", 
				[new Query("agent", "?agent", "?skill"), new Query("can_handle", "?agent", "?difficulty")], 
				(vars) => [
					Run<StringTask>().Configure("Preparing ?mission mission"),
					Run<CompoundThirdLevel>().Configure(vars.Get<string>("?mission"), vars.Get<string>("?agent"), vars.Get<string>("?difficulty")),
					Run<StringTask>().Configure("Mission ?mission completed")
				]),
			new Branch("Branch2", 
				[new Query("backup_plan", "?plan")], 
				(vars) => [Run<StringTask>().Configure("Using backup plan: ?plan for ?mission")])
		];
	}

	public ITask Configure(string mission, string difficulty)
	{
		Mission = mission;
		Difficulty = difficulty;
		return this;
	}
}

public class CompoundThirdLevel : CompoundTaskBase
{
	[Binding("?mission")]
	public string Mission { get; private set; }

	[Binding("?agent")]
	public string Agent { get; private set; }

	[Binding("?difficulty")]
	public string Difficulty { get; private set; }

	public CompoundThirdLevel()
	{
		Branches = [
			new Branch("Branch1", 
				[new Query("equipment", "?equipment"), new Query("compatible", "?equipment", "?agent")], 
				(vars) => [
					Run<StringTask>().Configure("Agent ?agent equipped with ?equipment"),
					Run<StringTask>().Configure("Executing ?difficulty ?mission mission"),
					Run<PrimTaskB>()
				]),
			new Branch("Branch2", 
				[new Query("improvise", "?method")], 
				(vars) => [Run<StringTask>().Configure("Agent ?agent improvising with ?method for ?mission")])
		];
	}

	public ITask Configure(string mission, string agent, string difficulty)
	{
		Mission = mission;
		Agent = agent;
		Difficulty = difficulty;
		return this;
	}
}
