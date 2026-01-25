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
    private const string _chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private string sessionName = null;
    private string randWord = "";

    private void Awake()
    {
        randWord = BasicSpawner.RoomCodeGenerator.Generate().ToUpper();
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
        createButton.interactable = BasicSpawner.IsLobbyReady;
    }

    private void OnCreateClicked()
    {
        Debug.Log("[CreateGame] Create button clicked.");


        string realRoomCode = randWord;

        string displayName = string.IsNullOrEmpty(gameNameInput.text)
            ? randWord
            : gameNameInput.text;

        int maxPlayers = Mathf.RoundToInt(playerCountSlider.value);
        bool isPrivate = privateToggle.isOn;

        Debug.Log($"Creating room: {realRoomCode}, displayName: {displayName}, private: {isPrivate}");

        BasicSpawner.Instance.CreateRoom(realRoomCode, maxPlayers, isPrivate, displayName);
    }
    private void OnReturnClicked()
    {
        createGameMenu.SetActive(false);
        sessionsMenu.SetActive(true);
    }
}
