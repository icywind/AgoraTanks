using System;
using UnityEngine;
using UnityEngine.Networking;
using Tanks.Data;
using Tanks.TankControllers;
using Tanks.UI;
using TanksNetworkManager = Tanks.Networking.NetworkManager;
using Tanks.Map;

namespace Tanks.Networking
{
	public class NetworkPlayer : NetworkBehaviour
	{
		public event Action<NetworkPlayer> syncVarsChanged;
		// Server only event
		public event Action<NetworkPlayer> becameReady;

		public event Action gameDetailsReady;

		[SerializeField]
		protected GameObject m_TankPrefab;
		[SerializeField]
		protected GameObject m_LobbyPrefab;

		// Set by commands
		[SyncVar(hook = "OnMyName")]
		private string m_PlayerName = "";
		[SyncVar(hook = "OnMyColor")]
		private Color m_PlayerColor = Color.clear;
		[SyncVar(hook = "OnMyTank")]
		private int m_PlayerTankType = -1;
		[SyncVar(hook = "OnMyDecoration")]
		private int m_PlayerTankDecoration = -1;
		[SyncVar(hook = "OnMyDecorationMaterial")]
		private int m_PlayerTankDecorationMaterial = -1;
		[SyncVar(hook = "OnReadyChanged")]
		private bool m_Ready = false;

		// Set on the server only
		[SyncVar(hook = "OnHasInitialized")]
		private bool m_Initialized = false;
		[SyncVar]
		private int m_PlayerId;

		private IColorProvider m_ColorProvider = null;
		private TanksNetworkManager m_NetManager;
		private GameSettings m_Settings;

		private bool lateSetupOfClientPlayer = false;

		/// <summary>
		/// Gets this player's id
		/// </summary>
		public int playerId
		{
			get { return m_PlayerId; }
		}

		/// <summary>
		/// Gets this player's name
		/// </summary>
		public string playerName
		{
			get { return m_PlayerName; }
		}

		/// <summary>
		/// Gets this player's colour
		/// </summary>
		public Color color
		{
			get { return m_PlayerColor; }
		}

		/// <summary>
		/// Gets this player's tank ID
		/// </summary>
		public int tankType
		{
			get { return m_PlayerTankType; }
		}

		/// <summary>
		/// Gets this player's tank decoration ID
		/// </summary>
		public int tankDecoration
		{
			get { return m_PlayerTankDecoration; }
		}

		/// <summary>
		/// Gets this player's tank material ID
		/// </summary>
		public int tankDecorationMaterial
		{
			get { return m_PlayerTankDecorationMaterial; }
		}

		/// <summary>
		/// Gets whether this player has marked themselves as ready in the lobby
		/// </summary>
		public bool ready
		{
			get { return m_Ready; }
		}

		/// <summary>
		/// Gets the tank manager associated with this player
		/// </summary>
		public TankManager tank
		{
			get;
			set;
		}

		/// <summary>
		/// Gets the lobby object associated with this player
		/// </summary>
		public LobbyPlayer lobbyObject
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the local NetworkPlayer object
		/// </summary>
		public static NetworkPlayer s_LocalPlayer
		{
			get;
			private set;
		}

		/// <summary>
		/// Set initial values
		/// </summary>
		[Client]
		public override void OnStartLocalPlayer()
		{
			if (m_Settings == null)
			{
				m_Settings = GameSettings.s_Instance;
			}

			base.OnStartLocalPlayer();
			Debug.Log("Local Network Player start");
			UpdatePlayerSelections();

			s_LocalPlayer = this;
		}

		/// <summary>
		/// Register us with the NetworkManager
		/// </summary>
		[Client]
		public override void OnStartClient()
		{
			DontDestroyOnLoad(this);

			if (m_Settings == null)
			{
				m_Settings = GameSettings.s_Instance;
			}
			if (m_NetManager == null)
			{
				m_NetManager = TanksNetworkManager.s_Instance;
			}
			
			base.OnStartClient();
			Debug.Log("Client Network Player start");

			m_NetManager.RegisterNetworkPlayer(this);
		}

