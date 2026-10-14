using NPCs.AI;
using NPCs.AI.Enums;
using NPCs.Animation;
using NPCs.Animation.Enums;
using NPCs.Animation.Models;
using NPCs.Core;
using NPCs.Utilities;
using UnityEngine;

namespace NPCs.Debugging
{
	[DefaultExecutionOrder(-39)]
	[DisallowMultipleComponent]
	public sealed class NPCDebugger : MonoBehaviour
	{
		private const float SearchInterval = 0.75f;

		private NPC _npc;
		private float _nextSearchTime;
		private string _status = "Searching for an NPC";
		private Vector2 _scroll;
		private bool _show = false;
		private Rect _windowRect = new Rect(12f, 12f, 390f, 700f);
		private const int WindowId = 81742;

		// Far enough ahead that "Walk forward" keeps going for as long as the gait needs watching.
		private const float ForwardDistance = 200f;

		private static readonly string[] _speedLabels = { "Walk", "Walk fast", "Jog", "Run" };

		private NPCSpeed _speed = NPCSpeed.Walk;

		public static NPCDebugger Create()
		{
			NPCDebugger existing = FindObjectOfType<NPCDebugger>();
			if (existing != null)
				return existing;

			GameObject debuggerObject = new GameObject("NPC Debugger");
			DontDestroyOnLoad(debuggerObject);
			return debuggerObject.AddComponent<NPCDebugger>();
		}

		private void Update()
		{
			if (Input.GetKeyDown(KeyCode.F8))
			{
				_show = !_show;
				if (!_show)
				{
					StopWalking();
					_npc = null;
				}
			}

			if (!_show) return;

			if (_npc != null
				&& (!_npc.gameObject.activeInHierarchy
					|| _npc.IsDead
					|| _npc.Body == null
					|| _npc.Body.IsRagdolled
					|| _npc.Animation == null
					|| !_npc.Animation.IsReady))
			{
				_npc = null;
				_status = "NPC unavailable, searching again";
				_nextSearchTime = 0f;
			}

			if (_npc != null || Time.unscaledTime < _nextSearchTime)
				return;

			_nextSearchTime = Time.unscaledTime + SearchInterval;
			TryBindNearestNpc();
		}

		private void TryBindNearestNpc()
		{
			Transform origin = mainscript.M.player.transform;
			NPC nearest = null;
			float nearestDistance = float.PositiveInfinity;
			NPC[] candidates = FindObjectsOfType<NPC>();

			for (int i = 0; i < candidates.Length; i++)
			{
				NPC candidate = candidates[i];
				if (candidate == null
					|| !candidate.gameObject.activeInHierarchy
					|| candidate.IsDead
					|| candidate.Body == null
					|| candidate.Body.IsRagdolled
					|| candidate.Animation == null
					|| !candidate.Animation.IsReady)
				{
					continue;
				}

				float distance = Vector3.Distance(origin.position, candidate.transform.position);
				if (distance >= nearestDistance)
					continue;

				nearest = candidate;
				nearestDistance = distance;
			}

			if (nearest == null)
			{
				_status = "No active NPC with a bound animation rig found";
				return;
			}

			_npc = nearest;
			_status = "Bound to " + nearest.name + " at " + nearestDistance.ToString("F2") + " m";
			Logging.LogDebug("[NPCs Debugger] " + _status);
		}

		private void OnGUI()
		{
			if (!_show)
				return;

			_windowRect = GUI.Window(
				WindowId,
				_windowRect,
				DrawWindow,
				"NPC Debugger"
			);
		}

		private void DrawWindow(int windowId)
		{
			_scroll = GUILayout.BeginScrollView(_scroll);

			GUILayout.Label("Status: " + _status);
			GUILayout.Label("NPC: " + (_npc != null ? _npc.name + " (" + _npc.Gender + ")" : "Not found"));

			if (_npc != null && _npc.Animation != null)
			{
				if (GUILayout.Button("Rebind Nearest NPC"))
				{
					_npc = null;
					_nextSearchTime = 0f;
				}

				NPCAnimation animation = _npc.Animation;
				DrawLocomotionControls();
				DrawGestureControls(animation);
				DrawLegControls(animation);
			}
			else
			{
				GUILayout.Label("Waiting for a living NPC with at least one bound arm.");
			}

			GUILayout.EndScrollView();

			// Drag the window using its title bar.
			GUI.DragWindow(new Rect(0f, 0f, 10000f, 24f));
		}

