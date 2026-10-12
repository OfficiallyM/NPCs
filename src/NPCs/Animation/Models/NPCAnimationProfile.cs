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
    }
}
