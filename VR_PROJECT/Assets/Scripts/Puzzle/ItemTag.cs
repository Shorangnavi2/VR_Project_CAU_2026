using UnityEngine;

// 실제 태그 (예: Clock, Pipette, Red). 어느 기준(그룹)에 속하는지 지정
[CreateAssetMenu(menuName = "EscapeRoom/Item Tag")]
public class ItemTag : ScriptableObject
{
    public ItemTagGroup group;
}
