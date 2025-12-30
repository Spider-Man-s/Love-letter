using UnityEngine;

public class SpyEffect : ICardEffect
{
    public void Resolve(GameManager game, int sourcePlayerId, EffectContext context)
    {
        var player = game.Players[sourcePlayerId];
        player.PlayedSpyThisRound = true;

        Debug.Log($"Player {sourcePlayerId} played Spy");
    }
}
