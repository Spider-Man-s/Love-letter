using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using LoveLetter.Networking;
using TMPro;
using System.Linq;
public class TableUIController : MonoBehaviour
{
    public static TableUIController Instance;

    [Header("Start Game")]
    [SerializeField] private Button startGameButton;

    [SerializeField] private TMP_Text gameCodeText;
    [Header("Restart Game")]
    [SerializeField] private Button restartGameButton;
    [SerializeField] private Button nextRoundButton;

    [Header("Hand Layout Groups")]
    [SerializeField] private HorizontalLayoutGroup[] handGroups = new HorizontalLayoutGroup[6];

    [Header("Played Layout Groups")]
    [SerializeField] private HorizontalLayoutGroup[] playedGroups = new HorizontalLayoutGroup[6];

    [Header("Victory Counters")]
    [SerializeField] private GameObject[] victoryCounterRoots = new GameObject[6];
    [SerializeField] private TextMeshProUGUI[] victoryCounterTexts = new TextMeshProUGUI[6];


    [Header("Card Prefab")]
    [SerializeField] private CardView cardPrefab;
    [Header("Announcement Text")]
    [SerializeField] private TextMeshProUGUI announcementText;
    [SerializeField] private TextMeshProUGUI winnerAnnouncementText;
    private List<CardView>[] handCards;
    private List<CardView>[] playedCards;

    private bool gameActive = false;
    private void Awake()
    {
        Debug.Log("[UI] TableUIController Awake ON: " + gameObject.name);
        Instance = this;
        ShowAnnouncement("");
    }

    private IEnumerator Start()
    {
        string code = BasicSpawner.Instance.Runner.SessionInfo.Name;
        gameCodeText.text = $"Code: {code}";
        handCards = new List<CardView>[6];
        playedCards = new List<CardView>[6];
        for (int i = 0; i < 6; i++)
        {
            handCards[i] = new List<CardView>();
            playedCards[i] = new List<CardView>();
        }

        ResetTable();

        // Wait for GameManager
        while (GameManager.Instance == null)
            yield return null;


        startGameButton.onClick.AddListener(OnStartGameClicked);
        restartGameButton.onClick.AddListener(OnRestartGameClicked);
        nextRoundButton.onClick.AddListener(OnNextRoundClicked);

    }

    private void Update()
    {
        CheckMinPLayers();
    }

    private void CheckMinPLayers()
    {

        if (BasicSpawner.Instance.Runner.IsServer)
        {
            if (BasicSpawner.Instance.Runner.ActivePlayers.Count() < 2 && !gameActive)
            {
                startGameButton.gameObject.SetActive(false);
            }
            else if (!gameActive)
            {
                startGameButton.gameObject.SetActive(true);
            }
        }
    }
    private void OnStartGameClicked()
    {
        gameActive = true;
        BasicSpawner.Instance.SetSessionState(SessionStateType.Playing);
        GameManager.Instance.BeginMatch();
        startGameButton.gameObject.SetActive(false);
        if (BasicSpawner.Instance.Runner.IsServer)
        {
            restartGameButton.gameObject.SetActive(true);
        }
    }

    public void OnRestartGameClicked()
    {
        if (!BasicSpawner.Instance.Runner.IsServer)
            return;
        TargetSelectionUI.Instance.Close();
        GameManager.Instance.ResetVictoryTokens();
        GameManager.Instance.RPC_ResetVictoryCounters();
        GameManager.Instance.RestartMatch();
        nextRoundButton.gameObject.SetActive(false);

    }


    public void OnNextRoundClicked()
    {
        if (!BasicSpawner.Instance.Runner.IsServer)
            return;

        nextRoundButton.gameObject.SetActive(false);
        TargetSelectionUI.Instance.Close();
        GameManager.Instance.RestartMatch();
    }

    public void ShowRoundWinner(int winnerSeat)
    {
        string sourceName = GameManager.Instance.GetPlayerName(winnerSeat);
        GameManager.Instance.RPC_AnnounceWinner($"{sourceName} wins the round!");

        if (BasicSpawner.Instance.Runner.IsServer)
            nextRoundButton.gameObject.SetActive(true);
    }


    // ============================================================
    // HAND UI
    // ============================================================

