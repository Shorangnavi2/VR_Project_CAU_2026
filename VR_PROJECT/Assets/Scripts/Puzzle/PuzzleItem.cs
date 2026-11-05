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
    public PlacementSlot CurrentSlot { get; set; }

    public bool TryGetPose(PlacementStyle style, out PlacementPose pose)
    {
        pose = poses.Find(p => p.style == style);
        return pose != null;
    }

    // 이 자세로 anchor에 놓일 때 물건(루트)의 월드 위치·회전 계산
    public void GetPlacement(PlacementPose pose, Transform anchor, out Vector3 pos, out Quaternion rot)
    {
        if (pose.marker != null)
        {
            // 마커가 Anchor와 겹치도록 물건 전체의 위치·회전을 계산
            Quaternion inv = Quaternion.Inverse(transform.rotation);
            Quaternion markerRel = inv * pose.marker.rotation;
            Vector3 markerOffset = inv * (pose.marker.position - transform.position);
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
        transform.SetParent(null, true);
        gameObject.SetActive(false);
    }

    // 자리에 놓일 때: 보이게 하고, 물리로 떨어지지 않게 고정
    public void AttachTo(Transform parent, Vector3 worldPos, Quaternion worldRot)
    {
        gameObject.SetActive(true);
        var rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
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

        foreach (var p in poses)
        {
            if (p == null || p.marker == null) continue;
            var t = p.marker;
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
