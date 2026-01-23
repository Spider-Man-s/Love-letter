using System.Collections.Generic;
using UnityEngine;
using LoveLetter.Networking;
using System.Linq;
using Fusion;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    [Networked] public TurnState CurrentTurnState { get; set; }

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

    [Networked] public int CurrentPlayerIndex { get; set; }


    [Networked, Capacity(6)]
    public NetworkArray<int> SeatToRawPlayer { get; } = default;



    private int[] _victoryTokens = new int[6];
    private int _lastRoundWinner = -1;
    private bool _roundHasEnded = false;


    public override void Spawned()
    {
        Instance = this;

        // Reset UI for new clients
        TableUIController.Instance.ResetVictoryCounters();
        TableUIController.Instance.ResetTable();
        BuildPlayersFromSeatArray();

        if (!Object.HasStateAuthority)
            return;

    }


    private void Awake()
    {
        if (Instance == null)
            Instance = this;

    }

    public override void FixedUpdateNetwork()
    {
        // Host only
        if (!Object.HasStateAuthority)
            return;

        // Ensure CurrentPlayerIndex stays valid
        if (CurrentPlayerIndex < 0 || CurrentPlayerIndex >= Players.Count)
            CurrentPlayerIndex = 0;
    }

    public void AssignSeat(PlayerRef player, int seat)
    {
        if (!Object.HasStateAuthority)
            return;

        Debug.Log($"[GM.AssignSeat] {player} -> seat {seat}");

        SeatToRawPlayer.Set(seat, player.RawEncoded);

        DebugSeatArray("After AssignSeat");
    }


    public void DebugSeatArray(string tag)
    {
        string s = $"[SeatArray:{tag}] ";
        for (int i = 0; i < SeatToRawPlayer.Length; i++)
            s += $"[{i}]={SeatToRawPlayer[i]} ";
        Debug.Log(s);
    }
    private void BuildPlayersFromSeatArray()
    {
        int count = 0;
        for (int i = 0; i < SeatToRawPlayer.Length; i++)
        {
            if (SeatToRawPlayer[i] != 0)
                count++;
        }

        Players.Clear();
        for (int i = 0; i < count; i++)
            Players.Add(new PlayerState(i));

        Debug.Log($"[GM] Players list rebuilt from SeatToRawPlayer. Count={Players.Count}");
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
        DebugSeatArray("BeginMatch");
        BuildPlayersFromSeatArray();

        if (TableUIController.Instance == null)
            Debug.LogError("UI ERROR: TableUIController.Instance is NULL");
        else
            Debug.Log("UI OK: TableUIController found");


        Debug.Log($"Authority Check: HasStateAuthority = {Object.HasStateAuthority}");


        if (!Object.HasStateAuthority)
            return;

        int playerCount = Runner.ActivePlayers.Count();

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
        Deck.Shuffle();
        Deck.Shuffle();
        DealInitialCards();

        RPC_ResetTableUI();

        int playerCount = Runner.ActivePlayers.Count();
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

        for (int seat = 0; seat < Players.Count; seat++)
        {
            PlayerRef player = GetPlayerRefBySeat(seat);
            if (player == PlayerRef.None)
                continue;

            PlayerState state = Players[seat];

            RPC_SendHandCount(seat, state.Hand.Count);

            int[] cards = state.Hand.Select(c => (int)c.Type).ToArray();

            NetworkObject obj = BasicSpawner.Instance.GetPlayerObject(player);
            if (obj != null)
            {
                Player comp = obj.GetComponent<Player>();
                comp.RPC_SendLocalHand(seat, cards);
            }
        }

    }
    public void StartGame(int playerCount)
    {
        CreatePlayers(playerCount);

        Deck = new Deck(CardDatabase.CreateDeck());
        Deck.Shuffle();
        Deck.Shuffle();
        Deck.Shuffle();
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
    public PlayerRef GetPlayerRefBySeat(int seat)
    {
        int raw = SeatToRawPlayer[seat];

        if (raw == 0)
        {
            Debug.LogWarning("[GM] Seat " + seat + " empty.");
            return PlayerRef.None;
        }

        foreach (var pr in Runner.ActivePlayers)
        {
            if (pr.RawEncoded == raw)
                return pr;
        }

        Debug.LogError("[GM] No matching PlayerRef found for raw=" + raw);
        return PlayerRef.None;
    }



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
        if (!Object.HasStateAuthority)
            return;

        var player = GetCurrentPlayer();
        var drawnCard = Deck.Draw();

        player.DrawCard(drawnCard);

        PlayerRef owner = GetPlayerRefBySeat(CurrentPlayerIndex);

        NetworkObject ownerObj = BasicSpawner.Instance.GetPlayerObject(owner);
        Player ownerComponent = ownerObj.GetComponent<Player>();


        int[] cardTypes = player.Hand.Select(c => (int)c.Type).ToArray();
        ownerComponent.RPC_SendLocalHand(CurrentPlayerIndex, cardTypes);

        // Everyone else sees card count
        RPC_SendHandCount(CurrentPlayerIndex, player.Hand.Count);

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
        TableUIController.Instance.RemoveLocalCard(cardView);
        Debug.Log($"[GM] Local player at seat {seatIndex} clicked {card.Type}");

        // Save only the card type
        _pendingCardType = card.Type;
        _hasPendingCard = true;
        DebugDumpState("LocalPlayerPlayedCard BEFORE UI");

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
        if (_roundHasEnded)
            return false;

        if (CurrentTurnState != TurnState.WaitingForPlay)
            return false;

        // Must be local seat’s turn
        if (cardView.CardData == null)
            return false;
        int myGlobalSeat = BasicSpawner.PlayerData.LocalSeatIndex;
        Debug.Log($"Check: mySeat={myGlobalSeat} current={CurrentPlayerIndex} state={CurrentTurnState}");
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
        if (!Object.HasStateAuthority)
            return;

        int next = CurrentPlayerIndex;

        do
        {
            next = (next + 1) % Players.Count;
        }
        while (!Players[next].IsAlive);

        CurrentPlayerIndex = next; // SAFE: inside StateAuthority code path
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
        _hasPendingCard = false;

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


    public void DebugDumpState(string tag = "")
    {
        Debug.Log("========== GAME STATE DUMP [" + tag + "] ==========");

        Debug.Log($"HasStateAuthority={Object.HasStateAuthority} | HasInputAuthority={Object.HasInputAuthority}");
        Debug.Log($"LocalPlayerRef={Runner.LocalPlayer}");

        Debug.Log($"CurrentPlayerIndex={CurrentPlayerIndex}");
        Debug.Log($"CurrentTurnState={CurrentTurnState}");



        // -------------------------
        // PLAYERS LIST
        // -------------------------
        Debug.Log("---- Players ----");

        if (Players == null)
        {
            Debug.Log("Players = NULL");
        }
        else
        {
            for (int i = 0; i < Players.Count; i++)
            {
                var p = Players[i];
                if (p == null)
                {
                    Debug.Log($"Players[{i}] = NULL");
                    continue;
                }

                Debug.Log($"Players[{i}] => Alive={p.IsAlive}, HandCount={p.Hand.Count}");

                // Log hand cards
                if (p.Hand.Count > 0)
                {
                    string cards = string.Join(",", p.Hand.Select(c => c.Type.ToString()));
                    Debug.Log($"   Hand: {cards}");
                }
                else
                {
                    Debug.Log("   Hand: EMPTY");
                }
            }
        }

        Debug.Log("========== END STATE DUMP ==========");
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
        TableUIController.Instance.AddPlayedCard(seatIndex, new Card((CardType)cardType));

        if (seatIndex == localSeat)
        {
            PlayerRef owner = GetPlayerRefBySeat(seatIndex);
            NetworkObject obj = BasicSpawner.Instance.GetPlayerObject(owner);

            if (obj == null)
            {
                Debug.LogError("[RPC_CardPlayed] ownerObj is NULL!");
                return;
            }

            Player ownerComp = obj.GetComponent<Player>();

            var realHand = GameManager.Instance.GetPlayer(seatIndex).Hand;
            int[] cards = realHand.Select(c => (int)c.Type).ToArray();

            ownerComp.RPC_SendLocalHand(seatIndex, cards);
        }
        else
        {
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
