using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using LoveLetter.Networking;
using TMPro;
public class TableUIController : MonoBehaviour
{
    public static TableUIController Instance;

    [Header("Start Game")]
    [SerializeField] private Button startGameButton;
    [Header("Restart Game")]
    [SerializeField] private Button restartGameButton;

    [Header("Hand Layout Groups")]
    [SerializeField] private HorizontalLayoutGroup[] handGroups = new HorizontalLayoutGroup[6];

    [Header("Played Layout Groups")]
    [SerializeField] private HorizontalLayoutGroup[] playedGroups = new HorizontalLayoutGroup[6];

    [Header("Card Prefab")]
    [SerializeField] private CardView cardPrefab;
    [Header("Announcement Text")]
    [SerializeField] private TextMeshProUGUI announcementText;

    private List<CardView>[] handCards;
    private List<CardView>[] playedCards;

    private void Awake()
    {
        Debug.Log("[UI] TableUIController Awake ON: " + gameObject.name);
        Instance = this;
        ShowAnnouncement("");
    }

    private IEnumerator Start()
    {


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

        // Start button = HOST ONLY
        if (BasicSpawner.Instance.Runner.IsServer)
        {
            startGameButton.gameObject.SetActive(true);
        }

        else
            startGameButton.gameObject.SetActive(false);

        startGameButton.onClick.AddListener(OnStartGameClicked);
        restartGameButton.onClick.AddListener(OnRestartGameClicked);
    }

    private void OnStartGameClicked()
    {
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

        GameManager.Instance.RestartMatch();

    }


    // ============================================================
    // HAND UI
    // ============================================================

    public void SetLocalHand(int seatIndex, List<Card> cards)
    {
        int localSeatIndex = GlobalToLocalSeat(seatIndex);
        ClearHand(localSeatIndex);

        foreach (var card in cards)
        {
            var cv = Instantiate(cardPrefab, handGroups[localSeatIndex].transform, false);
            cv.Setup(card);

            // Make only local card interactable
            cv.IsLocalCard = true;
            cv.SetInteractable(true);

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


    private void ClearHand(int seatIndex)
    {
        foreach (var cv in handCards[seatIndex])
            Destroy(cv.gameObject);

        handCards[seatIndex].Clear();
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
    }

    public static int GlobalToLocalSeat(int globalSeat)
    {
        int mySeat = BasicSpawner.PlayerData.LocalSeatIndex;
        const int TOTAL_SEATS = 6;

        return (globalSeat - mySeat + TOTAL_SEATS) % TOTAL_SEATS;
    }

}
