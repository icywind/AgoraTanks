using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;
using UnityEngine.Networking.Types;
using UnityEngine.SceneManagement;
using Tanks.Map;
using Tanks.UI;

namespace Tanks.Networking
{
	public enum SceneChangeMode
	{
		None,
		Game,
		Menu
	}

	public enum NetworkState
	{
		Inactive,
		Pregame,
		Connecting,
		InLobby,
		InGame
	}

	public enum NetworkGameType
	{
		Matchmaking,
		Direct,
		Singleplayer
	}

	public class NetworkManager : UnityEngine.Networking.NetworkManager
	{
		#region Constants

		private static readonly string s_LobbySceneName = "LobbyScene";

		#endregion


		#region Events

		/// <summary>
		/// Called on all clients when a player joins
		/// </summary>
		public event Action<NetworkPlayer> playerJoined;
		/// <summary>
		/// Called on all clients when a player leaves
		/// </summary>
		public event Action<NetworkPlayer> playerLeft;

		private Action m_NextHostStartedCallback;

		/// <summary>
		/// Called on a host when their server starts
		/// </summary>
		public event Action hostStarted;
		/// <summary>
		/// Called when the server is shut down
		/// </summary>
		public event Action serverStopped;
		/// <summary>
		/// Called when the client is shut down
		/// </summary>
		public event Action clientStopped;
		/// <summary>
		/// Called on a client when they connect to a game
		/// </summary>
		public event Action<NetworkConnection> clientConnected;
		/// <summary>
		/// Called on a client when they disconnect from a game
		/// </summary>
		public event Action<NetworkConnection> clientDisconnected;
		/// <summary>
		/// Called on a client when there is a networking error
		/// </summary>
		public event Action<NetworkConnection, int> clientError;
		/// <summary>
		/// Called on the server when there is a networking error
		/// </summary>
		public event Action<NetworkConnection, int> serverError;
		/// <summary>
		/// Called on clients and server when the scene changes
		/// </summary>
		public event Action<bool, string> sceneChanged;
		/// <summary>
		/// Called on the server when all players are ready
		/// </summary>
		public event Action serverPlayersReadied;
		/// <summary>
		/// Called on the server when a client disconnects
		/// </summary>
		public event Action serverClientDisconnected;
		/// <summary>
		/// Called when we've created a match
		/// </summary>
		public event Action<bool, MatchInfo> matchCreated;
		/// <summary>
		/// Called when game mode changes
		/// </summary>
		public event Action gameModeUpdated;

		private Action<bool, MatchInfo> m_NextMatchCreatedCallback;

		/// <summary>
		/// Called when we've joined a matchMade game
		/// </summary>
		public event Action<bool, MatchInfo> matchJoined;

		/// <summary>
		/// Called when we've been dropped from a matchMade game
		/// </summary>
		public event Action matchDropped;

		private Action<bool, MatchInfo> m_NextMatchJoinedCallback;

		#endregion


		#region Fields

		/// <summary>
		/// Maximum number of players in a multiplayer game
		/// </summary>
		[SerializeField]
		protected int m_MultiplayerMaxPlayers = 4;
		/// <summary>
		/// Prefab that is spawned for every connected player
		/// </summary>
		[SerializeField]
		protected NetworkPlayer m_NetworkPlayerPrefab;

		protected GameSettings m_Settings;

		private SceneChangeMode m_SceneChangeMode;

		#endregion

		
		#region Properties

		/// <summary>
		/// Gets whether we're in a lobby or a game
		/// </summary>
		public NetworkState state
		{
			get;
			protected set;
		}

		/// <summary>
		/// Gets whether we're a multiplayer or single player game
		/// </summary>
		public NetworkGameType gameType
		{
			get;
			protected set;
		}

		/// <summary>
		/// Gets whether or not we're a server
		/// </summary>
		public static bool s_IsServer
		{
			get
			{
				return NetworkServer.active;
			}
		}

		/// <summary>
		/// Collection of all connected players
		/// </summary>
		public List<NetworkPlayer> connectedPlayers
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets current number of connected player
		/// </summary>
		public int playerCount
		{
			get
			{
				return s_IsServer ? numPlayers : connectedPlayers.Count;
			}
		}

		/// <summary>
		/// Gets whether we're playing in single player
		/// </summary>
		public bool isSinglePlayer
		{
			get
			{
				return gameType == NetworkGameType.Singleplayer;
			}
		}

