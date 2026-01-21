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

    private CardType _pendingCardType;
    private bool _hasPendingCard;
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

    private int[] _victoryTokens = new int[6];
    private int _lastRoundWinner = -1;
    private bool _roundHasEnded = false;


    public override void Spawned()
    {
        Instance = this;

        if (seatByPlayer == null)
            seatByPlayer = new Dictionary<PlayerRef, int>();

        TableUIController.Instance.ResetVictoryCounters();
        TableUIController.Instance.ResetTable();

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
        Debug.Log("=== BeginMatch() CALLED ===");

        if (TableUIController.Instance == null)
            Debug.LogError("UI ERROR: TableUIController.Instance is NULL");
        else
            Debug.Log("UI OK: TableUIController found");


        Debug.Log($"Authority Check: HasStateAuthority = {Object.HasStateAuthority}");

        Debug.Log($"Players registered in seatByPlayer: {seatByPlayer.Count}");
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
        RPC_ShowCounters(playerCount);


        StartCoroutine(WaitAndStartFirstTurn());
        StartCoroutine(DelayedSendHands());
        _roundHasEnded = false;
    }

    public void RestartMatch()
    {
        Debug.Log("=== RESTART MATCH ===");

        CurrentTurnState = TurnState.WaitingForDraw;
        CurrentPlayerIndex = _lastRoundWinner >= 0 ? _lastRoundWinner : 0;

        for (int i = 0; i < Players.Count; i++)
            Players[i] = new PlayerState(i);

        Deck = new Deck(CardDatabase.CreateDeck());
        Deck.Shuffle();
        DealInitialCards();

        RPC_ResetTableUI();

        int playerCount = seatByPlayer.Count;
        RPC_ShowCounters(playerCount);

        RPC_SendDeckCount(Deck.Count);
        StartCoroutine(DelayedSendHands());
        StartCoroutine(WaitAndStartFirstTurn());
        _roundHasEnded = false;
    }



    private System.Collections.IEnumerator WaitAndStartFirstTurn()
    {
        // Wait until DeckView exists on this machine
        while (DeckView == null)
            yield return null;

        StartTurn();
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
        CheckRoundEnd();

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
        SyncDeck();
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
        SyncDeck();
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

        SyncDeck();
        if (Deck.Count == 0)
        {
            CheckRoundEnd();
            return;
        }
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
        RPC_CardPlayed(playerId, (int)card.Type, player.Hand.Count);

        CurrentTurnState = TurnState.Resolving;

        ResolveCard(card, playerId, context);
    }

    public void LocalPlayerPlayedCard(CardView cardView)
    {
        int seatIndex = BasicSpawner.PlayerData.LocalSeatIndex;
        var card = cardView.CardData;

        Debug.Log($"[GM] Local player at seat {seatIndex} clicked {card.Type}");

        // Save only the card type
        _pendingCardType = card.Type;
        _hasPendingCard = true;

        // Open UI
        TargetSelectionUI.Instance.OpenForPlayers((int)_pendingCardType);
    }


    public void LocalPlayerPlayNoContext()
    {
        if (!_hasPendingCard)
        {
            Debug.LogError("No pending card to play (no-context).");
            return;
        }

        int seat = BasicSpawner.PlayerData.LocalSeatIndex;

        RPC_RequestPlayCardContext(
            seat,
            (int)_pendingCardType,
            -1,      // no target
            -1       // no guess
        );

        _hasPendingCard = false;
    }

    public void LocalPlayerPlayTargetOnly(int targetSeat)
    {
        if (!_hasPendingCard)
        {
            Debug.LogError("No pending card to play (target only).");
            return;
        }

        int seat = BasicSpawner.PlayerData.LocalSeatIndex;

        RPC_RequestPlayCardContext(
            seat,
            (int)_pendingCardType,
            targetSeat,
            -1
        );

        _hasPendingCard = false;
    }


    public void LocalPlayerConfirmedTarget(int targetSeat, CardType guess)
    {
        if (!_hasPendingCard)
        {
            Debug.LogError("No pending card to play (full context).");
            return;
        }

        int seat = BasicSpawner.PlayerData.LocalSeatIndex;

        RPC_RequestPlayCardContext(
            seat,
            (int)_pendingCardType,
            targetSeat,
            (int)guess
        );

        _hasPendingCard = false;
    }


    public bool CanPlayerPlayThisCard(CardView cardView)
    {
        if (CurrentTurnState != TurnState.WaitingForPlay)
            return false;

        // Must be local seat’s turn
        if (cardView.CardData == null)
            return false;
        int myGlobalSeat = BasicSpawner.PlayerData.LocalSeatIndex;

        if (myGlobalSeat != CurrentPlayerIndex)
            return false;


        return true;
    }

    private void ResolveCard(Card card, int playerId, EffectContext context)
    {
        var effect = CardEffectFactory.Get(card.Type);
        effect.Resolve(this, playerId, context);

        EndTurn();
    }
    private void SyncDeck()
    {
        RPC_SendDeckCount(Deck.Count);
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
        CheckRoundEnd();

        if (CurrentTurnState == TurnState.TurnEnded)
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


    private void CheckRoundEnd()
    {
        // A) Only one player alive
        int aliveCount = Players.Count(p => p.IsAlive);
        if (aliveCount == 1)
        {
            int winner = Players.First(p => p.IsAlive).PlayerId;
            EndRound(winner);
            return;
        }

        // B) Deck is empty — compare highest hand card
        if (Deck.Count == 0)
        {
            // Convert CardType to int before comparing
            int bestValue = Players
                .Where(p => p.IsAlive)
                .Max(p => (int)p.Hand[0].Type);

            var winners = Players
                .Where(p => p.IsAlive && (int)p.Hand[0].Type == bestValue)
                .Select(p => p.PlayerId)
                .ToList();

            int winnerForStart = winners[0];
            EndRound(winnerForStart);
            return;
        }
    }


    private void EndRound(int winnerSeat)
    {
        if (_roundHasEnded)
            return;

        _roundHasEnded = true;
        Debug.Log("=== ROUND ENDED — Winner = " + winnerSeat + " ===");

        _lastRoundWinner = winnerSeat;
        _victoryTokens[winnerSeat]++;

        // Broadcast winner and update UI
        RPC_RoundEnded(winnerSeat);
        RPC_UpdateVictoryCounter(winnerSeat, _victoryTokens[winnerSeat]);

        // Stop game flow
        CurrentTurnState = TurnState.TurnEnded;
    }


    public void ResetVictoryTokens()
    {
        for (int i = 0; i < _victoryTokens.Length; i++)
            _victoryTokens[i] = 0;
    }




    // ====================================================================
    // RPCs     
    //
    // ===================================================================

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_RoundEnded(int winnerSeat)
    {
        TableUIController.Instance.ShowRoundWinner(winnerSeat);
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ResetTableUI()
    {
        TableUIController.Instance.ResetTable();
    }


    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestPlayCardContext(int seatIndex, int cardType, int targetSeat, int guessedType)
    {
        Debug.Log($"[Server] Context RPC: p{seatIndex} card={cardType}, target={targetSeat}, guess={guessedType}");

        // Find THIS player's matching card
        var card = Players[seatIndex].Hand.Find(c => c.Type == (CardType)cardType);

        if (card == null)
        {
            Debug.LogError("Server ERROR: Card type not found in player's hand!");
            return;
        }

        // Build context
        var ctx = new EffectContext
        {
            TargetPlayerId = targetSeat >= 0 ? targetSeat : null,
            GuessedCard = guessedType >= 0 ? (CardType?)guessedType : null
        };

        // Play it
        PlayCard(seatIndex, card, ctx);
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ResetVictoryCounters()
    {
        TableUIController.Instance.ResetVictoryCounters();
    }




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

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestPlayCard(int seatIndex, int cardType)
    {
        Debug.Log($"[Server] Player {seatIndex} requests to play {((CardType)cardType)}");

        var card = Players[seatIndex].Hand.Find(c => c.Type == (CardType)cardType);
        if (card == null)
        {
            Debug.LogError("Server: Card not found in hand.");
            return;
        }

        PlayCard(seatIndex, card, new EffectContext());
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_CardPlayed(int seatIndex, int cardType, int newHandCount)
    {
        int localSeat = BasicSpawner.PlayerData.LocalSeatIndex;

        // Everyone: show played card
        TableUIController.Instance.AddPlayedCard(seatIndex, new Card((CardType)cardType));

        if (seatIndex == localSeat)
        {
            // LOCAL PLAYER => show real hand
            var realHand = GameManager.Instance.GetPlayer(seatIndex).Hand;
            TableUIController.Instance.SetLocalHand(seatIndex, realHand);
        }
        else
        {
            // OTHERS => only show card count
            TableUIController.Instance.SetHandCount(seatIndex, newHandCount);
        }
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ShowCounters(int playerCount)
    {
        TableUIController.Instance.ShowActivePlayerCounters(playerCount);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_UpdateVictoryCounter(int winnerSeat, int score)
    {
        TableUIController.Instance.UpdateVictoryCounter(winnerSeat, score);
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    private void RPC_OpenTargetSelection(int cardType)
    {
        TargetSelectionUI.Instance.OpenForPlayers(cardType);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_AnnounceAction(string message)
    {
        Debug.Log("[ANNOUNCE] " + message);

        // TODO: Send message to UI popup
        TableUIController.Instance.ShowAnnouncement(message);
    }

}
