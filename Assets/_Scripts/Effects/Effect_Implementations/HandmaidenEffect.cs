using UnityEngine;

public class HandmaidenEffect : ICardEffect
{
    public void Resolve(GameManager game, int sourcePlayerId, EffectContext context)
    {
        var player = game.GetPlayer(sourcePlayerId);
        player.IsProtected = true;

        Debug.Log($"Player {sourcePlayerId} is protected until next turn");
    }
}
