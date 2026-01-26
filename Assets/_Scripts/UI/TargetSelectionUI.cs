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
    [SerializeField] private Button discardButton;
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

    [Header("Chancellor UI (Card 6)")]
    [SerializeField] private GameObject chancellorPanel;
    [SerializeField] private CardView chancellorCard1;
    [SerializeField] private CardView chancellorCard2;
    [SerializeField] private CardView chancellorCard3;
    [SerializeField] private TMP_Dropdown chancellorDrop1;
    [SerializeField] private TMP_Dropdown chancellorDrop2;
    [SerializeField] private TMP_Dropdown chancellorDrop3;
    [SerializeField] private Button chancellorConfirmButton;


    private int selectedPlayerSeat = -1;
    private int _currentCardId = -1;

    private CardType selectedCardGuess = CardType.Guard; // TEMP DEFAULT
    public static TargetSelectionUI Instance { get; private set; }

    private void Awake()
    {
        Instance = this;

        TargetPanel.SetActive(false);
        confirmButton.onClick.AddListener(OnConfirmClicked);
        discardButton.onClick.AddListener(OnDiscardClicked);
        discardButton.gameObject.SetActive(false);
        chancellorConfirmButton.onClick.AddListener(OnChancellorConfirm);
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

        // --- DEBUG BLOCK YOU REQUESTED ---
        Debug.Log("=== DEBUG TARGET UI — PLAYER LIST ===");
        for (int i = 0; i < GameManager.Instance.Players.Count; i++)
        {
            var psDbg = GameManager.Instance.Players[i];
            if (psDbg == null)
                Debug.Log($"Players[{i}] = NULL");
            else
                Debug.Log($"Players[{i}] Alive={psDbg.IsAlive} HandCount={psDbg.Hand.Count}");
        }
        Debug.Log("=== END DEBUG TARGET UI ===");
        // ----------------------------------

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

            // --- CRASH FIX: prevent null reference ---
            if (seat < 0 || seat >= GameManager.Instance.Players.Count)
            {
                Debug.LogError($"[TargetSelectionUI] ERROR: Seat {seat} outside Players.Count={GameManager.Instance.Players.Count}");
                continue; // skip invalid seats
            }

            var ps = GameManager.Instance.GetPlayer(seat);
            if (ps == null)
            {
                Debug.LogError($"[TargetSelectionUI] ERROR: Players[{seat}] is NULL");
                continue; // prevents the nullref
            }
            // -----------------------------------------

            bool interactable =
                ps.IsAlive &&
                !GetNetProtection(seat) &&
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

    private bool GetNetProtection(int seat)
    {
        foreach (var p in FindObjectsOfType<Player>())
        {
            if (p.SeatIndex == seat)
                return p.IsProtectedNet;
        }
        return false;
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
                allowSelfTarget = false;
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
                GameManager.Instance.LocalPlayerPlayNoContext();
                return false;

            case 7: // King
                TargetPanel.SetActive(true);
                playerSection.SetActive(true);
                playerSectionText.gameObject.SetActive(true);
                allowSelfTarget = false;
                break;
        }
        bool hasValidTargets = CheckForValidTargets(allowSelfTarget);
        discardButton.gameObject.SetActive(!hasValidTargets);
        confirmButton.interactable = hasValidTargets;
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

        ClearGroup();
        TargetPanel.SetActive(false);
    }

    public void ClearGroup()
    {// Clear playerSection
        if (playerSection != null)
        {
            Toggle[] playerToggles = playerSection.GetComponentsInChildren<Toggle>();
            foreach (var t in playerToggles)
            {
                t.SetIsOnWithoutNotify(false);
            }
        }

        // Clear cardSection
        if (cardSection != null)
        {
            Toggle[] cardToggles = cardSection.GetComponentsInChildren<Toggle>();
            foreach (var t in cardToggles)
            {
                t.SetIsOnWithoutNotify(false);
            }
        }
    }
    private void OnDiscardClicked()
    {
        Debug.Log("[UI] Discard clicked — playing card with no target");

        TargetPanel.SetActive(false);

        GameManager.Instance.LocalPlayerPlayNoContext();
    }

    private bool CheckForValidTargets(bool allowSelfTarget)
    {
        int mySeat = BasicSpawner.PlayerData.LocalSeatIndex;

        foreach (var p in GameManager.Instance.Players)
        {
            int seat = p.PlayerId;

            bool valid =
                p.IsAlive &&
                !GetNetProtection(p.PlayerId) &&
                (allowSelfTarget || seat != mySeat);

            if (valid)
                return true;
        }

        return false;
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
        baronUIPanel.SetActive(false);
        priestUIPanel.SetActive(false);
        chancellorPanel.SetActive(false);

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

    public void OpenChancellorUI(int deckBeforeDraw)
    {
        Debug.Log("[CLIENT UI] Opening Chancellor with deckBeforeDraw = " + deckBeforeDraw);

        Clear();
        TargetPanel.SetActive(false);
        TableUIController.Instance.SetLocalHandInteractable(false);

        chancellorPanel.SetActive(true);

        var hand = GameManager.Instance
            .GetPlayer(BasicSpawner.PlayerData.LocalSeatIndex)
            .Hand;

        int handCount = hand.Count;
        int originalDeck = deckBeforeDraw;

        // ===========================
        // CASE 0 — deck was 0
        // ===========================
        if (originalDeck == 0)
        {
            Debug.Log("[ChancellorUI] Auto-discard mode.");

            chancellorCard1.gameObject.SetActive(false);
            chancellorCard2.gameObject.SetActive(false);
            chancellorCard3.gameObject.SetActive(false);

            chancellorDrop1.gameObject.SetActive(false);
            chancellorDrop2.gameObject.SetActive(false);
            chancellorDrop3.gameObject.SetActive(false);

            chancellorConfirmButton.gameObject.SetActive(false);

            discardButton.gameObject.SetActive(true);
            discardButton.onClick.RemoveAllListeners();
            discardButton.onClick.AddListener(() =>
            {
                Debug.Log("[ChancellorUI] Discard confirmed.");
                chancellorPanel.SetActive(false);
                GameManager.Instance.LocalPlayerPlayNoContext();
            });

            return;
        }

        // ===========================
        // CASE 1 — only 1 card available
        // ===========================
        if (originalDeck == 1)
        {
            Debug.Log("[ChancellorUI] 2-card mode.");

            chancellorCard1.gameObject.SetActive(true);
            chancellorCard2.gameObject.SetActive(true);
            chancellorCard3.gameObject.SetActive(false);

            chancellorDrop1.gameObject.SetActive(true);
            chancellorDrop2.gameObject.SetActive(true);
            chancellorDrop3.gameObject.SetActive(false);

            chancellorCard1.Setup(hand[0]);
            chancellorCard2.Setup(hand[1]);

            chancellorDrop1.value = 0;
            chancellorDrop2.value = 1;
            chancellorDrop3.value = 2;

            chancellorDrop1.onValueChanged.AddListener(_ => ValidateChancellor());
            chancellorDrop2.onValueChanged.AddListener(_ => ValidateChancellor());

            ValidateChancellor();
            return;
        }

        // ===========================
        // CASE 2 — normal mode (2+)
        // ===========================
        Debug.Log("[ChancellorUI] 3-card mode.");

        chancellorCard1.gameObject.SetActive(true);
        chancellorCard2.gameObject.SetActive(true);
        chancellorCard3.gameObject.SetActive(true);

        chancellorDrop1.gameObject.SetActive(true);
        chancellorDrop2.gameObject.SetActive(true);
        chancellorDrop3.gameObject.SetActive(true);

        chancellorCard1.Setup(hand[0]);
        chancellorCard2.Setup(hand[1]);
        chancellorCard3.Setup(hand[2]);

        chancellorDrop1.value = 0;
        chancellorDrop2.value = 1;
        chancellorDrop3.value = 2;

        chancellorDrop1.onValueChanged.AddListener(_ => ValidateChancellor());
        chancellorDrop2.onValueChanged.AddListener(_ => ValidateChancellor());
        chancellorDrop3.onValueChanged.AddListener(_ => ValidateChancellor());

        ValidateChancellor();
    }


    private void ValidateChancellor()
    {
        int d1 = chancellorDrop1.value;
        int d2 = chancellorDrop2.value;
        int d3 = chancellorDrop3.gameObject.activeSelf ? chancellorDrop3.value : 2;

        bool unique = d1 != d2 && d1 != d3 && d2 != d3;

        chancellorConfirmButton.interactable = unique;
    }


    public void OnChancellorConfirm()
    {
        int[] choices =
        {
        chancellorDrop1.value,
        chancellorDrop2.value,
        chancellorDrop3.value
    };

        var localPlayerObj = BasicSpawner.Instance.GetLocalPlayerObject();
        localPlayerObj.GetComponent<Player>().RPC_SubmitChancellorChoices(choices);
    }



}
