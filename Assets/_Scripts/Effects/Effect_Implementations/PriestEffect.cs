using UnityEngine;
using LoveLetter.Networking;
public class PriestEffect : ICardEffect
{
    public void Resolve(GameManager game, int sourcePlayerId, EffectContext context)
    {
        if (context.TargetPlayerId == null)
        {
            Debug.LogError("PriestEffect: missing target");
            return;
        }

        int targetSeat = context.TargetPlayerId.Value;

        var target = game.GetPlayer(targetSeat);
        if (!target.IsAlive || target.IsProtected || target.Hand.Count == 0)
            return;

        Card targetCard = target.Hand[0];

        // Find the Player component of the SOURCE (the one who played Priest)
        Player sourcePlayerObj = BasicSpawner.Instance.GetPlayerBySeat(sourcePlayerId);

        if (sourcePlayerObj == null)
        {
            Debug.LogError("PriestEffect: Could not find source player object!");
            return;
        }

        // Send UI ONLY to the Priest player
        sourcePlayerObj.RPC_ShowPriestResult((int)targetCard.Type);

        // Public announce
        game.RPC_AnnounceAction(
            $"Player {sourcePlayerId} played Priest and looked at Player {targetSeat}'s hand."
        );
    }
}
