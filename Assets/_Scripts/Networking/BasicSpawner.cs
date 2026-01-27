using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using LoveLetter.Login;


namespace LoveLetter.Networking
{
    public enum SessionStateType
    {
        Waiting = 0,
        Playing = 1,
        Finished = 2
    }

    public class BasicSpawner : SingletonPersistent<BasicSpawner>, INetworkRunnerCallbacks
    {
        [Header("Player Prefabs")]
        [SerializeField] private NetworkPrefabRef _playerPrefab;

        private NetworkRunner _runner;
        private NetworkSceneManagerDefault _networkSceneManager;

        private Dictionary<PlayerRef, NetworkObject> _spawnedPlayers = new();
        private List<SessionInfo> _sessions = new();

        public List<SessionInfo> Sessions => _sessions;

        private static string _playerName;
        public string PlayerName => _playerName;

        public NetworkRunner Runner => _runner;

        private Transform[] _playerPositions;
        public Transform[] PlayerPositions => _playerPositions;

        [Header("Game Manager Prefab")]
        [SerializeField] private NetworkPrefabRef _gameManagerPrefab;

        static NetworkObject gameManagerInstance = null;

        private bool ManualLeave = false;
        private bool LeaveToMainMenu = false;


        /* --------------------------- PLAYER DATA --------------------------- */
        public static class PlayerData
        {
            public static string LocalPlayerName;
            public static int LocalAvatarId;
            public static int LocalSeatIndex = -1;
        }

        /* --------------------------- ROOM CODES --------------------------- */
        public static class RoomCodeGenerator
        {
            private const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

            public static string Generate(int length = 6)
            {
                System.Random r = new System.Random();
                char[] code = new char[length];

                for (int i = 0; i < length; i++)
                    code[i] = chars[r.Next(chars.Length)];

                return new string(code);
            }
        }

        /* ===================================================================
         * RUNNER CREATION
         * =================================================================== */
        public async void EnsureRunner()
        {
            if (_runner != null)
                return;

            GameObject go = new GameObject("NetworkRunner");
            DontDestroyOnLoad(go);

            _runner = go.AddComponent<NetworkRunner>();
            _networkSceneManager = go.AddComponent<NetworkSceneManagerDefault>();

            _runner.AddCallbacks(this);

            Debug.Log("[BasicSpawner] Runner created. Joining lobby...");

            var result = await _runner.JoinSessionLobby(SessionLobby.ClientServer);

            if (result.Ok)
            {
                Debug.Log("[BasicSpawner] Joined lobby successfully.");
                SessionView.Instance?.RefreshSessionList();
            }
            else
            {
                Debug.LogError("[BasicSpawner] Failed to join lobby: " + result.ShutdownReason);
            }
        }

        /* ===================================================================
         * CONNECT TO LOBBY
         * =================================================================== */
        public void ConnectToLobby(string playerName, int selectedAvatarId)
        {
            EnsureRunner();

            if (!string.IsNullOrEmpty(playerName))
                _playerName = playerName;

            PlayerData.LocalPlayerName = _playerName;
            PlayerData.LocalAvatarId = selectedAvatarId;

            _runner.JoinSessionLobby(SessionLobby.ClientServer);
        }
        public async void ReconnectToLobby()
        {
            if (_runner != null)
            {
                Destroy(_runner.gameObject);
                _runner = null;
            }

            GameObject go = new GameObject("NetworkRunner");
            DontDestroyOnLoad(go);

            _runner = go.AddComponent<NetworkRunner>();
            _networkSceneManager = go.AddComponent<NetworkSceneManagerDefault>();
            _runner.AddCallbacks(this);

            Debug.Log("[BasicSpawner] Reconnecting to lobby...");

            var result = await _runner.JoinSessionLobby(SessionLobby.ClientServer);

            if (!result.Ok)
                Debug.LogError("[BasicSpawner] Failed to reconnect: " + result.ShutdownReason);
        }

