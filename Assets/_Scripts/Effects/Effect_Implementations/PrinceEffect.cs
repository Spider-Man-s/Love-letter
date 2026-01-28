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
        string sourceName = game.GetPlayerName(sourcePlayerId);
        string targetName = game.GetPlayerName(targetId);


        if (!target.IsAlive)
        {
            game.RPC_AnnounceAction(
                $"{sourceName} tried to use Prince on {targetName}, but they are eliminated.");
            return;
        }
        if (target.IsProtected)
        {
            game.RPC_AnnounceAction(
                $"{sourceName} tried to use Prince on {targetName}, but they were protected.");
            return;
        }

        if (target.Hand.Count == 0)
        {
            Debug.LogError($"PrinceEffect: Player {targetId} has no card to discard.");
            return;
        }

        Card discarded = target.Hand[0];
        target.Hand.Clear();
        target.DiscardPile.Add(discarded);

        game.RPC_ShowDiscard(targetId, (int)discarded.Type);


        game.RPC_AnnounceAction(
            $"{sourceName} played Prince. {targetName} discarded {discarded.Type}.");

        if (discarded.Type == CardType.Princess)
        {
            game.EliminatePlayer(targetId);
            game.SyncPlayerHandToOwner(targetId);
            game.BroadcastHandCount(targetId);
            return;
        }

        Card newCard;

        if (game.Deck.Count == 0)
        {
            newCard = game.GetBonusCard();
        }
        else
        {
            newCard = game.Deck.Draw();
        }

        target.DrawCard(newCard);
        game.SyncDeck();

        Debug.Log($"[PrinceEffect] Player {targetId} draws {newCard.Type}");

        game.SyncPlayerHandToOwner(targetId);
        game.BroadcastHandCount(targetId);
    }
}
