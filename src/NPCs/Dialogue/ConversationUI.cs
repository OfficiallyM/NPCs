using NPCs.Dialogue.Core;
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
		private Vector2 _scroll;

		/// <summary>
		/// Whether a conversation is currently active.
		/// </summary>
		public static bool HasActiveConversation => _activeRunner != null;

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

		private static void SetUIState(bool state)
		{
			mainscript.M.crsrLocked = !state;
			mainscript.M.SetCursorVisible(state);
			mainscript.M.menu.gameObject.SetActive(!state);

			if (state)
				Animator.Play("mainUI", Animator.AnimationState.SlideIn);
			else
				Animator.Play("mainUI", Animator.AnimationState.SlideOut);
		}

		private void Update()
		{
			if (HasActiveConversation && Input.GetButtonDown("Cancel"))
				_activeRunner.EndConversation();
		}

		private void LateUpdate()
		{
			if (HasActiveConversation)
				Time.timeScale = 1f;
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
			if (!HasActiveConversation && Animator.IsIdle("mainUI")) return;

			GUILayout.BeginArea(animatedRect, GUIContent.none, "box");
			GUILayout.BeginHorizontal();
			GUILayout.Space(5f);
			GUILayout.Label("Trader", "LabelHeader", GUILayout.ExpandWidth(false));
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
						if (GUILayout.Button(node.Options[i].Text, GUILayout.MaxWidth(width - 40f), GUILayout.MaxHeight(35f)))
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
