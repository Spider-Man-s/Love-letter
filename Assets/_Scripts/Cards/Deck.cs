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
        // Convert stack (top → bottom) to list
        var list = cards.ToList();
        // Insert card at bottom (beginning)
        list.Insert(0, card);
        // Reverse back into push order
        list.Reverse();
        cards = new Stack<Card>(list);
    }

    public void PutSecondToLast(Card card)
    {
        var list = cards.ToList(); // top → bottom

        if (list.Count < 1)
        {
            PutOnBottom(card);
            return;
        }

        // second from bottom = index 1 from bottom
        list.Insert(1, card);

        // restore push order
        list.Reverse();
        cards = new Stack<Card>(list);
    }



    public int Count => cards.Count;
}
