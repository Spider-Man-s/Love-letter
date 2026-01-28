using UnityEngine;
using LoveLetter.Networking;
public class BaronEffect : ICardEffect
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
            Debug.Log("Baron target invalid or protected");
            return;
        }

        var sourceCard = source.Hand[0];
        var targetCard = target.Hand[0];

        int outcome = 0;
        if (sourceCard.Value > targetCard.Value) outcome = 1;
        else if (sourceCard.Value < targetCard.Value) outcome = -1;

        Player sourcePlayerObj = BasicSpawner.Instance.GetPlayerBySeat(sourcePlayerId);
        Player targetPlayerObj = BasicSpawner.Instance.GetPlayerBySeat(targetId);

        if (sourcePlayerObj == null || targetPlayerObj == null)
        {
            Debug.LogError("BaronEffect: Could not find Player objects for UI.");
            return;
        }

        sourcePlayerObj.RPC_ShowBaronResult(
          (int)sourceCard.Type,
          (int)targetCard.Type,
          outcome
      );

        targetPlayerObj.RPC_ShowBaronResult(
            (int)targetCard.Type,
            (int)sourceCard.Type,
            -outcome
        );

        if (outcome == 1)
        {
            game.EliminatePlayer(targetId);
            game.RPC_AnnounceAction(
                $"{sourceName} played Baron and eliminated {targetName}.");
        }
        else if (outcome == -1)
        {
            game.EliminatePlayer(sourcePlayerId);
            game.RPC_AnnounceAction(
                $"{sourceName} played Baron and was eliminated by {targetName}.");
        }
        else
        {
            game.RPC_AnnounceAction(
                $"{sourceName} played Baron against {targetName} but it was a tie.");
        }
    }
}