		/// <summary>
		/// Gets whether we've currently got enough players to start a game
		/// </summary>
		public bool hasSufficientPlayers
		{
			get
			{
				return isSinglePlayer ? playerCount >= 1 : playerCount >= 2;
			}
		}

		#endregion

		#region Singleton

		/// <summary>
		/// Gets the NetworkManager instance if it exists
		/// </summary>
		public static NetworkManager s_Instance
		{
			get;
			protected set;
		}

		public static bool s_InstanceExists
		{
			get { return s_Instance != null; }
		}

		#endregion


		#region Unity Methods

		/// <summary>
		/// Initialize our singleton
		/// </summary>
		protected virtual void Awake()
		{
			if (s_Instance != null)
			{
				Destroy(gameObject);
			}
			else
			{
				s_Instance = this;

				connectedPlayers = new List<NetworkPlayer>();
			}
		}

		protected virtual void Start()
		{
			m_Settings = GameSettings.s_Instance;
		}

		/// <summary>
		/// Progress to game scene when in transitioning state
		/// </summary>
		protected virtual void Update()
		{
			if (m_SceneChangeMode != SceneChangeMode.None)
			{
				LoadingModal modal = LoadingModal.s_Instance;

				bool ready = true;
				if (modal != null)
				{
					ready = modal.readyToTransition;

					if (!ready && modal.fader.currentFade == Fade.None)
					{
						modal.FadeIn();
					}
				}

				if (ready)
				{
					if (m_SceneChangeMode == SceneChangeMode.Menu)
					{
						if (state != NetworkState.Inactive)
						{
							ServerChangeScene(s_LobbySceneName);
							if (gameType == NetworkGameType.Singleplayer)
							{
								state = NetworkState.Pregame;
							}
							else
							{
								state = NetworkState.InLobby;
							}
						}
						else
						{
							SceneManager.LoadScene(s_LobbySceneName);
						}
					}
					else
					{
						MapDetails map = GameSettings.s_Instance.map;

						ServerChangeScene(map.sceneName);

						state = NetworkState.InGame;
					}

					m_SceneChangeMode = SceneChangeMode.None;
				}
			}
		}

		/// <summary>
		/// Clear the singleton
		/// </summary>
		protected virtual void OnDestroy()
		{
			if (s_Instance == this)
			{
				s_Instance = null;
			}
		}

		#endregion


		#region Methods

		/// <summary>
		/// Causes the network manager to disconnect
		/// </summary>
		public void Disconnect()
		{
			switch (gameType)
			{
				case NetworkGameType.Direct:
					StopDirectMultiplayerGame();
					break;
				case NetworkGameType.Matchmaking:
					StopMatchmakingGame();
					break;
				case NetworkGameType.Singleplayer:
					StopSingleplayerGame();
					break;
			}
		}

		/// <summary>
		/// Disconnect and return the game to the main menu scene
		/// </summary>
		public void DisconnectAndReturnToMenu()
		{
			Disconnect();
			ReturnToMenu(MenuPage.Home);
		}

		/// <summary>
		/// Initiate single player mode
		/// </summary>
		public void StartSinglePlayerMode(Action callback)
		{
			if (state != NetworkState.Inactive)
			{
				throw new InvalidOperationException("Network currently active. Disconnect first.");
			}

			// minPlayers = 1;
			// maxPlayers = 1;

			m_NextHostStartedCallback = callback;
			state = NetworkState.Pregame;
			gameType = NetworkGameType.Singleplayer;
			StartHost();
		}

		/// <summary>
		/// Initiate direct multiplayer mode
		/// </summary>
		public void StartMultiplayerServer(Action callback)
		{
			if (state != NetworkState.Inactive)
			{
				throw new InvalidOperationException("Network currently active. Disconnect first.");
			}

			// minPlayers = 2;
			// maxPlayers = multiplayerMaxPlayers;

			m_NextHostStartedCallback = callback;
			state = NetworkState.InLobby;
			gameType = NetworkGameType.Direct;
			StartHost();
		}

		/// <summary>
		/// Create a matchmaking game
		/// </summary>
		public void StartMatchmakingGame(string gameName, Action<bool, MatchInfo> onCreate)
		{
			if (state != NetworkState.Inactive)
			{
				throw new InvalidOperationException("Network currently active. Disconnect first.");
			}

			// minPlayers = 2;
			// maxPlayers = multiplayerMaxPlayers;

			state = NetworkState.Connecting;
			gameType = NetworkGameType.Matchmaking;

			StartMatchMaker();
			m_NextMatchCreatedCallback = onCreate;

			matchMaker.CreateMatch(gameName, (uint)m_MultiplayerMaxPlayers, true, string.Empty, string.Empty, string.Empty, 0, 0, OnMatchCreate);
		}

