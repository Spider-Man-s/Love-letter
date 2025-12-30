using UnityEngine;

public class PrinceEffect : ICardEffect
{
    public void Resolve(GameManager game, int sourcePlayerId, EffectContext context)
    {
        if (context.TargetPlayerId == null)
        {
            Debug.LogError("PrinceEffect: missing target");
            return;
        }

        game.ForceDiscardAndDraw(context.TargetPlayerId.Value);
    }
}
