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
    private Card _bonusCard;

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

    [SerializeField] private NetworkBehaviourId[] seatPlayerIds;

    private int[] _victoryTokens = new int[6];
    private int _lastRoundWinner = -1;
    private bool _roundHasEnded = false;

    public struct RoundResult
    {
        public List<int> Winners;
        public int SpyBonusSeat;
    }
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
        Players = new List<PlayerState>(new PlayerState[6]); // ALWAYS 6 seats

        for (int seat = 0; seat < 6; seat++)
        {
            int raw = SeatToRawPlayer[seat];
            if (raw != 0)
            {
                // Create or reuse PlayerState for this seat
                Players[seat] = new PlayerState(seat);
            }
            else
            {
                Players[seat] = null;
            }
        }

        Debug.Log($"[GM] Players list rebuilt. Seats populated.");
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
        BroadcastSeatMap();

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

        RPC_ResetAliveStates();
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
        RPC_UnlockAllPlayers();
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

        Deck.Print();
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
        Debug.Log("=== DEBUG DEAL PHASE ===");

        // Print deck before removing bonus
        Debug.Log("[DECK BEFORE BONUS]");
        Deck.Print();

        _bonusCard = Deck.Draw();
        Debug.Log("[BONUS CARD] " + _bonusCard.Type);

        // Deck after removing bonus card
        Debug.Log("[DECK AFTER BONUS]");
        Deck.Print();
        foreach (var player in Players)
            player.Hand.Add(Deck.Draw());
    }

    public PlayerState GetCurrentPlayer() => Players[CurrentPlayerIndex];
    public PlayerState GetPlayer(int seat)
    {
        if (seat < 0 || seat >= Players.Count)
        {
            Debug.LogError($"GetPlayer INVALID seat={seat}");
            return null;
        }

        return Players[seat];
    }
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

    public string GetPlayerName(int seatIndex)
    {
        // Get PlayerRef associated with this seat
        PlayerRef pref = GetPlayerRefBySeat(seatIndex);

        // Resolve spawned network player object
        var obj = BasicSpawner.Instance.GetPlayerObject(pref);
        if (obj == null)
            return $"Player {seatIndex}"; // fallback

        var p = obj.GetComponent<Player>();
        return p.PlayerName.ToString();  // NetworkString<_16>
    }

    public Card GetBonusCard()
    {
        var c = _bonusCard;
        _bonusCard = null; // bonus can only be used once per round
        return c;
    }

    public void EliminatePlayer(int playerId)
    {
        var player = Players[playerId];

        // Reveal and discard remaining hand card(s)
        if (player.Hand.Count > 0)
        {
            Card last = player.Hand[0];
            player.Hand.Clear();
            player.DiscardPile.Add(last);

            // Show the eliminated card to everyone
            RPC_ShowDiscard(playerId, (int)last.Type);
        }

        player.IsAlive = false;
        RPC_SetAlive(playerId, false);

        Debug.Log($"Player {playerId} is eliminated");

        // UI update — hand count forced to zero
        RPC_SendHandCount(playerId, 0);
        SyncPlayerHandToOwner(playerId);

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
            if (discarded.Type == CardType.Spy)
                target.PlayedSpyThisRound = true;

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


        // ==============================
        // SPECIAL: CHANCELLOR (Card 6)
        // ==============================
        if (card.Type == CardType.Chancellor)
        {
            Debug.Log("[SERVER] Chancellor played by seat " + playerId);

            player.RemoveCard(card);
            RPC_CardPlayed(playerId, (int)card.Type, player.Hand.Count);

            // REAL count before draws
            int originalDeckCount = Deck.Count;

            PlayerRef owner = GetPlayerRefBySeat(playerId);
            var obj = BasicSpawner.Instance.GetPlayerObject(owner);

            // =======================
            // CASE 1 — Deck empty
            // =======================
            if (originalDeckCount == 0)
            {
                Debug.Log("[SERVER] Chancellor: deck empty → discard only.");

                // No draws, no UI except discard mode
                obj.GetComponent<Player>()
                    .RPC_OpenChancellorUI(playerId, originalDeckCount);

                ServerResolveChancellor_NoChoices(playerId);
                return;
            }

            // =======================
            // CASE 2 — Only 1 card left
            // =======================
            if (originalDeckCount == 1)
            {
                Debug.Log("[SERVER] Chancellor: 1 card in deck.");

                Card c1 = Deck.Draw();
                player.Hand.Add(c1);

                SyncDeck();
                BroadcastHandCount(playerId);

                obj.GetComponent<Player>().RPC_SendLocalHand(
                    playerId,
                    player.Hand.Select(c => (int)c.Type).ToArray()
                );

                obj.GetComponent<Player>()
                    .RPC_OpenChancellorUI(playerId, originalDeckCount);

                CurrentTurnState = TurnState.Resolving;
                return;
            }

            // =======================
            // CASE 3 — Normal (2+ cards)
            // =======================
            Card d1 = Deck.Draw();
            Card d2 = Deck.Draw();

            player.Hand.Add(d1);
            player.Hand.Add(d2);

            SyncDeck();
            BroadcastHandCount(playerId);

            obj.GetComponent<Player>().RPC_SendLocalHand(
                playerId,
                player.Hand.Select(c => (int)c.Type).ToArray()
            );

            obj.GetComponent<Player>()
                .RPC_OpenChancellorUI(playerId, originalDeckCount);

            CurrentTurnState = TurnState.Resolving;
            return;
        }



        // ==============================
        // NORMAL CARDS
        // ==============================
        player.RemoveCard(card);
        RPC_CardPlayed(playerId, (int)card.Type, player.Hand.Count);
        SyncPlayerHandToOwner(playerId);
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

    public void LocalPlayerPlayChancellor(int[] choices)
    {
        int seat = BasicSpawner.PlayerData.LocalSeatIndex;

        // Send the play request (same as any normal card)
        RPC_RequestPlayCardContext(
            seat,
            (int)CardType.Chancellor,
            -1,   // no target
            -1    // no guess
        );

        // Immediately send the choices to server
        RPC_SubmitChancellorChoices(seat, choices);
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
        if (card.Type == CardType.Spy)
            Players[playerId].PlayedSpyThisRound = true;
        var effect = CardEffectFactory.Get(card.Type);
        effect.Resolve(this, playerId, context);

        EndTurn();
    }

    public void ServerResolveChancellor(PlayerRef playerRef, int[] choices)
    {
        int seatIndex = GetSeatByPlayerRef(playerRef);
        var player = GetPlayer(seatIndex);
        var hand = player.Hand;

        int count = hand.Count;

        // SAFETY CHECKS =======================================
        if (count == 0)
        {
            Debug.LogError("[Chancellor] ERROR: Hand is empty on resolve.");
            return;
        }

        if (count == 1)
        {
            Debug.LogWarning("[Chancellor] Only 1 card in hand → nothing to resolve.");
            // No changes required — simply end turn
            EndTurn();
            return;
        }

        if (choices == null)
        {
            Debug.LogError("[Chancellor] choices NULL!");
            return;
        }

        // If UI accidentally sent too many choices → trim safely
        if (choices.Length > count)
        {
            Debug.LogWarning("[Chancellor] Received more choices than cards — trimming.");
            System.Array.Resize(ref choices, count);
        }

        // ======================================================

        Card keep = null;
        Card bottom = null;
        Card secondBottom = null;

        if (count == 3)
        {
            // Classic mode — 3 choices: 0=keep,1=second last,2=last
            for (int i = 0; i < 3; i++)
            {
                if (choices[i] == 0) keep = hand[i];
                if (choices[i] == 1) secondBottom = hand[i];
                if (choices[i] == 2) bottom = hand[i];
            }
        }
        else if (count == 2)
        {
            // choices[0] → card index 0
            // choices[2] → card index 1
            for (int i = 0; i < choices.Length; i++)
            {
                Debug.Log($"[Chancellor] choices[{i}] = {choices[i]}");
            }

            int c0 = choices[0];

            if (c0 == 0)
            {
                keep = hand[0];
                bottom = hand[1];
            }

            else if (c0 == 2)
            {
                keep = hand[1];
                bottom = hand[0];
            }



        }

        // VALIDATE FINAL SELECTIONS ===========================
        if (keep == null)
        {
            Debug.LogError("[Chancellor] INVALID keep selection.");
            return;
        }
        if (bottom == null && count >= 2)
        {
            Debug.LogError("[Chancellor] INVALID bottom selection.");
            return;
        }

        // NOTE: secondBottom is optional (only in 3-card mode)

        // FIX HAND =============================================
        hand.Clear();
        hand.Add(keep);

        // STACK RETURN =========================================
        if (count == 3)
        {
            Deck.PutOnBottom(bottom);
            Deck.PutSecondToLast(secondBottom);
        }
        else if (count == 2)
        {
            Deck.PutOnBottom(bottom);
            // second-to-last does not exist in this mode
        }

        // SYNC & UI ============================================
        SyncDeck();
        SyncPlayerHandToOwner(seatIndex);
        BroadcastHandCount(seatIndex);

        string name = GetPlayerName(seatIndex);
        RPC_AnnounceAction($"{name} resolved Chancellor.");

        EndTurn();
    }
    public void ServerResolveChancellor_NoChoices(int seatIndex)
    {
        // No choices were needed, just end the turn
        Debug.Log("[Chancellor] No-choice flow. Ending turn.");
        EndTurn();
    }



    public int GetSeatByPlayerRef(PlayerRef playerRef)
    {
        for (int i = 0; i < Players.Count; i++)
        {
            var obj = BasicSpawner.Instance.GetPlayerObject(playerRef);
            if (obj != null)
            {
                var p = obj.GetComponent<Player>();
                if (p.SeatIndex == Players[i].PlayerId)
                    return Players[i].PlayerId;
            }
        }

        Debug.LogError("[GameManager] Could not resolve seat index for PlayerRef: " + playerRef);
        return -1;
    }


    public void BroadcastSeatMap()
    {
        var playerObjects = FindObjectsOfType<Player>();
        int count = Players.Count;

        int[] avatarIds = new int[count];
        string[] names = new string[count];

        // Initialise all as empty
        for (int i = 0; i < count; i++)
        {
            avatarIds[i] = -1;
            names[i] = "";
        }

        // Fill from real Player network objects
        foreach (var p in playerObjects)
        {
            int seat = p.SeatIndex;
            if (seat < 0 || seat >= count)
                continue;

            avatarIds[seat] = p.AvatarId;
            names[seat] = p.PlayerName.ToString();
        }

        RPC_SendFullSeatMap(avatarIds, names);
    }


    public void SyncDeck()
    {
        RPC_SendDeckCount(Deck.Count);
        Deck.Print();
    }

    public void StartTurn()
    {
        var player = GetCurrentPlayer();

        player.IsProtected = false;
        var pObj = BasicSpawner.Instance.GetPlayerObject(
    GetPlayerRefBySeat(CurrentPlayerIndex)
    );
        if (pObj != null)
            pObj.GetComponent<Player>().IsProtectedNet = false;
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
        // A) only 1 player alive → single winner situation, no tie logic needed
        int aliveCount = Players.Count(p => p != null && p.IsAlive);
        if (aliveCount == 1)
        {
            int loneWinner = Players.First(p => p != null && p.IsAlive).PlayerId;

            var result = new RoundResult
            {
                Winners = new List<int> { loneWinner },
                SpyBonusSeat = ComputeSpyBonus()   // use a helper
            };

            EndRound(result);
            return;
        }

        // B) deck empty → determine highest hand
        if (Deck.Count == 0 && CurrentTurnState == TurnState.TurnEnded)
        {
            var alive = Players.Where(p => p != null && p.IsAlive).ToList();
            int bestValue = alive.Max(p => (int)p.Hand[0].Type);

            var winners = alive
                .Where(p => (int)p.Hand[0].Type == bestValue)
                .Select(p => p.PlayerId)
                .ToList();

            // find Spy bonus seat
            var result = new RoundResult
            {
                Winners = winners,
                SpyBonusSeat = ComputeSpyBonus()
            };

            EndRound(result);
            return;
        }
    }
    private int ComputeSpyBonus()
    {
        var alive = Players.Where(p => p != null && p.IsAlive).ToList();

        var spyUsers = alive
            .Where(p => p.PlayedSpyThisRound)
            .Select(p => p.PlayerId)
            .ToList();

        if (spyUsers.Count == 1)
            return spyUsers[0];

        // 0 or >1 spy users → no bonus
        return -1;
    }



    private void EndRound(RoundResult result)
    {
        if (_roundHasEnded)
            return;

        _roundHasEnded = true;
        _hasPendingCard = false;

        Debug.Log("=== ROUND ENDED ===");
        Debug.Log("Winners: " + string.Join(",", result.Winners));
        Debug.Log("SpyBonusSeat: " + result.SpyBonusSeat);

        // ------------------------------------------------
        // Assign normal winner tokens
        // ------------------------------------------------
        foreach (int seat in result.Winners)
        {
            _victoryTokens[seat]++;
            RPC_UpdateVictoryCounter(seat, _victoryTokens[seat]);
        }

        // next starting player = first normal winner
        _lastRoundWinner = result.Winners[0];

        // ------------------------------------------------
        // Spy bonus
        // ------------------------------------------------
        if (result.SpyBonusSeat >= 0)
        {
            _victoryTokens[result.SpyBonusSeat]++;
            RPC_UpdateVictoryCounter(result.SpyBonusSeat, _victoryTokens[result.SpyBonusSeat]);
            string spyName = GetPlayerName(result.SpyBonusSeat);
            RPC_AnnounceAction($"{spyName} gains a Spy bonus token.");

        }

        // ------------------------------------------------
        // Reveal all final cards
        // ------------------------------------------------
        for (int i = 0; i < Players.Count; i++)
        {
            var p = Players[i];
            if (p == null || !p.IsAlive)
                continue;   // Skip dead or null players

            if (p.Hand.Count > 0)
            {
                var finalCard = p.Hand[0];
                RPC_RevealFinalCard(i, (int)finalCard.Type);

                // Move the card to discard pile
                p.Hand.Clear();
                p.DiscardPile.Add(finalCard);

                // Force sync to owner UI
                SyncPlayerHandToOwner(i);
                BroadcastHandCount(i);
            }
        }


        // ------------------------------------------------
        // UI Announcement
        // ------------------------------------------------
        RPC_RoundEnded(result.Winners.ToArray());
        if (result.Winners.Count == 1)
        {
            string name = GetPlayerName(result.Winners[0]);
            RPC_AnnounceWinner($"{name} wins the round.");
        }
        else
        {
            var names = result.Winners
                .Select(w => GetPlayerName(w));

            RPC_AnnounceWinner($"Tie! {string.Join(", ", names)} win the round.");
        }

        DebugDumpState("EndRound");
    }


    public void ResetVictoryTokens()
    {
        for (int i = 0; i < _victoryTokens.Length; i++)
            _victoryTokens[i] = 0;
    }


    public void SyncPlayerHandToOwner(int seatIndex)
    {
        PlayerRef owner = GetPlayerRefBySeat(seatIndex);
        NetworkObject ownerObj = BasicSpawner.Instance.GetPlayerObject(owner);

        if (ownerObj == null)
        {
            Debug.LogError($"[SyncPlayerHandToOwner] ownerObj NULL for seat {seatIndex}");
            return;
        }

        Player p = ownerObj.GetComponent<Player>();
        if (p == null)
        {
            Debug.LogError($"[SyncPlayerHandToOwner] Player component missing for seat {seatIndex}");
            return;
        }

        var realHand = GetPlayer(seatIndex).Hand;
        int[] cards = realHand.ConvertAll(c => (int)c.Type).ToArray();

        p.RPC_SendLocalHand(seatIndex, cards);
    }

    public void BroadcastHandCount(int seatIndex)
    {
        int count = GetPlayer(seatIndex).Hand.Count;
        RPC_SendHandCount(seatIndex, count);
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

    public void DebugPlayersList(string tag)
    {
        Debug.Log("===== PLAYER STATE DUMP: " + tag + " =====");

        if (Players == null)
        {
            Debug.Log("Players LIST IS NULL!");
            return;
        }

        Debug.Log("Players.Count = " + Players.Count);

        for (int i = 0; i < Players.Count; i++)
        {
            var ps = Players[i];

            if (ps == null)
            {
                Debug.Log($"Players[{i}] = NULL");
                continue;
            }

            Debug.Log($"Players[{i}] Alive={ps.IsAlive} HandCount={ps.Hand.Count}");

            if (ps.Hand.Count > 0)
            {
                string h = string.Join(",", ps.Hand.Select(c => c.Type.ToString()));
                Debug.Log($"   Hand: {h}");
            }
            else
            {
                Debug.Log("   Hand: EMPTY");
            }
        }

        Debug.Log("===== END PLAYER STATE DUMP =====");
    }




    // ====================================================================
    // RPCs     
    //
    // ===================================================================

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_RoundEnded(int[] winnerSeats)
    {
        _roundHasEnded = true;
        CurrentTurnState = TurnState.TurnEnded;

        var names = winnerSeats
       .Select(seat => GetPlayerName(seat))
       .ToArray();

        string winnerText;

        if (names.Length == 1)
        {
            winnerText = $"{names[0]} wins the round.";
        }
        else
        {
            winnerText = $"Tie! {string.Join(", ", names)} win the round.";
        }

        TableUIController.Instance.ShowRoundWinner(winnerText);
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_UnlockAllPlayers()
    {
        _roundHasEnded = false;
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
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_AnnounceWinner(string message)
    {
        Debug.Log("[ANNOUNCEWINNER] " + message);

        // TODO: Send message to UI popup
        TableUIController.Instance.ShowWinner(message);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ShowDiscard(int seatIndex, int cardType)
    {
        TableUIController.Instance.AddPlayedCard(seatIndex, new Card((CardType)cardType));
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SubmitChancellorChoices(int seatIndex, int[] choices)
    {
        Debug.Log($"[SERVER] Chancellor choices received from seat {seatIndex}: {choices[0]},{choices[1]},{choices[2]}");

        var ctx = new EffectContext
        {
            ChancellorChoices = choices
        };

        var effect = new ChancellorEffect();
        effect.Resolve(this, seatIndex, ctx);
    }



    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SendFullSeatMap(int[] avatarIds, string[] names)
    {
        Debug.Log("[GM] Received full seat map from host.");

        int count = avatarIds.Length;

        Players.Clear();
        Players.Capacity = count;

        // Rebuild PlayerState list — authoritative gameplay state stays untouched
        for (int i = 0; i < count; i++)
        {
            if (SeatToRawPlayer[i] != 0)
                Players.Add(new PlayerState(i));
            else
                Players.Add(null);
        }

        // Apply avatar + name to real networked Player objects
        var playerObjects = FindObjectsOfType<Player>();

        foreach (var p in playerObjects)
        {
            int seat = p.SeatIndex;
            if (seat < 0 || seat >= count)
                continue;

            if (avatarIds[seat] >= 0)
                p.AvatarId = avatarIds[seat];

            if (!string.IsNullOrEmpty(names[seat]))
                p.PlayerName = names[seat];
        }

        Debug.Log("[GM] Full seat map applied.");
        BuildPlayersFromSeatArray();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_RevealFinalCard(int seatIndex, int cardType)
    {
        TableUIController.Instance.AddPlayedCard(seatIndex, new Card((CardType)cardType));

    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SetAlive(int seatIndex, bool alive)
    {
        if (Players[seatIndex] != null)
            Players[seatIndex].IsAlive = alive;
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ResetAliveStates() //and protective status
    {
        for (int i = 0; i < Players.Count; i++)
        {
            var ps = Players[i];
            if (ps == null)
                continue;

            ps.IsAlive = true;
            ps.IsProtected = false;
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_CloseAllTargetUIs()
    {
        TargetSelectionUI.Instance.Close();
    }
}
