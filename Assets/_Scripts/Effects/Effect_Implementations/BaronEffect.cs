using UnityEngine;
using LoveLetter.Networking;
public class BaronEffect : ICardEffect
{
    public void Resolve(GameManager game, int sourcePlayerId, EffectContext context)
    {
        if (context.TargetPlayerId == null)
        {
            Debug.LogError("BaronEffect: missing target");
            return;
        }

        int targetId = context.TargetPlayerId.Value;

        var source = game.GetPlayer(sourcePlayerId);
        var target = game.GetPlayer(targetId);

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

        // ====================================================================
        // GET PLAYER OBJECTS FOR RPC TARGETING
        // ====================================================================
        Player sourcePlayerObj = BasicSpawner.Instance.GetPlayerBySeat(sourcePlayerId);
        Player targetPlayerObj = BasicSpawner.Instance.GetPlayerBySeat(targetId);

        if (sourcePlayerObj == null || targetPlayerObj == null)
        {
            Debug.LogError("BaronEffect: Could not find Player objects for UI.");
            return;
        }
        // ====================================================================
        // PRIVATE UI
        // ====================================================================

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

        // ====================================================================
        // PUBLIC ANNOUNCE
        // ====================================================================
        if (outcome == 1)
        {
            game.EliminatePlayer(targetId);
            game.RPC_AnnounceAction(
                $"Player {sourcePlayerId} played Baron and eliminated Player {targetId}.");
        }
        else if (outcome == -1)
        {
            game.EliminatePlayer(sourcePlayerId);
            game.RPC_AnnounceAction(
                $"Player {sourcePlayerId} played Baron and was eliminated by Player {targetId}.");
        }
        else
        {
            game.RPC_AnnounceAction(
                $"Player {sourcePlayerId} played Baron against Player {targetId} but it was a tie.");
        }
    }
}

