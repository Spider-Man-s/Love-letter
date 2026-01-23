using System.Collections.Generic;
using System.Linq;

public class Deck
{
    private Stack<Card> cards;

    public Deck(List<Card> initialCards)
    {
        cards = new Stack<Card>(initialCards);
    }

    public void Shuffle()
    {
        var shuffled = cards.OrderBy(c => UnityEngine.Random.value).ToList();
        cards = new Stack<Card>(shuffled);
    }

    public Card Draw()
    {
        if (cards.Count == 0)
            return null;

        return cards.Pop();
    }

    public void Print()
    {
        string cardsList = "Deck contains:\n";
        foreach (var card in cards)
        {
            cardsList += $"- {card.Type}\n";
        }
        UnityEngine.Debug.Log(cardsList);
    }

    public int Count => cards.Count;
}
