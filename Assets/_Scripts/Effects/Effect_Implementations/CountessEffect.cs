using UnityEngine;

public class CountessEffect : ICardEffect
{
    public void Resolve(GameManager game, int sourcePlayerId, EffectContext context)
    {
        string sourceName = game.GetPlayerName(sourcePlayerId);
        Debug.Log($"{sourceName} played Countess");
        game.RPC_AnnounceAction($"{sourceName} played Countess");
    }
}
