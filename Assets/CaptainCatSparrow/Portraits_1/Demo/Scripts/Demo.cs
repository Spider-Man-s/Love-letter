using UnityEngine;
using UnityEngine.UI;

public class Demo : MonoBehaviour
{
    [SerializeField]
    private Sprite[] backgroundSprites;

    [SerializeField]
    private Sprite[] avatarSprites;

    private int _currentBackgroundSpriteIndex, _currentAvatarSpriteIndex;
    private Image _backgroundImage, _avatarImage;

    void Awake()
    {
        _backgroundImage = GameObject.Find("ImageBackground").GetComponent<Image>();
        _avatarImage = GameObject.Find("Portrait").GetComponent<Image>();
        _currentBackgroundSpriteIndex = 0;
        _currentAvatarSpriteIndex = 0;
        GameObject.Find("Left_RightArrow").GetComponent<Button>().onClick.AddListener(() => MoveAvatar(1));
        GameObject.Find("Left_LeftArrow").GetComponent<Button>().onClick.AddListener(() => MoveAvatar(-1));
        GameObject.Find("Right_RightArrow").GetComponent<Button>().onClick.AddListener(() => MoveBackground(1));
        GameObject.Find("Right_LeftArrow").GetComponent<Button>().onClick.AddListener(() => MoveBackground(-1));
        GameObject.Find("Left_RandomBtn").GetComponent<Button>().onClick.AddListener(() => RandomAvatar());
        GameObject.Find("Right_RandomBtn").GetComponent<Button>().onClick.AddListener(() => RandomBackground());
    }
    private void RandomBackground()
    {
        var random = new System.Random();
        _currentBackgroundSpriteIndex = random.Next(0, backgroundSprites.Length - 1);
        _backgroundImage.sprite = backgroundSprites[_currentBackgroundSpriteIndex];
    }

    private void RandomAvatar()
    {
        var random = new System.Random();
        _currentAvatarSpriteIndex = random.Next(0, avatarSprites.Length - 1);
        _avatarImage.sprite = avatarSprites[_currentAvatarSpriteIndex];
    }

    private void MoveBackground(int step)
    {
        _currentBackgroundSpriteIndex += step;
        if (_currentBackgroundSpriteIndex < 0)
            _currentBackgroundSpriteIndex = backgroundSprites.Length - 1;
        else if (_currentBackgroundSpriteIndex > backgroundSprites.Length - 1)
            _currentBackgroundSpriteIndex = 0;
        _backgroundImage.sprite = backgroundSprites[_currentBackgroundSpriteIndex];
    }

    private void MoveAvatar(int step)
    {
        _currentAvatarSpriteIndex += step;
        if (_currentAvatarSpriteIndex < 0)
            _currentAvatarSpriteIndex = avatarSprites.Length - 1;
        else if (_currentAvatarSpriteIndex > avatarSprites.Length - 1)
            _currentAvatarSpriteIndex = 0;
        _avatarImage.sprite = avatarSprites[_currentAvatarSpriteIndex];
    }


}
