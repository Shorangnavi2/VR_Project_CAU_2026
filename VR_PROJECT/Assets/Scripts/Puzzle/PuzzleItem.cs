using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlacementPose
{
    public PlacementStyle style;
    [Tooltip("이 자세로 놓일 때 바닥에 닿는 점 (자식 빈 오브젝트). Y=위, Z=앞. 지정하면 아래 숫자는 무시")]
    public Transform marker;
    public Vector3 localPosition;   // 마커가 없을 때만 사용: Anchor 기준 오프셋 (미터)
    public Vector3 localEuler;      // 마커가 없을 때만 사용: Anchor 기준 회전 (도)
}

public class PuzzleItem : MonoBehaviour
{
    public ItemDefinition definition;
    public List<PlacementPose> poses = new();
    [Tooltip("방식별 마커가 없을 때 쓰는 공통 마커. 지정하면 모든 방식의 자리에 놓을 수 있게 됨 (제한은 종류·크기로)")]
    public Transform defaultMarker;
    PlacementPose defaultPose;   // 매번 새로 만들지 않도록 재사용
    public PlacementSlot CurrentSlot { get; set; }
    public bool IsHeld { get; private set; }     // 지금 손에 표시 중인지

    // 원래 상태 기억 (손에 들면 축소·콜라이더 끔 → 놓을 때 정확히 복원하기 위함)
    bool baseCaptured;
    Vector3 baseWorldScale = Vector3.one;
    Collider[] colliders;
    bool[] colliderWasEnabled;

    void Awake() => CaptureBase();

    void CaptureBase()
    {
        if (baseCaptured) return;
        baseCaptured = true;
        baseWorldScale = transform.lossyScale;
        colliders = GetComponentsInChildren<Collider>(true);
        colliderWasEnabled = new bool[colliders.Length];
        for (int i = 0; i < colliders.Length; i++) colliderWasEnabled[i] = colliders[i].enabled;
    }

    void SetCollidersEnabled(bool on)
    {
        CaptureBase();
        for (int i = 0; i < colliders.Length; i++)
            if (colliders[i] != null) colliders[i].enabled = on && colliderWasEnabled[i];
    }

    void MakeKinematic()
    {
        var rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
    }

    // 원래 크기로 되돌리고 부모에서 분리
    void RestoreBaseTransform()
    {
        CaptureBase();
        transform.SetParent(null, true);
        transform.localScale = baseWorldScale;
    }

    public bool TryGetPose(PlacementStyle style, out PlacementPose pose)
    {
        pose = poses.Find(p => p != null && p.style == style);
        if (pose != null) return true;

        if (defaultMarker != null)
        {
            defaultPose ??= new PlacementPose();
            defaultPose.style = style;
            defaultPose.marker = defaultMarker;
            pose = defaultPose;
            return true;
        }
        return false;
    }

    // 이 자세로 anchor에 놓일 때 물건(루트)의 월드 위치·회전 계산
    public void GetPlacement(PlacementPose pose, Transform anchor, out Vector3 pos, out Quaternion rot)
    {
        if (pose.marker != null)
        {
            // 마커가 Anchor와 겹치도록 물건 전체의 위치·회전을 계산
            // 현재 크기(손에서 축소됐을 수 있음)와 무관하게 원래 크기 기준으로 계산
            CaptureBase();
            Quaternion markerRel = Quaternion.Inverse(transform.rotation) * pose.marker.rotation;
            Vector3 markerLocal = transform.InverseTransformPoint(pose.marker.position);
            Vector3 markerOffset = Vector3.Scale(markerLocal, baseWorldScale);
            rot = anchor.rotation * Quaternion.Inverse(markerRel);
            pos = anchor.position - rot * markerOffset;
        }
        else
        {
            pos = anchor.position + anchor.rotation * pose.localPosition;
            rot = anchor.rotation * Quaternion.Euler(pose.localEuler);
        }
    }

    // 인벤토리에 들어갈 때: 자리에서 빠지고 숨김
    public void StoreInInventory()
    {
        if (CurrentSlot != null) CurrentSlot.Release();
        HideInInventory();
    }

