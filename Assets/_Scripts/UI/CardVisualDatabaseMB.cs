using UnityEngine;

public class CardVisualDatabaseMB : MonoBehaviour
{
    public static CardVisualDatabaseMB Instance { get; private set; }

    [SerializeField] private CardVisualDatabase database;

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public Sprite GetSprite(CardType type)
    {
        if (database == null)
        {
            Debug.LogError("CardVisualDatabaseMB: database is NULL!");
            return null;
        }

        return database.GetSprite(type);
    }
}