    public void SetLocalHand(int seatIndex, List<Card> cards)
    {
        int localSeatIndex = GlobalToLocalSeat(seatIndex);
        ClearHand(localSeatIndex);
        bool hasCountess = cards.Any(c => c.Type == CardType.Countess);

        foreach (var card in cards)
        {
            var cv = Instantiate(cardPrefab, handGroups[localSeatIndex].transform, false);
            cv.Setup(card);

            cv.IsLocalCard = true;

            bool interactable = true;


            if (hasCountess)
            {
                if (card.Type == CardType.Prince || card.Type == CardType.King)
                {
                    interactable = false;
                }
            }

            cv.SetInteractable(interactable);

            handCards[localSeatIndex].Add(cv);
        }
    }

    public void SetHandCount(int seatIndex, int count)
    {
        if (seatIndex == BasicSpawner.PlayerData.LocalSeatIndex)
            return;

        int localSeatIndex = GlobalToLocalSeat(seatIndex);
        ClearHand(localSeatIndex);

        for (int i = 0; i < count; i++)
        {
            var cv = Instantiate(cardPrefab, handGroups[localSeatIndex].transform, false);
            cv.Setup(new Card(CardType.CardBack));
            cv.IsLocalCard = false;
            cv.SetInteractable(false);
            handCards[localSeatIndex].Add(cv);
        }
    }


    public void ShowAnnouncement(string msg)
    {
        announcementText.text = msg;
        announcementText.gameObject.SetActive(true);

    }

    public void ShowWinner(string msg)
    {
        winnerAnnouncementText.text = msg;
        winnerAnnouncementText.gameObject.SetActive(true);

    }

    public void RemoveLocalCard(CardView cardView)
    {
        int localSeat = 0; // always 0 for local player
        handCards[localSeat].Remove(cardView);
        Destroy(cardView.gameObject);
    }

    private void ClearHand(int seatIndex)
    {
        foreach (var cv in handCards[seatIndex])
            Destroy(cv.gameObject);

        handCards[seatIndex].Clear();
    }

    public List<CardView> GetLocalHand()
    {
        int seat = BasicSpawner.PlayerData.LocalSeatIndex;
        int localSeat = GlobalToLocalSeat(seat);
        return handCards[localSeat];
    }


    // ============================================================
    // PLAYED CARDS UI
    // ============================================================

    public void AddPlayedCard(int globalSeatIndex, Card card)
    {
        int localSeatIndex = GlobalToLocalSeat(globalSeatIndex);

        var cv = Instantiate(cardPrefab, playedGroups[localSeatIndex].transform, false);
        cv.Setup(card);
        playedCards[localSeatIndex].Add(cv);
    }


    public void ClearPlayed(int seatIndex)
    {
        foreach (var cv in playedCards[seatIndex])
            Destroy(cv.gameObject);

        playedCards[seatIndex].Clear();
    }
    public void ShowActivePlayerCounters(int playerCount)
    {
        for (int globalSeat = 0; globalSeat < victoryCounterRoots.Length; globalSeat++)
        {
            int localSeat = GlobalToLocalSeat(globalSeat);

            bool active = globalSeat < playerCount;
            victoryCounterRoots[localSeat].SetActive(active);
        }
    }

    public void UpdateVictoryCounter(int globalSeat, int newValue)
    {
        int localSeat = GlobalToLocalSeat(globalSeat);
        victoryCounterTexts[localSeat].text = newValue.ToString();
    }


    public void SetLocalHandInteractable(bool state)
    {
        foreach (var cv in GetLocalHand())
            cv.SetInteractable(state);
    }
    // ============================================================
    // FULL RESET
    // ============================================================

    public void ResetTable()
    {
        for (int i = 0; i < 6; i++)
        {
            foreach (Transform ch in handGroups[i].transform)
                Destroy(ch.gameObject);

            foreach (Transform ch in playedGroups[i].transform)
                Destroy(ch.gameObject);

            handCards[i].Clear();
            playedCards[i].Clear();

        }
        announcementText.gameObject.SetActive(false);
        winnerAnnouncementText.gameObject.SetActive(false);
    }
    public void ResetVictoryCounters()
    {
        for (int i = 0; i < 6; i++)
        {
            victoryCounterTexts[i].text = "0";
            victoryCounterRoots[i].SetActive(false);
        }
    }


    public static int GlobalToLocalSeat(int globalSeat)
    {
        int mySeat = BasicSpawner.PlayerData.LocalSeatIndex;
        const int TOTAL_SEATS = 6;

        return (globalSeat - mySeat + TOTAL_SEATS) % TOTAL_SEATS;
    }

}
