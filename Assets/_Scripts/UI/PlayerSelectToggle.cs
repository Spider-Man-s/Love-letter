using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerSelectToggle : MonoBehaviour
{
    [SerializeField] private Toggle toggle;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image avatar;
    [SerializeField] private CanvasGroup canvasGroup;

    private int seatIndex;

    public void Setup(
        int seat,
        string playerName,
        Sprite avatarSprite,
        bool interactable,
        ToggleGroup group,
        System.Action<int> onSelected
    )
    {
        seatIndex = seat;

        nameText.text = playerName;
        avatar.sprite = avatarSprite;

        toggle.group = group;
        toggle.isOn = false;
        toggle.interactable = interactable;

        canvasGroup.alpha = interactable ? 1f : 0.35f;

        toggle.onValueChanged.RemoveAllListeners();
        if (interactable)
        {
            toggle.onValueChanged.AddListener(isOn =>
            {
                if (isOn)
                    onSelected(seatIndex);
            });
        }
    }
}
