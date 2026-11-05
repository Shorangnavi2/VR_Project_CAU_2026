using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PuzzleManager : MonoBehaviour
{
    public List<PlacementSlot> slots = new();
    public UnityEvent onCleared;
    bool cleared;

    void OnEnable()  { foreach (var s in slots) s.Changed += Check; }
    void OnDisable() { foreach (var s in slots) s.Changed -= Check; }

    void Check()
    {
        if (cleared) return;
        foreach (var s in slots)
            if (!s.IsCorrect) return;

        cleared = true;
        Debug.Log("클리어!");
        onCleared.Invoke();
    }
}
