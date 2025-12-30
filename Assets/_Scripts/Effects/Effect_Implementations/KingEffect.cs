using UnityEngine;
using System.Collections.Generic;

public class KingEffect : ICardEffect
{
    public void Resolve(GameManager game, int sourcePlayerId, EffectContext context)
    {
        if (context.TargetPlayerId == null)
        {
            Debug.LogError("KingEffect: missing target");
            return;
        }

        var source = game.GetPlayer(sourcePlayerId);
        var target = game.GetPlayer(context.TargetPlayerId.Value);

        if (!target.IsAlive || target.IsProtected)
        {
            Debug.Log("King target invalid or protected");
            return;
        }

        var temp = new List<Card>(source.Hand);
        source.Hand.Clear();
        source.Hand.AddRange(target.Hand);
        target.Hand.Clear();
        target.Hand.AddRange(temp);

        Debug.Log($"Player {sourcePlayerId} and Player {target.PlayerId} swapped hands");
    }
}
