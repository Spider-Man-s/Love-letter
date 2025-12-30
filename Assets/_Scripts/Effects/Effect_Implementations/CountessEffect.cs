using UnityEngine;

public class CountessEffect : ICardEffect
{
    public void Resolve(GameManager game, int sourcePlayerId, EffectContext context)
    {
        // samo log za test, nema dodatnog efekta
        Debug.Log($"Player {sourcePlayerId} played Countess");
    }
}
