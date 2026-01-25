using UnityEngine;

public class SpyEffect : ICardEffect
{
    public void Resolve(GameManager game, int sourcePlayerId, EffectContext context)
    {
        var player = game.Players[sourcePlayerId];
        player.PlayedSpyThisRound = true;
        string sourceName = game.GetPlayerName(sourcePlayerId);

        Debug.Log($"{sourceName} played Spy");
        game.RPC_AnnounceAction($"{sourceName} played Spy.");
    }
}
