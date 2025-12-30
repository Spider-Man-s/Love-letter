using UnityEngine;

public class ChancellorEffect : ICardEffect
{
    public void Resolve(GameManager game, int sourcePlayerId, EffectContext context)
    {
        game.ChancellorDrawAndReturn(sourcePlayerId);
    }
}
