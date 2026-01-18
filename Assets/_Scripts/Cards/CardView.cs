using UnityEngine;
using UnityEngine.UI;

public class CardView : MonoBehaviour
{
    [SerializeField] private Image art;
    [SerializeField] private GameObject highlight;

    public Card CardData { get; private set; }

    public void Setup(Card card)
    {
        CardData = card;

        // Set sprite from database
        art.sprite = CardVisualDatabaseMB.Instance.GetSprite(card.Type);


        // Turn highlight off by default
        if (highlight != null)
            highlight.SetActive(false);
    }

    public void SetHighlighted(bool value)
    {
        if (highlight != null)
            highlight.SetActive(value);
    }
}
