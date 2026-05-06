using NPCs.Dialogue;
using NPCs.Utilities;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NPCs.Trading
{
	/// <summary>
	/// Manages the state of an active trade between the player and a trader.
	/// Owned by the trader, activated via the open_trade dialogue action.
	/// </summary>
	internal class TradeSession : MonoBehaviour
	{
		private Trader _trader;
		private TraderBillboard _traderBillboard;
		private PlayerBillboard _playerBillboard;
		private TradeZone _tradeZone;
		private ConversationRunner _runner;

		// Player's current offer — items in trade zone plus any pointer-offered vehicles.
		private Dictionary<GameObject, ItemData> _playerOffer = new Dictionary<GameObject, ItemData>();
		private float _playerOfferTotal = 0f;

		// TODO:
		// - Add support for multiple of the same trader item. Needs to roll quantity in inventory and change to +/- buttons.
		// - Add option for raw gold/silver from trader.
		// - Check trader personality rolls / margin / deal threshold are working.

		/// <summary>
		/// Whether a trade session is currently active.
		/// </summary>
		public bool IsActive { get; private set; }

		public void Init()
		{
			_trader = GetComponent<Trader>();
			_runner = GetComponent<ConversationRunner>();

			// Set up billboards.
			_traderBillboard = gameObject.AddComponent<TraderBillboard>();
			_traderBillboard.Initialise(_trader.Inventory, _trader.Personality, GetPerceivedValue);
			_traderBillboard.Hide();

			// Set up player billboard.
			_playerBillboard = gameObject.AddComponent<PlayerBillboard>();
			_playerBillboard.Initialise();
			_playerBillboard.OnProposed += OnProposed;
			_playerBillboard.Hide();

			// Set up trade zone.
			_tradeZone = gameObject.AddComponent<TradeZone>();
			_tradeZone.OnItemsChanged += OnPlayerOfferChanged;
			_tradeZone.Hide();

			_runner.OnConversationEnded += OnConversationEnd;
		}

		/// <summary>
		/// Opens the trade session.
		/// </summary>
		public void Begin()
		{
			if (IsActive) return;

			IsActive = true;
			_playerOffer.Clear();
			_playerOfferTotal = 0f;

			_traderBillboard.Build();
			_traderBillboard.Show();
			_playerBillboard.Build(_playerOffer, 0f);
			_playerBillboard.Show();
			_tradeZone.Open();
			_tradeZone.Show();

			_runner.ConversationRange = 15f;
		}

		/// <summary>
		/// Closes the trade session without completing a trade.
		/// Returns trader items to inventory and leaves player items in the world.
		/// </summary>
		public void Cancel()
		{
			if (!IsActive) return;

			IsActive = false;

			_traderBillboard.Hide();
			_playerBillboard.Hide();
			_tradeZone.Hide();

			// Return to dialogue.
			ConversationUI.Show();
			_runner.AdvanceTo("trade_cancelled");
			_runner.ConversationRange = 5f;
		}

		public float GetPerceivedValue(ItemData data)
		{
			if (data == null) return 0f;

			float fluctuation = _trader.Personality.ItemFluctuation.TryGetValue(data.Category, out float f) ? f : 0f;
			return Mathf.Max(0.5f, Maths.RoundToNearestHalf(data.Value * (1f + fluctuation)));
		}

		private void OnPlayerOfferChanged(Dictionary<GameObject, ItemData> items)
		{
			_playerOffer = items;
			_playerOfferTotal = items.Values.Sum(data => data.Value);
			_playerBillboard.Build(_playerOffer, Maths.RoundToNearestHalf(_playerOfferTotal));
		}

		private void OnConversationEnd()
		{
			Cancel();
		}

		private void OnProposed()
		{
			if (_playerOfferTotal == 0)
			{
				_runner.AdvanceTo("trade_empty");
				return;
			}

			float traderOfferTotal = _traderBillboard.SelectedItems.Values.Sum();
			bool accepted = EvaluateProposal(_playerOfferTotal, traderOfferTotal);

			if (accepted)
				ResolveAccepted();
			else
				ResolveRejected();
		}

		private bool EvaluateProposal(float playerOffer, float traderOffer)
		{
			if (traderOffer <= 0f) return true;
			return playerOffer >= traderOffer * _trader.Personality.MinimumDealThreshold;
		}

		private void ResolveAccepted()
		{
			IsActive = false;

			// Hand trader items to the player — spawn near the player.
			Vector3 spawnPos = mainscript.M.player.transform.position + mainscript.M.player.transform.forward * 1.5f;
			foreach (GameObject item in _traderBillboard.SelectedItems.Keys)
			{
				var spawned = GameObject.Instantiate(item);
				spawned.transform.position = spawnPos;
				_trader.Inventory.Remove(item);
			}

			// Remove player items from the world.
			foreach (GameObject item in _playerOffer.Keys)
			{
				if (item != null)
				{
					foreach (tosaveitemscript save in item.GetComponentsInChildren<tosaveitemscript>())
						save.removeFromMemory = true;
					Destroy(item);
				}
			}

			_traderBillboard.Hide();
			_playerBillboard.Hide();
			_tradeZone.Hide();
			_tradeZone.Close();

			// Return to dialogue at the accepted node.
			ConversationUI.Show();
			_runner.AdvanceTo("trade_accepted");
		}

		private void ResolveRejected()
		{
			// Leave everything in place, let the player adjust and try again.
			_runner.AdvanceTo("trade_rejected");
		}

		//private void Update()
		//{
		//	if (!IsActive) return;

		//	if (Input.GetButtonDown("Cancel"))
		//		Cancel();
		//}
	}
}