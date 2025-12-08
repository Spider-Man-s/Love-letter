using LoveLetter.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LoveLetter.Login
{
    public class LoginView : MonoBehaviour
    {
        [Header("UI References")]

        [Header("Buttons")]
        [SerializeField] private Button _startButton = null;
        [SerializeField] private Button _quitButton = null;
        [SerializeField] private Button _confirmNameButton = null;
        [SerializeField] private Button _returnButton = null;
        [Header("Text")]
        [SerializeField] private TextMeshProUGUI _notificationText = null;
        [SerializeField] private TextMeshProUGUI _autoFillPlayerName = null;
        [SerializeField] private TMP_InputField _playerName = null;
        [Header("Menus")]
        [SerializeField] private GameObject _mainMenu = null;
        [SerializeField] private GameObject _lobbyMenu = null;
        [SerializeField] private GameObject _playerNameMenu = null;
        [SerializeField] private GameObject _sessionsMenu = null;

        private const string _chars = "abcdefghijklmnoprstuzv";
        private void Awake()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;

            string randWord = "";
            for (int i = 0; i < 8; i++) randWord += _chars[Random.Range(0, _chars.Length - 1)];
            _autoFillPlayerName.text = randWord;

            _startButton.onClick.AddListener(() =>
            {
                _mainMenu.SetActive(false);
                _lobbyMenu.SetActive(true);
                _playerNameMenu.SetActive(true);
            });

            _quitButton.onClick.AddListener(() =>
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            });

            _returnButton.onClick.AddListener(() =>
            {
                _mainMenu.SetActive(true);
                _lobbyMenu.SetActive(false);
                _playerNameMenu.SetActive(false);
            });

            _confirmNameButton.onClick.AddListener(() =>
            {
                string enteredName = _playerName.text;
                if (string.IsNullOrEmpty(enteredName))
                {
                    enteredName = _autoFillPlayerName.text;
                }

                if (enteredName.Length > 15)
                {
                    _notificationText.text = "Your name can be up to 15 characters long";
                    _notificationText.gameObject.SetActive(true);
                    return;
                }
                else
                {
                    _notificationText.gameObject.SetActive(false);
                }

                BasicSpawner.Instance.ConnectToLobby(enteredName);
                _playerNameMenu.SetActive(false);
                _sessionsMenu.SetActive(true);
            });


        }
    }
}