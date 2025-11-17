using LoveLetter.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LoveLetter.Login
{
    public class LoginView : MonoBehaviour
    {
        [SerializeField] private TMP_InputField _playerName = null;
        [SerializeField] private Button _startButton = null;
        [SerializeField] private Button _quitButton = null;
        [SerializeField] private Button _confirmNameButton = null;
        [SerializeField] private Button _returnButton = null;
        [SerializeField] private TextMeshProUGUI _notificationText = null;
        [SerializeField] private GameObject _mainMenu = null;
        [SerializeField] private GameObject _lobbyMenu = null;
        [SerializeField] private GameObject _playerNameMenu = null;
        [SerializeField] private GameObject _newRoomMenu = null;
        [SerializeField] private GameObject _sessionsMenu = null;


        private void Awake()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;

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



            /*
                        if (BasicSpawner.Instance.PlayerName != null)
                        {
                            _mainMenu.SetActive(false);
                            _lobbyMenu.SetActive(true);
                            BasicSpawner.Instance.ConnectToLobby();
                        }
                        */
        }

        /*
                private void LoginPressed()
                {
                    if (_playerName.text.Length > 15)
                    {
                        _notificationText.text = "Your name can be 15 characters long.";
                        _notificationText.gameObject.SetActive(true);
                        return;
                    }
                    else
                    {
                        _notificationText.gameObject.SetActive(false);
                    }

                    _startButton.interactable = false;
                    _playerName.interactable = false;

                    BasicSpawner.Instance.ConnectToLobby(_playerName.text);
                    _mainMenu.SetActive(false);
                    _lobbyMenu.SetActive(true);
                }

        */
    }
}