		private void DrawGestureControls(NPCAnimation animation)
		{
			GUILayout.Label("Left arm: " + DescribeHand(animation, NPCHand.Left));
			GUILayout.Label("Right arm: " + DescribeHand(animation, NPCHand.Right));

			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Wave Left") && animation.IsHandReady(NPCHand.Left))
				animation.PlayGesture(NPCGesture.Wave, NPCHand.Left);

			if (GUILayout.Button("Wave Right") && animation.IsHandReady(NPCHand.Right))
				animation.PlayGesture(NPCGesture.Wave, NPCHand.Right);
			GUILayout.EndHorizontal();

			if (GUILayout.Button("Wave Both Hands"))
			{
				animation.PlayGesture(NPCGesture.Wave, NPCHand.Left);
				animation.PlayGesture(NPCGesture.Wave, NPCHand.Right);
			}

			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Point Left at Player"))
				PointAtPlayer(animation, NPCHand.Left);

			if (GUILayout.Button("Point Right at Player"))
				PointAtPlayer(animation, NPCHand.Right);
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Stop Left"))
				animation.StopGesture(NPCHand.Left);

			if (GUILayout.Button("Stop Right"))
				animation.StopGesture(NPCHand.Right);
			GUILayout.EndHorizontal();

			if (GUILayout.Button("Stop Both Gestures"))
				animation.StopGesture();
		}

		private void DrawLegControls(NPCAnimation animation)
		{
			NPCLegGait legs = animation.Legs;
			GUILayout.Space(6f);

			if (legs == null)
			{
				GUILayout.Label("Legs: unbound");
				return;
			}

			GUILayout.Label("Speed " + legs.Speed.ToString("F2") + " m/s, step " + legs.StepLength.ToString("F2")
				+ " m, phase " + legs.Phase.ToString("F2") + ", pelvis drop " + legs.PelvisDrop.ToString("F3") + " m");
			GUILayout.Label("Leg rest extension " + legs.RestExtension.ToString("F3") + " (1 is straight), ankle height " + legs.AnkleHeight.ToString("F3")
				+ " m, torso " + (legs.HasTorso ? "bound" : "UNBOUND"));
			GUILayout.Label("Left foot: " + DescribeFoot(legs.Left));
			GUILayout.Label("Right foot: " + DescribeFoot(legs.Right));

			DrawTuning(animation.Profile);

		}

		// Edits the live profile, which the gait reads every frame, so changes show straight away.
		private static void DrawTuning(NPCAnimationProfile profile)
		{
			profile.MaxLegExtension = Slider("Max leg extension", profile.MaxLegExtension, 0.85f, 1f);
			profile.PelvisStandDrop = Slider("Pelvis stand drop", profile.PelvisStandDrop, -0.05f, 0.1f);
			profile.StepWidthScale = Slider("Step width scale", profile.StepWidthScale, 0.3f, 1.3f);
			profile.StepLengthBase = Slider("Step length base", profile.StepLengthBase, 0f, 0.8f);
			profile.StepLengthPerSpeed = Slider("Step length per m/s", profile.StepLengthPerSpeed, 0f, 0.6f);
			profile.StepLengthMax = Slider("Step length max", profile.StepLengthMax, 0.3f, 1.5f);
			profile.LiftSlow = Slider("Foot lift slow", profile.LiftSlow, 0f, 0.3f);
			profile.LiftFast = Slider("Foot lift fast", profile.LiftFast, 0f, 0.4f);
			profile.PelvisBobSlow = Slider("Pelvis bob slow", profile.PelvisBobSlow, 0f, 0.1f);
			profile.PelvisBobFast = Slider("Pelvis bob fast", profile.PelvisBobFast, 0f, 0.15f);
			profile.PelvisYaw = Slider("Pelvis yaw", profile.PelvisYaw, 0f, 15f);
			profile.ShoulderYaw = Slider("Shoulder yaw", profile.ShoulderYaw, 0f, 20f);
			profile.PelvisRoll = Slider("Pelvis roll", profile.PelvisRoll, 0f, 12f);
			profile.TorsoRollCompensation = Slider("Torso roll compensation", profile.TorsoRollCompensation, 0f, 1.5f);
			profile.PelvisSway = Slider("Pelvis sway", profile.PelvisSway, 0f, 0.08f);
			profile.TorsoFastScale = Slider("Torso fast scale", profile.TorsoFastScale, 0.5f, 3f);
			profile.ArmSwingSlow = Slider("Arm swing slow", profile.ArmSwingSlow, 0f, 70f);
			profile.ArmSwingFast = Slider("Arm swing fast", profile.ArmSwingFast, 0f, 90f);
			profile.ArmElbowFlexSlow = Slider("Elbow flex slow", profile.ArmElbowFlexSlow, 0f, 70f);
			profile.ArmElbowFlexFast = Slider("Elbow flex fast", profile.ArmElbowFlexFast, 0f, 90f);
		}

