using UnityEngine;

// 인벤토리의 활성 아이템을 오른손(holdPoint)에 실제 물체로 표시
public class HeldItemDisplay : MonoBehaviour
{
    [SerializeField] PlayerInventory inventory;
    [Tooltip("Right Controller 아래에 만든 빈 오브젝트")]
    [SerializeField] Transform holdPoint;
    [Tooltip("손에 보일 때 가장 긴 변의 최대 길이 (m). 이보다 크면 균일하게 축소")]
    [SerializeField] float maxDisplaySize = 0.15f;
    [Tooltip("holdPoint 기준 추가 위치 조정")]
    [SerializeField] Vector3 localOffset = Vector3.zero;

    PuzzleItem shown;

    void OnEnable()
    {
        if (inventory == null) { Debug.LogWarning("HeldItemDisplay: Inventory가 연결되지 않음"); return; }
        inventory.Changed += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        if (inventory == null) return;
        inventory.Changed -= Refresh;
        Hide(shown);
        shown = null;
    }

    void Refresh()
    {
        var next = inventory.Active;
        if (shown != null && shown != next) Hide(shown);
        shown = next;
        if (shown != null && holdPoint != null)
            shown.HoldAt(holdPoint, maxDisplaySize, localOffset);
    }

    void Hide(PuzzleItem item)
    {
        if (item == null) return;
        // 인벤토리에서 빠진 물건(지금 자리에 놓이는 중)은 숨기지 않음 → 배치 과정을 방해하지 않기 위해
        if (!InInventory(item)) return;
        item.HideInInventory();
    }

    bool InInventory(PuzzleItem item)
    {
        foreach (var i in inventory.Items)
            if (i == item) return true;
        return false;
    }
}
