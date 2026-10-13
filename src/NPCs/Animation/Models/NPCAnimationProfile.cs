using UnityEngine;

namespace NPCs.Animation.Models
{
    public sealed class NPCAnimationProfile
    {
        // How quickly the head catches up with what it is looking at. Zero or less uses the speed the vanilla NPCs use.
        public float HeadLookLerp { get; set; } = 0f;
        // Head gestures. Amplitudes are in degrees, durations in seconds.
        public float NodAmplitude { get; set; } = 18f;
        public float NodDuration { get; set; } = 1.2f;
        public float ShakeAmplitude { get; set; } = 35f;
        public float ShakeDuration { get; set; } = 1.4f;
        public float HeadGestureCycles { get; set; } = 2f;
        public float GestureBlendIn { get; set; } = 0.25f;
        public float GestureBlendOut { get; set; } = 0.3f;
        public float WaveDuration { get; set; } = 2.4f;
        public float WaveCycles { get; set; } = 3f;
        public float WaveIkWeight { get; set; } = 1f;
        public float WaveRaise { get; set; } = 0.28f;
        public float WaveOutward { get; set; } = 0.12f;
        public float WaveForward { get; set; } = 0.08f;
        public float WaveSweep { get; set; } = 0.09f;
        public float ElbowPoleForward { get; set; } = 0.1f;
        public float ElbowPoleOutward { get; set; } = 0.1f;
        public float PointDuration { get; set; } = 2.2f;
        public float PointReach { get; set; } = 0.98f;
        public float PointElbowPoleForward { get; set; } = 0f;
        public float PointElbowPoleOutward { get; set; } = 0.025f;
        public float PointElbowPoleDown { get; set; } = 0.15f;
        public float PointIkWeight { get; set; } = 1f;
        public float PointHandOrientationWeight { get; set; } = 1f;

        // Leg gait. Distances are in metres and speeds in metres per second.
        public bool LegsEnabled { get; set; } = true;
        // Speeds the slow and fast ends of the gait are tuned for. Everything in between is blended.
        public float GaitSlowSpeed { get; set; } = 1.2f;
        public float GaitFastSpeed { get; set; } = 3.5f;
        // Distance between one footfall and the next grows with speed up to a limit.
        public float StepLengthBase { get; set; } = 0.2f;
        public float StepLengthPerSpeed { get; set; } = 0.22f;
        public float StepLengthMax { get; set; } = 1f;
        // How wide the feet are set compared with the model's own stance. 1 is the model's width and less brings them closer together.
        public float StepWidthScale { get; set; } = 1f;
        // The share of each stride a foot spends on the ground.
        public float StanceFractionSlow { get; set; } = 0.62f;
        public float StanceFractionFast { get; set; } = 0.42f;
        // How high a foot lifts and how far the pelvis dips in time with the steps.
        public float LiftSlow { get; set; } = 0.08f;
        public float LiftFast { get; set; } = 0.16f;
        public float PelvisBobSlow { get; set; } = 0.02f;
        public float PelvisBobFast { get; set; } = 0.045f;
        // A constant extra lowering on top of what the legs need. Zero keeps the legs as straight as MaxLegExtension allows.
        public float PelvisStandDrop { get; set; } = -0.015f;
        public float MaxPelvisDrop { get; set; } = 0.35f;
        // How far the pelvis may lift above the model's own pose to straighten legs that start out bent.
        public float MaxPelvisRaise { get; set; } = 0.15f;
        public float PelvisDropLerp { get; set; } = 14f;
        // Speed over which the stride fades in from standing still.
        public float MoveBlendSpeed { get; set; } = 0.4f;
        public float SpeedSmoothing { get; set; } = 8f;
        // How much the leg IK holds the feet to the ground when stopped. Zero leaves the legs in the model's own pose.
        public float IdleLegWeight { get; set; } = 1f;
        // How straight a leg may get, as a share of its length.
        public float MaxLegExtension { get; set; } = 0.99f;
        public float KneePoleForward { get; set; } = 1f;
        public float KneePoleOutward { get; set; } = 0.05f;
        // The ground ray starts this far above the NPC's feet and looks this far down from there.
        public float FootRayStart { get; set; } = 0.5f;
        public float FootRayLength { get; set; } = 1.5f;
        public int GroundMask { get; set; } = Physics.DefaultRaycastLayers;
        // Degrees.
        public float MaxFootTilt { get; set; } = 35f;
        public float FootSwingPitch { get; set; } = 25f;

        // Upper body motion while walking. Angles are in degrees and the sway is in metres.
        public bool TorsoMotionEnabled { get; set; } = true;
        // How far the pelvis turns each way, and how far the shoulders turn the opposite way.
        public float PelvisYaw { get; set; } = 4f;
        public float ShoulderYaw { get; set; } = 5f;
        // How far the pelvis tips, dropping on the side of the foot that is off the ground.
        public float PelvisRoll { get; set; } = 2f;
        // How much of that tip the spine cancels out to keep the shoulders level. 1 cancels it completely.
        public float TorsoRollCompensation { get; set; } = 0.75f;
        // How far the pelvis shifts sideways over the foot taking the weight.
        public float PelvisSway { get; set; } = 0.02f;
        // How much larger the pelvis and spine motion gets at the fast end of the gait.
        public float TorsoFastScale { get; set; } = 1.5f;
        // How far the arms swing each way, and how much the elbows bend as the arm comes forward.
        public float ArmSwingSlow { get; set; } = 4.5f;
        public float ArmSwingFast { get; set; } = 10f;
        public float ArmElbowFlexSlow { get; set; } = 10f;
        public float ArmElbowFlexFast { get; set; } = 40f;
    }
}
