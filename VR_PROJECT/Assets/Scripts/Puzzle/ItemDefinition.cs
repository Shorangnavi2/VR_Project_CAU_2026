using UnityEngine;

public enum ItemSize { Small, Medium, Large }

[System.Flags]
public enum ItemCategory { None = 0, Key = 1, Gem = 2, Book = 4, Card = 8, Bottle = 16 }

public enum PlacementStyle { Surface, Shelf, Hook }

[CreateAssetMenu(menuName = "EscapeRoom/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    public string displayName;
    public ItemSize size = ItemSize.Small;
    public ItemCategory category = ItemCategory.None;
}