		/// <summary>
		/// Initialize the matchmaking client to receive match lists
		/// </summary>
		public void StartMatchingmakingClient()
		{
			if (state != NetworkState.Inactive)
			{
				throw new InvalidOperationException("Network currently active. Disconnect first.");
			}

			// minPlayers = 2;
			// maxPlayers = multiplayerMaxPlayers;

			state = NetworkState.Pregame;
			gameType = NetworkGameType.Matchmaking;
			StartMatchMaker();
		}

		/// <summary>
		/// Join a matchmaking game
		/// </summary>
		public void JoinMatchmakingGame(NetworkID networkId, Action<bool, MatchInfo> onJoin)
		{
			if (gameType != NetworkGameType.Matchmaking ||
			    state != NetworkState.Pregame)
			{
				throw new InvalidOperationException("Game not in matching state. Make sure you call StartMatchmakingClient first.");
			}

			state = NetworkState.Connecting;

			m_NextMatchJoinedCallback = onJoin;
			matchMaker.JoinMatch(networkId, string.Empty, string.Empty, string.Empty, 0, 0, OnMatchJoined);
		}

		/// <summary>
		/// Makes the server change to the correct game scene for our map, and tells all clients to do the same
		/// </summary>
		public void ProgressToGameScene()
		{
			// Clear all client's ready states
			ClearAllReadyStates();

			// Remove us from matchmaking lists
			UnlistMatch();

			// Update will change scenes once loading screen is visible
			m_SceneChangeMode = SceneChangeMode.Game;

			// Tell NetworkPlayers to show their loading screens
			for (int i = 0; i < connectedPlayers.Count; ++i)
			{
				NetworkPlayer player = connectedPlayers[i];
				if (player != null)
				{
					player.RpcPrepareForLoad();
				}
			}
		}

		/// <summary>
		/// Makes the server change to the menu scene, and bring all clients with it
		/// </summary>
		public void ReturnToMenu(MenuPage returnPage)
		{
			MainMenuUI.s_ReturnPage = returnPage;

			// Update will change scenes once loading screen is visible
			m_SceneChangeMode = SceneChangeMode.Menu;

			if (s_IsServer && state == NetworkState.InGame)
			{
				// Clear all client's ready states
				ClearAllReadyStates();

				// Tell NetworkPlayers to show their loading screens
				for (int i = 0; i < connectedPlayers.Count; ++i)
				{
					NetworkPlayer player = connectedPlayers[i];
					if (player != null)
					{
						player.RpcPrepareForLoad();
					}
				}
			}
			else
			{
				// Show loading screen
				LoadingModal loading = LoadingModal.s_Instance;

				if (loading != null)
				{
					loading.FadeIn();
				}
			}
		}

		/// <summary>
		/// Gets the NetworkPlayer object for a given connection
		/// </summary>
		public static NetworkPlayer GetPlayerForConnection(NetworkConnection conn)
		{
			return conn.playerControllers[0].gameObject.GetComponent<NetworkPlayer>();
		}

		/// <summary>
		/// Gets a newwork player by its index
		/// </summary>
		public NetworkPlayer GetPlayerById(int id)
		{
			return connectedPlayers[id];
		}

		/// <summary>
		/// Gets whether all players are ready
		/// </summary>
		public bool AllPlayersReady()
		{
			if (!hasSufficientPlayers)
			{
				return false;
			}
			
			// Check all players
			for (int i = 0; i < connectedPlayers.Count; ++i)
			{
				if (!connectedPlayers[i].ready)
				{
					return false;
				}
			}

			return true;
		}


		/// <summary>
		/// Reset the ready states for all players
		/// </summary>
		public void ClearAllReadyStates()
		{
			for (int i = 0; i < connectedPlayers.Count; ++i)
			{
				NetworkPlayer player = connectedPlayers[i];
				if (player != null)
				{
					player.ClearReady();
				}
			}
		}

		protected void StopSingleplayerGame()
		{
			switch (state)
			{
				case NetworkState.InLobby:
					Debug.LogWarning("Single player game in lobby state. This should never happen");
					break;
				case NetworkState.Connecting:
				case NetworkState.Pregame:
				case NetworkState.InGame:
					StopHost();
					break;
			}

			state = NetworkState.Inactive;
		}

