using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using Fusion.Sockets;
using LoveLetter.Networking;
using System.Collections.Generic;
using System.Collections;

namespace LoveLetter.Login
{
    public class SessionView : Singleton<SessionView>, INetworkRunnerCallbacks
    {
        [Header("UI References")]
        [SerializeField] private Button createRoomButton;
        [SerializeField] private Button refreshButton;
        [SerializeField] private Button returnButton;
        [SerializeField] private Button joinCustomGameButton;

        [SerializeField] private TMP_InputField gameCodeInput;

        [Header("Sessions List")]
        [SerializeField] private Transform[] sessionSlots;
        [SerializeField] private SessionData sessionPrefab;

        [Header("Menus")]
        [SerializeField] private GameObject createNewGameMenu = null;
        [SerializeField] private GameObject sessionsMenu = null;
        [SerializeField] private GameObject playerMenu = null;

        private void Start()
        {
            createRoomButton.onClick.AddListener(OnCreateRoomClicked);
            refreshButton.onClick.AddListener(RefreshSessionList);

            joinCustomGameButton.onClick.AddListener(() => JoinCustomGame(gameCodeInput.text));

            returnButton.onClick.AddListener(() =>
            {
                sessionsMenu.SetActive(false);
                playerMenu.SetActive(true);
            });

            if (PlayerPrefs.GetInt("ReturnedFromGame", 0) == 1)
            {
                PlayerPrefs.SetInt("ReturnedFromGame", 0);

                BasicSpawner.Instance.ReconnectToLobby();

                playerMenu.SetActive(false);
                sessionsMenu.SetActive(true);
            }

            BasicSpawner.Instance.EnsureRunner();
            BasicSpawner.Instance.Runner.AddCallbacks(this);

            RefreshSessionList();
        }


        private void OnCreateRoomClicked()
        {
            createNewGameMenu.SetActive(true);
            sessionsMenu.SetActive(false);
        }
        public void OnConnectedToServer(NetworkRunner runner)
        {
            Debug.Log("[SessionView] Connected to server.");
        }


        // ====================================================================
        // JOIN BY CODE (Public or Private)
        // ====================================================================
        public void JoinCustomGame(string code)
        {
            code = code.Trim().ToUpper();

            if (string.IsNullOrEmpty(code))
            {
                StartCoroutine(ShowInvalidCodeFeedback());
                return;
            }

            // 1. Check if room exists in visible PUBLIC lobby list
            bool existsInLobby = BasicSpawner.Instance.Sessions
                .Exists(s => s.Name.Equals(code, System.StringComparison.OrdinalIgnoreCase));

            if (existsInLobby)
            {
                // Join public room directly
                Debug.Log($"Joining visible public room: {code}");
                BasicSpawner.Instance.StartClient(code);
                return;
            }

            // 2. If it's not in the lobby it may be PRIVATE → Attempt join
            Debug.Log($"Room not found in lobby, trying private join: {code}");
            TryJoinRoomByCode(code);
        }

        private async void TryJoinRoomByCode(string code)
        {
            var result = await BasicSpawner.Instance.Runner.StartGame(new StartGameArgs()
            {
                GameMode = GameMode.Client,
                SessionName = code,
                SceneManager = BasicSpawner.Instance.Runner.gameObject.GetComponent<NetworkSceneManagerDefault>()
            });

            if (!result.Ok)
            {
                Debug.LogWarning($"Invalid room code: {code}");
                StartCoroutine(ShowInvalidCodeFeedback());
            }
        }

        private IEnumerator ShowInvalidCodeFeedback()
        {
            gameCodeInput.textComponent.color = Color.red;
            yield return new WaitForSeconds(0.5f);
            gameCodeInput.textComponent.color = Color.white;
        }

        // ====================================================================
        // SESSION LIST
        // ====================================================================
        public void RefreshSessionList()
        {
            List<SessionInfo> sessions = BasicSpawner.Instance.Sessions;

            for (int i = 0; i < sessionSlots.Length; i++)
            {
                Transform slot = sessionSlots[i];

                foreach (Transform child in slot)
                    Destroy(child.gameObject);

                if (i < sessions.Count)
                {
                    SessionData entry = Instantiate(sessionPrefab, slot);
                    entry.Setup(sessions[i]);
                }
            }
        }

        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {
            var sessions = BasicSpawner.Instance.Sessions;
            sessions.Clear();
            sessions.AddRange(sessionList);
            RefreshSessionList();
        }

        // ====================================================================
        // UNUSED CALLBACKS
        // ====================================================================
        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
        public void OnInput(NetworkRunner runner, NetworkInput input) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }

        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    }
}
