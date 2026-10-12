using NPCs.Animation;
using NPCs.Animation.Enums;
using NPCs.Core;
using NPCs.Utilities;
using UnityEngine;

namespace NPCs.Debugging
{
    [DefaultExecutionOrder(-39)]
    [DisallowMultipleComponent]
    public sealed class NpcIkDebugger : MonoBehaviour
    {
        private const float SearchInterval = 0.75f;

        private NPC _npc;
        private float _nextSearchTime;
        private string _status = "Searching for an NPC";
        private Vector2 _scroll;

        public static NpcIkDebugger Create()
        {
            NpcIkDebugger existing = FindObjectOfType<NpcIkDebugger>();
            if (existing != null)
                return existing;

            GameObject debuggerObject = new GameObject("NPC Animation Debugger");
            DontDestroyOnLoad(debuggerObject);
            return debuggerObject.AddComponent<NpcIkDebugger>();
        }

        private void Update()
        {
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
            Transform origin = ResolveSearchOrigin();
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
            Logging.LogDebug("[NPCs Animation Debugger] " + _status);
        }

        private Transform ResolveSearchOrigin()
        {
            if (mainscript.M != null && mainscript.M.player != null)
                return mainscript.M.player.transform;

            if (Camera.main != null)
                return Camera.main.transform;

            return transform;
        }

        private void LateUpdate()
        {
            if (_npc == null || _npc.Animation == null)
                return;

            DrawArm(NPCHand.Left, Color.cyan);
            DrawArm(NPCHand.Right, Color.magenta);
        }

        private void DrawArm(NPCHand hand, Color color)
        {
            NPCAnimation animation = _npc.Animation;
            Transform upper = animation.GetDebugUpperArm(hand);
            Transform lower = animation.GetDebugLowerArm(hand);
            Transform endpoint = animation.GetDebugHand(hand);

            if (upper == null || lower == null || endpoint == null)
                return;

            Debug.DrawLine(upper.position, lower.position, color);
            Debug.DrawLine(lower.position, endpoint.position, color);

            if (animation.GetCurrentGesture(hand) != NPCGesture.None && animation.GetGestureWeight(hand) > 0f)
            {
                Vector3 target = animation.GetHandTarget(hand);
                Debug.DrawLine(upper.position, target, Color.green);
                Debug.DrawLine(endpoint.position, target, Color.yellow);
            }
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(12f, 12f, 370f, 390f), GUI.skin.box);
            _scroll = GUILayout.BeginScrollView(_scroll);
            GUILayout.Label("NPC Animation Debugger");
            GUILayout.Label("Status: " + _status);
            GUILayout.Label("NPC: " + (_npc != null ? _npc.name : "Not found"));

            if (_npc != null && _npc.Animation != null)
            {
                NPCAnimation animation = _npc.Animation;
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

                if (GUILayout.Button("Rebind Nearest NPC"))
                {
                    _npc = null;
                    _nextSearchTime = 0f;
                }
            }
            else
            {
                GUILayout.Label("Waiting for a living NPC with at least one bound arm.");
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
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
            Transform player = ResolveSearchOrigin();
            if (player == null || !animation.IsHandReady(hand))
                return;

            Vector3 target = player.position + Vector3.up * 0.5f;
            if (!animation.PlayPoint(hand, target))
                _status = "Could not start point gesture for " + hand;
        }
    }
}
