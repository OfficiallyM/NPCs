namespace NPCs.Animation.Models
{
	/// <summary>
	/// Animation profile values that replace the base ones. Anything left unset keeps whatever the base profile has.
	/// </summary>
	public sealed class NPCAnimationOverride
	{
		// Leg gait.
		public float? StepWidthScale { get; set; }
		public float? StepLengthBase { get; set; }
		public float? StepLengthPerSpeed { get; set; }
		public float? StepLengthMax { get; set; }
		public float? LiftSlow { get; set; }
		public float? LiftFast { get; set; }
		public float? PelvisBobSlow { get; set; }
		public float? PelvisBobFast { get; set; }
		public float? KneePoleOutward { get; set; }

		// Upper body.
		public float? PelvisYaw { get; set; }
		public float? ShoulderYaw { get; set; }
		public float? PelvisSway { get; set; }
		public float? PelvisRoll { get; set; }
		public float? TorsoRollCompensation { get; set; }
		public float? TorsoFastScale { get; set; }
		public float? ArmSwingSlow { get; set; }
		public float? ArmSwingFast { get; set; }
		public float? ArmElbowFlexSlow { get; set; }
		public float? ArmElbowFlexFast { get; set; }

		/// <summary>
		/// Writes every value that is set onto the profile.
		/// </summary>
		public void ApplyTo(NPCAnimationProfile profile)
		{
			if (StepWidthScale.HasValue) profile.StepWidthScale = StepWidthScale.Value;
			if (StepLengthBase.HasValue) profile.StepLengthBase = StepLengthBase.Value;
			if (StepLengthPerSpeed.HasValue) profile.StepLengthPerSpeed = StepLengthPerSpeed.Value;
			if (StepLengthMax.HasValue) profile.StepLengthMax = StepLengthMax.Value;
			if (LiftSlow.HasValue) profile.LiftSlow = LiftSlow.Value;
			if (LiftFast.HasValue) profile.LiftFast = LiftFast.Value;
			if (PelvisBobSlow.HasValue) profile.PelvisBobSlow = PelvisBobSlow.Value;
			if (PelvisBobFast.HasValue) profile.PelvisBobFast = PelvisBobFast.Value;
			if (KneePoleOutward.HasValue) profile.KneePoleOutward = KneePoleOutward.Value;
			if (PelvisYaw.HasValue) profile.PelvisYaw = PelvisYaw.Value;
			if (ShoulderYaw.HasValue) profile.ShoulderYaw = ShoulderYaw.Value;
			if (PelvisSway.HasValue) profile.PelvisSway = PelvisSway.Value;
			if (PelvisRoll.HasValue) profile.PelvisRoll = PelvisRoll.Value;
			if (TorsoRollCompensation.HasValue) profile.TorsoRollCompensation = TorsoRollCompensation.Value;
			if (TorsoFastScale.HasValue) profile.TorsoFastScale = TorsoFastScale.Value;
			if (ArmSwingSlow.HasValue) profile.ArmSwingSlow = ArmSwingSlow.Value;
			if (ArmSwingFast.HasValue) profile.ArmSwingFast = ArmSwingFast.Value;
			if (ArmElbowFlexSlow.HasValue) profile.ArmElbowFlexSlow = ArmElbowFlexSlow.Value;
			if (ArmElbowFlexFast.HasValue) profile.ArmElbowFlexFast = ArmElbowFlexFast.Value;
		}

		/// <summary>
		/// Lays another override on top of this one. Values the other sets win, and everything else stays as it is here.
		/// </summary>
		/// <param name="other">The override to lay on top, or null for none</param>
		/// <returns>A new override, leaving both of the originals untouched</returns>
		public NPCAnimationOverride Merge(NPCAnimationOverride other)
		{
			if (other == null)
				other = new NPCAnimationOverride();

			return new NPCAnimationOverride
			{
				StepWidthScale = other.StepWidthScale ?? StepWidthScale,
				StepLengthBase = other.StepLengthBase ?? StepLengthBase,
				StepLengthPerSpeed = other.StepLengthPerSpeed ?? StepLengthPerSpeed,
				StepLengthMax = other.StepLengthMax ?? StepLengthMax,
				LiftSlow = other.LiftSlow ?? LiftSlow,
				LiftFast = other.LiftFast ?? LiftFast,
				PelvisBobSlow = other.PelvisBobSlow ?? PelvisBobSlow,
				PelvisBobFast = other.PelvisBobFast ?? PelvisBobFast,
				KneePoleOutward = other.KneePoleOutward ?? KneePoleOutward,
				PelvisYaw = other.PelvisYaw ?? PelvisYaw,
				ShoulderYaw = other.ShoulderYaw ?? ShoulderYaw,
				PelvisSway = other.PelvisSway ?? PelvisSway,
				PelvisRoll = other.PelvisRoll ?? PelvisRoll,
				TorsoRollCompensation = other.TorsoRollCompensation ?? TorsoRollCompensation,
				TorsoFastScale = other.TorsoFastScale ?? TorsoFastScale,
				ArmSwingSlow = other.ArmSwingSlow ?? ArmSwingSlow,
				ArmSwingFast = other.ArmSwingFast ?? ArmSwingFast,
				ArmElbowFlexSlow = other.ArmElbowFlexSlow ?? ArmElbowFlexSlow,
				ArmElbowFlexFast = other.ArmElbowFlexFast ?? ArmElbowFlexFast,
			};
		}
	}
}