		private static float Slider(string label, float value, float min, float max)
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label(label + " " + value.ToString("F3"), GUILayout.Width(190f));
			value = GUILayout.HorizontalSlider(value, min, max);
			GUILayout.EndHorizontal();
			return value;
		}

		private static string DescribeFoot(NPCFootState foot)
		{
			return (foot.Swinging ? "swing" : "planted")
				+ ", ground " + (foot.GroundHit ? "found" : "MISSING")
				+ ", solver " + (foot.SolveSucceeded ? "OK" : "idle")
				+ ", error " + Vector3.Distance(foot.Position, foot.Target).ToString("F3") + " m";
		}

		private void DrawLocomotionControls()
		{
			NPCLocomotion locomotion = _npc.Ai?.Locomotion;
			GUILayout.Space(6f);

			if (locomotion == null)
			{
				GUILayout.Label("Locomotion: unavailable");
				return;
			}

			GUILayout.Label("Locomotion: " + (locomotion.HasDestination ? "heading to " + locomotion.Destination.ToString("F1") : "no destination")
				+ ", speed " + locomotion.CurrentSpeed.ToString("F2") + " m/s");

			NPCSpeed selected = (NPCSpeed)GUILayout.Toolbar((int)_speed, _speedLabels);
			if (selected != _speed)
			{
				_speed = selected;
				locomotion.SetSpeed(_speed);
			}

			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Walk here"))
				WalkToPlayer();

			if (GUILayout.Button("Walk forward"))
				WalkForward();

			if (GUILayout.Button("Stop"))
				StopWalking();
			GUILayout.EndHorizontal();
		}

		private void WalkToPlayer()
		{
			Transform player = mainscript.M.player.transform;
			Vector3 offset = Vector3.ProjectOnPlane(_npc.transform.position - player.position, Vector3.up);

			_npc.Ai.WalkTo(player.position + offset.normalized, _speed);
		}

		private void WalkForward()
		{
			Vector3 forward = Vector3.ProjectOnPlane(_npc.transform.forward, Vector3.up).normalized;
			_npc.Ai.WalkTo(_npc.transform.position + forward * ForwardDistance, _speed);
		}

		// Only interrupts a walk, so an NPC that's talking isn't dropped out of its conversation.
		private void StopWalking()
		{
			if (_npc?.Ai?.Locomotion == null)
				return;

			if (_npc.Ai.Locomotion.HasDestination)
				_npc.Ai.Rest();
		}

		private static string DescribeHand(NPCAnimation animation, NPCHand hand)
		{
			if (!animation.IsHandReady(hand))
				return "unbound";

			return animation.GetCurrentGesture(hand)
				+ ", weight " + animation.GetGestureWeight(hand).ToString("F2")
				+ ", solver " + (animation.GetLastSolveSucceeded(hand) ? "OK" : "idle")
				+ ", error " + animation.GetLastHandError(hand).ToString("F3") + " m";
		}

		private void PointAtPlayer(NPCAnimation animation, NPCHand hand)
		{
			Transform player = mainscript.M.player.transform;
			if (player == null || !animation.IsHandReady(hand))
				return;

			Vector3 target = player.position + Vector3.up * 0.5f;
			if (!animation.PlayPoint(hand, target))
				_status = "Could not start point gesture for " + hand;
		}
	}
}
