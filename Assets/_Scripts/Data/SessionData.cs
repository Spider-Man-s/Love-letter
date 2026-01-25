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

        SessionStateType stateEnum = SessionStateType.Waiting;

        if (info.Properties != null &&
            info.Properties.TryGetValue("state", out SessionProperty value))
        {
            int val = (int)value;
            stateEnum = (SessionStateType)val;
        }

        statusText.text = stateEnum.ToString();


        joinButton.interactable = stateEnum == SessionStateType.Waiting;

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
