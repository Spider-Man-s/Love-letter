using UnityEngine;
using System.Collections.Generic;
using LoveLetter.Networking;
public class KingEffect : ICardEffect
{
    public void Resolve(GameManager game, int sourcePlayerId, EffectContext context)
    {
        if (context.TargetPlayerId == null)
        {
            Debug.Log($"{GetType().Name}: Player {sourcePlayerId} discarded the card (no valid targets).");

            game.RPC_AnnounceAction(
                $"{game.GetPlayerName(sourcePlayerId)} discarded {GetType().Name.Replace("Effect", "")}.");

            return;
        }

        int targetId = context.TargetPlayerId.Value;

        var source = game.GetPlayer(sourcePlayerId);
        var target = game.GetPlayer(targetId);

        string sourceName = game.GetPlayerName(sourcePlayerId);
        string targetName = game.GetPlayerName(targetId);

        if (!target.IsAlive || target.IsProtected)
        {
            Debug.Log("King target invalid or protected");
            return;
        }

        // Swap hands
        var temp = new List<Card>(source.Hand);
        source.Hand.Clear();
        source.Hand.AddRange(target.Hand);
        target.Hand.Clear();
        target.Hand.AddRange(temp);

        Debug.Log($"Player {sourcePlayerId} and Player {targetId} swapped hands");

        game.RPC_AnnounceAction(
            $"{sourceName} played King and swapped hands with {targetName}.");

        // NEW: Sync real hands to owners
        game.SyncPlayerHandToOwner(sourcePlayerId);
        game.SyncPlayerHandToOwner(targetId);

        // NEW: Sync hand counts to everyone else
        game.BroadcastHandCount(sourcePlayerId);
        game.BroadcastHandCount(targetId);
    }
}
