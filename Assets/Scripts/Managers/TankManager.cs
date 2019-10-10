using System;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Tanks.Data;
using Tanks.Analytics;
using TanksNetworkPlayer = Tanks.Networking.NetworkPlayer;
using TanksNetworkManager = Tanks.Networking.NetworkManager;

namespace Tanks.TankControllers
{
	/// <summary>
	/// This class is to manage various settings on a tank.
	/// It works with the GameManager class to control how the tanks behave
	/// and whether or not players have control of their tank in the
	/// different phases of the game.
	/// </summary>
	[RequireComponent(typeof(TankMovement))]
	[RequireComponent(typeof(TankShooting))]
	[RequireComponent(typeof(TankHealth))]
	public class TankManager : NetworkBehaviour
	{
		#region Fields

		//Current spawn point used
		private Transform m_AssignedSpawnPoint;

		//Synced player ID, -1 means it has not changed yet (as the lowest valid player id is 0)
		[SyncVar(hook = "OnPlayerIdChanged")]
		protected int m_PlayerId = -1;

		//Synced score
		[SyncVar]
		protected int m_Score = 0;

		//Synced rank, used at the end of the game to calculate the player's award
		[SyncVar(hook = "OnRankChanged")]
		protected int m_Rank = -1;

		#endregion


		#region Events

		//Fired when the pickup is collected
		public event Action<string> onPickupCollected;
		
		//Fired when the round currency changes
		public event Action<int> onCurrencyChanged;

		//Fired when the player's rank has changed
		public event Action rankChanged;

		//Fired when the player's award currency has changed
		public event Action awardCurrencyChanged;

		#endregion


		#region Properties

		public TanksNetworkPlayer player
		{
			get;
			protected set;
		}

		public TankMovement movement
		{
			get;
			protected set;
		}

		public TankShooting shooting
		{
			get;
			protected set;
		}

		public TankHealth health
		{
			get;
			protected set;
		}

		public TankDisplay display
		{
			get;
			protected set;
		}

		public Color playerColor
		{
			get { return player.color; }
		}

		public string playerName
		{
			get { return player.playerName; }
		}

		public int playerNumber
		{
//Return the local player ID instead
//			get { return player.playerId; }
			get { return m_PlayerId; }
		}

		public int score
		{
			get { return m_Score; }
		}

		public int rank
		{
			get { return m_Rank; }
		}

		public TankTypeDefinition playerTankType
		{
			get;
			protected set;
		}

		public bool removedTank
		{
			get;
			private set;
		}

		public bool ready
		{
			get { return player.ready; }
		}

		public bool initialized
		{
			get;
			private set;
		}

		//Sync currency for a specific round
		[SyncVar(hook = "OnRoundCurrencyChanged")]
		protected int m_RoundCurrencyCollected = 0;

		public int roundCurrencyCollected
		{
			get
			{
				return m_RoundCurrencyCollected;
			}
		}

		//Synced currency for end of game
		[SyncVar(hook = "OnAwardCurrencyChanged")]
		protected int m_AwardCurrency = 0;

		public int awardCurrency
		{
			get
			{
				return m_AwardCurrency;
			}
		}

		#endregion


		#region Methods

		public override void OnStartClient()
		{
			base.OnStartClient();

			if (!initialized && m_PlayerId >= 0)
			{
				Initialize();
			}
		}


		private void Initialize()
		{
			Initialize(TanksNetworkManager.s_Instance.GetPlayerById(m_PlayerId));
		}

		/// <summary>
		/// Set up this tank with the correct properties
		/// </summary>
		private void Initialize(TanksNetworkPlayer player)
		{
			if (initialized)
			{
				return;
			}

			initialized = true;

			this.player = player;
			playerTankType = TankLibrary.s_Instance.GetTankDataForIndex(player.tankType);

			// Create visual tank
			GameObject tankDisplay = (GameObject)Instantiate(playerTankType.displayPrefab, transform.position, transform.rotation);
			tankDisplay.transform.SetParent(transform, true);

			// Analytics messages on server
			if (isServer)
			{
				AnalyticsHelper.PlayerUsedTankInGame(playerTankType.id);

				TankDecorationDefinition itemData = TankDecorationLibrary.s_Instance.GetDecorationForIndex(player.tankDecoration);
				if (itemData.id != "none")
				{
					AnalyticsHelper.PlayerUsedDecorationInGame(itemData.id);
				}
			}

			// Get references to the components.
			display = tankDisplay.GetComponent<TankDisplay>();
			movement = GetComponent<TankMovement>();
			shooting = GetComponent<TankShooting>();
			health = GetComponent<TankHealth>();

			// Initialize components
			movement.Init(this);
			shooting.Init(this);
			health.Init(this);
			display.Init(this);

			GameManager.AddTank(this);

			if (player.hasAuthority)
			{
				DisableShooting();
				player.CmdSetReady();
			}
		}

		public override void OnNetworkDestroy()
		{
			base.OnNetworkDestroy();
			if (player != null)
			{
				player.tank = null;
			}

			GameManager.s_Instance.RemoveTank(this);
		}

		public void DisableShooting()
		{
			shooting.enabled = false;
			shooting.canShoot = false;
		}

