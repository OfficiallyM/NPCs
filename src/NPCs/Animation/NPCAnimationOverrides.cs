using NPCs.Animation.Models;
using NPCs.Core.Enums;

namespace NPCs.Animation
{
	/// <summary>
	/// The standard walking style for each gender. Every NPC gets one unless its type supplies something else.
	/// </summary>
	public static class NPCAnimationOverrides
	{
		public static NPCAnimationOverride For(Gender gender) => gender == Gender.Female ? Female() : Male();

		// Shoulders lead the motion, the hips stay quieter and the feet are set a little wider.
		public static NPCAnimationOverride Male() => new NPCAnimationOverride
		{
			StepWidthScale = 0.9f,
			PelvisYaw = 3f,
			ShoulderYaw = 8f,
			PelvisSway = 0.015f,
			PelvisRoll = 2f,
		};

		// The hips lead the motion, the shoulders stay quieter and the feet are set closer together.
		public static NPCAnimationOverride Female() => new NPCAnimationOverride
		{
			StepWidthScale = 0.7f,
			PelvisYaw = 6f,
			ShoulderYaw = 3f,
			PelvisSway = 0.035f,
			PelvisRoll = 4.5f,
		};
	}
}
