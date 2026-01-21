using UnityEngine;

public class BaronEffect : ICardEffect
{
    public void Resolve(GameManager game, int sourcePlayerId, EffectContext context)
    {
        if (context.TargetPlayerId == null)
        {
            Debug.LogError("BaronEffect: missing target");
            return;
        }

        var source = game.GetPlayer(sourcePlayerId);
        var target = game.GetPlayer(context.TargetPlayerId.Value);

        if (!target.IsAlive || target.IsProtected)
        {
            Debug.Log("Baron target invalid or protected");
            return;
        }

        if (source.Hand.Count == 0 || target.Hand.Count == 0)
        {
            Debug.LogError("BaronEffect: empty hand");
            return;
        }

        var sourceCard = source.Hand[0];
        var targetCard = target.Hand[0];

        Debug.Log($"Baron compare: P{sourcePlayerId}({sourceCard}) vs P{target.PlayerId}({targetCard})");

        if (sourceCard.Value > targetCard.Value)
        {
            game.EliminatePlayer(target.PlayerId);
            game.RPC_AnnounceAction(
        $"Player {sourcePlayerId} played Baron and eliminated Player {target.PlayerId}.");
        }
        else if (sourceCard.Value < targetCard.Value)
        {
            game.EliminatePlayer(sourcePlayerId);
            game.RPC_AnnounceAction(
        $"Player {sourcePlayerId} played Baron and was eliminated by Player {target.PlayerId}.");
        }
        else
        {
            Debug.Log("Baron tie – no elimination");
            game.RPC_AnnounceAction(
        $"Player {sourcePlayerId} played Baron against Player {target.PlayerId} but it was a tie.");
        }
    }
}
