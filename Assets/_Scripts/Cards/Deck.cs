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

    public IEnumerable<Card> GetCards()
    {
        return cards.ToArray();
    }

    public void OverrideStack(List<Card> ordered)
    {
        cards = new Stack<Card>();

        for (int i = 0; i < ordered.Count; i++)
            cards.Push(ordered[i]);
    }
    public void PutOnBottom(Card card)
    {
        // Convert stack → list so list[0] = TOP, list[last] = BOTTOM
        var list = cards.ToList();

        // Add to the bottom (END of list)
        list.Add(card);

        // Now rebuild the stack so last element remains BOTTOM
        cards = new Stack<Card>(list.Reverse<Card>());
    }

    public void PutSecondToLast(Card card)
    {
        var list = cards.ToList(); // list[0] = top

        if (list.Count == 0)
        {
            PutOnBottom(card);
            return;
        }

        // Insert just before the last element (second from bottom)
        list.Insert(list.Count - 1, card);

        cards = new Stack<Card>(list.Reverse<Card>());
    }




    public int Count => cards.Count;
}
