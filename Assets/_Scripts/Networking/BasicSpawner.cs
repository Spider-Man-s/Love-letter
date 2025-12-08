using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LoveLetter.Networking
{
    public class BasicSpawner : SingletonPersistent<BasicSpawner>, INetworkRunnerCallbacks
    {
        [Header("Player Prefabs")]
        [SerializeField] private NetworkPrefabRef _playerPrefabOWN;
        [SerializeField] private NetworkPrefabRef _playerPrefabEnemy;

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



        private void Awake()
        {
            base.Awake();
            _runner = GetComponent<NetworkRunner>();
            _networkSceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();
        }


        public void ConnectToLobby(string playerName)
        {
            if (!string.IsNullOrEmpty(playerName))
                _playerName = playerName;

            _runner.JoinSessionLobby(SessionLobby.ClientServer);
        }


        public async void StartHost()
        {
            PlayerData.LocalPlayerName = _playerName;

            await _runner.StartGame(new StartGameArgs()
            {
                GameMode = GameMode.Host,
                SessionName = "LoveLetterRoom",
                Scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex + 1),
                SceneManager = _networkSceneManager
            });
        }


        public async void StartClient()
        {
            PlayerData.LocalPlayerName = _playerName;

            await _runner.StartGame(new StartGameArgs()
            {
                GameMode = GameMode.Client,
                SessionName = "LoveLetterRoom",
                SceneManager = _networkSceneManager
            });
        }


        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            if (!runner.IsServer)
                return;

            int seatIndex = player.RawEncoded;

            Transform spawnPoint = PlayerPositions[seatIndex];

            NetworkPrefabRef prefab = (player == runner.LocalPlayer)
                ? _playerPrefabOWN
                : _playerPrefabEnemy;

            NetworkObject obj = runner.Spawn(prefab, spawnPoint.position, spawnPoint.rotation, player);

            _spawnedPlayers.Add(player, obj);
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

        public void OnInput(NetworkRunner runner, NetworkInput input) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
        public void OnConnectedToServer(NetworkRunner runner) { }
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { _sessions = sessionList; }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }

        public void OnSceneLoadStart(NetworkRunner runner) { }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    }

    // Helper to store UI player name for spawning
    public static class PlayerData
    {
        public static string LocalPlayerName;
    }
}
