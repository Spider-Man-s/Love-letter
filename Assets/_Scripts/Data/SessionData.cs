using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using LoveLetter.Networking;

public class SessionData : MonoBehaviour
{
    [SerializeField] private TMP_Text gameNameText;
    [SerializeField] private TMP_Text playerCountText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button joinButton;

    private string sessionName;

    public void Setup(SessionInfo info)
    {
        sessionName = info.Name;
        gameNameText.text = info.Name;
        playerCountText.text = $"{info.PlayerCount}/{info.MaxPlayers}";
        statusText.text = info.IsOpen ? "Waiting" : "Playing";

        joinButton.onClick.RemoveAllListeners();
        joinButton.onClick.AddListener(OnJoinClicked);
    }

    private void OnJoinClicked()
    {
        BasicSpawner.Instance.StartGame(
            GameMode.Client,
            sessionName,
            maxPlayers: 6,
            isPrivate: false
        );
    }
}
