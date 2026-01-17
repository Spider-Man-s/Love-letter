using LoveLetter.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

namespace LoveLetter.Login
{
    public class LoginView : MonoBehaviour
    {
        [Header("UI References")]

        [Header("Buttons")]
        [SerializeField] private Button _startButton = null;
        [SerializeField] private Button _quitButton = null;
        [SerializeField] private Button _confirmPlayerInfoButton = null;
        [SerializeField] private Button _returnButton = null;

        [Header("Text")]
        [SerializeField] private TextMeshProUGUI _notificationText = null;
        [SerializeField] private TextMeshProUGUI _autoFillPlayerName = null;
        [SerializeField] private TMP_InputField _playerName = null;

        [Header("Menus (assign EXACTLY as described)")]
        [SerializeField] private GameObject _mainMenu = null;        // BeginScreen
        [SerializeField] private GameObject _lobbyMenu = null;       // LobbyScreen
        [SerializeField] private GameObject _playerNameMenu = null;  // LobbyScreen/PlayerMenu
        [SerializeField] private GameObject _sessionsMenu = null;    // LobbyScreen/SessionsScreen

        [Header("Avatar Selection")]
        [SerializeField] private ToggleGroup AvatarToggleGroup = null;

        private const string _chars = "abcdefghijklmnoprstuzv";

        private void Awake()
        {

            // Generate random name
            string randWord = "";
            for (int i = 0; i < 8; i++)
                randWord += _chars[Random.Range(0, _chars.Length - 1)];
            _autoFillPlayerName.text = randWord;

            // START BUTTON: go to lobby + player menu
            _startButton.onClick.AddListener(() =>
            {
                _mainMenu.SetActive(false);           // BeginScreen OFF
                _lobbyMenu.SetActive(true);           // LobbyScreen ON

                _playerNameMenu.SetActive(true);      // Show PlayerMenu
                _sessionsMenu.SetActive(false);       // Hide SessionsScreen
            });

            // QUIT BUTTON
            _quitButton.onClick.AddListener(() =>
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            });

            // RETURN BUTTON: back to BeginScreen
            _returnButton.onClick.AddListener(() =>
            {
                _mainMenu.SetActive(true);            // BeginScreen ON
                _lobbyMenu.SetActive(false);          // LobbyScreen OFF
                _playerNameMenu.SetActive(false);     // PlayerMenu OFF
                _sessionsMenu.SetActive(false);       // SessionsScreen OFF
            });

            // CONFIRM PLAYER INFO
            _confirmPlayerInfoButton.onClick.AddListener(OnConfirmPlayerInfo);
        }

        private void OnConfirmPlayerInfo()
        {
            Debug.Log("Confirm Player Info button clicked");

            string enteredName = _playerName.text;

            if (string.IsNullOrEmpty(enteredName))
                enteredName = _autoFillPlayerName.text;

            if (enteredName.Length > 15)
            {
                _notificationText.text = "Your name can be up to 15 characters long";
                _notificationText.gameObject.SetActive(true);
                return;
            }

            _notificationText.gameObject.SetActive(false);

            int selectedAvatarId = GetSelectedIndex();
            if (selectedAvatarId == -1)
            {
                Debug.LogWarning("No avatar selected, defaulting to 0");
                selectedAvatarId = 0;
            }

            PlayerPrefs.SetInt("SelectedAvatarId", selectedAvatarId);

            BasicSpawner.Instance.ConnectToLobby(enteredName, selectedAvatarId);

            // Switch UI screens
            _playerNameMenu.SetActive(false);
            _sessionsMenu.SetActive(true);
        }

        public int GetSelectedIndex()
        {
            var selected = AvatarToggleGroup.ActiveToggles().FirstOrDefault();
            if (selected == null)
                return -1;

            Debug.Log("Selected avatar index: " + selected.transform.GetSiblingIndex());
            return selected.transform.GetSiblingIndex();
        }
    }
}
