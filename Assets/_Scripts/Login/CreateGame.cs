using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LoveLetter.Networking;

public class CreateGame : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_InputField gameNameInput;
    [SerializeField] private TMP_Text autoFillGameName;
    [SerializeField] private Slider playerCountSlider;
    [SerializeField] private TMP_Text playerCountText;
    [SerializeField] private Toggle privateToggle;
    [SerializeField] private Button createButton;
    [SerializeField] private Button returnButton;
    [SerializeField] private GameObject createGameMenu;
    [SerializeField] private GameObject sessionsMenu;
    private const string _chars = "abcdefghijklmnoprstuzv";
    private string sessionName = null;

    private void Awake()
    {
        string randWord = "";
        for (int i = 0; i < 8; i++) randWord += _chars[Random.Range(0, _chars.Length - 1)];
        autoFillGameName.text = randWord;
    }
    private void Start()
    {
        createButton.onClick.AddListener(OnCreateClicked);
        returnButton.onClick.AddListener(OnReturnClicked);
    }

    private void Update()
    {
        playerCountText.text = Mathf.RoundToInt(playerCountSlider.value).ToString() + "/6";
    }

    private void OnCreateClicked()
    {
        if (string.IsNullOrEmpty(sessionName))
        {
            sessionName = autoFillGameName.text;
            return;
        }
        else
        {
            sessionName = gameNameInput.text;
        }

        int maxPlayers = Mathf.RoundToInt(playerCountSlider.value);
        bool isPrivate = privateToggle.isOn;

        Debug.Log($"Creating room: {sessionName}, max players: {maxPlayers}, private: {isPrivate}");

        BasicSpawner.Instance.CreateRoom(sessionName, maxPlayers, isPrivate);
    }
    private void OnReturnClicked()
    {
        createGameMenu.SetActive(false);
        sessionsMenu.SetActive(true);
    }
}
