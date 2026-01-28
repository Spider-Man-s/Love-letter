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
    public bool IsLocalCard { get; set; } = false;
    public bool interactable { get; set; } = true;

    private void Awake()
    {
        originalScale = transform.localScale;

        if (highlight != null)
            highlight.SetActive(false);
    }

    public void Setup(Card card)
    {
        CardData = card;

        art.sprite = CardVisualDatabaseMB.Instance.GetSprite(card.Type);

        if (highlight != null)
            highlight.SetActive(false);
    }

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
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!IsLocalCard) return;
        if (!interactable) return;

        UI_InputRouter.Instance.OnCardClicked(this);
    }

    public void SetInteractable(bool value)
    {
        interactable = value;

        art.color = interactable ? Color.white : Color.red;
    }
}
