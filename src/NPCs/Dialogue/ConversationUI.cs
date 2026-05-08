using NPCs.Dialogue.Core;
using NPCs.Trading;
using NPCs.Utilities;
using NPCs.Utilities.UI;
using UnityEngine;
using Animator = NPCs.Utilities.UI.Animator;

namespace NPCs.Dialogue
{
	/// <summary>
	/// Handles the IMGUI for active conversations.
	/// </summary>
	internal class ConversationUI : MonoBehaviour
	{
		private static ConversationRunner _activeRunner;
		private static bool _uiEnabled;
		private Vector2 _scroll;

		/// <summary>
		/// Whether a conversation is currently active.
		/// </summary>
		public static bool HasActiveConversation => _activeRunner != null;
		public static bool HasBackgroundConversation => _activeRunner != null && !_uiEnabled;

		/// <summary>
		/// Sets the active conversation runner, driving the UI.
		/// </summary>
		/// <param name="runner">The runner to display.</param>
		public static void SetActiveRunner(ConversationRunner runner)
		{
			_activeRunner = runner;
			SetUIState(true);

			// TODO:
			// - Make NPC step back if player is too close.
			// - Make NPC look at player when speaking to them.
		}

		/// <summary>
		/// Clears the active conversation runner and hides the UI.
		/// </summary>
		public static void ClearActiveRunner()
		{
			_activeRunner = null;
			SetUIState(false);
		}

		public static void Show()
		{
			if (!HasActiveConversation) return;
			SetUIState(true);
		}

		public static void Hide()
		{
			SetUIState(false);
		}

		private static void SetUIState(bool state)
		{
			mainscript.M.crsrLocked = !state;
			mainscript.M.SetCursorVisible(state);
			mainscript.M.menu.gameObject.SetActive(!state);

			if (_uiEnabled != state)
			{
				if (state)
					Animator.Play("mainUI", Animator.AnimationState.SlideIn);
				else
					Animator.Play("mainUI", Animator.AnimationState.SlideOut);
			}

			_uiEnabled = state;
		}

		private void Update()
		{
			if (HasActiveConversation && !HasBackgroundConversation && Input.GetButtonDown("Cancel"))
				_activeRunner.EndConversation();
		}

		private void LateUpdate()
		{
			if (HasActiveConversation)
				Time.timeScale = 1f;

			fpscontroller player = mainscript.M.player;
			if (HasActiveConversation && !HasBackgroundConversation)
				return;

			if (Physics.Raycast(mainscript.M.player.Cam.transform.position, mainscript.M.player.Cam.transform.forward, out var hitInfo, mainscript.M.player.FrayRange, (int)mainscript.M.player.useLayer))
			{
				if (!HasBackgroundConversation)
				{
					var runner = hitInfo.transform.GetComponentInParent<ConversationRunner>();
					if (runner != null)
					{
						player.E = $"Speak to {runner.GetVariable("npcName") ?? "Stranger"}";
						player.BcanE = true;

						if (Input.GetKeyDown(KeyCode.E))
						{
							runner.StartConversation();
							return;
						}
					}
				}
				else
				{
					var tradeSession = hitInfo.transform.GetComponentInParent<TradeSession>();
					if (tradeSession != null && tradeSession.IsActive && !hitInfo.collider.name.Contains("Zone"))
					{
						player.E = "Cancel trade";
						player.BcanE = true;

						if (Input.GetKeyDown(KeyCode.E))
						{
							tradeSession.Cancel();

							return;
						}
					}
				}
			}
		}

		private void OnGUI()
		{
			Styling.Bootstrap();
			GUI.skin = Styling.GetSkin();

			float width = Screen.width * 0.7f;
			float height = Screen.height * 0.3f;
			float x = Screen.width / 2 - width / 2;
			float y = Screen.height - height - 20f;
			Rect targetRect = new Rect(x, y, width, height);
			Rect animatedRect = Animator.Slide("mainUI", targetRect, Animator.SlideDirection.Bottom);
			if ((!HasActiveConversation || HasBackgroundConversation) && Animator.IsIdle("mainUI")) return;

			GUILayout.BeginArea(animatedRect, GUIContent.none, "box");
			GUILayout.BeginHorizontal();
			GUILayout.Space(5f);
			GUILayout.Label(_activeRunner?.GetVariable("npcName") ?? "Stranger", "LabelHeader", GUILayout.ExpandWidth(false));
			GUILayout.EndHorizontal();
			GUILayout.Space(10f);
			ConversationNode node = _activeRunner?.CurrentNode;
			bool hasOptions = node?.Options != null && node.Options.Count > 0;

			_scroll = GUILayout.BeginScrollView(_scroll);
			if (node != null)
			{
				if (hasOptions)
				{
					for (int i = 0; i < node.Options.Count; i++)
					{
						GUILayout.BeginHorizontal();
						GUILayout.FlexibleSpace();
						if (GUILayout.Button(_activeRunner.ResolveText(node.Options[i].Text), GUILayout.MaxWidth(width - 40f), GUILayout.MaxHeight(35f)))
							_activeRunner.SelectOption(i);
						GUILayout.FlexibleSpace();
						GUILayout.EndHorizontal();
					}
				}
				else
				{
					GUILayout.BeginHorizontal();
					GUILayout.FlexibleSpace();
					if (GUILayout.Button(node.Next != null ? "Continue..." : "Close", GUILayout.MaxWidth(width - 40f), GUILayout.MaxHeight(35f)))
						_activeRunner.Advance();
					GUILayout.FlexibleSpace();
					GUILayout.EndHorizontal();
				}
			}
			GUILayout.EndScrollView();
			GUILayout.EndArea();

			// Reset back to default Unity skin to avoid styling bleeding to other UI mods.
			GUI.skin = null;
		}
	}
}
