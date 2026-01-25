using UnityEngine;

public class GuardEffect : ICardEffect
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

        var target = game.GetPlayer(context.TargetPlayerId.Value);
        string sourceName = game.GetPlayerName(sourcePlayerId);
        string targetName = game.GetPlayerName(context.TargetPlayerId.Value);
        if (!target.IsAlive)
            return;

        if (target.IsProtected)
        {
            Debug.Log("Guard target is protected");
            return;
        }

        if (context.GuessedCard == CardType.Guard)
        {
            Debug.Log("Guard cannot guess Guard");
            return;
        }

        bool hit = target.Hand.Exists(c => c.Type == context.GuessedCard);

        if (hit)
        {
            game.EliminatePlayer(target.PlayerId);
            game.RPC_AnnounceAction(
        $"{sourceName} guessed {context.GuessedCard} correctly! {targetName} is eliminated."
    );
        }
        else
        {
            Debug.Log("Guard guess was wrong");
            game.RPC_AnnounceAction(
        $"{sourceName} guessed {context.GuessedCard} but was wrong."
    );
        }
    }
}
