using UnityEngine;

public class UI_InputRouter : MonoBehaviour
{
    public static UI_InputRouter Instance;

    private void Awake()
    {
        Instance = this;
    }

    public void OnCardClicked(CardView cardView)
    {
        if (cardView == null) return;

        Debug.Log($"[UI] Card clicked: {cardView.CardData.Type}");

        // Ask GameManager if it can be played right now
        if (!GameManager.Instance.CanPlayerPlayThisCard(cardView))
        {
            Debug.Log("[UI] Not allowed to play this card right now.");
            return;
        }

        GameManager.Instance.LocalPlayerPlayedCard(cardView);
    }
}
