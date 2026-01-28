using UnityEngine;
using UnityEngine.UI;

public class MusicToggle : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField] private Sprite soundOnSprite;
    [SerializeField] private Sprite soundOffSprite;

    [Header("Button Icon")]
    [SerializeField] private Image icon;

    [Header("Audio Source")]
    [SerializeField] private AudioSource musicSource;
    private void Start()
    {
        UpdateIcon();
    }

    public void ToggleMusic()
    {
        if (musicSource == null)
        {
            Debug.LogWarning("MusicToggle: No AudioSource assigned.");
            return;
        }

        musicSource.mute = !musicSource.mute;
        UpdateIcon();
    }

    private void UpdateIcon()
    {
        if (icon == null) return;

        icon.sprite = musicSource.mute ? soundOffSprite : soundOnSprite;
    }
}
