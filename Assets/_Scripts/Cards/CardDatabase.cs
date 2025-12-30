using System.Collections.Generic;

public static class CardDatabase
{
    public static List<Card> CreateDeck()
    {
        var deck = new List<Card>();

        Add(deck, CardType.Spy, 2);
        Add(deck, CardType.Guard, 6);
        Add(deck, CardType.Priest, 2);
        Add(deck, CardType.Baron, 2);
        Add(deck, CardType.Handmaiden, 2);
        Add(deck, CardType.Prince, 2);
        Add(deck, CardType.Chancellor, 2);
        Add(deck, CardType.King, 1);
        Add(deck, CardType.Countess, 1);
        Add(deck, CardType.Princess, 1);

        return deck;
    }

    private static void Add(List<Card> deck, CardType type, int count)
    {
        for (int i = 0; i < count; i++)
            deck.Add(new Card(type));
    }
}
