using NPCs.Animation.Enums;
using NPCs.Animation.Models;
using NPCs.Core;
using NPCs.Utilities;
using UnityEngine;

namespace NPCs.Animation
{
    [DefaultExecutionOrder(-40)]
    [DisallowMultipleComponent]
    public sealed class NPCAnimation : MonoBehaviour
    {
        private const float MinimumBlendTime = 0.001f;
        private const float MinimumDuration = 0.1f;
        private const float MinimumArmLength = 0.001f;
        private const float MinimumWeight = 0.001f;
        private const float DefaultHeadLookLerp = 5f;
        private const float LookDistance = 5f;
        private const float LookHeightFallback = 1.6f;
        private const float MinimumLookDistance = 2f;

        private sealed class ArmState
        {
            public Transform Upper;
            public Transform Lower;
            public Transform Hand;
            public Transform IndexFinger;
            public Quaternion HandRestLocalRotation;
            public float OutwardSign = 1f;
            public NPCGesture Gesture;
            public float Elapsed;
            public float Duration;
            public float BlendWeight;
            public float BlendStartWeight;
            public float StopElapsed;
            public float StopStartWeight;
            public float StopPoseElapsed;
            public bool Stopping;
            public bool Ready;
            public bool LastSolveSucceeded;
            public float LastHandError;
            public Vector3 LastTarget;
            public Vector3 TransitionStartTarget;
            public Vector3 PointTargetPosition;
            public Transform PointTargetTransform;
        }

        private NPCBody _body;
        private NPCAnimationProfile _profile = new NPCAnimationProfile();
        private readonly ArmState _left = new ArmState();
        private readonly ArmState _right = new ArmState();
        private NPCHeadLook _headLook;
        private NPCLegGait _legs;
        private Transform _head;
        private Vector3 _lookTarget;
        private NPCHeadGesture _headGesture;
        private float _headGestureElapsed;
        private bool _initialised;

        public bool IsReady => _left.Ready || _right.Ready;

        /// <summary>
        /// The leg gait, or null if the model's legs couldn't be bound.
        /// </summary>
        public NPCLegGait Legs => _legs;

        /// <summary>
        /// The live tuning values. The debugger edits these while the game runs.
        /// </summary>
        public NPCAnimationProfile Profile => _profile;

        /// <summary>
        /// Lays an override over this NPC's own profile. The gait reads the profile every frame, so it takes effect straight away.
        /// </summary>
        public void ApplyOverride(NPCAnimationOverride overrides)
        {
            overrides?.ApplyTo(_profile);
        }

        /// <summary>
        /// The world point the head is actually looking at, which trails the point it was asked to look at.
        /// </summary>
        public Vector3 LookPoint => _headLook != null ? _headLook.Current : transform.position + transform.forward * LookDistance;

        private Vector3 HeadPosition => _head != null ? _head.position : transform.position + Vector3.up * LookHeightFallback;

        public void Initialise(NPC npc, NPCAnimationProfile profile)
        {
            if (_initialised)
                return;

            _initialised = true;
            _body = npc != null ? npc.Body : null;
            _profile = profile ?? new NPCAnimationProfile();

            if (_body == null)
            {
                Logging.LogWarning("Could not bind NPC animation because the body is unavailable on " + name + ".");
                return;
            }

            BindArm(_left, "l");
            BindArm(_right, "r");

            if (!IsReady)
                Logging.LogWarning("Could not bind either arm for NPC animation on " + name + ".");

            BindHead();
            BindLegs();
        }

        // The head's look point starts at the world origin, and the NPC may not be in place yet when this is built.
        // Starting here, once everything is placed, stops the head swinging round on the first frame.
        private void Start()
        {
            LookAhead();
            SnapLook();
            _legs?.Snap();
        }

        /// <summary>
        /// Turns the head towards a world point. It lerps there on its own, so this only needs to move the point.
        /// </summary>
        public void LookAt(Vector3 point)
        {
            _lookTarget = point;
        }

        public void LookAhead()
        {
            LookAt(HeadPosition + transform.forward * LookDistance);
        }

        public NPCHeadGesture CurrentHeadGesture => _headGesture;

        public void Nod()
        {
            PlayHeadGesture(NPCHeadGesture.Nod);
        }

        public void ShakeHead()
        {
            PlayHeadGesture(NPCHeadGesture.Shake);
        }

