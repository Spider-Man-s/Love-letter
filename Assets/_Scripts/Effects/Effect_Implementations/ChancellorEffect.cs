using UnityEngine;
using System.Collections.Generic;
using LoveLetter.Networking;
using Fusion;
public class ChancellorEffect : ICardEffect
{
    public void Resolve(GameManager game, int sourcePlayerId, EffectContext context)
    {
        var player = game.GetPlayer(sourcePlayerId);

        // Draw 2 cards
        Card c1 = game.Deck.Draw();
        Card c2 = game.Deck.Draw();

        player.Hand.Add(c1);
        player.Hand.Add(c2);

        // Update deck count for everyone
        game.SyncDeck();

        // Sync the full 3-card hand to this client
        game.SyncPlayerHandToOwner(sourcePlayerId);

        // Find the NetworkObject of the player
        PlayerRef owner = game.GetPlayerRefBySeat(sourcePlayerId);
        var obj = BasicSpawner.Instance.GetPlayerObject(owner);

        if (obj == null)
        {
            Debug.LogError("[ChancellorEffect] Player object is NULL — cannot open UI!");
            return;
        }

        // CALL THE RPC ON THE PLAYER COMPONENT (NOT GameManager)
        obj.GetComponent<Player>()
            .RPC_OpenChancellorUI(sourcePlayerId);
    }

}