    // 인벤토리 안에서 숨김 (원래 크기·콜라이더 상태로 되돌려 둠)
    public void HideInInventory()
    {
        IsHeld = false;
        RestoreBaseTransform();
        SetCollidersEnabled(true);
        gameObject.SetActive(false);
    }

    // 손(holdPoint)에 표시: 콜라이더 끄고, 가장 긴 변이 maxSize를 넘으면 균일 축소, 모델 중심을 holdPoint에 맞춤
    public void HoldAt(Transform holdPoint, float maxSize, Vector3 localOffset)
    {
        CaptureBase();
        gameObject.SetActive(true);
        MakeKinematic();
        SetCollidersEnabled(false);              // 레이가 손에 든 물건에 막히지 않게

        float parentScale = Mathf.Abs(holdPoint.lossyScale.x);
        if (parentScale < 1e-6f) parentScale = 1f;

        transform.SetParent(holdPoint, false);
        transform.localRotation = Quaternion.identity;
        transform.localScale = baseWorldScale / parentScale;   // 먼저 원래 크기로

        Bounds b = GetLocalBounds();              // 물건 루트 기준 범위
        Vector3 worldSize = Vector3.Scale(b.size, baseWorldScale);
        float largest = Mathf.Max(worldSize.x, Mathf.Max(worldSize.y, worldSize.z));
        float factor = (maxSize > 0f && largest > maxSize) ? maxSize / largest : 1f;

        transform.localScale = baseWorldScale * factor / parentScale;
        transform.localPosition = localOffset - Vector3.Scale(transform.localScale, b.center);
        IsHeld = true;
    }

    // 모든 렌더러 범위를 물건 루트의 로컬 좌표로 합친 것 (손에 들기 표시, 마커 생성 도구에서 사용)
    public Bounds GetLocalBounds()
    {
        var renderers = GetComponentsInChildren<Renderer>();
        bool has = false;
        var result = new Bounds(Vector3.zero, Vector3.zero);
        Matrix4x4 toRoot = transform.worldToLocalMatrix;
        foreach (var r in renderers)
        {
            Bounds lb = r.localBounds;
            Matrix4x4 m = toRoot * r.transform.localToWorldMatrix;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = lb.center + Vector3.Scale(lb.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 p = m.MultiplyPoint3x4(corner);
                if (!has) { result = new Bounds(p, Vector3.zero); has = true; }
                else result.Encapsulate(p);
            }
        }
        return result;
    }

    // 자리에 놓일 때: 원래 크기·콜라이더로 복원하고 고정
    public void AttachTo(Transform parent, Vector3 worldPos, Quaternion worldRot)
    {
        IsHeld = false;
        gameObject.SetActive(true);
        MakeKinematic();
        SetCollidersEnabled(true);
        RestoreBaseTransform();                   // 손에서 축소된 크기 되돌리기
        transform.SetParent(parent, true);
        transform.SetPositionAndRotation(worldPos, worldRot);
    }

#if UNITY_EDITOR
    // 마커 축은 항상 표시 (초록=위, 파랑=앞, 노랑=닿는 점)
    // 물건이나 그 자식(마커 등)이 선택되어 있으면 모델 범위 상자(하늘색)도 표시
    // Scene 뷰 상단 Gizmos 메뉴에서 PuzzleItem 체크를 끄면 숨길 수 있음
    void OnDrawGizmos()
    {
        bool selected = IsSelfOrChildSelected();
        float len = selected ? 0.1f : 0.05f;

        var markers = new List<Transform>();
        foreach (var p in poses) if (p != null && p.marker != null) markers.Add(p.marker);
        if (defaultMarker != null) markers.Add(defaultMarker);

        foreach (var t in markers)
        {
            Gizmos.color = Color.green;  Gizmos.DrawLine(t.position, t.position + t.up * len);
            Gizmos.color = Color.blue;   Gizmos.DrawLine(t.position, t.position + t.forward * len);
            Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(t.position, len * 0.1f);
        }

        if (!selected) return;
        Gizmos.color = Color.cyan;
        foreach (var r in GetComponentsInChildren<Renderer>())
        {
            Gizmos.matrix = r.transform.localToWorldMatrix;
            Gizmos.DrawWireCube(r.localBounds.center, r.localBounds.size);
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
