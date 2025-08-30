using HTN.Debug;

namespace HTN.Planner;

/// <summary>
/// Misc metadata about a query evaluation, needs reworked once I better know what form it will take
/// </summary>
public class PlanDebugState
{
	public EventRecorder PreconditionEvents { get; set; } = new();
	public int QueryEvaluations { get; set; }
	public int ResultsEnumerated { get; set; }
	public WorldState WorldState { get; set; }

	public void QueryEvaluated() => QueryEvaluations++;
	public void ResultEnumerated() => ResultsEnumerated++;

	internal void Reset()
	{
		QueryEvaluations = 0;
		ResultsEnumerated = 0;
		PreconditionEvents = new();
		WorldState = null;
	}
}
