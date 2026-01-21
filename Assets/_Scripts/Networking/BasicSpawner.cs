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



        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            if (!runner.IsServer)
                return;

            // 1. Spawn GameManager once on the server
            if (gameManagerInstance == null)
            {
                gameManagerInstance = runner.Spawn(_gameManagerPrefab, Vector3.zero, Quaternion.identity);
                Debug.Log("GameManager network-spawned.");
            }

            // 2. Seat assignment
            int seatIndex = runner.ActivePlayers
                .OrderBy(p => p.RawEncoded)
                .ToList()
                .IndexOf(player);

            UnityEngine.Debug.Log($"[OnPlayerJoined] Server assigning seat {seatIndex} to {player}");

            if (seatIndex >= PlayerPositions.Length)
            {
                Debug.LogWarning("Max players reached!");
                return;
            }

            // 3. Player spawn
            NetworkObject obj = runner.Spawn(_playerPrefab, Vector3.zero, Quaternion.identity, player);
            _spawnedPlayers.Add(player, obj);

            Player p = obj.GetComponent<Player>();
            p.SeatIndex = seatIndex;

            // 4. Now GameManager.Instance is guaranteed to exist
            GameManager.Instance.RegisterNetworkPlayer(player, seatIndex);

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

        public void OnSceneLoadDone(NetworkRunner runner)
        {
            GameObject positionsRoot = GameObject.Find("Players");

            if (positionsRoot == null)
            {
                Debug.LogError("PlayerPositions object not found in the scene!");
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


        public NetworkObject GetPlayerObject(PlayerRef pr)
        {
            return _spawnedPlayers.TryGetValue(pr, out var obj) ? obj : null;
        }
        public PlayerRef GetPlayerRefBySeat(int seatIndex)
        {
            foreach (var kvp in GameManager.Instance.GetSeatDictionary())
            {
                if (kvp.Value == seatIndex)
                    return kvp.Key;
            }

            Debug.LogError($"[BasicSpawner] No PlayerRef found for seat {seatIndex}");
            return PlayerRef.None;
        }



        public void OnInput(NetworkRunner runner, NetworkInput input) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }

        public void OnConnectedToServer(NetworkRunner runner) { }
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
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {
            var sessions = BasicSpawner.Instance.Sessions;
            sessions.Clear();
            sessions.AddRange(sessionList);
        }
    }
}
