using MLib.UI;
using NPCs.AI;
using NPCs.AI.Enums;
using NPCs.AI.Routes;
using NPCs.Animation;
using NPCs.Animation.Enums;
using NPCs.Animation.Models;
using NPCs.Core;
using NPCs.Utilities;
using System.Collections.Generic;
using UnityEngine;

namespace NPCs.Debugging
{
	[DefaultExecutionOrder(-39)]
	[DisallowMultipleComponent]
	public sealed class NPCDebugger : MonoBehaviour
	{
		private const float SearchInterval = 0.75f;

		// Width of the name column in slider rows.
		private const float SliderLabelWidth = 160f;

		private NPC _npc;
		private float _nextSearchTime;
		private string _status = "Searching for an NPC";
		private readonly ScrollState _scroll = new ScrollState();
		private bool _show = false;
		private bool _unlocked = false;
		private Rect _windowRect = new Rect(12f, 12f, 500f, 700f);
		private WindowOptions _windowOptions;

		// Far enough ahead that "Walk forward" keeps going for as long as the gait needs watching.
		private const float ForwardDistance = 200f;

		private static readonly string[] _speedLabels = { "Walk", "Walk fast", "Jog", "Run" };

		private NPCSpeed _speed = NPCSpeed.Walk;

		private static readonly string[] _directionLabels = { "Forwards", "Backwards" };

		// Points are kept as global positions so they stay put when the game shifts the world.
		private readonly List<GlobalPoint> _points = new List<GlobalPoint>();
		private RouteDirection _direction = RouteDirection.Forwards;

		// Starts as the NPC type's own lane so the slider shows what it would really do.
		private float _laneOffset = float.NaN;

		public static NPCDebugger Create()
		{
			NPCDebugger existing = FindObjectOfType<NPCDebugger>();
			if (existing != null)
				return existing;

			GameObject debuggerObject = new GameObject("NPC Debugger");
			DontDestroyOnLoad(debuggerObject);
			return debuggerObject.AddComponent<NPCDebugger>();
		}

		private void Awake()
		{
			// The close button runs the same logic as F8 so the cursor and menu are restored.
			_windowOptions = new WindowOptions
			{
				OnClose = () => Toggle(false),
			};
		}

