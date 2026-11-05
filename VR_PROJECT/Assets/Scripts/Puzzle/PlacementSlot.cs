using System;
using UnityEngine;

public class PlacementSlot : MonoBehaviour
{
    [Header("배치 위치와 방식")]
    public Transform anchor;                       // Y=위, Z=앞 방향으로 맞춰 둘 것
    public PlacementStyle style = PlacementStyle.Surface;

    [Header("제약: 놓아볼 수 있는 조건")]
    public ItemSize maxSize = ItemSize.Medium;
    public ItemCategory acceptedCategories = (ItemCategory)~0; // 기본: 전부 허용

    [Header("정답 (비워두면 '비어 있어야 정답')")]
    public ItemDefinition correctItem;

    public PuzzleItem Current { get; private set; }
    public event Action Changed;

    // 규칙만 검사: 크기, 종류, 이 방식으로 놓을 자세가 있는지
    public bool Fits(PuzzleItem item)
    {
        var d = item.definition;
        if (d == null) return false;
        if (d.size > maxSize) return false;
        if ((acceptedCategories & d.category) == 0) return false;
        if (!item.TryGetPose(style, out _)) return false;
        return true;
    }

    public bool IsCorrect => correctItem == null
        ? Current == null
        : Current != null && Current.definition == correctItem;

    public void Place(PuzzleItem item)
    {
        item.TryGetPose(style, out var pose);
        Transform a = anchor != null ? anchor : transform;
        item.GetPlacement(pose, a, out var pos, out var rot);
        item.AttachTo(a, pos, rot);

        Current = item;
        item.CurrentSlot = this;
        Changed?.Invoke();
    }

    public PuzzleItem Release()
    {
        var item = Current;
        if (item == null) return null;
        Current = null;
        item.CurrentSlot = null;
        Changed?.Invoke();
        return item;
    }

#if UNITY_EDITOR
    // Anchor 축은 항상 표시 (노랑=놓이는 점, 초록=위, 파랑=앞)
    // 자리나 그 자식(Anchor, Highlight 등)이 선택되어 있으면 Box Collider 범위(주황)도 표시
    // Scene 뷰 상단 Gizmos 메뉴에서 PlacementSlot 체크를 끄면 숨길 수 있음
    void OnDrawGizmos()
    {
        bool selected = IsSelfOrChildSelected();
        float len = selected ? 0.1f : 0.05f;

        Transform a = anchor != null ? anchor : transform;
        Gizmos.color = Color.green;  Gizmos.DrawLine(a.position, a.position + a.up * len);
        Gizmos.color = Color.blue;   Gizmos.DrawLine(a.position, a.position + a.forward * len);
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(a.position, len * 0.1f);

        if (!selected) return;
        Gizmos.color = new Color(1f, 0.55f, 0f);
        foreach (var box in GetComponents<BoxCollider>())
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(box.center, box.size);
        }
        Gizmos.matrix = Matrix4x4.identity;
    }

    bool IsSelfOrChildSelected()
    {
        foreach (var t in UnityEditor.Selection.transforms)
            if (t == transform || t.IsChildOf(transform)) return true;
        return false;
    }
#endif
}
