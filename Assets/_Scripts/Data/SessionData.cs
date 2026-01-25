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
        string displayName = info.Name;

        if (info.Properties != null &&
      info.Properties.TryGetValue("displayName", out SessionProperty val))
        {
            string dn = val;

            if (!string.IsNullOrEmpty(dn))
                gameNameText.text = dn;
            else
                gameNameText.text = sessionName;
        }
        else
        {
            gameNameText.text = sessionName;
        }

        playerCountText.text = $"{info.PlayerCount}/{info.MaxPlayers}";
        SessionStateType stateEnum = SessionStateType.Waiting;

        if (info.Properties != null &&
            info.Properties.TryGetValue("state", out SessionProperty stateProp))
        {
            int stateInt = (int)stateProp;
            stateEnum = (SessionStateType)stateInt;
        }

        statusText.text = stateEnum.ToString();

        bool isWaiting = stateEnum == SessionStateType.Waiting;
        bool hasSpace = info.PlayerCount < info.MaxPlayers;

        joinButton.interactable = isWaiting && hasSpace;

        joinButton.onClick.RemoveAllListeners();
        joinButton.onClick.AddListener(OnJoinClicked);
    }

    private void OnJoinClicked()
    {
        BasicSpawner.Instance.StartGame(GameMode.Client, sessionName);
    }



}
