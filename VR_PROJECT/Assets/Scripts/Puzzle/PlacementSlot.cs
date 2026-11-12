using System;
using System.Collections.Generic;
using UnityEngine;

// 받는 조건 하나: "이 그룹에서, 이 태그들 중 하나라도 가진 물건만"
// 자리의 모든 필터를 동시에 통과해야 놓을 수 있음. 태그 목록이 비어 있는 필터는 무시
[Serializable]
public class TagFilter
{
    [Tooltip("이 필터가 다루는 기준 (예: Type, Color). 아래 태그가 다른 그룹이면 경고")]
    public ItemTagGroup group;
    [Tooltip("이 중 하나라도 가진 물건만 통과")]
    public List<ItemTag> anyOf = new();

    public bool IsActive => anyOf != null && anyOf.Count > 0;

    public string Describe()
    {
        var names = new List<string>();
        foreach (var t in anyOf) if (t != null) names.Add(t.name);
        string g = group != null ? group.name : "그룹 없음";
        return $"{g}: {string.Join(", ", names)}";
    }
}

public enum SlotGoal
{
    MustContain,   // Correct Item이 놓여야 정답
    MustBeEmpty,   // 비어 있어야 정답 (함정 자리)
    Ignore,        // 판정에서 제외 (장식·자유 배치용)
}

public class PlacementSlot : MonoBehaviour
{
    [Header("배치 위치와 방식")]
    public Transform anchor;                       // Y=위, Z=앞 방향으로 맞춰 둘 것
    public PlacementStyle style = PlacementStyle.Surface;

    [Header("제약: 놓아볼 수 있는 조건")]
    [Tooltip("물건 Size가 이 값 이하일 때만 놓을 수 있음 (예전 Small=0, Medium=1, Large=2 값이 그대로 이어짐)")]
    [Min(0)] public int maxSize = 1;
    [Tooltip("받는 조건. 모든 필터를 통과해야 함. 비워 두면 종류 상관없이 전부 허용")]
    public List<TagFilter> filters = new();

    [Header("정답")]
    public SlotGoal goal = SlotGoal.MustContain;
    [Tooltip("Goal이 Must Contain일 때 여기에 놓여야 하는 물건")]
    public ItemDefinition correctItem;

    public PuzzleItem Current { get; private set; }
    public event Action Changed;

    // 규칙만 검사: 크기, 종류, 이 방식으로 놓을 자세가 있는지
    public bool Fits(PuzzleItem item)
    {
        var d = item.definition;
        if (d == null) return false;
        if (d.size > maxSize) return false;
        if (!PassesFilters(d)) return false;
        if (!item.TryGetPose(style, out _)) return false;
        return true;
    }

    public bool PassesFilters(ItemDefinition d)
    {
        foreach (var f in filters)
            if (f != null && f.IsActive && !d.HasAnyTag(f.anyOf)) return false;
        return true;
    }

    public bool IsCorrect => goal switch
    {
        SlotGoal.MustContain => correctItem != null && Current != null && Current.definition == correctItem,
        SlotGoal.MustBeEmpty => Current == null,
        _                    => true,   // Ignore
    };

    // 설정 때문에 풀 수 없는지 검사. sceneItems를 주면 정답 물건의 마커까지 확인
    public List<string> FindSetupProblems(IEnumerable<PuzzleItem> sceneItems = null)
    {
        var problems = new List<string>();
        if (goal != SlotGoal.MustContain) return problems;

        if (correctItem == null)
        {
            problems.Add("Goal이 Must Contain인데 Correct Item이 비어 있음");
            return problems;
        }

        string n = correctItem.name;
        if (correctItem.size > maxSize)
            problems.Add($"정답 '{n}'의 Size({correctItem.size})가 Max Size({maxSize})보다 큼");
        foreach (var f in filters)
            if (f != null && f.IsActive && !correctItem.HasAnyTag(f.anyOf))
                problems.Add($"정답 '{n}'이(가) 받는 조건 [{f.Describe()}]을 통과하지 못함");

        if (sceneItems != null)
        {
            bool found = false, hasPose = false;
            foreach (var it in sceneItems)
            {
                if (it == null || it.definition != correctItem) continue;
                found = true;
                if (it.TryGetPose(style, out _)) hasPose = true;
            }
            if (!found) problems.Add($"씬에 정답 '{n}'을 쓰는 물건이 없음");
            else if (!hasPose) problems.Add($"정답 '{n}' 물건에 {style}용 마커도 Default Marker도 없음");
        }
        return problems;
    }

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
    // Inspector에서 값을 바꾸거나 씬을 열 때 바로 경고 (프리팹 원본 편집 중에는 생략)
    void OnValidate()
    {
        if (maxSize < 0) maxSize = 0;
        if (!gameObject.scene.IsValid()) return;
        foreach (var f in filters)
        {
            if (f == null || f.group == null) continue;
            foreach (var t in f.anyOf)
                if (t != null && t.group != f.group)
                    Debug.LogWarning($"[{name}] 필터 그룹 '{f.group.name}'에 다른 그룹의 태그 '{t.name}'이(가) 들어 있음", this);
        }
        foreach (var p in FindSetupProblems())
            Debug.LogWarning($"[{name}] {p}", this);
    }

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