		// Used during the phases of the game where the player shouldn't be able to control their tank.
		public void DisableControl()
		{
			movement.DisableMovement();
			shooting.enabled = false;
			shooting.canShoot = false;
		}

		// Used during the phases of the game where the player should be able to control their tank.
		public void EnableControl()
		{
			movement.EnableMovement();
			shooting.enabled = true;
			shooting.canShoot = true;
			display.ReEnableTrackParticles();
		}

		/// <summary>
		/// Moves tank to a spawn location transform
		/// </summary>
		/// <param name="spawnPoint">Spawn point.</param>
		public void MoveToSpawnLocation(Transform spawnPoint)
		{
			if (spawnPoint != null)
			{
				m_AssignedSpawnPoint = spawnPoint;
			}

			movement.Rigidbody.position = m_AssignedSpawnPoint.position;
			movement.transform.position = m_AssignedSpawnPoint.position;
			
			movement.Rigidbody.rotation = m_AssignedSpawnPoint.rotation;
			movement.transform.rotation = m_AssignedSpawnPoint.rotation;
		}

		/// <summary>
		/// Resets the tank at a specified spawn point
		/// </summary>
		/// <param name="spawnPoint">Spawn point.</param>
		public void Reset(Transform spawnPoint)
		{
			movement.SetDefaults();
			shooting.SetDefaults();
			health.SetDefaults();
			display.enabled = true;
			MoveToSpawnLocation(spawnPoint);
			display.SetVisibleObjectsActive(true);
			display.SetTankDecoration(player.tankDecoration, player.tankDecorationMaterial, true);
		}

		/// <summary>
		/// Prespawning, used by round based modes to ensure the tank is in the correct state prior to running spawn flow
		/// </summary>
		public void Prespawn()
		{
			health.SetDefaults();
			display.SetVisibleObjectsActive(false);
		}

		/// <summary>
		/// Respawns the tank at a position and ensures that it is invisible to prevent visible interpolation artifacts on clients
		/// </summary>
		/// <param name="position">Position.</param>
		/// <param name="rotation">Rotation.</param>
		public void RespawnReposition(Vector3 position, Quaternion rotation)
		{
			if (removedTank)
			{
				return;
			}

			health.SetDefaults(); 
			movement.Rigidbody.position = position;
			movement.transform.position = position;

			movement.Rigidbody.rotation = rotation;
			movement.transform.rotation = rotation;
			
			display.SetVisibleObjectsActive(false);
		}

		/// <summary>
		/// Reactivates the tank as part of the spawn process. Turns on movement and shooting and enables visuals
		/// </summary>
		public void RespawnReactivate()
		{
			if (removedTank)
			{
				return;
			}

			display.SetVisibleObjectsActive(true);
			movement.SetDefaults();
			movement.SetAudioSourceActive(true);
			shooting.SetDefaults();
			display.SetTankDecoration(player.tankDecoration, player.tankDecorationMaterial, false);
		}

		/// <summary>
		/// Convenience function for increasing the player score
		/// </summary>
		public void IncrementScore()
		{
			m_Score++;
		}

		/// <summary>
		/// Convenience function for decreasing the player score
		/// </summary>
		public void DecrementScore()
		{
			m_Score--;
		}

		/// <summary>
		/// Helper function for awarding the player money (sum of round currency and award currency)
		/// </summary>
		public void AssignMoneyToPlayerData()
		{
			if (!hasAuthority)
			{
				return;
			}
			int currency = m_RoundCurrencyCollected + m_AwardCurrency;

			Debug.Log("Assigning " + currency + " Gears to player " + playerName);
			PlayerDataManager.s_Instance.AddCurrency(currency);
			m_RoundCurrencyCollected = 0;
		}

		#region SYNCVAR HOOKS

		//sync var hooks that call their corresponding events
		private void OnRoundCurrencyChanged(int currency)
		{
			m_RoundCurrencyCollected = currency;

			if (onCurrencyChanged != null)
			{
				onCurrencyChanged(currency);
			}
		}

		private void OnAwardCurrencyChanged(int currency)
		{
			m_AwardCurrency = currency;

			if (awardCurrencyChanged != null)
			{
				awardCurrencyChanged();
			}
		}

		private void OnRankChanged(int rank)
		{
			this.m_Rank = rank;
			if (rankChanged != null)
			{
				rankChanged();
			}
		}

		private void OnPlayerIdChanged(int playerId)
		{
			this.m_PlayerId = playerId;
			Initialize();
		}

		#endregion

		public void MarkTankAsRemoved()
		{
			removedTank = true;
		}

		#region Networking

		[ClientRpc]
		private void RpcOnPickupCollected(string pickupName)
		{
			if (onPickupCollected != null)
			{
				onPickupCollected(pickupName);
			}
		}

		[Server]
		public void SetRank(int rank)
		{
			this.m_Rank = rank;
		}

		[Server]
		public void AddPickupName(string pickupName)
		{
			RpcOnPickupCollected(pickupName);
		}

		[Server]
		public void AddPickupCurrency(int addCurrency)
		{
			m_RoundCurrencyCollected += addCurrency;
		}

		[Server]
		public void SetAwardCurrency(int currency)
		{
			m_AwardCurrency = currency;
		}

		[Server]
		public void SetPlayerId(int id)
		{
			m_PlayerId = id;
		}

		#endregion

		#endregion
	}
}
