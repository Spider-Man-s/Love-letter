using UnityEngine;
using UnityEngine.UI;
using Fusion;
using LoveLetter.Networking;

public class TableUIController : MonoBehaviour
{
    [SerializeField] private Button startGameButton;

    void Start()
    {
        // Hide for everyone first
        startGameButton.gameObject.SetActive(false);

        // Show ONLY for host/server
        if (BasicSpawner.Instance.Runner.IsServer)
            startGameButton.gameObject.SetActive(true);

        startGameButton.onClick.AddListener(OnStartGameClicked);
    }

    private void OnStartGameClicked()
    {
        GameManager.Instance.BeginMatch();
        startGameButton.gameObject.SetActive(false);
    }
}
