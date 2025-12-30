using System.Collections.Generic;
using UnityEngine;

public class PlayerState
{
    public int PlayerId;
    public List<Card> Hand = new();
    public bool IsProtected;
    public bool IsAlive = true;
    public bool PlayedSpyThisRound;
    public List<Card> DiscardPile = new();

    public PlayerState(int id)
    {
        PlayerId = id;
    }
    public void DrawCard(Card card)
    {
        if (card != null)
            Hand.Add(card);
    }

    public void RemoveCard(Card card)
    {
        Hand.Remove(card);
    }
    public void DiscardHand()
    {
        foreach (var card in Hand)
            DiscardPile.Add(card);

        Hand.Clear();
    }

}
