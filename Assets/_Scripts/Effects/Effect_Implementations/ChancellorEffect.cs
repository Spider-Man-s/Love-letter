using UnityEngine;
using LoveLetter.Networking;
using Fusion;

public class ChancellorEffect : ICardEffect
{
    public void Resolve(GameManager game, int sourcePlayerId, EffectContext context)
    {
        var player = game.GetPlayer(sourcePlayerId);
        int deckCount = game.Deck.Count;

        // ======================================================
        // CASE 1: Deck has 0 cards → Chancellor is a dead card
        // ======================================================
        if (deckCount == 0)
        {
            Debug.Log("[Chancellor] Deck empty → No draw → No UI → No choices.");
            // Sync (so clients see empty deck)
            game.SyncDeck();

            // Inform all clients
            string name = game.GetPlayerName(sourcePlayerId);
            game.RPC_AnnounceAction($"{name} played Chancellor, but the deck was empty.");

            // End turn normally
            game.ServerResolveChancellor_NoChoices(sourcePlayerId);
            return;
        }

        // ======================================================
        // CASE 2: Deck has 1 card → draw only that one
        // ======================================================
        Card c1 = game.Deck.Draw();
        player.Hand.Add(c1);

        Card c2 = null;

        if (deckCount >= 2)
        {
            c2 = game.Deck.Draw();
            player.Hand.Add(c2);
        }

        // Now sync deck count to all clients
        game.SyncDeck();

        int handCount = player.Hand.Count; // Will be 2 or 3

        // Sync hand to correct player ONLY
        game.SyncPlayerHandToOwner(sourcePlayerId);

        // ======================================================
        // CASE 3: Show UI only if 2 or 3 cards exist
        // ======================================================

        PlayerRef owner = game.GetPlayerRefBySeat(sourcePlayerId);
        var playerObj = BasicSpawner.Instance.GetPlayerObject(owner);

        if (playerObj == null)
        {
            Debug.LogError("[ChancellorEffect] ERROR: Player object missing.");
            return;
        }

        Debug.Log($"[Chancellor] Opening UI with hand count {handCount}");

        // Show Chancellor panel (2-card or 3-card logic handled in UI)
        playerObj.GetComponent<Player>()
     .RPC_OpenChancellorUI(sourcePlayerId, deckCount);
    }
}