		protected void StopDirectMultiplayerGame()
		{
			switch (state)
			{
				case NetworkState.Connecting:
				case NetworkState.InLobby:
				case NetworkState.InGame:
					if (s_IsServer)
					{
						StopHost();
					}
					else
					{
						StopClient();
					}
					break;
			}

			state = NetworkState.Inactive;
		}

		protected void StopMatchmakingGame()
		{
			switch (state)
			{
				case NetworkState.Pregame:
					if (s_IsServer)
					{
						Debug.LogError("Server should never be in this state.");
					}
					else
					{
						StopMatchMaker();
					}
					break;

				case NetworkState.Connecting:
					if (s_IsServer)
					{
						StopMatchMaker();
						StopHost();
						matchInfo = null;
					}
					else
					{
						StopMatchMaker();
						StopClient();
						matchInfo = null;
					}
					break;

				case NetworkState.InLobby:
				case NetworkState.InGame:
					if (s_IsServer)
					{
						if (matchMaker != null && matchInfo != null)
						{
							matchMaker.DestroyMatch(matchInfo.networkId, 0, (success, info) =>
								{
									if (!success)
									{
										Debug.LogErrorFormat("Failed to terminate matchmaking game. {0}", info);
									}
									StopMatchMaker();
									StopHost();

									matchInfo = null;
								});
						}
						else
						{
							Debug.LogWarning("No matchmaker or matchInfo despite being a server in matchmaking state.");

							StopMatchMaker();
							StopHost();
							matchInfo = null;
						}
					}
					else
					{
						if (matchMaker != null && matchInfo != null)
						{
							matchMaker.DropConnection(matchInfo.networkId, matchInfo.nodeId, 0, (success, info) =>
								{
									if (!success)
									{
										Debug.LogErrorFormat("Failed to disconnect from matchmaking game. {0}", info);
									}
									StopMatchMaker();
									StopClient();
									matchInfo = null;
								});
						}
						else
						{
							Debug.LogWarning("No matchmaker or matchInfo despite being a client in matchmaking state.");

							StopMatchMaker();
							StopClient();
							matchInfo = null;
						}
					}
					break;
			}

			state = NetworkState.Inactive;
		}


		/// <summary>
		/// Sets the current matchmaking game as unlisted
		/// </summary>
		protected void UnlistMatch()
		{
			if (gameType == NetworkGameType.Matchmaking &&
			    matchMaker != null)
			{
				matchMaker.SetMatchAttributes(matchInfo.networkId, false, 0, (success, info) => Debug.Log("Match hidden"));
			}
		}


		/// <summary>
		/// Causes the current matchmaking game to become listed again
		/// </summary>
		protected void ListMatch()
		{
			if (gameType == NetworkGameType.Matchmaking &&
			    matchMaker != null)
			{
				matchMaker.SetMatchAttributes(matchInfo.networkId, true, 0, (success, info) => Debug.Log("Match shown"));
			}
		}


		protected virtual void UpdatePlayerIDs()
		{
			for (int i = 0; i < connectedPlayers.Count; ++i)
			{
				connectedPlayers[i].SetPlayerId(i);
			}
		}


		protected void FireGameModeUpdated()
		{
			if (gameModeUpdated != null)
			{
				gameModeUpdated();
			}
		}


		/// <summary>
		/// Register network players so we have all of them
		/// </summary>
		public void RegisterNetworkPlayer(NetworkPlayer newPlayer)
		{
			MapDetails currentMap = m_Settings.map;
			Debug.Log("Player joined");

			connectedPlayers.Add(newPlayer);
			newPlayer.becameReady += OnPlayerSetReady;

			if (s_IsServer)
			{
				UpdatePlayerIDs();
			}

			// Send initial scene message
			string sceneName = SceneManager.GetActiveScene().name;
			if (currentMap != null && sceneName == currentMap.sceneName)
			{
				newPlayer.OnEnterGameScene();
			}
			else if (sceneName == s_LobbySceneName)
			{
				newPlayer.OnEnterLobbyScene();
			}

			if (playerJoined != null)
			{
				playerJoined(newPlayer);
			}

			newPlayer.gameDetailsReady += FireGameModeUpdated;
		}


