using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using LoveLetter.Networking;
using TMPro;
using System.Linq;
using UnityEngine.SceneManagement;
using Fusion;
using LoveLetter.Networking;

public class ReturnToSessions : MonoBehaviour
{
    public GameObject beginScreenPanel;
    public GameObject sessionPanel;
    public GameObject lobbyScreen;

    void Start()
    {
        bool fromGame = PlayerPrefs.GetInt("ReturnedFromGame", 0) == 1;

        if (fromGame)
        {
            PlayerPrefs.SetInt("ReturnedFromGame", 0);

            beginScreenPanel.SetActive(false);
            sessionPanel.SetActive(true);
            lobbyScreen.SetActive(true);
        }
    }
}
