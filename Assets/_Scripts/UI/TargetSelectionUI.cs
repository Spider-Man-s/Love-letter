using UnityEngine;
using UnityEngine.UI;
using LoveLetter.Networking;

public class TargetSelectionUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Transform playerToggleContainer;
    [SerializeField] private PlayerSelectToggle playerTogglePrefab;
    [SerializeField] private Button confirmButton;
    [SerializeField] private GameObject panelRoot;

    private int selectedPlayerSeat = -1;
    private CardType selectedCardGuess = CardType.Guard; // TEMP DEFAULT
    public static TargetSelectionUI Instance { get; private set; }

    private void Awake()
    {
        Instance = this;

        panelRoot.SetActive(false);
        confirmButton.onClick.AddListener(OnConfirmClicked);
    }

    // ===============================
    // OPEN PANEL
    // ===============================
    public void OpenForPlayers(bool allowSelfTarget = true)
    {
        Clear();

        var playerObjects = FindObjectsOfType<Player>();
        int localSeat = BasicSpawner.PlayerData.LocalSeatIndex;
        ToggleGroup group = playerToggleContainer.GetComponent<ToggleGroup>();

        foreach (var p in playerObjects)
        {
            int seat = p.SeatIndex;
            var ps = GameManager.Instance.GetPlayer(seat);

            bool interactable =
                ps.IsAlive &&
                !ps.IsProtected &&
                (allowSelfTarget || seat != localSeat); //here goes if the card played is 3, he cant interact with himself

            var ui = Instantiate(playerTogglePrefab, playerToggleContainer);

            ui.Setup(
                seat,
                p.PlayerName.ToString(),
                AvatarScriptable.Instance.avatars[p.AvatarId],
                interactable,
                group,
                OnPlayerSelected
            );
        }

        panelRoot.SetActive(true);
    }

    // ===============================
    // CALLBACKS
    // ===============================
    private void OnPlayerSelected(int seatIndex)
    {
        selectedPlayerSeat = seatIndex;
        Debug.Log($"[UI] Selected target seat {seatIndex}");
    }

    public void SetCardGuess(int typeInt)
    {
        switch (typeInt)
        {
            case 0: selectedCardGuess = CardType.Spy; break;
            case 2: selectedCardGuess = CardType.Priest; break;
            case 3: selectedCardGuess = CardType.Baron; break;
            case 4: selectedCardGuess = CardType.Handmaiden; break;
            case 5: selectedCardGuess = CardType.Prince; break;
            case 6: selectedCardGuess = CardType.Chancellor; break;
            case 7: selectedCardGuess = CardType.King; break;
            case 8: selectedCardGuess = CardType.Countess; break;
            case 9: selectedCardGuess = CardType.Princess; break;
            default:
                Debug.LogWarning("Unknown guessed type ID: " + typeInt);
                return;
        }

        Debug.Log("[UI] Card guess set to: " + selectedCardGuess);
    }


    // ===============================
    // CONFIRM
    // ===============================
    private void OnConfirmClicked()
    {
        if (selectedPlayerSeat < 0)
        {
            Debug.LogWarning("No target selected.");
            return;
        }

        GameManager.Instance.LocalPlayerConfirmedTarget(
            selectedPlayerSeat,
            selectedCardGuess
        );

        panelRoot.SetActive(false);
    }

    private void Clear()
    {
        foreach (Transform c in playerToggleContainer)
            Destroy(c.gameObject);

        selectedPlayerSeat = -1;
        selectedCardGuess = CardType.Guard;
    }
}
