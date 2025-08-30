using System;

namespace HTN.Planner;

/// <summary>
/// Properties with the Binding attriubte are injected into the variable context of a given compound task.
/// For example:
/// 
/// <![CDATA[
/// internal class WarmSelfAtHome : CompoundTaskBase
/// {
/// 	public HousingService HouseService { get; private set; }
/// 
/// --> [Binding( "?house_fueled" )] // This property is made available to branch preconditions for querying, and in the variables during task generation.
/// 	public bool HasFuel { get; private set; }
/// 
/// 	public ITask Configure( HousingService houseService, bool hasFuel )
/// 	{
/// 		HouseService = houseService;
/// 		HasFuel = hasFuel;
/// 		return this;
/// 	}
/// 
/// 	public WarmSelfAtHome()
/// 	{
/// 		Branches = [
/// 			new Branch( "WarmSelfAtHome", 
/// 				// If we have fuel (made available via [Binding( "?house_fueled" )])
/// 				[ new Compare("?house_fueled", Compare.Operator.Equal, true) ], 
/// 			(vars) => [
/// 				// Walk to our house and warm ourselves
/// 				Run<WalkToStructure>().Configure( HouseService.Structure ),
/// 				Run<ServiceAction>().Configure( HouseService, "warm_self" )
/// 			]),
/// 			
/// 			new Branch( "RefuelMyHouse", 
/// 			[
/// 				// If we don't have fuel, and if we can find a service with fuel, write it to ?storage_service
///					new Compare("?house_fueled", Compare.Operator.Equal, false),
/// 				new FindRawMaterialType( ItemType.Fuel, 10, "?storage_service" ),
/// 			], 
/// 			(vars) => [
/// 				// Navigate to the storage service structure, pick up the fuel
/// 				Run<WalkToStructure>().Configure( vars.Get<StructureService>( "?storage_service" ).Structure ),
/// 				Run<ServiceAction>().Configure( vars.Get<StructureService>( "?storage_service" ), "pickup_type", ItemType.Fuel, 10 ),
/// 				
///					// Navigate back to our house and deposit the fuel
/// 				Run<WalkToStructure>().Configure( HouseService.Structure ),
/// 				Run<ServiceAction>().Configure( HouseService.StorageService, "deposit", ItemType.Fuel ),
/// 				
///					// Recurse into ourself but now with HasFuel = true, resulting in the first branch being taken and walking home
/// 				Run<WarmSelfAtHome>().Configure( HouseService, true ),
/// 			])
/// 		]
/// 	}
/// }
///	]]>
///	
/// Correctly binds the Enemy property's value to ?enemy in the variable context before branch evaluation, allowing it to be 
/// used for pattern matching and passing to other tasks.
/// TODO: Static analysis to ensure prefixes with ? AND ensure that it's only used in the right place? Would roslyn let us do this when added?
/// </summary>
[AttributeUsage( AttributeTargets.Property, Inherited = true )]
public class BindingAttribute : Attribute
{
	public string Name { get; init; }

	public BindingAttribute( string name )
	{
		if ( !name.StartsWith( '?' ) )
			throw new ArgumentException( "Bound variables must be prefixed with a ?" );

		Name = name;
	}
}



