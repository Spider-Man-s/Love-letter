[System.Serializable]
public class Card
{
    public CardType Type;

    public int Value => (int)Type;

    public Card(CardType type)
    {
        Type = type;
    }

    public override string ToString()
    {
        return $"{Type} ({Value})";
    }
}
