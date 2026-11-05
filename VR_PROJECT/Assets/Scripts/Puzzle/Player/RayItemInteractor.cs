using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class RayItemInteractor : MonoBehaviour
{
    [SerializeField] XRRayInteractor ray;          // 오른손 Ray Interactor
    [SerializeField] PlayerInventory inventory;
    [SerializeField] bool swapWhenOccupied = true; // 찬 자리에 놓으면 맞바꾸기

    [Header("입력 (기본: 오른손 트리거 / B / A)")]
    [SerializeField] InputAction pickAction  = new("Pick",  InputActionType.Button, "<XRController>{RightHand}/{TriggerButton}");
    [SerializeField] InputAction cycleAction = new("Cycle", InputActionType.Button, "<XRController>{RightHand}/{SecondaryButton}");
    [SerializeField] InputAction placeAction = new("Place", InputActionType.Button, "<XRController>{RightHand}/{PrimaryButton}");

    [Header("하이라이트")]
    [SerializeField] bool highlight = true;
    Highlightable hoveredItem, hoveredSlot;

    void OnEnable()
    {
        pickAction.performed  += OnPick;
        cycleAction.performed += OnCycle;
        placeAction.performed += OnPlace;
        pickAction.Enable(); cycleAction.Enable(); placeAction.Enable();
    }

    void OnDisable()
    {
        pickAction.performed  -= OnPick;
        cycleAction.performed -= OnCycle;
        placeAction.performed -= OnPlace;
        pickAction.Disable(); cycleAction.Disable(); placeAction.Disable();

        Apply(ref hoveredItem, null, HighlightState.None);
        Apply(ref hoveredSlot, null, HighlightState.None);
    }

    void Update()
    {
        if (!highlight) return;
        Highlightable itemH = null, slotH = null;
        var slotState = HighlightState.None;

        if (TryGetHit(out var hit))
        {
            var slot = hit.collider.GetComponentInParent<PlacementSlot>();
            var item = hit.collider.GetComponentInParent<PuzzleItem>();
            if (item == null && slot != null) item = slot.Current;
            if (item != null) itemH = item.GetComponent<Highlightable>();

            var active = inventory.Active;
            if (slot != null && active != null)
            {
                slotH = slot.GetComponent<Highlightable>();
                bool ok = slot.Fits(active) && (swapWhenOccupied || slot.Current == null);
                slotState = ok ? HighlightState.Valid : HighlightState.Invalid;
            }
        }
        Apply(ref hoveredItem, itemH, HighlightState.Hover);
        Apply(ref hoveredSlot, slotH, slotState);
    }

    static void Apply(ref Highlightable current, Highlightable next, HighlightState state)
    {
        if (current != null && current != next) current.SetState(HighlightState.None);
        current = next;
        if (current != null) current.SetState(state);
    }

    bool TryGetHit(out RaycastHit hit)
    {
        hit = default;
        return ray != null && ray.isActiveAndEnabled && ray.TryGetCurrent3DRaycastHit(out hit);
    }

    void OnPick(InputAction.CallbackContext _)
    {
        if (!TryGetHit(out var hit)) return;

        var item = hit.collider.GetComponentInParent<PuzzleItem>();
        if (item == null)
        {
            // 자리의 영역(트리거)에 먼저 맞은 경우: 그 자리에 놓인 물건을 집음
            var slot = hit.collider.GetComponentInParent<PlacementSlot>();
            if (slot != null) item = slot.Current;
        }
        if (item == null) return;

        if (!inventory.Add(item)) Debug.Log("인벤토리가 가득 참");
    }

    void OnCycle(InputAction.CallbackContext _) => inventory.Cycle();

    void OnPlace(InputAction.CallbackContext _)
    {
        var active = inventory.Active;
        if (active == null) { Debug.Log("들고 있는 물건이 없음"); return; }
        if (!TryGetHit(out var hit)) return;

        var slot = hit.collider.GetComponentInParent<PlacementSlot>();
        if (slot == null) { Debug.Log("놓을 수 있는 곳이 아님"); return; }
        if (!slot.Fits(active)) { Debug.Log($"{active.definition.displayName}: 여기엔 안 맞음"); return; }

        var previous = slot.Current;
        if (previous != null && !swapWhenOccupied) { Debug.Log("이미 차 있음"); return; }

        inventory.TakeActive();
        if (previous != null) slot.Release();
        slot.Place(active);
        if (previous != null) inventory.Add(previous);   // 원래 있던 물건은 인벤토리로
    }
}
