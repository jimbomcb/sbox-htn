namespace HTN.Tasks;

/// <summary>
/// A continuation is a pseudo-task that must be the ONLY task returned. Continuation signals that the planner should continue execution 
/// without any changes to the ongoing plan.
/// 
/// See https://www.gameaipro.com/GameAIPro/GameAIPro_Chapter29_Hierarchical_AI_for_Multiplayer_Bots_in_Killzone_3.pdf page 383 
///     Tim Verweij - HTN planning in Decima (PPT slide 83): https://www.guerrilla-games.com/read/htn-planning-in-decima
/// </summary>
public class ContinuationTask : PrimitiveTaskBase
{
}