        /* ===================================================================
         * START HOST / CLIENT
         * =================================================================== */
        public async void StartHost()
        {
            EnsureRunner();

            PlayerData.LocalPlayerName = _playerName;
            PlayerData.LocalAvatarId = PlayerPrefs.GetInt("SelectedAvatarId", 0);

            string roomCode = RoomCodeGenerator.Generate();

            await _runner.StartGame(new StartGameArgs()
            {
                GameMode = GameMode.Host,
                SessionName = roomCode,
                Scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex + 1),
                SceneManager = _networkSceneManager
            });
        }

        public async void StartClient(string roomCode)
        {
            EnsureRunner();

            PlayerData.LocalPlayerName = _playerName;
            PlayerData.LocalAvatarId = PlayerPrefs.GetInt("SelectedAvatarId", 0);

            await _runner.StartGame(new StartGameArgs()
            {
                GameMode = GameMode.Client,
                SessionName = roomCode,
                SceneManager = _networkSceneManager
            });
        }

        /* ===================================================================
         * PLAYER JOINING
         * =================================================================== */
        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            if (!runner.IsServer)
                return;

            if (gameManagerInstance == null)
            {
                gameManagerInstance = runner.Spawn(_gameManagerPrefab, Vector3.zero, Quaternion.identity);
            }

            int seatIndex = runner.ActivePlayers
                .OrderBy(p => p.RawEncoded)
                .ToList()
                .IndexOf(player);

            if (seatIndex >= PlayerPositions.Length)
                return;

            NetworkObject obj = runner.Spawn(_playerPrefab, Vector3.zero, Quaternion.identity, player);
            _spawnedPlayers[player] = obj;

