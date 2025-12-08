using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using Fusion.Sockets;
using LoveLetter.Networking;
using System.Collections.Generic;

namespace LoveLetter.Login
{
    public class SessionView : Singleton<SessionView>, INetworkRunnerCallbacks
    {
        [Header("UI References")]
        [SerializeField] private Button createRoomButton;
        [SerializeField] private Button refreshButton;
        [Header("Sessions List")]
        [SerializeField] private Transform[] sessionSlots;
        [SerializeField] private SessionData sessionPrefab;

        [Header("Menus")]
        [SerializeField] private GameObject createNewGameMenu = null;
        [SerializeField] private GameObject sessionsMenu = null;


        private void Start()
        {
            createRoomButton.onClick.AddListener(OnCreateRoomClicked);
            refreshButton.onClick.AddListener(RefreshSessionList);

            BasicSpawner.Instance.Runner.AddCallbacks(this);

            RefreshSessionList();
        }

        private void OnCreateRoomClicked()
        {
            createNewGameMenu.SetActive(true);
            sessionsMenu.SetActive(false);

        }

        public void RefreshSessionList()
        {
            List<SessionInfo> sessions = BasicSpawner.Instance.Sessions;

            for (int i = 0; i < sessionSlots.Length; i++)
            {
                Transform slot = sessionSlots[i];

                // Clear existing session data in the slot
                foreach (Transform child in slot)
                    Destroy(child.gameObject);

                if (i < sessions.Count)
                {
                    // Instantiate a session entry inside the slot
                    SessionData entry = Instantiate(sessionPrefab, slot);
                    entry.Setup(sessions[i]);
                }
                else
                {
                    // Slot is empty; you can optionally hide it or leave empty
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



        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }

        public void OnInput(NetworkRunner runner, NetworkInput input) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
        public void OnConnectedToServer(NetworkRunner runner) { }
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
