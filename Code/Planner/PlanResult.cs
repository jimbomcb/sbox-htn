namespace HTN.Planner;

public enum PlanResult
{
	/// <summary>
	/// Valid plan was created for execution
	/// </summary>
	Success,
	/// <summary>
	/// Failed generating
	/// </summary>
	Failed,
	/// <summary>
	/// The existing plan we have is valid and should still be ran
	/// </summary>
	Continue
}
