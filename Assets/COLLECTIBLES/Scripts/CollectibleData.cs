using UnityEngine;

[CreateAssetMenu(fileName = "CollectibleData", menuName = "Collectible Items")]
public class CollectibleData : ScriptableObject
{
    public ItemType itemType;
    public string itemName;
    public Sprite icon;
    public float value;
    public int dropChance;
}

public enum ItemType
{
    Food,
    Armor,
    Helmet,
    Weapon,
    HealthPotion,
    StaminaPotion,
    Health,
    Stamina,
}
