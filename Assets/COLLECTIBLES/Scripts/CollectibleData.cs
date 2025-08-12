using UnityEngine;

[CreateAssetMenu(fileName = "CollectibleData", menuName = "Collectible Items")]
public class CollectibleData : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public float value;
    public ItemType itemType;
}

public enum ItemType
{
    Food,
    Armor,
    Helmet,
    Weapon,
    HealthPotion,
    StaminaPotion,
    Powerup
}
