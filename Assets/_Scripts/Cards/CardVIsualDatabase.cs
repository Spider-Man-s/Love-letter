using UnityEngine;

[CreateAssetMenu(menuName = "LoveLetter/Card Visual Database")]
public class CardVisualDatabase : ScriptableObject
{
    public CardVisual[] visuals;

    private static CardVisualDatabase _instance;
    public static CardVisualDatabase Instance
    {
        get
        {
            if (_instance == null)
                _instance = Resources.Load<CardVisualDatabase>("CardVisualDatabase");
            return _instance;
        }
    }

    private void OnEnable()
    {
        _instance = this;
    }

    public Sprite GetSprite(CardType type)
    {
        foreach (var v in visuals)
        {
            if (v.type == type)
                return v.sprite;
        }
        return null;
    }
}

[System.Serializable]
public class CardVisual
{
    public CardType type;
    public Sprite sprite;
}