        /// <summary>
        /// Swings the gaze around whatever the head is looking at. The look point still follows the AI while this plays.
        /// </summary>
        public void PlayHeadGesture(NPCHeadGesture gesture)
        {
            _headGesture = gesture;
            _headGestureElapsed = 0f;
        }

        public void StopHeadGesture()
        {
            PlayHeadGesture(NPCHeadGesture.None);
        }

        /// <summary>
        /// Jumps the head straight to where it is meant to look, so there is no swing.
        /// </summary>
        public void SnapLook()
        {
            if (_headLook == null)
                return;

            _headLook.Target = _lookTarget;
            _headLook.Snap();
        }

        // Rotates the look point away from the straight-line view of it. Positive pitch looks down and positive yaw looks right.
        private Vector3 OffsetLook(Vector3 point, float pitch, float yaw)
        {
            Vector3 head = HeadPosition;
            Vector3 toPoint = point - head;
            float distance = Mathf.Max(toPoint.magnitude, MinimumLookDistance);
            Vector3 direction = toPoint.sqrMagnitude > 0.0001f ? toPoint.normalized : transform.forward;

            Vector3 right = Vector3.Cross(Vector3.up, direction);
            right = right.sqrMagnitude > 0.0001f ? right.normalized : transform.right;

            direction = Quaternion.AngleAxis(yaw, Vector3.up) * Quaternion.AngleAxis(pitch, right) * direction;
            return head + direction * distance;
        }

        private Vector3 ApplyHeadGesture(Vector3 point, float deltaTime)
        {
            if (_headGesture == NPCHeadGesture.None)
                return point;

            bool nod = _headGesture == NPCHeadGesture.Nod;
            float duration = Mathf.Max(MinimumDuration, nod ? _profile.NodDuration : _profile.ShakeDuration);

            _headGestureElapsed += deltaTime;
            if (_headGestureElapsed >= duration)
            {
                _headGesture = NPCHeadGesture.None;
                return point;
            }

            // Whole cycles, so the swing starts and ends exactly on the point being looked at.
            float cycles = Mathf.Max(0.1f, _profile.HeadGestureCycles);
            float amplitude = nod ? _profile.NodAmplitude : _profile.ShakeAmplitude;
            float swing = amplitude * Mathf.Sin(_headGestureElapsed / duration * Mathf.PI * 2f * cycles);

            return nod ? OffsetLook(point, swing, 0f) : OffsetLook(point, 0f, swing);
        }

        private void BindHead()
        {
            _head = _body.Head;

            if (!_body.TryGetBone("neck_01", out Transform neck)
                || !_body.TryGetNeutralRotation(neck, out Quaternion neckRest)
                || !_body.TryGetNeutralRotation(_head, out Quaternion headRest))
            {
                Logging.LogWarning("Could not bind the head for NPC animation on " + name + ".");
                return;
            }

            _headLook = new NPCHeadLook(transform, neck, _head, neckRest, headRest, ResolveHeadLookLerp());
        }

        private void BindLegs()
        {
            if (!_profile.LegsEnabled)
                return;

            if (!NPCLegGait.TryCreate(_body, transform, _profile, out _legs))
            {
                Logging.LogWarning("Could not bind the legs for NPC animation on " + name + ".");
                return;
            }

            if (_profile.TorsoMotionEnabled && !_legs.HasTorso)
                Logging.LogWarning("Could not bind the torso for NPC walking on " + name + ".");
        }

        private float ResolveHeadLookLerp()
        {
            if (_profile.HeadLookLerp > 0f)
                return _profile.HeadLookLerp;

            // Matches how fast the vanilla NPCs turn their heads.
            newAiAnim vanilla = itemdatabase.d.gmunkas01.GetComponentInChildren<newAiAnim>(true);
            return vanilla != null ? vanilla.lookLerp : DefaultHeadLookLerp;
        }

        public bool IsHandReady(NPCHand hand)
        {
            return GetArm(hand).Ready;
        }

        public NPCGesture GetCurrentGesture(NPCHand hand)
        {
            return GetArm(hand).Gesture;
        }

        public float GetGestureWeight(NPCHand hand)
        {
            return GetArm(hand).BlendWeight;
        }

        public bool GetLastSolveSucceeded(NPCHand hand)
        {
            return GetArm(hand).LastSolveSucceeded;
        }

        public float GetLastHandError(NPCHand hand)
        {
            return GetArm(hand).LastHandError;
        }

