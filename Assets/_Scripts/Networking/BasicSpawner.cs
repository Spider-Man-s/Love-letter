using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

namespace LoveLetter.Networking
{
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

        public static class PlayerData
        {
            public static string LocalPlayerName;
            public static int LocalAvatarId;
            public static int LocalSeatIndex = -1;
        }

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

        private void Awake()
        {
            base.Awake();
            _runner = GetComponent<NetworkRunner>();
            _networkSceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();
        }

        public void ConnectToLobby(string playerName, int selectedAvatarId)
        {
            if (!string.IsNullOrEmpty(playerName))
                _playerName = playerName;

            PlayerData.LocalPlayerName = _playerName;
            PlayerData.LocalAvatarId = selectedAvatarId;

            _runner.JoinSessionLobby(SessionLobby.ClientServer);
        }

        public async void StartHost()
        {
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
            PlayerData.LocalPlayerName = _playerName;
            PlayerData.LocalAvatarId = PlayerPrefs.GetInt("SelectedAvatarId", 0);

            await _runner.StartGame(new StartGameArgs()
            {
                GameMode = GameMode.Client,
                SessionName = roomCode,
                SceneManager = _networkSceneManager
            });
        }

        // ====================================================================
        // PLAYER JOIN FLOW
        // ====================================================================
        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            if (!runner.IsServer)
                return;

            // FIX #1: GameManager MUST exist BEFORE registering players
            if (gameManagerInstance == null)
            {
                gameManagerInstance = runner.Spawn(_gameManagerPrefab, Vector3.zero, Quaternion.identity);
                Debug.Log("GameManager network-spawned (OnPlayerJoined).");
            }

            // Seat assignment (RawEncoded deterministic)
            int seatIndex = runner.ActivePlayers
                .OrderBy(p => p.RawEncoded)
                .ToList()
                .IndexOf(player);

            Debug.Log($"[OnPlayerJoined] Server assigning seat {seatIndex} to {player}");

            if (seatIndex >= PlayerPositions.Length)
            {
                Debug.LogWarning("Max players reached!");
                return;
            }

            // Player spawn
            NetworkObject obj = runner.Spawn(_playerPrefab, Vector3.zero, Quaternion.identity, player);
            _spawnedPlayers[player] = obj;

            obj.GetComponent<Player>().SeatIndex = seatIndex;
            GameManager.Instance.AssignSeat(player, seatIndex);



            Debug.Log($"Player {player} registered to seat {seatIndex}");
        }


        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            if (_spawnedPlayers.TryGetValue(player, out var obj))
            {
                runner.Despawn(obj);
                _spawnedPlayers.Remove(player);
            }
        }

        public void CreateRoom(string sessionName, int maxPlayers, bool isPrivate)
        {
            if (!IsLobbyReady)
            {
                Debug.LogWarning("[CreateRoom] Cannot create room yet. Lobby not ready.");
                return;
            }
            StartGame(GameMode.Host, sessionName, maxPlayers, isPrivate);
        }

        public async void StartGame(GameMode mode, string sessionName, int maxPlayers, bool isPrivate)
        {
            if (_runner == null)
                _runner = gameObject.AddComponent<NetworkRunner>();

            _runner.AddCallbacks(this);
            _runner.ProvideInput = false;

            var scene = SceneRef.FromIndex(1);

            await _runner.StartGame(new StartGameArgs()
            {
                GameMode = mode,
                SessionName = sessionName,
                PlayerCount = maxPlayers,
                IsVisible = !isPrivate,
                Scene = scene,
                SceneManager = _networkSceneManager
            });
        }

        // ====================================================================
        // SCENE LOAD
        // ====================================================================
        public void OnSceneLoadDone(NetworkRunner runner)
        {
            // FIX #3: Guarantee GM exists BEFORE seating uses it
            if (runner.IsServer && gameManagerInstance == null)
            {
                gameManagerInstance = runner.Spawn(_gameManagerPrefab);
                Debug.Log("GameManager spawned in scene (OnSceneLoadDone).");
            }

            GameObject positionsRoot = GameObject.Find("Players");

            if (positionsRoot == null)
            {
                Debug.LogError("PlayerPositions object not found in scene!");
                return;
            }

            _playerPositions = new Transform[6];
            _playerPositions[0] = positionsRoot.transform.Find("PlayerPositionOWN");
            _playerPositions[1] = positionsRoot.transform.Find("PlayerPosition_1");
            _playerPositions[2] = positionsRoot.transform.Find("PlayerPosition_2");
            _playerPositions[3] = positionsRoot.transform.Find("PlayerPosition_3");
            _playerPositions[4] = positionsRoot.transform.Find("PlayerPosition_4");
            _playerPositions[5] = positionsRoot.transform.Find("PlayerPosition_5");
        }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            Debug.Log($"Runner shutdown: {shutdownReason}");

            if (runner.GameMode == GameMode.Client)
            {
                ReturnToSessionScreen();
            }
        }

        private void ReturnToSessionScreen()
        {
            if (_runner != null)
            {
                _runner.Shutdown();
                _runner = null;
            }
            SceneManager.LoadScene(0);
        }

        // ====================================================================
        // HELPERS
        // ====================================================================
        public NetworkObject GetPlayerObject(PlayerRef pr)
        {
            return _spawnedPlayers.TryGetValue(pr, out var obj) ? obj : null;
        }
        public void RegisterSpawnedPlayer(PlayerRef player, NetworkObject obj)
        {
            _spawnedPlayers[player] = obj;
        }
        public static bool IsLobbyReady = false;

        public void OnConnectedToServer(NetworkRunner runner)
        {
            Debug.Log("[Fusion] Connected to server (NameServer).");
        }

        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {
            // This callback only fires after joining the lobby
            if (!IsLobbyReady)
            {
                IsLobbyReady = true;
                Debug.Log("[Fusion] Lobby ready.");
            }
            var sessions = BasicSpawner.Instance.Sessions;
            sessions.Clear();
            sessions.AddRange(sessionList);
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



        // ====================================================================
        // UNUSED CALLBACKS
        // ====================================================================
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

    }
}
