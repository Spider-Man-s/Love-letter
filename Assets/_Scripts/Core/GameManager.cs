using System.Collections.Generic;
using UnityEngine;
using LoveLetter.Networking;
using System.Linq;
using Fusion;


public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;
    public TurnState CurrentTurnState;
    public List<PlayerState> Players = new();
    public Deck Deck;
    public DeckView DeckView;

    public int CurrentPlayerIndex;

    private void Awake()
    {
        Instance = this;

    }
    #region test method
    private void Start()
    {

    }
    public void PlayRound(int maxPlayers = 4)
    {
        Debug.Log("=== NEW ROUND ===");

        // inicijalizacija
        Players = new List<PlayerState>();
        for (int i = 0; i < maxPlayers; i++)
            Players.Add(new PlayerState(i));

        StartGame(maxPlayers);
        CurrentPlayerIndex = 0;

        while (Players.FindAll(p => p.IsAlive).Count > 1 && Deck.Count > 0)
        {
            StartTurn();
            var current = GetCurrentPlayer();

            if (!current.IsAlive)
            {
                AdvanceTurn();
                continue;
            }

            var cardToPlay = ChooseCardToPlay(current);
            var context = new EffectContext
            {
                TargetPlayerId = ChooseTarget(current, cardToPlay)
            };

            PlayCard(current.PlayerId, cardToPlay, context);
        }

        var winner = Players.Find(p => p.IsAlive);
        if (winner != null)
            Debug.Log($"=== Player {winner.PlayerId} WINS THE ROUND! ===");
        else
            Debug.Log("=== Round ended with no winner ===");
    }

    private Card ChooseCardToPlay(PlayerState player)
    {
        // Countess prisilno pravilo
        bool hasCountess = player.Hand.Exists(c => c.Type == CardType.Countess);
        bool hasPrinceOrKing = player.Hand.Exists(c => c.Type == CardType.Prince || c.Type == CardType.King);

        if (hasCountess && hasPrinceOrKing)
        {
            return player.Hand.Find(c => c.Type == CardType.Countess);
        }

        // inače odabere prvu kartu (simple AI)
        return player.Hand[0];
    }

    private int? ChooseTarget(PlayerState player, Card card)
    {
        // target only if card needs it
        if (!(card.Type == CardType.Guard ||
            card.Type == CardType.Priest ||
            card.Type == CardType.Baron ||
            card.Type == CardType.Prince ||
            card.Type == CardType.King))
            return null;

        var targets = Players.FindAll(p => p.IsAlive && p.PlayerId != player.PlayerId && !p.IsProtected);
        if (targets.Count == 0)
            return null;

        return targets[Random.Range(0, targets.Count)].PlayerId;
    }

    public void PrintDeck()
    {
        Debug.Log("Deck contents from top to bottom:");
        var deckList = new List<Card>();
        var field = typeof(Deck).GetField("cards", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        deckList = new List<Card>((Stack<Card>)field.GetValue(Deck));

        deckList.Reverse();

        for (int i = 0; i < deckList.Count; i++)
        {
            Debug.Log($"{i + 1}: {deckList[i]}");
        }
    }


    #endregion

    // ******************************************************************************************************************************
    // Actual game methods
    // ******************************************************************************************************************************
    public void BeginMatch()
    {
        if (!Object.HasStateAuthority)
            return;

        int playerCount = BasicSpawner.Instance.Runner.ActivePlayers.Count();

        StartGame(playerCount);

        RPC_StartMatch(Deck.Count);
    }

    public void StartGame(int playerCount)
    {
        CreatePlayers(playerCount);

        Deck = new Deck(CardDatabase.CreateDeck());
        Deck.Shuffle();


        if (DeckView != null)
        {
            DeckView.Initialize();
            DeckView.UpdateCount(Deck.Count);
        }

        DealInitialCards();
        CurrentPlayerIndex = 0;
    }

    private void CreatePlayers(int count)
    {
        Players.Clear();
        for (int i = 0; i < count; i++)
            Players.Add(new PlayerState(i));
    }

    private void DealInitialCards()
    {
        foreach (var player in Players)
        {
            player.Hand.Add(Deck.Draw());
        }
    }

    public PlayerState GetCurrentPlayer()
    {
        return Players[CurrentPlayerIndex];
    }

    public PlayerState GetPlayer(int playerId)
    {
        return Players[playerId];
    }

    public void EliminatePlayer(int playerId)
    {
        var player = Players[playerId];
        player.IsAlive = false;
        player.Hand.Clear();

        Debug.Log($"Player {playerId} is eliminated");
    }

    public Card ForceDiscardAndDraw(int targetPlayerId)
    {
        var target = Players[targetPlayerId];

        if (!target.IsAlive)
            return null;

        if (target.IsProtected)
            return null;

        Card discarded = null;

        if (target.Hand.Count > 0)
        {
            discarded = target.Hand[0];
            target.DiscardPile.Add(discarded);
            target.Hand.Clear();

            Debug.Log($"Player {targetPlayerId} discarded {discarded}");

            if (discarded.Type == CardType.Princess)
            {
                EliminatePlayer(targetPlayerId);
                return discarded;
            }
        }

        var newCard = Deck.Draw();
        target.DrawCard(newCard);

        Debug.Log($"Player {targetPlayerId} draws {newCard}");

        return discarded;
    }
    public void ChancellorDrawAndReturn(int playerId)
    {
        var player = Players[playerId];

        if (!player.IsAlive)
            return;

        if (Deck.Count < 2)
        {
            Debug.LogWarning("Not enough cards in deck for Chancellor");
        }

        var drawnCards = new List<Card>();
        for (int i = 0; i < 2 && Deck.Count > 0; i++)
        {
            var card = Deck.Draw();
            player.Hand.Add(card);
            drawnCards.Add(card);
            Debug.Log($"Player {playerId} draws {card} for Chancellor");
        }

        // Za sada deterministički: sve na dno u istom redoslijedu
        foreach (var card in drawnCards)
        {
            player.Hand.Remove(card);
            var list = DeckToList();
            list.Add(card); // prvo drawnCard[0] ide prvi natrag, treba namjestiti da odabrana karta ide
            SetDeckFromList(list);

            Debug.Log($"Player {playerId} returns {card} to bottom of deck");
        }
    }
    private List<Card> DeckToList()
    {
        return DeckToListInternal(Deck);
    }

    private List<Card> DeckToListInternal(Deck deck)
    {
        var field = typeof(Deck).GetField("cards", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return new List<Card>((Stack<Card>)field.GetValue(deck));
    }

    private void SetDeckFromList(List<Card> list)
    {
        var field = typeof(Deck).GetField("cards", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field.SetValue(Deck, new Stack<Card>(list));
    }
    #region card play


    private void DrawPhase()
    {
        if (CurrentTurnState != TurnState.WaitingForDraw)
            return;

        var player = GetCurrentPlayer();
        var drawnCard = Deck.Draw();

        player.DrawCard(drawnCard);
        DeckView.UpdateCount(Deck.Count);
        Debug.Log($"Player {player.PlayerId} draws {drawnCard}");

        CurrentTurnState = TurnState.WaitingForPlay;
    }

    public void PlayCard(int playerId, Card card, EffectContext context)
    {
        if (CurrentTurnState != TurnState.WaitingForPlay)
            return;

        var player = Players[playerId];
        bool hasCountess = player.Hand.Exists(c => c.Type == CardType.Countess);
        bool hasPrinceOrKing = player.Hand.Exists(c => c.Type == CardType.Prince || c.Type == CardType.King);

        if (hasCountess && hasPrinceOrKing && card.Type != CardType.Countess)
        {
            Debug.Log("Must play Countess due to Prince/King rule");
            card = player.Hand.Find(c => c.Type == CardType.Countess);
        }

        if (!player.Hand.Contains(card))
            return;

        player.RemoveCard(card);

        Debug.Log($"Player {playerId} plays {card}");

        CurrentTurnState = TurnState.Resolving;

        ResolveCard(card, playerId, context);
    }

    private void ResolveCard(Card card, int playerId, EffectContext context)
    {
        var effect = CardEffectFactory.Get(card.Type);

        //var context = new EffectContext(); // kasnije dolazi iz UI-ja / networka

        effect.Resolve(this, playerId, context);

        EndTurn();
    }
    #endregion

    #region turn manage methods
    public void StartTurn()
    {
        var player = GetCurrentPlayer();

        player.IsProtected = false; // reset Handmaiden zaštite
        CurrentTurnState = TurnState.WaitingForDraw;

        DrawPhase();
    }
    private void EndTurn()
    {
        CurrentTurnState = TurnState.TurnEnded;
        AdvanceTurn();
    }

    private void AdvanceTurn()
    {
        do
        {
            CurrentPlayerIndex = (CurrentPlayerIndex + 1) % Players.Count;
        }
        while (!Players[CurrentPlayerIndex].IsAlive);

        StartTurn();
    }
    #endregion


    //*********************************************************************
    // RPC METHODS
    //*********************************************************************
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_StartMatch(int deckCount)
    {
        Debug.Log("RPC_StartMatch received");

        if (DeckView != null)
        {
            DeckView.Initialize();
            DeckView.UpdateCount(deckCount);
        }
    }



}