        public Vector3 GetHandTarget(NPCHand hand)
        {
            return GetArm(hand).LastTarget;
        }

        public Transform GetDebugUpperArm(NPCHand hand)
        {
            return GetArm(hand).Upper;
        }

        public Transform GetDebugLowerArm(NPCHand hand)
        {
            return GetArm(hand).Lower;
        }

        public Transform GetDebugHand(NPCHand hand)
        {
            return GetArm(hand).Hand;
        }

        public bool PlayGesture(NPCGesture gesture, NPCHand hand)
        {
            if (gesture == NPCGesture.None)
            {
                StopGesture(hand);
                return true;
            }

            if (gesture == NPCGesture.Point)
                return false;

            if (gesture != NPCGesture.Wave)
                return false;

            ArmState arm = GetArm(hand);
            return StartGesture(arm, NPCGesture.Wave, Mathf.Max(MinimumDuration, _profile.WaveDuration), null, Vector3.zero);
        }

        public bool PlayGesture(NPCGesture gesture, NPCHand hand, Vector3 targetPosition, float duration = -1f)
        {
            if (gesture == NPCGesture.Point)
                return PlayPoint(hand, targetPosition, duration);

            if (gesture == NPCGesture.None)
            {
                StopGesture(hand);
                return true;
            }

            return PlayGesture(gesture, hand);
        }

        public bool PlayGesture(NPCGesture gesture, NPCHand hand, Transform target, float duration = -1f)
        {
            if (gesture == NPCGesture.Point)
                return PlayPoint(hand, target, duration);

            if (gesture == NPCGesture.None)
            {
                StopGesture(hand);
                return true;
            }

            return PlayGesture(gesture, hand);
        }

        public bool PlayPoint(NPCHand hand, Vector3 targetPosition, float duration = -1f)
        {
            float actualDuration = ResolvePointDuration(duration);
            return StartGesture(GetArm(hand), NPCGesture.Point, actualDuration, null, targetPosition);
        }

        public bool PlayPoint(NPCHand hand, Transform target, float duration = -1f)
        {
            if (target == null)
                return false;

            float actualDuration = ResolvePointDuration(duration);
            return StartGesture(GetArm(hand), NPCGesture.Point, actualDuration, target, target.position);
        }

        public void StopGesture()
        {
            StopGesture(NPCHand.Left);
            StopGesture(NPCHand.Right);
        }

        public void StopGesture(NPCHand hand)
        {
            ArmState arm = GetArm(hand);
            if (!arm.Ready || arm.Gesture == NPCGesture.None || arm.Stopping)
                return;

            arm.Stopping = true;
            arm.StopElapsed = 0f;
            arm.StopStartWeight = arm.BlendWeight;
            arm.StopPoseElapsed = arm.Elapsed;
        }

        private float ResolvePointDuration(float duration)
        {
            if (duration < 0f)
                return Mathf.Max(0f, _profile.PointDuration);

            return duration;
        }

        private bool StartGesture(
            ArmState arm,
            NPCGesture gesture,
            float duration,
            Transform pointTarget,
            Vector3 pointPosition)
        {
            if (arm == null || !arm.Ready)
                return false;

            Vector3 currentTarget = arm.LastTarget;
            arm.TransitionStartTarget = currentTarget;
            arm.BlendStartWeight = arm.BlendWeight;
            arm.Gesture = gesture;
            arm.Elapsed = 0f;
            arm.Duration = duration;
            arm.StopElapsed = 0f;
            arm.StopStartWeight = 0f;
            arm.StopPoseElapsed = 0f;
            arm.Stopping = false;
            arm.PointTargetTransform = pointTarget;
            arm.PointTargetPosition = pointPosition;
            arm.LastSolveSucceeded = false;
            return true;
        }

        private void LateUpdate()
        {
            if (_body == null || _body.IsRagdolled)
            {
                ResetGestureState(_left);
                ResetGestureState(_right);
                _headGesture = NPCHeadGesture.None;
                return;
            }

            if (_left.Ready)
                _left.Hand.localRotation = _left.HandRestLocalRotation;

            if (_right.Ready)
                _right.Hand.localRotation = _right.HandRestLocalRotation;

            float deltaTime = Mathf.Max(0f, Time.deltaTime);

            // The legs go first because the pelvis carries the shoulders, and the arms should be solved from where those end up.
            _legs?.SetArmFreedom(1f - _left.BlendWeight, 1f - _right.BlendWeight);
            _legs?.Tick(deltaTime);

            EvaluateArm(_left, deltaTime);
            EvaluateArm(_right, deltaTime);

            if (_headLook != null)
            {
                _headLook.Target = ApplyHeadGesture(_lookTarget, deltaTime);
                _headLook.Tick(deltaTime);
                _headLook.Apply();
            }
        }

