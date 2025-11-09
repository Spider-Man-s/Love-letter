using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.VisualScripting;
using WebSocketSharp;

/*
using SpellFlinger.PlayScene;
using SpellFlinger.Enum;
using SpellFlinger.LoginScene;

 */
namespace LoveLetter.Networking
{
    public class BasicSpawner : SingletonPersistent<BasicSpawner>, INetworkRunnerCallbacks
    {
        private static string _playerName = null;
        // [SerializeField] private PlayerCharacterController _playerPrefab = null;
        // [SerializeField] private GameManager _gameManagerPrefab = null;
        // [SerializeField] private NetworkRunner _networkRunnerPrefab = null;
        [SerializeField] private int _playerCount = 10;
        private NetworkRunner _runner = null;
        private NetworkSceneManagerDefault _networkSceneManager = null;
        private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();
        private List<SessionInfo> _sessions = new List<SessionInfo>();
        // private static GameModeType _gameModeType;

        // public PlayerCharacterController LocalCharacterController { get; set; }
        public List<SessionInfo> Sessions => _sessions;
        // public static GameModeType GameModeType => _gameModeType;

        public string PlayerName => _playerName;

        private void Awake()
        {
            base.Awake();
            _networkSceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();
            _runner = gameObject.AddComponent<NetworkRunner>();
        }

        public void ConnectToLobby(String playerName = null)
        {
            if (!playerName.IsNullOrEmpty()) _playerName = playerName;
            _runner.JoinSessionLobby(SessionLobby.ClientServer);
        }
        // Lab2 -> CreateSession
        async void StartGame(GameMode mode)
        {
            // Create the Fusion runner and let it know that we will be providing user input
            _runner = gameObject.AddComponent<NetworkRunner>();
            _runner.ProvideInput = true;

            // Create the NetworkSceneInfo from the current scene
            var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
            var sceneInfo = new NetworkSceneInfo();
            if (scene.IsValid)
            {
                sceneInfo.AddSceneRef(scene, LoadSceneMode.Additive);
            }

            // Start or join (depends on gamemode) a session with a specific name
            await _runner.StartGame(new StartGameArgs()
            {
                GameMode = mode,
                SessionName = "TestRoom",
                Scene = scene,
                SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
            });
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {

        }
        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {

        }
        public void OnInput(NetworkRunner runner, NetworkInput input)
        {

        }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
        {

        }
        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {

        }
        public void OnConnectedToServer(NetworkRunner runner)
        {

        }
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {

        }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
        {

        }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {

        }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
        {

        }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {

        }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
        {

        }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
        {

        }
        public void OnSceneLoadDone(NetworkRunner runner)
        {

        }
        public void OnSceneLoadStart(NetworkRunner runner)
        {

        }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {

        }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {

        }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
        {

        }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
        {

        }
    }

}
