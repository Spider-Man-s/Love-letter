using UnityEngine;
using LoveLetter.Networking;
public class PriestEffect : ICardEffect
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

        int targetSeat = context.TargetPlayerId.Value;
        string sourceName = game.GetPlayerName(sourcePlayerId);
        string targetName = game.GetPlayerName(targetSeat);
        var target = game.GetPlayer(targetSeat);
        if (!target.IsAlive || target.IsProtected || target.Hand.Count == 0)
            return;

        Card targetCard = target.Hand[0];
        Player sourcePlayerObj = BasicSpawner.Instance.GetPlayerBySeat(sourcePlayerId);

        if (sourcePlayerObj == null)
        {
            Debug.LogError("PriestEffect: Could not find source player object!");
            return;
        }

        sourcePlayerObj.RPC_ShowPriestResult((int)targetCard.Type);

        game.RPC_AnnounceAction(
            $"{sourceName} played Priest and looked at {targetName}'s hand."
        );
    }
}
