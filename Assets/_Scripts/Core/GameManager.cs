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

    // DeckView is now resolved at runtime because prefab cannot reference scene objects.
    private DeckView deckView;
    public DeckView DeckView
    {
        get
        {
            if (deckView == null)
                deckView = FindObjectOfType<DeckView>(true);
            return deckView;
        }
    }

    public int CurrentPlayerIndex;
    private Dictionary<PlayerRef, int> seatByPlayer = new();

    public override void Spawned()
    {
        Instance = this;

        if (seatByPlayer == null)
            seatByPlayer = new Dictionary<PlayerRef, int>();
    }

    private void Awake()
    {
        // For host-only editor play
        if (Instance == null)
            Instance = this;

        if (seatByPlayer == null)
            seatByPlayer = new Dictionary<PlayerRef, int>();
        //  Debug.Log($"GameManager Awake | NetId: {Object?.Id} | InstanceHash: {GetHashCode()}");
    }

    #region test method (unchanged)
    private void Start() { }

    public void PlayRound(int maxPlayers = 4)
    {
        Debug.Log("=== NEW ROUND ===");

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
    #endregion

    public void RegisterNetworkPlayer(PlayerRef player, int seatIndex)
    {
        if (!Object || !Object.HasStateAuthority)
            return;

        if (seatByPlayer == null)
            seatByPlayer = new Dictionary<PlayerRef, int>();

        if (!seatByPlayer.ContainsKey(player))
        {
            seatByPlayer[player] = seatIndex;
            Debug.Log($"Registered player {player} at seat {seatIndex}");
        }
        //  Debug.Log($"RegisterNetworkPlayer | InstanceHash: {GetHashCode()}");

    }

    private Card ChooseCardToPlay(PlayerState player)
    {
        bool hasCountess = player.Hand.Exists(c => c.Type == CardType.Countess);
        bool hasPrinceOrKing = player.Hand.Exists(c => c.Type == CardType.Prince || c.Type == CardType.King);

        if (hasCountess && hasPrinceOrKing)
            return player.Hand.Find(c => c.Type == CardType.Countess);

        return player.Hand[0];
    }

    private int? ChooseTarget(PlayerState player, Card card)
    {
        if (!(card.Type == CardType.Guard ||
              card.Type == CardType.Priest ||
              card.Type == CardType.Baron ||
              card.Type == CardType.Prince ||
              card.Type == CardType.King))
            return null;

        var targets = Players.FindAll(p =>
            p.IsAlive && p.PlayerId != player.PlayerId && !p.IsProtected);

        if (targets.Count == 0)
            return null;

        return targets[Random.Range(0, targets.Count)].PlayerId;
    }

    public void BeginMatch()
    {

        if (!Object.HasStateAuthority)
            return;

        if (seatByPlayer.Count == 0)
        {
            Debug.LogError("BeginMatch called but no players registered!");
            return;
        }

        int playerCount = seatByPlayer.Count;

        StartGame(playerCount);

        RPC_StartMatch(Deck.Count);

        StartCoroutine(DelayedSendHands());
    }


    private System.Collections.IEnumerator DelayedSendHands()
    {
        yield return null; // wait 1 frame so Player.Spawned() runs

        foreach (var kvp in seatByPlayer)
        {
            PlayerRef player = kvp.Key;
            int seatIndex = kvp.Value;

            PlayerState state = Players[seatIndex];

            // Everyone sees card count
            RPC_SendHandCount(seatIndex, state.Hand.Count);

            // Only owner sees real hand
            int[] cardTypes = state.Hand.Select(c => (int)c.Type).ToArray();

            NetworkObject playerObj = BasicSpawner.Instance.GetPlayerObject(player);
            Player playerComponent = playerObj.GetComponent<Player>();

            playerComponent.RPC_SendLocalHand(seatIndex, cardTypes);
        }
    }
    public void StartGame(int playerCount)
    {
        CreatePlayers(playerCount);

        Deck = new Deck(CardDatabase.CreateDeck());
        Deck.Shuffle();

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
            player.Hand.Add(Deck.Draw());
    }

    public PlayerState GetCurrentPlayer() => Players[CurrentPlayerIndex];
    public PlayerState GetPlayer(int playerId) => Players[playerId];

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

        if (!target.IsAlive || target.IsProtected)
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

        var drawnCards = new List<Card>();
        for (int i = 0; i < 2 && Deck.Count > 0; i++)
        {
            var card = Deck.Draw();
            player.Hand.Add(card);
            drawnCards.Add(card);
        }

        foreach (var card in drawnCards)
        {
            player.Hand.Remove(card);
            var list = DeckToList();
            list.Add(card);
            SetDeckFromList(list);
        }
    }

    private List<Card> DeckToList()
    {
        var field = typeof(Deck).GetField("cards",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        return new List<Card>((Stack<Card>)field.GetValue(Deck));
    }

    private void SetDeckFromList(List<Card> list)
    {
        var field = typeof(Deck).GetField("cards",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        field.SetValue(Deck, new Stack<Card>(list));
    }

    private void DrawPhase()
    {
        if (CurrentTurnState != TurnState.WaitingForDraw)
            return;

        var player = GetCurrentPlayer();
        var drawnCard = Deck.Draw();

        player.DrawCard(drawnCard);

        if (DeckView != null)
            DeckView.UpdateCount(Deck.Count);

        CurrentTurnState = TurnState.WaitingForPlay;
    }

    public void PlayCard(int playerId, Card card, EffectContext context)
    {
        if (CurrentTurnState != TurnState.WaitingForPlay)
            return;

        var player = Players[playerId];
        bool mustPlayCountess =
            player.Hand.Exists(c => c.Type == CardType.Countess) &&
            player.Hand.Exists(c => c.Type == CardType.Prince || c.Type == CardType.King);

        if (mustPlayCountess && card.Type != CardType.Countess)
            card = player.Hand.Find(c => c.Type == CardType.Countess);

        if (!player.Hand.Contains(card))
            return;

        player.RemoveCard(card);

        CurrentTurnState = TurnState.Resolving;

        ResolveCard(card, playerId, context);
    }

    private void ResolveCard(Card card, int playerId, EffectContext context)
    {
        var effect = CardEffectFactory.Get(card.Type);
        effect.Resolve(this, playerId, context);

        EndTurn();
    }

    public void StartTurn()
    {
        var player = GetCurrentPlayer();

        player.IsProtected = false;
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

    // RPCs (unchanged)
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_StartMatch(int deckCount)
    {
        if (DeckView != null)
        {
            DeckView.Initialize();
            DeckView.UpdateCount(deckCount);
        }
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SendHandCount(int seatIndex, int count)
    {
        TableUIController.Instance?.SetHandCount(seatIndex, count);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestSync()
    {
        RPC_SendDeckCount(Deck.Count);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SendDeckCount(int count)
    {
        if (DeckView != null)
            DeckView.UpdateCount(count);
    }
}