        private void EvaluateArm(ArmState arm, float deltaTime)
        {
            if (!arm.Ready)
                return;

            Vector3 baselineHandPosition = arm.Hand.position;
            if (arm.Gesture == NPCGesture.None)
            {
                arm.BlendWeight = 0f;
                arm.LastSolveSucceeded = false;
                arm.LastHandError = 0f;
                arm.LastTarget = baselineHandPosition;
                return;
            }

            float blendIn = Mathf.Max(MinimumBlendTime, _profile.GestureBlendIn);
            float blendOut = Mathf.Max(MinimumBlendTime, _profile.GestureBlendOut);
            arm.Elapsed += deltaTime;

            if (!arm.Stopping && arm.Duration > 0f && arm.Elapsed >= arm.Duration)
            {
                arm.Stopping = true;
                arm.StopElapsed = 0f;
                arm.StopStartWeight = arm.BlendWeight;
                arm.StopPoseElapsed = arm.Duration;
            }

            if (arm.Stopping)
            {
                arm.StopElapsed += deltaTime;
                float stopProgress = Mathf.Clamp01(arm.StopElapsed / blendOut);
                arm.BlendWeight = arm.StopStartWeight * (1f - Mathf.SmoothStep(0f, 1f, stopProgress));
            }
            else
            {
                float blendProgress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(arm.Elapsed / blendIn));
                arm.BlendWeight = Mathf.Lerp(arm.BlendStartWeight, 1f, blendProgress);
            }

			CalculatePoseTarget(arm, out Vector3 poseTarget, out Vector3 pointTargetPosition);

			float targetBlend = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(arm.Elapsed / blendIn));
            Vector3 transitioningTarget = Vector3.Lerp(arm.TransitionStartTarget, poseTarget, targetBlend);
            arm.LastTarget = Vector3.Lerp(baselineHandPosition, transitioningTarget, arm.BlendWeight);

            float ikWeight = GetGestureIkWeight(arm.Gesture) * arm.BlendWeight;
            arm.LastSolveSucceeded = ikWeight > MinimumWeight && TwoBoneIkSolver.Solve(
                arm.Upper,
                arm.Lower,
                arm.Hand,
                arm.LastTarget,
                GetElbowPole(arm),
                ikWeight
			);

            if (arm.Gesture == NPCGesture.Point && arm.IndexFinger != null && arm.BlendWeight > MinimumWeight)
                ApplyPointingHandRotation(arm, pointTargetPosition);

            arm.LastHandError = Vector3.Distance(arm.Hand.position, arm.LastTarget);

