using NPCs.Dialogue;
using NPCs.Enums;
using NPCs.Utilities;
using System.Collections.Generic;
using System.Linq;
using TLDLoader;
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

		private GameObject _goldObj;
		private GameObject _silverObj;
		private Vector3 _spawnPos;

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
			_traderBillboard.OnSelectionChanged += OnSelectionChanged;
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

			_goldObj = itemdatabase.d.items.FirstOrDefault(i => i != null && i.name == "gold");
			_silverObj = NPCs.I.GetItem(100);
			_spawnPos = transform.position + new Vector3(-2.5f, 0, 2.5f);
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
			UpdateProposeLabel();
		}

		private void OnSelectionChanged()
		{
			UpdateProposeLabel();
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

			bool isSell = _playerOfferTotal > 0f && _traderBillboard.SelectedItems.Count == 0;
			if (isSell)
				ResolveSell();
			else
				ResolveTradeProposal();
		}

		private void ResolveSell()
		{
			IsActive = false;

			// Calculate gold and silver bar counts from the total.
			float total = Maths.RoundToNearestHalf(_playerOfferTotal);
			int goldCount = Mathf.FloorToInt(total);
			bool needsSilver = (total - goldCount) >= 0.5f;

			// Remove player items from the world.
			foreach (GameObject item in _playerOffer.Keys)
			{
				if (item != null)
				{
					var data = _playerOffer[item];
					_trader.Inventory.Add(item, data);

					foreach (tosaveitemscript save in item.GetComponentsInChildren<tosaveitemscript>())
						save.removeFromMemory = true;
					Destroy(item);
				}
			}

			for (int i = 0; i < goldCount; i++)
			{
				var spawned = GameObject.Instantiate(_goldObj);
				Vector2 randomCircle = Random.insideUnitCircle * 0.2f;
				spawned.transform.position = _spawnPos + new Vector3(randomCircle.x, 0f, randomCircle.y);
			}

			if (needsSilver)
			{
				var spawned = GameObject.Instantiate(_silverObj);
				Vector2 randomCircle = Random.insideUnitCircle * 0.2f;
				spawned.transform.position = _spawnPos + new Vector3(randomCircle.x, 0f, randomCircle.y);
			}

			_traderBillboard.Hide();
			_playerBillboard.Hide();
			_tradeZone.Hide();
			_tradeZone.Close();

			ConversationUI.Show();
			_runner.AdvanceTo("trade_accepted");
			_runner.ConversationRange = 5f;
		}

		private void ResolveTradeProposal()
		{
			float traderOfferTotal = _traderBillboard.SelectedItems.Values.Sum(e => e.TotalValue);
			bool accepted = EvaluateProposal(_playerOfferTotal, traderOfferTotal);

			if (accepted)
				ResolveAccepted();
			else
				ResolveRejected();
		}

		private bool EvaluateProposal(float playerOffer, float traderOffer)
		{
			if (traderOffer <= 0f) return true;
			float effectiveOffer = IsAllCurrency() ? playerOffer * 1.05f : playerOffer;
			return effectiveOffer >= traderOffer * _trader.Personality.MinimumDealThreshold;
		}

		private bool IsAllCurrency()
		{
			if (_playerOffer.Count == 0) return false;
			return _playerOffer.Values.All(data => data.Category == ItemCategory.Currency);
		}

		private void ResolveAccepted()
		{
			IsActive = false;

			float traderOfferTotal = _traderBillboard.SelectedItems.Values.Sum(e => e.TotalValue);
			float overpayment = Maths.RoundToNearestHalf(_playerOfferTotal - traderOfferTotal);

			if (overpayment >= 0.5f)
			{
				int goldChange = Mathf.FloorToInt(overpayment);
				bool silverChange = (overpayment - goldChange) >= 0.5f;

				for (int i = 0; i < goldChange; i++)
				{
					var spawned = GameObject.Instantiate(_goldObj);
					Vector2 randomCircle = Random.insideUnitCircle * 2.5f;
					spawned.transform.position = _spawnPos + new Vector3(randomCircle.x, 0f, randomCircle.y);
				}

				if (silverChange)
				{
					var spawned = GameObject.Instantiate(_silverObj);
					Vector2 randomCircle = Random.insideUnitCircle * 2.5f;
					spawned.transform.position = _spawnPos + new Vector3(randomCircle.x, 0f, randomCircle.y);
				}
			}

			// Spawn the correct quantity of each selected trader item.
			foreach (var entry in _traderBillboard.SelectedItems)
			{
				GameObject prefab = entry.Key;
				int quantity = entry.Value.Quantity;

				for (int i = 0; i < quantity; i++)
				{
					var spawned = GameObject.Instantiate(prefab);
					Vector2 randomCircle = Random.insideUnitCircle * 2.5f;
					spawned.transform.position = _spawnPos + new Vector3(randomCircle.x, 0f, randomCircle.y);
				}

				_trader.Inventory.Remove(prefab, quantity);
			}

			// Remove player items from the world.
			foreach (GameObject item in _playerOffer.Keys)
			{
				if (item != null)
				{
					var data = _playerOffer[item];
					_trader.Inventory.Add(item, data);
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
			_runner.ConversationRange = 5f;
		}

		private void ResolveRejected()
		{
			// Leave everything in place, let the player adjust and try again.
			_runner.AdvanceTo("trade_rejected");
		}

		private void UpdateProposeLabel()
		{
			bool isSell = _playerOfferTotal > 0f && _traderBillboard.SelectedItems.Count == 0;

			if (isSell)
				_playerBillboard.SetProposeLabel($"Sell for {Maths.RoundToNearestHalf(_playerOfferTotal)}g");
			else
				_playerBillboard.SetProposeLabel("Propose trade");
		}
	}
}