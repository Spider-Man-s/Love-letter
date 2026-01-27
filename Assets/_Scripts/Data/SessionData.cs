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

        /* ---------------------------------------------------------
         * DISPLAY NAME
         * --------------------------------------------------------- */
        if (info.Properties != null &&
            info.Properties.TryGetValue("displayName", out SessionProperty val))
        {
            string dn = val;
            gameNameText.text = string.IsNullOrEmpty(dn) ? sessionName : dn;
        }
        else
        {
            gameNameText.text = sessionName;
        }

        /* ---------------------------------------------------------
         * CLOSED ROOM CHECK
         * --------------------------------------------------------- */
        bool isClosedRoom =
            (info.Properties != null &&
             info.Properties.TryGetValue("closed", out SessionProperty closedProp) &&
             (int)closedProp == 1);

        if (isClosedRoom)
        {
            // Force UI state
            playerCountText.text = "-";
            statusText.text = "Closed";
            joinButton.interactable = false;

            Debug.Log($"[SessionData] Room {sessionName} is CLOSED → hiding join.");
            return;
        }

        /* ---------------------------------------------------------
         * PLAYER COUNT
         * --------------------------------------------------------- */
        playerCountText.text = $"{info.PlayerCount}/{info.MaxPlayers}";

        /* ---------------------------------------------------------
         * STATE (Waiting / Playing / Finished)
         * --------------------------------------------------------- */
        SessionStateType stateEnum = SessionStateType.Waiting;

        if (info.Properties != null &&
            info.Properties.TryGetValue("state", out SessionProperty stateProp))
        {
            stateEnum = (SessionStateType)(int)stateProp;
        }

        statusText.text = stateEnum.ToString();

        /* ---------------------------------------------------------
         * ENABLE/DISABLE JOIN BUTTON
         * --------------------------------------------------------- */
        bool isWaiting = stateEnum == SessionStateType.Waiting;
        bool hasSpace = info.PlayerCount < info.MaxPlayers;

        joinButton.interactable = isWaiting && hasSpace;

        /* ---------------------------------------------------------
         * CLICK -> JOIN
         * --------------------------------------------------------- */
        joinButton.onClick.RemoveAllListeners();
        joinButton.onClick.AddListener(OnJoinClicked);
    }

    private void OnJoinClicked()
    {
        BasicSpawner.Instance.StartGame(GameMode.Client, sessionName);
    }



}