            if (arm.Stopping && arm.StopElapsed >= blendOut)
                ResetGestureState(arm);
        }

        private void CalculatePoseTarget(ArmState arm, out Vector3 poseTarget, out Vector3 pointTargetPosition)
        {
            pointTargetPosition = arm.PointTargetPosition;
            if (arm.Gesture == NPCGesture.Wave)
            {
                float duration = Mathf.Max(MinimumDuration, arm.Duration);
                float poseElapsed = arm.Stopping ? arm.StopPoseElapsed : arm.Elapsed;
                poseElapsed = Mathf.Clamp(poseElapsed, 0f, duration);
                float cycles = Mathf.Max(0.1f, _profile.WaveCycles);
                float wave = Mathf.Sin((poseElapsed / duration) * cycles * Mathf.PI * 2f);

                Vector3 rootUp = transform.up;
                Vector3 rootForward = transform.forward;
                Vector3 outward = transform.right * arm.OutwardSign;

                Vector3 waveCentre = arm.Upper.position
                    + rootUp * _profile.WaveRaise
                    + outward * _profile.WaveOutward
                    + rootForward * _profile.WaveForward;

                poseTarget = waveCentre + outward * (wave * _profile.WaveSweep);
                return;
            }

            if (arm.PointTargetTransform != null)
                arm.PointTargetPosition = arm.PointTargetTransform.position;

            pointTargetPosition = arm.PointTargetPosition;
            Vector3 direction = pointTargetPosition - arm.Upper.position;
            if (direction.sqrMagnitude <= MinimumArmLength * MinimumArmLength)
                direction = transform.forward;

            direction.Normalize();
            float upperLength = Vector3.Distance(arm.Upper.position, arm.Lower.position);
            float lowerLength = Vector3.Distance(arm.Lower.position, arm.Hand.position);
            float reach = (upperLength + lowerLength) * Mathf.Clamp(_profile.PointReach, 0.1f, 0.99f);
            poseTarget = arm.Upper.position + direction * reach;
        }

        private float GetGestureIkWeight(NPCGesture gesture)
        {
            switch (gesture)
            {
                case NPCGesture.Wave:
                    return Mathf.Clamp01(_profile.WaveIkWeight);
                case NPCGesture.Point:
                    return Mathf.Clamp01(_profile.PointIkWeight);
                default:
                    return 0f;
            }
        }

        private Vector3 GetElbowPole(ArmState arm)
        {
            Vector3 outward = transform.right * arm.OutwardSign;

            if (arm.Gesture == NPCGesture.Point)
            {
                return arm.Upper.position
                    + transform.forward * _profile.PointElbowPoleForward
                    + outward * _profile.PointElbowPoleOutward
                    - transform.up * _profile.PointElbowPoleDown;
            }

            return arm.Upper.position
                + transform.forward * _profile.ElbowPoleForward
                + outward * _profile.ElbowPoleOutward;
        }

        private void ApplyPointingHandRotation(ArmState arm, Vector3 targetPosition)
        {
            Vector3 fingerDirection = arm.IndexFinger.position - arm.Hand.position;
            Vector3 targetDirection = targetPosition - arm.Hand.position;
            if (fingerDirection.sqrMagnitude <= MinimumArmLength * MinimumArmLength || targetDirection.sqrMagnitude <= MinimumArmLength * MinimumArmLength)
                return;

            Quaternion correction = Quaternion.FromToRotation(fingerDirection, targetDirection);
            Quaternion desiredRotation = correction * arm.Hand.rotation;
            float weight = arm.BlendWeight * Mathf.Clamp01(_profile.PointHandOrientationWeight);
            arm.Hand.rotation = Quaternion.Slerp(arm.Hand.rotation, desiredRotation, weight);
        }

        private void BindArm(ArmState arm, string side)
        {
            if (!_body.TryGetBone("upperarm_" + side, out arm.Upper)
                || !_body.TryGetBone("lowerarm_" + side, out arm.Lower)
                || !_body.TryGetBone("hand_" + side, out arm.Hand))
            {
                return;
            }

            if (!arm.Lower.IsChildOf(arm.Upper) || !arm.Hand.IsChildOf(arm.Lower))
                return;

            if (!_body.TryGetBone("index_03_" + side + "_end", out arm.IndexFinger)
                && !_body.TryGetBone("index_03_" + side, out arm.IndexFinger)
                && !_body.TryGetBone("index_02_" + side, out arm.IndexFinger))
            {
                _body.TryGetBone("index_01_" + side, out arm.IndexFinger);
            }

            arm.HandRestLocalRotation = arm.Hand.localRotation;

            float localSide = Vector3.Dot(arm.Upper.position - transform.position, transform.right);
            if (Mathf.Abs(localSide) > MinimumArmLength)
                arm.OutwardSign = Mathf.Sign(localSide);

            arm.LastTarget = arm.Hand.position;
            arm.TransitionStartTarget = arm.LastTarget;
            arm.PointTargetPosition = arm.LastTarget;
            arm.Ready = true;
        }

        private ArmState GetArm(NPCHand hand)
			=> hand == NPCHand.Left ? _left : _right;

        private static void ResetGestureState(ArmState arm)
        {
            if (arm == null)
                return;

            arm.Gesture = NPCGesture.None;
            arm.Elapsed = 0f;
            arm.Duration = 0f;
            arm.BlendWeight = 0f;
            arm.BlendStartWeight = 0f;
            arm.StopElapsed = 0f;
            arm.StopStartWeight = 0f;
            arm.StopPoseElapsed = 0f;
            arm.Stopping = false;
            arm.PointTargetTransform = null;
            arm.LastSolveSucceeded = false;

            if (arm.Hand != null)
            {
                arm.LastTarget = arm.Hand.position;
                arm.LastHandError = 0f;
            }
        }
    }
}
