using MLib.UI;
using NPCs.Core;
using NPCs.Dialogue.Models;
using NPCs.Trading;
using UnityEngine;
using Animator = MLib.UI.Animator;

namespace NPCs.Dialogue
{
	/// <summary>
	/// Handles the IMGUI for active conversations.
	/// </summary>
	internal class ConversationUI : MonoBehaviour
	{
		private const string AnimationKey = "ConversationUI";

		private static ConversationRunner _activeRunner;
		private static SpeechRenderer _activeSpeech;
		private static bool _uiEnabled;
		private static string _lastName;
		private readonly ScrollState _scroll = new ScrollState();
		private WindowOptions _windowOptions;

		/// <summary>
		/// Whether a conversation is currently active.
		/// </summary>
		public static bool HasActiveConversation => _activeRunner != null;
		public static bool HasBackgroundConversation => _activeRunner != null && !_uiEnabled;

		public static ConversationRunner ActiveRunner => _activeRunner;

		private void Awake()
		{
			_windowOptions = WindowOptions.Static();
			_windowOptions.AnimationKey = AnimationKey;
			_windowOptions.SlideDirection = Animator.SlideDirection.Bottom;
			Animator.SetVisible(AnimationKey, _uiEnabled, animate: false);
		}

		/// <summary>
		/// Sets the active conversation runner, driving the UI.
		/// </summary>
		/// <param name="runner">The runner to display.</param>
		public static void SetActiveRunner(ConversationRunner runner)
		{
			_activeRunner = runner;
			_activeSpeech = runner.GetComponent<SpeechRenderer>();
			SetUIState(true);

			// TODO: Make NPC step back if player is too close.
		}

		/// <summary>
		/// Clears the active conversation runner and hides the UI.
		/// </summary>
		public static void ClearActiveRunner()
		{
			_activeRunner = null;
			_activeSpeech = null;
			SetUIState(false);
		}

		public static void Hide()
		{
			SetUIState(false);
		}

		private static void SetUIState(bool state)
		{
			if (state)
				_lastName = _activeRunner?.GetVariable("npcName") ?? "Stranger";

			mainscript.M.crsrLocked = !state;
			mainscript.M.SetCursorVisible(state);
			mainscript.M.menu.gameObject.SetActive(!state);
			_uiEnabled = state;
			Animator.SetVisible(AnimationKey, _uiEnabled);
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
				if (hitInfo.transform.GetComponentInParent<NPC>()?.IsDead ?? false)
					return;

				var runner = hitInfo.transform.GetComponentInParent<ConversationRunner>();
				if (runner != null && runner != _activeRunner)
				{
					player.E = $"Speak to {runner.GetVariable("npcName") ?? "Stranger"}";
					player.BcanE = true;

					if (Input.GetKeyDown(KeyCode.E))
					{
						runner.StartConversation();
						return;
					}
				}
				else if (HasBackgroundConversation)
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
			float width = Screen.width * 0.7f;
			float height = Screen.height * 0.3f;
			float x = Screen.width / 2 - width / 2;
			float y = Screen.height - height - 20f;
			Rect rect = new Rect(x, y, width, height);
			if (!_uiEnabled && Animator.IsHidden(AnimationKey)) return;

			// Clicks are acted on once the layout is done, because they can end the conversation and clear the active runner mid-draw.
			ConversationRunner runner = _activeRunner;
			int selectedOption = -1;
			bool advance = false;

			using (Ui ui = Ui.StaticWindow(rect, "Conversation", _windowOptions))
			{
				ui.Header(_lastName);
				ui.Divider();
				ui.Space(10);

				ConversationNode node = runner?.CurrentNode;
				if (node == null) return;
				bool hasOptions = node?.Options != null && node.Options.Count > 0;

				using (Ui box = ui.Scroll(_scroll))
				{
					// Fall back to showing the NPC's line here when the player can't see the speech box above their head.
					if (_activeSpeech != null && !_activeSpeech.IsVisibleToPlayer && !string.IsNullOrEmpty(runner?.CurrentText))
					{
						box.Label(runner.ResolveText(runner.CurrentText));
						box.Space(5);
					}

					if (hasOptions)
					{
						for (int i = 0; i < node.Options.Count; i++)
						{
							if (!runner.IsOptionAvailable(node.Options[i])) continue;

							if (box.Button(runner.ResolveText(node.Options[i].Text)))
								selectedOption = i;
						}
					}
					else
					{
						if (box.Button("Continue..."))
							advance = true;
					}
				}
			}

			if (selectedOption >= 0)
				runner.SelectOption(selectedOption);
			else if (advance)
				runner.Advance();
		}
	}
}