using UnityEngine;
using TMPro;
using LoveLetter.Networking;
public class DeckView : MonoBehaviour
{
    [SerializeField] private CardView cardPrefab;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private Transform cardParent;

    private CardView deckCard;
    void Start()
    {


    }
    public void Initialize()
    {
        deckCard = Instantiate(cardPrefab, cardParent, false);
        deckCard.Setup(new Card(CardType.CardBack));

        var rect = deckCard.GetComponent<RectTransform>();
        rect.anchoredPosition3D = Vector3.zero;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    public void UpdateCount(int count)
    {

        deckCard.gameObject.SetActive(count > 0);
        countText.text = count.ToString();
    }
}
