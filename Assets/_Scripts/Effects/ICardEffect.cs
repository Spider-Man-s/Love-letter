public interface ICardEffect
{
    void Resolve(GameManager game, int sourcePlayerId, EffectContext context);
}
