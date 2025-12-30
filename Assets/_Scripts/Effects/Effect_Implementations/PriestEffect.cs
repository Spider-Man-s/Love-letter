using UnityEngine;

public class PriestEffect : ICardEffect
{
    public void Resolve(GameManager game, int sourcePlayerId, EffectContext context)
    {
        if (context.TargetPlayerId == null)
        {
            Debug.LogError("PriestEffect: missing target");
            return;
        }

        var target = game.GetPlayer(context.TargetPlayerId.Value);

        if (!target.IsAlive)
        {
            Debug.Log("Priest target is eliminated");
            return;
        }

        if (target.IsProtected)
        {
            Debug.Log("Priest target is protected");
            return;
        }

        if (target.Hand.Count == 0)
        {
            Debug.LogError("Priest target has empty hand");
            return;
        }

        var card = target.Hand[0];
        Debug.Log($"Player {sourcePlayerId} looks at Player {target.PlayerId}'s hand: {card}");
    }
}
