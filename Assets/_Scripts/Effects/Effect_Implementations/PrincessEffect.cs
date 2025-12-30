using UnityEngine;

public class PrincessEffect : ICardEffect
{
    public void Resolve(GameManager game, int sourcePlayerId, EffectContext context)
    {
        game.EliminatePlayer(sourcePlayerId);

        Debug.Log($"Player {sourcePlayerId} played Princess and is eliminated");
    }
}
