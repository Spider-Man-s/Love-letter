using UnityEngine;

public class AvatarScriptable : MonoBehaviour
{
    public static AvatarScriptable Instance;
    public Sprite[] avatars;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