		/// <summary>
		/// Get network manager
		/// </summary>
		protected virtual void Start()
		{
			if (m_NetManager == null)
			{
				m_NetManager = TanksNetworkManager.s_Instance;
			}
		}

		/// <summary>
		/// Deregister us with the manager
		/// </summary>
		public override void OnNetworkDestroy()
		{
			base.OnNetworkDestroy();
			Debug.Log("Client Network Player OnNetworkDestroy");

			if (lobbyObject != null)
			{
				Destroy(lobbyObject.gameObject);
			}

			if (m_NetManager != null)
			{
				m_NetManager.DeregisterNetworkPlayer(this);
			}
		}

		/// <summary>
		/// Fired when we enter the game scene
		/// </summary>
		[Client]
		public void OnEnterGameScene()
		{
			if (hasAuthority)
			{
				CmdClientReadyInGameScene();
			}
		}

		/// <summary>
		/// Fired when we return to the lobby scene, or are first created in the lobby
		/// </summary>
		[Client]
		public void OnEnterLobbyScene()
		{
			Debug.Log("OnEnterLobbyScene");
			if (m_Initialized && lobbyObject == null)
			{
				CreateLobbyObject();
			}
		}


		[Server]
		public void ClearReady()
		{
			m_Ready = false;
		}


		[Server]
		public void SetPlayerId(int playerId)
		{
			this.m_PlayerId = playerId;
		}



		/// <summary>
		/// Clean up lobby object for us
		/// </summary>
		protected virtual void OnDestroy()
		{
			if (lobbyObject != null)
			{
				Destroy(lobbyObject.gameObject);
			}
		}


		/// <summary>
		/// Create our lobby object
		/// </summary>
		private void CreateLobbyObject()
		{
			lobbyObject = Instantiate(m_LobbyPrefab).GetComponent<LobbyPlayer>();
			lobbyObject.Init(this);
		}


		/// <summary>
		/// Set up our player choices, changing local values too
		/// </summary>
		[Client]
		private void UpdatePlayerSelections()
		{
			Debug.Log("UpdatePlayerSelections");
			PlayerDataManager dataManager = PlayerDataManager.s_Instance;
			if (dataManager != null)
			{
				m_PlayerTankType = dataManager.selectedTank;
				m_PlayerTankDecoration = dataManager.selectedDecoration;
				m_PlayerTankDecorationMaterial = dataManager.GetSelectedMaterialForDecoration(m_PlayerTankDecoration);
				m_PlayerName = dataManager.playerName;
				CmdSetInitialValues(m_PlayerTankType, m_PlayerTankDecoration, m_PlayerTankDecorationMaterial, m_PlayerName);
			}
		}

		[Server]
		private void LazyLoadColorProvider()
		{
			if (m_ColorProvider != null)
			{
				return;
			}
			
			if (m_Settings.mode == null)
			{
				Debug.Log("Missing mode - assigning PlayerColorProvider by default");
				m_ColorProvider = new PlayerColorProvider();
				return;
			}
	
			m_ColorProvider = m_Settings.mode.rulesProcessor.GetColorProvider();
		}

		[Server]
		private void SelectColour()
		{
			LazyLoadColorProvider();

			if (m_ColorProvider == null)
			{
				Debug.LogWarning("Could not find color provider");
				return;
			}

			Color newPlayerColor = m_ColorProvider.ServerGetColor(this);

			m_PlayerColor = newPlayerColor;
		}

		[ClientRpc]
		public void RpcSetGameSettings(int mapIndex, int modeIndex)
		{
			GameSettings settings = GameSettings.s_Instance;
			if (!isServer)
			{
				settings.SetMapIndex(mapIndex);
				settings.SetModeIndex(modeIndex);
			}
			if (gameDetailsReady != null && isLocalPlayer)
			{
				gameDetailsReady();
			}
		}

		[ClientRpc]
		public void RpcPrepareForLoad()
		{
			if (isLocalPlayer)
			{
				// Show loading screen
				LoadingModal loading = LoadingModal.s_Instance;

				if (loading != null)
				{
					loading.FadeIn();
				}
			}
		}

