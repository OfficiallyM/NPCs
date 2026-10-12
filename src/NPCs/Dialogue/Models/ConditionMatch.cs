namespace NPCs.Dialogue.Models
{
	public enum ConditionMatch
	{
		/// <summary>
		/// Every condition must pass.
		/// </summary>
		All,

		/// <summary>
		/// At least one condition must pass.
		/// </summary>
		Any,
	}
}
