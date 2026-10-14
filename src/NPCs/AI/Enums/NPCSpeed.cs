namespace NPCs.AI.Enums
{
	/// <summary>
	/// How fast an NPC should travel. The speeds themselves are decided by the locomotion, so callers only say what they want.
	/// </summary>
	public enum NPCSpeed
	{
		Walk,
		WalkFast,
		Jog,
		Run,
	}
}
