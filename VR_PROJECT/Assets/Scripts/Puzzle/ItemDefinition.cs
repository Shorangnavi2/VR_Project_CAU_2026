using System.Collections.Generic;
using UnityEngine;

public enum PlacementStyle { Surface, Shelf, Hook }

[CreateAssetMenu(menuName = "EscapeRoom/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    public string displayName;
    [Tooltip("크기 숫자. 자리의 Max Size 이하일 때만 놓을 수 있음")]
    [Min(0)] public int size = 0;
    [Tooltip("이 물건의 태그들 (예: Pipette, Red, Glass)")]
    public List<ItemTag> tags = new();

    // 주어진 태그 중 하나라도 가지고 있는지
    public bool HasAnyTag(List<ItemTag> candidates)
    {
        if (candidates == null) return false;
        foreach (var t in tags)
        {
            if (t == null) continue;
            for (int i = 0; i < candidates.Count; i++)
                if (candidates[i] == t) return true;
        }
        return false;
    }
}
