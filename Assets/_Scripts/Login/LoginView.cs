using LoveLetter.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LoveLetter.Login
{
    public class LoginView : MonoBehaviour
    {
        [SerializeField] private TMP_InputField _playerName = null;
        [SerializeField] private Button _loginButton = null;
        [SerializeField] private Button _quitButton = null;
        [SerializeField] private TextMeshProUGUI _notificationText = null;
        [SerializeField] private GameObject _sessionExplorer = null;
        [SerializeField] private GameObject _login = null;

        private void Awake()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;

            _loginButton.onClick.AddListener(LoginPressed);
            _quitButton.onClick.AddListener(() =>
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            });

            if (BasicSpawner.Instance.PlayerName != null)
            {
                _login.SetActive(false);
                _sessionExplorer.SetActive(true);
                BasicSpawner.Instance.ConnectToLobby();
            }
        }

        private void LoginPressed()
        {
            if (_playerName.text.Length < 5)
            {
                _notificationText.text = "Your name must be at least 5 characters long.";
                _notificationText.gameObject.SetActive(true);
                return;
            }
            else
            {
                _notificationText.gameObject.SetActive(false);
            }

            _loginButton.interactable = false;
            _playerName.interactable = false;

            BasicSpawner.Instance.ConnectToLobby(_playerName.text);
            _login.SetActive(false);
            _sessionExplorer.SetActive(true);
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}