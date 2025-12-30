using System.Collections.Generic;

public static class CardEffectFactory
{
    private static Dictionary<CardType, ICardEffect> effects;

    static CardEffectFactory()
    {
        effects = new Dictionary<CardType, ICardEffect>
        {
            { CardType.Spy, new SpyEffect() },
            { CardType.Guard, new GuardEffect() },
            { CardType.Priest, new PriestEffect() },
            { CardType.Baron, new BaronEffect() },
            { CardType.Handmaiden, new HandmaidenEffect() },
            { CardType.Prince, new PrinceEffect() },
            { CardType.Chancellor, new ChancellorEffect() },
            { CardType.King, new KingEffect() },
            { CardType.Countess, new CountessEffect() },
            { CardType.Princess, new PrincessEffect() }
        };
    }

    public static ICardEffect Get(CardType type)
    {
        return effects[type];
    }
}
