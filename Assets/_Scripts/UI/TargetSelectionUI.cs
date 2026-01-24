using UnityEngine;
using UnityEngine.UI;
using LoveLetter.Networking;
using TMPro;

public class TargetSelectionUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Transform playerToggleContainer;
    [SerializeField] private PlayerSelectToggle playerTogglePrefab;
    [SerializeField] private Button confirmButton;
    [SerializeField] private GameObject TargetPanel;
    [SerializeField] private GameObject playerSection;
    [SerializeField] private GameObject cardSection;
    [SerializeField] private TextMeshProUGUI playerSectionText;
    [SerializeField] private TextMeshProUGUI cardSectionText;
    [Header("Baron UI")]
    [SerializeField] private GameObject baronUIPanel;
    [SerializeField] private TextMeshProUGUI baronCardVerdictText;
    [SerializeField] private CardView baronOpponentCard;
    [SerializeField] private CardView baronPlayerCard;
    [Header("Priest UI")]
    [SerializeField] private GameObject priestUIPanel;
    [SerializeField] private CardView priestOpponentCard;

    private int selectedPlayerSeat = -1;
    private int _currentCardId = -1;

    private CardType selectedCardGuess = CardType.Guard; // TEMP DEFAULT
    public static TargetSelectionUI Instance { get; private set; }

    private void Awake()
    {
        Instance = this;

        TargetPanel.SetActive(false);
        confirmButton.onClick.AddListener(OnConfirmClicked);
    }

    // ===============================
    // OPEN PANEL
    // ===============================
    public void OpenForPlayers(int cardId)
    {
        int localSeat = BasicSpawner.PlayerData.LocalSeatIndex;
        if (localSeat < 0)
        {
            Debug.LogError("[UI:TargetSelection] ERROR - Local seat not initialized yet!");
            return;
        }
        if (localSeat >= GameManager.Instance.Players.Count)
        {
            Debug.LogError($"[UI:TargetSelection] ERROR - Local seat {localSeat} out of Players.Count={GameManager.Instance.Players.Count}");
            return;
        }

        Clear();
        Debug.Log("[UI:TargetSelection] Opening with cardId=" + cardId);
        GameManager.Instance.DebugDumpState("TargetSelection");


        // Determine what should be shown
        bool allowSelfTarget = ConfigureForCard(cardId);

        // If UI is not needed, exit
        if (!TargetPanel.activeSelf)
            return;

        var playerObjects = FindObjectsOfType<Player>();
        ToggleGroup group = playerToggleContainer.GetComponent<ToggleGroup>();

        foreach (var p in playerObjects)
        {
            int seat = p.SeatIndex;
            var ps = GameManager.Instance.GetPlayer(seat);

            bool interactable =
                ps.IsAlive &&
                !ps.IsProtected &&
                (allowSelfTarget || seat != localSeat);

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

        // TargetPanel MUST stay active here
        TargetPanel.SetActive(true);
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

    private bool ConfigureForCard(int cardId)
    {
        _currentCardId = cardId;

        // Hide everything by default
        playerSection.SetActive(false);
        cardSection.SetActive(false);
        playerSectionText.gameObject.SetActive(false);
        cardSectionText.gameObject.SetActive(false);
        TargetPanel.SetActive(false);

        bool allowSelfTarget = true;

        switch (cardId)
        {
            case 0: // Spy
            case 4: // Handmaiden
            case 8: // Countess
            case 9: // Princess
                PlayWithoutContext();
                return false;

            case 1: // Guard
                TargetPanel.SetActive(true);
                playerSection.SetActive(true);
                playerSectionText.gameObject.SetActive(true);
                cardSection.SetActive(true);
                cardSectionText.gameObject.SetActive(true);
                allowSelfTarget = false;
                break;

            case 2: // Priest
                TargetPanel.SetActive(true);
                playerSection.SetActive(true);
                playerSectionText.gameObject.SetActive(true);
                break;

            case 3: // Baron
                TargetPanel.SetActive(true);
                playerSection.SetActive(true);
                playerSectionText.gameObject.SetActive(true);
                allowSelfTarget = false;
                break;

            case 5: // Prince
                TargetPanel.SetActive(true);
                playerSection.SetActive(true);
                playerSectionText.gameObject.SetActive(true);
                allowSelfTarget = true;
                break;

            case 6: // Chancellor
                    // No UI YET
                return false;

            case 7: // King
                TargetPanel.SetActive(true);
                playerSection.SetActive(true);
                playerSectionText.gameObject.SetActive(true);
                allowSelfTarget = false;
                break;
        }

        return allowSelfTarget;
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

        if (_currentCardId == 1) // Guard (needs target + guess)
        {
            GameManager.Instance.LocalPlayerConfirmedTarget(selectedPlayerSeat, selectedCardGuess);
        }
        else
        {
            // Priest, Baron, Prince, King → target-only
            GameManager.Instance.LocalPlayerPlayTargetOnly(selectedPlayerSeat);
        }


        TargetPanel.SetActive(false);
    }
    private void PlayWithoutContext()
    {
        GameManager.Instance.LocalPlayerPlayNoContext();
    }

    private void Clear()
    {
        foreach (Transform c in playerToggleContainer)
            Destroy(c.gameObject);

        selectedPlayerSeat = -1;
        selectedCardGuess = CardType.Guard;
    }

    public void Close()
    {
        TargetPanel.SetActive(false);
    }

    // ===============================
    // Roles
    // ===============================

    public void ShowBaronDuel(int myCard, int opponentCard, int result)
    {
        baronUIPanel.SetActive(true);

        baronPlayerCard.Setup(new Card((CardType)myCard));
        baronOpponentCard.Setup(new Card((CardType)opponentCard));

        switch (result)
        {
            case 1:
                baronCardVerdictText.text = "You Win!";
                break;
            case 0:
                baronCardVerdictText.text = "It's a Tie";
                break;
            case -1:
                baronCardVerdictText.text = "You Lose";
                break;
        }
    }

    public void ShowPriestCard(int cardType)
    {
        priestUIPanel.SetActive(true);

        priestOpponentCard.Setup(new Card((CardType)cardType));
    }
}
