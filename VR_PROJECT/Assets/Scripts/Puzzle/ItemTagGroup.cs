using UnityEngine;

// 분류 기준 (예: Type, Color, Material)
[CreateAssetMenu(menuName = "EscapeRoom/Item Tag Group")]
public class ItemTagGroup : ScriptableObject
{
    [TextArea] public string description;
}