		protected void AddClientToServer()
		{
			Debug.Log("CmdClientReadyInScene");
			GameObject tankObject = Instantiate(m_TankPrefab);
			NetworkServer.SpawnWithClientAuthority(tankObject, connectionToClient);
			tank = tankObject.GetComponent<TankManager>();
			tank.SetPlayerId(playerId);
			if (lateSetupOfClientPlayer)
			{
				lateSetupOfClientPlayer = false;
				SpawnManager.InstanceSet -= AddClientToServer;
			}
		}

		#region Commands

		/// <summary>
		/// Create our tank
		/// </summary>
		[Command]
		private void CmdClientReadyInGameScene()
		{
			if (SpawnManager.s_InstanceExists)
			{
				AddClientToServer();
			}
			else
			{
				lateSetupOfClientPlayer = true;
				SpawnManager.InstanceSet += AddClientToServer;
			}
		}

		[Command]
		private void CmdSetInitialValues(int tankType, int decorationIndex, int decorationMaterial, string newName)
		{
			Debug.Log("CmdChangeTank");
			m_PlayerTankType = tankType;
			m_PlayerTankDecoration = decorationIndex;
			m_PlayerTankDecorationMaterial = decorationMaterial;
			m_PlayerName = newName;
			SelectColour();
			m_Initialized = true;
		}

		[Command]
		public void CmdChangeTank(int tankType)
		{
			Debug.Log("CmdChangeTank");
			m_PlayerTankType = tankType;
		}

		[Command]
		public void CmdChangeDecorationProperties(int decorationIndex, int decorationMaterial)
		{
			Debug.Log("CmdChangeDecorationProperties");
			m_PlayerTankDecoration = decorationIndex;
			m_PlayerTankDecorationMaterial = decorationMaterial;
		}

		[Command]
		public void CmdColorChange()
		{
			Debug.Log("CmdColorChange");
			SelectColour();
		}

		[Command]
		public void CmdNameChanged(string name)
		{
			Debug.Log("CmdNameChanged");
			m_PlayerName = name;
		}

		[Command]
		public void CmdSetReady()
		{
			Debug.Log("CmdSetReady");
			if (m_NetManager.hasSufficientPlayers)
			{
				m_Ready = true;

				if (becameReady != null)
				{
					becameReady(this);
				}
			}
		}

		#endregion


		#region Syncvar callbacks

		private void OnMyName(string newName)
		{
			m_PlayerName = newName;

			if (syncVarsChanged != null)
			{
				syncVarsChanged(this);
			}
		}

		private void OnMyColor(Color newColor)
		{
			m_PlayerColor = newColor;

			if (syncVarsChanged != null)
			{
				syncVarsChanged(this);
			}
		}

		private void OnMyTank(int tankIndex)
		{
			if (tankIndex != -1)
			{
				m_PlayerTankType = tankIndex;

				if (syncVarsChanged != null)
				{
					syncVarsChanged(this);
				}
			}
		}

		private void OnMyDecoration(int decorationIndex)
		{
			if (decorationIndex != -1)
			{
				m_PlayerTankDecoration = decorationIndex;

				if (syncVarsChanged != null)
				{
					syncVarsChanged(this);
				}
			}
		}

		private void OnMyDecorationMaterial(int decorationMatIndex)
		{
			if (decorationMatIndex != -1)
			{
				m_PlayerTankDecorationMaterial = decorationMatIndex;

				if (syncVarsChanged != null)
				{
					syncVarsChanged(this);
				}
			}
		}

		private void OnReadyChanged(bool value)
		{
			m_Ready = value;

			if (syncVarsChanged != null)
			{
				syncVarsChanged(this);
			}
		}

		private void OnHasInitialized(bool value)
		{
			if (!m_Initialized && value)
			{
				m_Initialized = value;
				CreateLobbyObject();

				if (isServer && !m_Settings.isSinglePlayer)
				{
					RpcSetGameSettings(m_Settings.mapIndex, m_Settings.modeIndex);
				}
			}
		}

		#endregion
	}
}