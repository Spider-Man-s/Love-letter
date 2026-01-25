using UnityEngine;
using LoveLetter.Networking;
public class HandmaidenEffect : ICardEffect
{
    public void Resolve(GameManager game, int sourcePlayerId, EffectContext context)
    {
        var player = game.GetPlayer(sourcePlayerId);
        player.IsProtected = true;
        string sourceName = game.GetPlayerName(sourcePlayerId);
        var pObj = BasicSpawner.Instance.GetPlayerObject(
    game.GetPlayerRefBySeat(sourcePlayerId)
);
        if (pObj != null)
            pObj.GetComponent<Player>().IsProtectedNet = true;

        Debug.Log($"{sourceName} is protected until next turn");
        game.RPC_AnnounceAction($"{sourceName} is protected until next turn.");
    }
}
