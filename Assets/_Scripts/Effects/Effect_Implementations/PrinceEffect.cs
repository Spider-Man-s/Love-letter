using UnityEngine;

public class PrinceEffect : ICardEffect
{
    public void Resolve(GameManager game, int sourcePlayerId, EffectContext context)
    {
        if (context.TargetPlayerId == null)
        {
            Debug.LogError("PrinceEffect: target was unexpectedly null.");
            return;
        }

        int targetId = context.TargetPlayerId.Value;
        var target = game.GetPlayer(targetId);

        if (!target.IsAlive)
        {
            game.RPC_AnnounceAction(
                $"Player {sourcePlayerId} tried to use Prince on Player {targetId}, but they are eliminated.");
            return;
        }

        // Protection still matters
        if (target.IsProtected)
        {
            game.RPC_AnnounceAction(
                $"Player {sourcePlayerId} tried to use Prince on Player {targetId}, but they were protected.");
            return;
        }

        if (target.Hand.Count == 0)
        {
            Debug.LogError($"PrinceEffect: Player {targetId} has no card to discard.");
            return;
        }

        // DISCARD
        Card discarded = target.Hand[0];
        target.Hand.Clear();
        target.DiscardPile.Add(discarded);
        // SHOW discarded card to everyone
        game.RPC_ShowDiscard(targetId, (int)discarded.Type);


        game.RPC_AnnounceAction(
            $"Player {sourcePlayerId} played Prince. Player {targetId} discarded {discarded.Type}.");

        // Princess instant loss
        if (discarded.Type == CardType.Princess)
        {
            game.EliminatePlayer(targetId);

            // Update UI for everyone
            game.SyncPlayerHandToOwner(targetId);
            game.BroadcastHandCount(targetId);
            return;
        }

        // DRAW NEW CARD (Prince Draw)
        Card newCard = game.Deck.Draw();
        target.DrawCard(newCard);
        game.SyncDeck();

        Debug.Log($"[PrinceEffect] Player {targetId} draws {newCard.Type}");

        // SYNC UI ON ALL CLIENTS
        game.SyncPlayerHandToOwner(targetId);
        game.BroadcastHandCount(targetId);
    }
}
