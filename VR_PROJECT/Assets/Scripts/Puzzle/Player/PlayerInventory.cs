using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [SerializeField] int capacity = 8;
    readonly List<PuzzleItem> items = new();

    public IReadOnlyList<PuzzleItem> Items => items;
    public int ActiveIndex { get; private set; } = -1;
    public PuzzleItem Active =>
        ActiveIndex >= 0 && ActiveIndex < items.Count ? items[ActiveIndex] : null;
    public bool IsFull => items.Count >= capacity;
    public event Action Changed;

    public bool Add(PuzzleItem item)
    {
        if (item == null || IsFull || items.Contains(item)) return false;
        item.StoreInInventory();
        items.Add(item);
        ActiveIndex = items.Count - 1;          // 방금 주운 것을 활성으로
        Changed?.Invoke();
        return true;
    }

    public PuzzleItem TakeActive()
    {
        var item = Active;
        if (item == null) return null;
        items.RemoveAt(ActiveIndex);
        if (ActiveIndex >= items.Count) ActiveIndex = items.Count - 1;
        Changed?.Invoke();
        return item;
    }

    public void Cycle(int dir = 1)
    {
        if (items.Count == 0) return;
        ActiveIndex = (ActiveIndex + dir + items.Count) % items.Count;
        Changed?.Invoke();
    }
}
