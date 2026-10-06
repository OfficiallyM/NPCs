using NPCs.Common;
using NPCs.Dialogue.Core;
using System.Collections.Generic;
using UnityEngine;

namespace NPCs.Dialogue
{
	public class SpeechRenderer : MonoBehaviour
	{
		private ConversationRunner _runner;
		private WorldspaceDisplay _display;
		private bool _lingering;

		/// <summary>
		/// Whether the player can currently see the speech box.
		/// </summary>
		public bool IsVisibleToPlayer => _display != null && _display.IsOnScreen;

		public void Start()
		{
			_runner = GetComponent<ConversationRunner>();
			// The anchor stays upright when the head turns, so the box doesn't wobble with the gaze.
			_display = _runner.Npc.Body.SpeechAnchor.gameObject.AddComponent<WorldspaceDisplay>();
			_display.SetPosition(Vector3.zero);
			_display.SetFontSize(25);
			_display.SetMaxWidth(600);

			_runner.OnNodeChanged += node => _display.RenderMessage(
				new WorldspaceDisplay.Message(new List<string>() { _runner.ResolveText(node.Text) })
			);
			_runner.OnConversationEnding += node =>
			{
				_lingering = true;
				_display.ClearMessageAfterDelay(_runner.ResolveText(node.Text), 0.03f);
			};

			_runner.OnConversationEnded += () =>
			{
				if (_lingering)
				{
					_lingering = false;
					return;
				}
				_display.ClearMessage();
			};

			_runner.OnBackground += () =>
			{
				ConversationNode node = _runner.CurrentNode;
				if (node == null) return;
				_display.ClearMessageAfterDelay(_runner.ResolveText(node.Text), 0.03f);
			};

			_runner.Npc.OnDeath += () =>
			{
				_display.ClearMessage();
			};
		}
	}
}