            obj.GetComponent<Player>().SeatIndex = seatIndex;
            GameManager.Instance.AssignSeat(player, seatIndex);
            UpdatePlayerCount();
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            if (_spawnedPlayers.TryGetValue(player, out var obj))
            {
                runner.Despawn(obj);
                _spawnedPlayers.Remove(player);
            }
            if (runner.IsServer)
                UpdatePlayerCount();
        }




        /* ===================================================================
         * CREATE ROOM
         * =================================================================== */
        public void CreateRoom(string sessionName, int maxPlayers, bool isPrivate, string displayName)
        {
            if (!IsLobbyReady)
                return;

            StartGame(GameMode.Host, sessionName, maxPlayers, isPrivate, displayName);
        }

        public async void StartGame(GameMode mode, string sessionName, int maxPlayers, bool isPrivate, string displayName)
        {
            EnsureRunner();

            var props = new Dictionary<string, SessionProperty>()
            {
                ["state"] = (int)SessionStateType.Waiting,
                ["displayName"] = displayName
            };

            await _runner.StartGame(new StartGameArgs()
            {
                GameMode = GameMode.Host,
                SessionName = sessionName,
                PlayerCount = maxPlayers,
                IsVisible = !isPrivate,
                Scene = SceneRef.FromIndex(1),
                SceneManager = _networkSceneManager,
                SessionProperties = props
            });
        }

        public async void StartGame(GameMode mode, string sessionName)
        {
            EnsureRunner();

            await _runner.StartGame(new StartGameArgs()
            {
                GameMode = mode,
                SessionName = sessionName,
                SceneManager = _networkSceneManager
            });
        }

        /* ===================================================================
         * SCENE LOAD
         * =================================================================== */
        public void OnSceneLoadDone(NetworkRunner runner)
        {
            if (runner.IsServer && gameManagerInstance == null)
            {
                gameManagerInstance = runner.Spawn(_gameManagerPrefab);
            }

            GameObject positionsRoot = GameObject.Find("Players");
            if (positionsRoot == null)
                return;

            _playerPositions = new Transform[6];
            _playerPositions[0] = positionsRoot.transform.Find("PlayerPositionOWN");
            _playerPositions[1] = positionsRoot.transform.Find("PlayerPosition_1");
            _playerPositions[2] = positionsRoot.transform.Find("PlayerPosition_2");
            _playerPositions[3] = positionsRoot.transform.Find("PlayerPosition_3");
            _playerPositions[4] = positionsRoot.transform.Find("PlayerPosition_4");
            _playerPositions[5] = positionsRoot.transform.Find("PlayerPosition_5");
        }

        /* ===================================================================
         * LEAVING ROOM
         * =================================================================== */
        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            if (ManualLeave)
                return;

            if (!runner.IsServer)
            {
                PlayerPrefs.SetInt("ReturnedFromGame", 1);
                SceneManager.LoadScene(0);
            }
        }

        public async void LeaveRoom()
        {
            ManualLeave = true;

            if (_runner != null && _runner.IsServer)
            {
                _runner.SessionInfo.UpdateCustomProperties(new Dictionary<string, SessionProperty> { ["closed"] = 1 });
                await _runner.Shutdown(true);
            }
            else if (_runner != null)
            {
                await _runner.Shutdown();
            }

            if (_runner != null)
            {
                Destroy(_runner.gameObject);
                _runner = null;
            }

            SceneManager.sceneLoaded += OnMenuLoaded;
            SceneManager.LoadScene(0);
        }

        public void ReturnToMainMenu()
        {
            ManualLeave = true;

            if (_runner != null)
            {
                Destroy(_runner.gameObject);
                _runner = null;
            }

            SceneManager.sceneLoaded += OnMenuLoaded;
            SceneManager.LoadScene(0);
        }
        public void ReturnToSessionList()
        {
            ManualLeave = true;

            PlayerPrefs.SetInt("ReturnedFromGame", 1);

            if (_runner != null)
            {
                Destroy(_runner.gameObject);
                _runner = null;
            }

            SceneManager.sceneLoaded += OnMenuLoaded;
            SceneManager.LoadScene(0);
        }

        /* ===================================================================
         * HELPERS
         * =================================================================== */
        public NetworkObject GetPlayerObject(PlayerRef pr)
        {
            return _spawnedPlayers.TryGetValue(pr, out var obj) ? obj : null;
        }

        public NetworkObject GetLocalPlayerObject()
        {
            return GetPlayerObject(Runner.LocalPlayer);
        }

        public void RegisterSpawnedPlayer(PlayerRef player, NetworkObject obj)
        {
            _spawnedPlayers[player] = obj;
        }

        public static bool IsLobbyReady = false;

        public void OnConnectedToServer(NetworkRunner runner) { }

        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {
            if (!IsLobbyReady)
                IsLobbyReady = true;

            _sessions.Clear();
            _sessions.AddRange(sessionList);
        }

        public Player GetPlayerBySeat(int seatIndex)
        {
            foreach (var kv in _spawnedPlayers)
            {
                Player p = kv.Value.GetComponent<Player>();
                if (p != null && p.SeatIndex == seatIndex)
                    return p;
            }
            return null;
        }
        private void UpdatePlayerCount()
        {
            if (!Runner.IsServer)
                return;

            int count = Runner.ActivePlayers.Count();

            var props = new Dictionary<string, SessionProperty>();
            foreach (var kvp in Runner.SessionInfo.Properties)
                props[kvp.Key] = kvp.Value;

            props["players"] = count;

            Runner.SessionInfo.UpdateCustomProperties(props);
        }

        public void SetSessionState(SessionStateType newState)
        {
            if (!Runner.IsServer)
                return;

            var props = new Dictionary<string, SessionProperty>();
            foreach (var kvp in Runner.SessionInfo.Properties)
                props[kvp.Key] = kvp.Value;

            props["state"] = (int)newState;

            Runner.SessionInfo.UpdateCustomProperties(props);

            Debug.Log($"[BasicSpawner] Session state updated → {newState}");
        }
        private void OnMenuLoaded(Scene s, LoadSceneMode m)
        {
            SceneManager.sceneLoaded -= OnMenuLoaded;
            EnsureRunner();
        }

        /* ===================================================================
         * UNUSED CALLBACKS
         * =================================================================== */
        public void OnInput(NetworkRunner runner, NetworkInput input) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }


        /* ===================================================================
         * RPC
         * =================================================================== */


        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_HostClosingRoom()
        {
            ManualLeave = true;

            PlayerPrefs.SetInt("ReturnedFromGame", 1);

            if (_runner != null)
            {
                Destroy(_runner.gameObject);
                _runner = null;
            }

            SceneManager.LoadScene(0);
        }

    }
}
