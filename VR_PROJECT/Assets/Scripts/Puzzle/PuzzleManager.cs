using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PuzzleManager : MonoBehaviour
{
    [Tooltip("켜면 씬의 모든 PlacementSlot을 자동으로 찾아 판정 (아래 Slots 목록은 무시). 판정에서 뺄 자리는 Goal을 Ignore로")]
    public bool autoCollect = true;
    [Tooltip("Auto Collect가 꺼져 있을 때만 사용")]
    public List<PlacementSlot> slots = new();
    public UnityEvent onCleared;

    readonly List<PlacementSlot> watched = new();
    bool cleared;

    void OnEnable()
    {
        watched.Clear();
        if (autoCollect) watched.AddRange(FindObjectsOfType<PlacementSlot>(true));
        else foreach (var s in slots) if (s != null) watched.Add(s);

        foreach (var s in watched) s.Changed += Check;
    }

    void OnDisable()
    {
        foreach (var s in watched) if (s != null) s.Changed -= Check;
        watched.Clear();
    }

    // Play 시작 시 전체 설정 점검: 풀 수 없는 자리가 있으면 경고
    void Start()
    {
        var items = FindObjectsOfType<PuzzleItem>(true);
        int judged = 0;
        foreach (var s in watched)
        {
            if (s.goal == SlotGoal.Ignore) continue;
            judged++;
            foreach (var p in s.FindSetupProblems(items))
                Debug.LogWarning($"[퍼즐 점검] {s.name}: {p}", s);
        }
        if (judged == 0) Debug.LogWarning("[퍼즐 점검] 판정 대상 자리가 하나도 없음", this);
    }

    void Check()
    {
        if (cleared) return;
        int judged = 0;
        foreach (var s in watched)
        {
            if (s.goal == SlotGoal.Ignore) continue;
            judged++;
            if (!s.IsCorrect) return;
        }
        if (judged == 0) return;

        cleared = true;
        Debug.Log("클리어!");
        onCleared.Invoke();
    }
}
