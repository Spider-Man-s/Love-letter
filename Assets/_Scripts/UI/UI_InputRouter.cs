using UnityEngine;
using LoveLetter.Networking;

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

        if (BasicSpawner.PlayerData.LocalSeatIndex < 0)
        {
            Debug.LogWarning("[UI] Cannot click card yet: local seat not assigned.");
            return;
        }

        if (!GameManager.Instance.CanPlayerPlayThisCard(cardView))
        {
            Debug.Log("[UI] Not allowed to play this card right now.");
            return;
        }

        GameManager.Instance.LocalPlayerPlayedCard(cardView);
    }
}