		/// <summary>
		/// Deregister network players
		/// </summary>
		public void DeregisterNetworkPlayer(NetworkPlayer removedPlayer)
		{
			Debug.Log("Player left");
			int index = connectedPlayers.IndexOf(removedPlayer);

			if (index >= 0)
			{
				connectedPlayers.RemoveAt(index);
			}

			UpdatePlayerIDs();

			if (playerLeft != null)
			{
				playerLeft(removedPlayer);
			}

			removedPlayer.gameDetailsReady -= FireGameModeUpdated;

			if (removedPlayer != null)
			{
				removedPlayer.becameReady -= OnPlayerSetReady;
			}
		}

		#endregion


		#region Networking events

		public override void OnClientError(NetworkConnection conn, int errorCode)
		{
			Debug.Log("OnClientError");

			base.OnClientError(conn, errorCode);

			if (clientError != null)
			{
				clientError(conn, errorCode);
			}
		}

		public override void OnClientConnect(NetworkConnection conn)
		{
			Debug.Log("OnClientConnect");

			ClientScene.Ready(conn);
			ClientScene.AddPlayer(0);

			if (clientConnected != null)
			{
				clientConnected(conn);
			}
		}

		public override void OnClientDisconnect(NetworkConnection conn)
		{
			Debug.Log("OnClientDisconnect");

			base.OnClientDisconnect(conn);

			if (clientDisconnected != null)
			{
				clientDisconnected(conn);
			}
		}

		public override void OnServerError(NetworkConnection conn, int errorCode)
		{
			Debug.Log("OnClientDisconnect");

			base.OnClientDisconnect(conn);

			if (serverError != null)
			{
				serverError(conn, errorCode);
			}
		}

		public override void OnServerSceneChanged(string sceneName)
		{
			Debug.Log("OnServerSceneChanged");

			base.OnServerSceneChanged(sceneName);

			if (sceneChanged != null)
			{
				sceneChanged(true, sceneName);
			}

			if (sceneName == s_LobbySceneName)
			{
				// Restore us to the matchmaking list
				ListMatch();

				// Reset this to prevent new clients from changing scenes when joining
				networkSceneName = string.Empty;
			}
		}

		public override void OnClientSceneChanged(NetworkConnection conn)
		{
			MapDetails currentMap = m_Settings.map;
			Debug.Log("OnClientSceneChanged");

			base.OnClientSceneChanged(conn);

			PlayerController pc = conn.playerControllers[0];

			if (!pc.unetView.isLocalPlayer)
			{
				return;
			}

			string sceneName = SceneManager.GetActiveScene().name;

			if (currentMap != null && sceneName == currentMap.sceneName)
			{
				state = NetworkState.InGame;

				// Tell all network players that they're in the game scene
				for (int i = 0; i < connectedPlayers.Count; ++i)
				{
					NetworkPlayer np = connectedPlayers[i];
					if (np != null)
					{
						np.OnEnterGameScene();
					}
				}
			}
			else if (sceneName == s_LobbySceneName)
			{
				if (state != NetworkState.Inactive)
				{
					if (gameType == NetworkGameType.Singleplayer)
					{
						state = NetworkState.Pregame;
					}
					else
					{
						state = NetworkState.InLobby;
					}
				}

				// Tell all network players that they're in the lobby scene
				for (int i = 0; i < connectedPlayers.Count; ++i)
				{
					NetworkPlayer np = connectedPlayers[i];
					if (np != null)
					{
						np.OnEnterLobbyScene();
					}
				}
			}

			if (sceneChanged != null)
			{
				sceneChanged(false, sceneName);
			}
		}

		public override void OnServerAddPlayer(NetworkConnection conn, short playerControllerId)
		{
			// Intentionally not calling base here - we want to control the spawning of prefabs
			Debug.Log("OnServerAddPlayer");

			NetworkPlayer newPlayer = Instantiate<NetworkPlayer>(m_NetworkPlayerPrefab);
			DontDestroyOnLoad(newPlayer);
			NetworkServer.AddPlayerForConnection(conn, newPlayer.gameObject, playerControllerId);
		}

		public override void OnServerRemovePlayer(NetworkConnection conn, PlayerController player)
		{
			Debug.Log("OnServerRemovePlayer");
			base.OnServerRemovePlayer(conn, player);

			NetworkPlayer connectedPlayer = GetPlayerForConnection(conn);
			if (connectedPlayer != null)
			{
				Destroy(connectedPlayer);
				connectedPlayers.Remove(connectedPlayer);
			}
		}

		public override void OnServerReady(NetworkConnection conn)
		{
			Debug.Log("OnServerReady");
			base.OnServerReady(conn);
		}

