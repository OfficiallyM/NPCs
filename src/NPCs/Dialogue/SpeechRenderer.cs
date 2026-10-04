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
			Transform spine = transform.Find("munkas01/Default simplified/root/spine05/spine04/spine03/spine02/spine01");
			_display = spine.gameObject.AddComponent<WorldspaceDisplay>();
			_display.SetPosition(new Vector3(0, 0.5f, -0.1f));
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