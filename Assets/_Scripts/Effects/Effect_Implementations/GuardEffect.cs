using UnityEngine;

public class GuardEffect : ICardEffect
{
    public void Resolve(GameManager game, int sourcePlayerId, EffectContext context)
    {
        if (context.TargetPlayerId == null || context.GuessedCard == null)
        {
            Debug.LogError("GuardEffect: missing context");
            return;
        }

        var target = game.GetPlayer(context.TargetPlayerId.Value);

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
        $"Player {sourcePlayerId} guessed {context.GuessedCard} correctly! Player {target.PlayerId} is eliminated."
    );
        }
        else
        {
            Debug.Log("Guard guess was wrong");
            game.RPC_AnnounceAction(
        $"Player {sourcePlayerId} guessed {context.GuessedCard} but was wrong."
    );
        }
    }
}