		public override void OnServerConnect(NetworkConnection conn)
		{
			Debug.LogFormat("OnServerConnect\nID {0}\nAddress {1}\nHostID {2}", conn.connectionId, conn.address, conn.hostId);

			if (numPlayers >= m_MultiplayerMaxPlayers ||
			    state != NetworkState.InLobby)
			{
				conn.Disconnect();
			}
			else
			{
				// Reset ready flags for everyone because the game state changed
				if (state == NetworkState.InLobby)
				{
					ClearAllReadyStates();
				}
			}

			base.OnServerConnect(conn);
		}

		public override void OnServerDisconnect(NetworkConnection conn)
		{
			Debug.Log("OnServerDisconnect");
			base.OnServerDisconnect(conn);

			// Reset ready flags for everyone because the game state changed
			if (state == NetworkState.InLobby)
			{
				ClearAllReadyStates();
			}
			
			if (serverClientDisconnected != null)
			{
				serverClientDisconnected();
			}
		}

		public override void OnMatchCreate(bool success, string extendedInfo, MatchInfo matchInfo)
		{
			base.OnMatchCreate(success, extendedInfo, matchInfo);
			Debug.Log("OnMatchCreate");

			if (success)
			{
				state = NetworkState.InLobby;
			}
			else
			{
				state = NetworkState.Inactive;
			}

			// Fire callback
			if (m_NextMatchCreatedCallback != null)
			{
				m_NextMatchCreatedCallback(success, matchInfo);
				m_NextMatchCreatedCallback = null;
			}

			// Fire event
			if (matchCreated != null)
			{
				matchCreated(success, matchInfo);
			}
		}

		public override void OnMatchJoined(bool success, string extendedInfo, MatchInfo matchInfo)
		{
			base.OnMatchJoined(success, extendedInfo, matchInfo);
			Debug.Log("OnMatchJoined");

			if (success)
			{
				state = NetworkState.InLobby;
			}
			else
			{
				state = NetworkState.Pregame;
			}

			// Fire callback
			if (m_NextMatchJoinedCallback != null)
			{
				m_NextMatchJoinedCallback(success, matchInfo);
				m_NextMatchJoinedCallback = null;
			}

			// Fire event
			if (matchJoined != null)
			{
				matchJoined(success, matchInfo);
			}
		}

		public override void OnDropConnection(bool success, string extendedInfo)
		{
			base.OnDropConnection(success, extendedInfo);
			Debug.Log("OnDropConnection");

			if (matchDropped != null)
			{
				matchDropped();
			}
		}

		/// <summary>
		/// Server resets networkSceneName
		/// </summary>
		public override void OnStartServer()
		{
			base.OnStartServer();
			networkSceneName = string.Empty;
		}

		/// <summary>
		/// Server destroys NetworkPlayer objects
		/// </summary>
		public override void OnStopServer()
		{
			base.OnStopServer();
			Debug.Log("OnStopServer");

			for (int i = 0; i < connectedPlayers.Count; ++i)
			{
				NetworkPlayer player = connectedPlayers[i];
				if (player != null)
				{
					NetworkServer.Destroy(player.gameObject);
				}
			}

			connectedPlayers.Clear();

			// Reset this
			networkSceneName = string.Empty;

			if (serverStopped != null)
			{
				serverStopped();
			}
		}


		/// <summary>
		/// Clients also destroy their copies of NetworkPlayer
		/// </summary>
		public override void OnStopClient()
		{
			Debug.Log("OnStopClient");
			base.OnStopClient();

			for (int i = 0; i < connectedPlayers.Count; ++i)
			{
				NetworkPlayer player = connectedPlayers[i];
				if (player != null)
				{
					Destroy(player.gameObject);
				}
			}

			connectedPlayers.Clear();

			if (clientStopped != null)
			{
				clientStopped();
			}
		}

		/// <summary>
		/// Fire host started messages
		/// </summary>
		public override void OnStartHost()
		{
			Debug.Log("OnStartHost");
			base.OnStartHost();

			if (m_NextHostStartedCallback != null)
			{
				m_NextHostStartedCallback();
				m_NextHostStartedCallback = null;
			}
			if (hostStarted != null)
			{
				hostStarted();
			}
		}

		/// <summary>
		/// Called on the server when a player is set to ready
		/// </summary>
		public virtual void OnPlayerSetReady(NetworkPlayer player)
		{
			if (AllPlayersReady() && serverPlayersReadied != null)
			{
				serverPlayersReadied();
			}
		}

		#endregion
	}
}