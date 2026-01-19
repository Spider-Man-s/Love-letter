using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CardView : MonoBehaviour,
    IPointerClickHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [SerializeField] private Image art;
    [SerializeField] private GameObject highlight;

    private Vector3 originalScale;

    public Card CardData { get; private set; }

    // These are controlled by GameManager / TableUIController
    public bool IsLocalCard { get; set; } = false;
    public bool interactable { get; set; } = true;

    private void Awake()
    {
        originalScale = transform.localScale;

        if (highlight != null)
            highlight.SetActive(false);
    }

    // === SETUP ===
    public void Setup(Card card)
    {
        CardData = card;

        art.sprite = CardVisualDatabaseMB.Instance.GetSprite(card.Type);

        if (highlight != null)
            highlight.SetActive(false);
    }

    // === HOVER EFFECTS ===
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsLocalCard || !interactable)
            return;

        transform.localScale = originalScale * 1.2f;

        if (highlight != null)
            highlight.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = originalScale;

        if (highlight != null)
            highlight.SetActive(false);
    }

    // === CLICK ===
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!IsLocalCard) return;
        if (!interactable) return;

        UI_InputRouter.Instance.OnCardClicked(this);
    }

    // === CALLED BY UI MANAGER ===
    public void SetInteractable(bool value)
    {
        interactable = value;

        // Optional: gray out art
        art.color = interactable ? Color.white : Color.red;
    }
}
