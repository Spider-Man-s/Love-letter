using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using LoveLetter.Networking;
public class TableUIController : MonoBehaviour
{
    public static TableUIController Instance;

    [Header("Start Game")]
    [SerializeField] private Button startGameButton;

    [Header("Hand Layout Groups")]
    [SerializeField] private HorizontalLayoutGroup[] handGroups = new HorizontalLayoutGroup[6];

    [Header("Played Layout Groups")]
    [SerializeField] private HorizontalLayoutGroup[] playedGroups = new HorizontalLayoutGroup[6];

    [Header("Card Prefab")]
    [SerializeField] private CardView cardPrefab;

    private List<CardView>[] handCards;
    private List<CardView>[] playedCards;

    private IEnumerator Start()
    {
        Instance = this;

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
            startGameButton.gameObject.SetActive(true);
        else
            startGameButton.gameObject.SetActive(false);

        startGameButton.onClick.AddListener(OnStartGameClicked);
    }

    private void OnStartGameClicked()
    {
        GameManager.Instance.BeginMatch();
        startGameButton.gameObject.SetActive(false);
    }

    // ============================================================
    // HAND UI
    // ============================================================

    public void SetHandCount(int seatIndex, int count)
    {
        ClearHand(seatIndex);

        for (int i = 0; i < count; i++)
        {
            var cv = Instantiate(cardPrefab, handGroups[seatIndex].transform, false);
            cv.Setup(new Card(CardType.CardBack));
            handCards[seatIndex].Add(cv);
        }
    }

    public void SetLocalHand(int seatIndex, List<Card> cards)
    {
        ClearHand(seatIndex);

        foreach (var card in cards)
        {
            var cv = Instantiate(cardPrefab, handGroups[seatIndex].transform, false);
            cv.Setup(card);
            handCards[seatIndex].Add(cv);
        }
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

    public void AddPlayedCard(int seatIndex, Card card)
    {
        var cv = Instantiate(cardPrefab, playedGroups[seatIndex].transform, false);
        cv.Setup(card);
        playedCards[seatIndex].Add(cv);
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
    }
}