		private void Update()
		{
			if (Input.GetKeyDown(KeyCode.F8))
				Toggle();

			if (!_show) return;

			if (!mainscript.M.menu.Menu.activeSelf && Input.GetButtonDown("Cancel"))
				Toggle(false);

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

		private void Toggle(bool? forcedState = null)
		{
			// Something else has control of the cursor, don't toggle the UI.
			if (!_show && !mainscript.M.crsrLocked)
				return;

			if (forcedState.HasValue)
				_show = forcedState.Value;
			else
			{
				if (_unlocked)
				{
					SetCursorState(true);
					_unlocked = false;
					return;
				}

				_show = !_show;
			}
			_unlocked = false;

			SetCursorState(_show);
		}

		private void SetCursorState(bool isActive)
		{
			mainscript.M.crsrLocked = !isActive;
			mainscript.M.SetCursorVisible(isActive);
			mainscript.M.menu.gameObject.SetActive(!isActive);
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
			_status = $"Bound to {nearest.name} at {nearestDistance:F2} m";
			Logging.LogDebug($"[NPCs Debugger] {_status}");
		}

		private void UnbindNpc()
		{
			StopWalking();
			_npc = null;
		}

		private void OnGUI()
		{
			if (!_show)
				return;

			using (Ui ui = Ui.Window(ref _windowRect, "NPC Debugger", _windowOptions))
			{
				DrawWindow(ui);
			}
		}

		private void DrawWindow(Ui ui)
		{
			if (ui.Button($"{(_unlocked ? "Lock" : "Unlock")} cursor", ButtonStyle.Secondary))
			{
				SetCursorState(false);
				_unlocked = true;
			}

			ui.Label($"Status: {_status}", wrap: true);
			ui.Label($"NPC: {(_npc != null ? _npc.name : "Not found")}");
			ui.Label($"Gender: {(_npc?.Gender.ToString() ?? "Not found")}");
			ui.Divider();

			using (Ui list = ui.Scroll(_scroll))
			{
				if (_npc == null || _npc.Animation == null)
				{
					list.Label("Waiting for a living NPC.");
					return;
				}

				if (list.Button("Rebind Nearest NPC"))
				{
					_npc = null;
					_nextSearchTime = 0f;
					return;
				}

				NPCAnimation animation = _npc.Animation;
				DrawLocomotionControls(list);
				DrawRouteControls(list);
				DrawGestureControls(list, animation);
				DrawLegControls(list, animation);
			}
		}

		private void DrawGestureControls(Ui ui, NPCAnimation animation)
		{
			ui.Space(8f);
			ui.SubHeader("Gestures");
			ui.Label($"Left arm: {DescribeHand(animation, NPCHand.Left)}", size: 13, wrap: true);
			ui.Label($"Right arm: {DescribeHand(animation, NPCHand.Right)}", size: 13, wrap: true);

			using (Ui row = ui.Columns(Ui.ControlHeight, 1f, 1f))
			{
				if (row.Button("Wave Left") && animation.IsHandReady(NPCHand.Left))
					animation.PlayGesture(NPCGesture.Wave, NPCHand.Left);

				if (row.Button("Wave Right") && animation.IsHandReady(NPCHand.Right))
					animation.PlayGesture(NPCGesture.Wave, NPCHand.Right);
			}

			if (ui.Button("Wave Both Hands"))
			{
				animation.PlayGesture(NPCGesture.Wave, NPCHand.Left);
				animation.PlayGesture(NPCGesture.Wave, NPCHand.Right);
			}

			using (Ui row = ui.Columns(Ui.ControlHeight, 1f, 1f))
			{
				if (row.Button("Point Left at Player"))
					PointAtPlayer(animation, NPCHand.Left);

				if (row.Button("Point Right at Player"))
					PointAtPlayer(animation, NPCHand.Right);
			}

			using (Ui row = ui.Columns(Ui.ControlHeight, 1f, 1f))
			{
				if (row.Button("Stop Left", ButtonStyle.Secondary))
					animation.StopGesture(NPCHand.Left);

				if (row.Button("Stop Right", ButtonStyle.Secondary))
					animation.StopGesture(NPCHand.Right);
			}

			if (ui.Button("Stop Both Gestures", ButtonStyle.Secondary))
				animation.StopGesture();
		}

		private void DrawLegControls(Ui ui, NPCAnimation animation)
		{
			ui.Space(8f);
			ui.SubHeader("Legs");
			NPCLegGait legs = animation.Legs;
			ui.Space(6f);

			if (legs == null)
			{
				ui.Label("Legs: unbound");
				return;
			}

			ui.Label($"Speed {legs.Speed:F2} m/s, step {legs.StepLength:F2} m, phase {legs.Phase:F2}, pelvis drop {legs.PelvisDrop:F3} m", size: 13, wrap: true);
			ui.Label($"Leg rest extension {legs.RestExtension:F3} (1 is straight), ankle height {legs.AnkleHeight:F3} m, torso {(legs.HasTorso ? "bound" : "UNBOUND")}", size: 13, wrap: true);
			ui.Label($"Left foot: {DescribeFoot(legs.Left)}", size: 13, wrap: true);
			ui.Label($"Right foot: {DescribeFoot(legs.Right)}", size: 13, wrap: true);

			DrawTuning(ui, animation.Profile);
		}

		// Edits the live profile, which the gait reads every frame, so changes show straight away.
		private static void DrawTuning(Ui ui, NPCAnimationProfile profile)
		{
			ui.Space(8f);
			ui.SubHeader("IK Tuning");
			profile.MaxLegExtension = Slider(ui, "Max leg extension", profile.MaxLegExtension, 0.85f, 1f);
			profile.PelvisStandDrop = Slider(ui, "Pelvis stand drop", profile.PelvisStandDrop, -0.05f, 0.1f);
			profile.StepWidthScale = Slider(ui, "Step width scale", profile.StepWidthScale, 0.3f, 1.3f);
			profile.StepLengthBase = Slider(ui, "Step length base", profile.StepLengthBase, 0f, 0.8f);
			profile.StepLengthPerSpeed = Slider(ui, "Step length per m/s", profile.StepLengthPerSpeed, 0f, 0.6f);
			profile.StepLengthMax = Slider(ui, "Step length max", profile.StepLengthMax, 0.3f, 1.5f);
			profile.LiftSlow = Slider(ui, "Foot lift slow", profile.LiftSlow, 0f, 0.3f);
			profile.LiftFast = Slider(ui, "Foot lift fast", profile.LiftFast, 0f, 0.4f);
			profile.PelvisBobSlow = Slider(ui, "Pelvis bob slow", profile.PelvisBobSlow, 0f, 0.1f);
			profile.PelvisBobFast = Slider(ui, "Pelvis bob fast", profile.PelvisBobFast, 0f, 0.15f);
			profile.PelvisYaw = Slider(ui, "Pelvis yaw", profile.PelvisYaw, 0f, 15f);
			profile.ShoulderYaw = Slider(ui, "Shoulder yaw", profile.ShoulderYaw, 0f, 20f);
			profile.PelvisRoll = Slider(ui, "Pelvis roll", profile.PelvisRoll, 0f, 12f);
			profile.TorsoRollCompensation = Slider(ui, "Torso roll compensation", profile.TorsoRollCompensation, 0f, 1.5f);
			profile.PelvisSway = Slider(ui, "Pelvis sway", profile.PelvisSway, 0f, 0.08f);
			profile.TorsoFastScale = Slider(ui, "Torso fast scale", profile.TorsoFastScale, 0.5f, 3f);
			profile.ArmSwingSlow = Slider(ui, "Arm swing slow", profile.ArmSwingSlow, 0f, 70f);
			profile.ArmSwingFast = Slider(ui, "Arm swing fast", profile.ArmSwingFast, 0f, 90f);
			profile.ArmElbowFlexSlow = Slider(ui, "Elbow flex slow", profile.ArmElbowFlexSlow, 0f, 70f);
			profile.ArmElbowFlexFast = Slider(ui, "Elbow flex fast", profile.ArmElbowFlexFast, 0f, 90f);
		}

		// A name on the left, then the slider with its value on the right.
		private static float Slider(Ui ui, string label, float value, float min, float max)
		{
			using (Ui row = ui.Row())
			{
				row.Label(label, size: 13, width: SliderLabelWidth);
				return row.Slider(value, min, max, "0.000");
			}
		}

		private static string DescribeFoot(NPCFootState foot)
		{
			return $"{(foot.Swinging ? "swing" : "planted")}, ground {(foot.GroundHit ? "found" : "MISSING")}, solver {(foot.SolveSucceeded ? "OK" : "idle")}, error {Vector3.Distance(foot.Position, foot.Target):F3} m";
		}

		private void DrawLocomotionControls(Ui ui)
		{
			ui.Space(8f);
			ui.SubHeader("Locomotion");
			NPCLocomotion locomotion = _npc.Ai?.Locomotion;
			ui.Space(6f);

			if (locomotion == null)
			{
				ui.Label("Locomotion: unavailable");
				return;
			}

			ui.Label($"Locomotion: {(locomotion.HasDestination ? $"heading to {locomotion.Destination:F1}" : "no destination")}, speed {locomotion.CurrentSpeed:F2} m/s", size: 13, wrap: true);

			NPCSpeed selected = (NPCSpeed)ui.Tabs((int)_speed, _speedLabels);
			if (selected != _speed)
			{
				_speed = selected;
				locomotion.SetSpeed(_speed);
				_npc.Ai.SetTravelSpeed(_speed);
			}

			using (Ui row = ui.Columns(Ui.ControlHeight, 1f, 1f, 1f))
			{
				if (row.Button("Walk here"))
					WalkToPlayer();

				if (row.Button("Walk forward"))
					WalkForward();

				if (row.Button("Stop", ButtonStyle.Danger))
					StopWalking();
			}
		}

		private void DrawRouteControls(Ui ui)
		{
			ui.Space(8f);
			ui.SubHeader("Route");
			NPCAi ai = _npc.Ai;
			if (ai == null)
				return;

			if (float.IsNaN(_laneOffset))
				_laneOffset = ai.RoadLaneOffset;

			ui.Space(6f);

			ui.Label("State: " + ai.DebugSummary, size: 13, wrap: true);
			ui.Label("Player " + PlayerMotion.Speed.ToString("F1") + " m/s, " + ai.DistanceToPlayer.ToString("F0") + " m away", size: 13, wrap: true);

			NPCRouteFollower follower = ai.Follower;
			ui.Label($"Route: {(follower != null ? $"{follower.Status}, target {follower.TargetDistance:F1} m away" : "not travelling")}", size: 13, wrap: true);

			_direction = (RouteDirection)ui.Tabs((int)_direction, _directionLabels);
			_laneOffset = Slider(ui, "Lane offset (right of travel)", _laneOffset, -12f, 12f);

			if (ui.Button("Follow road"))
				FollowRoad();

			ui.Label($"Points: {_points.Count}");
			using (Ui row = ui.Columns(Ui.ControlHeight, 1f, 1f, 1f))
			{
				if (row.Button("Add point here"))
					_points.Add(GlobalPoint.FromWorld(mainscript.M.player.transform.position));

				if (row.Button("Clear points", ButtonStyle.Secondary))
					_points.Clear();

				if (row.Button("Walk points"))
					WalkPoints();
			}
		}

		private void FollowRoad()
		{
			if (!RoadRoute.TryCreateNear(_npc.transform.position, _direction, _laneOffset, out RoadRoute route))
			{
				_status = "No placed road piece found near the NPC";
				return;
			}

			_npc.Ai.Travel(route, _speed);
			_status = $"Following the road {_direction}";
		}

		private void WalkPoints()
		{
			if (_points.Count == 0)
			{
				_status = "Add at least one point first";
				return;
			}

			_npc.Ai.Travel(new PointRoute(_points), _speed);
			_status = $"Walking {_points.Count} points";
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

			// A route that's waiting for road to load has no destination but is still travelling.
			if (_npc.Ai.Locomotion.HasDestination || _npc.Ai.Follower != null)
				_npc.Ai.Rest();
		}

		private static string DescribeHand(NPCAnimation animation, NPCHand hand)
		{
			if (!animation.IsHandReady(hand))
				return "unbound";

			return $"{animation.GetCurrentGesture(hand)}, weight {animation.GetGestureWeight(hand):F2}, solver {(animation.GetLastSolveSucceeded(hand) ? "OK" : "idle")}, error {animation.GetLastHandError(hand):F3} m";
		}

		private void PointAtPlayer(NPCAnimation animation, NPCHand hand)
		{
			Transform player = mainscript.M.player.transform;
			if (player == null || !animation.IsHandReady(hand))
				return;

			Vector3 target = player.position + Vector3.up * 0.5f;
			if (!animation.PlayPoint(hand, target))
				_status = $"Could not start point gesture for {hand}";
		}
	}
}