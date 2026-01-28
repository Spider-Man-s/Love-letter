using UnityEngine;

public class OpenMenuFirst : MonoBehaviour
{
    void Awake()
    {
        if (!PlayerPrefs.HasKey("HasLaunchedBefore"))
        {
            PlayerPrefs.SetInt("ReturnedFromGame", 0);
            PlayerPrefs.SetInt("HasLaunchedBefore", 1);

            Debug.Log("First launch → ReturnedFromGame set to 0");
        }
    }
}
