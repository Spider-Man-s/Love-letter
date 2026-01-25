using UnityEngine;

public class PrincessEffect : ICardEffect
{
    public void Resolve(GameManager game, int sourcePlayerId, EffectContext context)
    {
        game.EliminatePlayer(sourcePlayerId);
        string sourceName = game.GetPlayerName(sourcePlayerId);

        Debug.Log($"{sourceName} played Princess and is eliminated");
        game.RPC_AnnounceAction($"{sourceName} played Princess and is eliminated.");
    }
}
