using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// PuzzleItem Inspector 아래에 "마커 만들기" 도구를 추가 (에디터 전용, 빌드에 포함되지 않음)
[CustomEditor(typeof(PuzzleItem))]
public class PuzzleItemEditor : Editor
{
    enum Usage { Rest, Hang }
    static readonly string[] UsageLabels = { "놓기 (이 면이 바닥에 닿음)", "걸기 (이 점에 매달림)" };
    static readonly string[] AngleLabels = { "0°", "90°", "180°", "270°" };

    struct BoxPoint { public Vector3Int d; public string label; }
    static List<BoxPoint> faces;     // 면 중심 6개
    static List<BoxPoint> all;       // 면 6 + 모서리 12 + 꼭짓점 8 (앞 6개는 faces와 같은 순서)

    // 도구 설정 (에디터를 켜 둔 동안 유지)
    static bool foldout = true;
    static bool showAdvanced;
    static int pointIndex;
    static Usage usage = Usage.Rest;
    static int angleIndex;
    static int targetIndex;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        foldout = EditorGUILayout.Foldout(foldout, "마커 만들기", true, EditorStyles.foldoutHeader);
        if (!foldout) return;

        var item = (PuzzleItem)target;
        BuildPoints();

        EditorGUI.BeginChangeCheck();
        showAdvanced = EditorGUILayout.Toggle("모서리·꼭짓점 포함", showAdvanced);
        var list = showAdvanced ? all : faces;
        pointIndex = Mathf.Clamp(pointIndex, 0, list.Count - 1);
        pointIndex = EditorGUILayout.Popup("위치", pointIndex, Labels(list));
        usage = (Usage)EditorGUILayout.Popup("용도", (int)usage, UsageLabels);
        angleIndex = EditorGUILayout.Popup("앞(파랑) 방향 회전", angleIndex, AngleLabels);

        string[] styleNames = System.Enum.GetNames(typeof(PlacementStyle));
        var targets = new string[styleNames.Length + 1];
        for (int i = 0; i < styleNames.Length; i++) targets[i] = $"Poses: {styleNames[i]}";
        targets[styleNames.Length] = "Default Marker";
        targetIndex = Mathf.Clamp(targetIndex, 0, targets.Length - 1);
        targetIndex = EditorGUILayout.Popup("등록 대상", targetIndex, targets);
        if (EditorGUI.EndChangeCheck()) SceneView.RepaintAll();

        Bounds b = item.GetLocalBounds();
        if (b.size == Vector3.zero)
        {
            EditorGUILayout.HelpBox("모델(렌더러)을 찾을 수 없어서 범위를 계산할 수 없습니다. 모델이 켜져 있는지 확인하세요.", MessageType.Warning);
            return;
        }

        EditorGUILayout.HelpBox("Scene 뷰의 분홍 점이 마커가 생길 위치입니다. (초록=위, 파랑=앞)\n" +
                                "같은 대상에 이미 이 물건 바로 아래의 마커가 있으면 그 마커를 옮깁니다.", MessageType.None);

        PlacementStyle? style = targetIndex < styleNames.Length ? (PlacementStyle)targetIndex : (PlacementStyle?)null;
        if (GUILayout.Button("만들기"))
            CreateOrMoveMarker(item, list[pointIndex].d, b, style);
    }

    void OnSceneGUI()
    {
        if (!foldout) return;
        var item = (PuzzleItem)target;
        BuildPoints();
        var list = showAdvanced ? all : faces;
        if (pointIndex < 0 || pointIndex >= list.Count) return;

        Bounds b = item.GetLocalBounds();
        if (b.size == Vector3.zero) return;

        ComputePose(b, list[pointIndex].d, usage, angleIndex, out var lp, out var lr);
        Vector3 wp = item.transform.TransformPoint(lp);
        Quaternion wr = item.transform.rotation * lr;
        float size = HandleUtility.GetHandleSize(wp);

        Handles.color = Color.magenta;
        Handles.SphereHandleCap(0, wp, Quaternion.identity, size * 0.08f, EventType.Repaint);
        Handles.color = Color.green; Handles.DrawLine(wp, wp + wr * Vector3.up * size * 0.5f);
        Handles.color = Color.blue;  Handles.DrawLine(wp, wp + wr * Vector3.forward * size * 0.5f);
    }

    // 상자 위 점 위치와 마커 회전 (물건 루트 기준 로컬)
    static void ComputePose(Bounds b, Vector3Int d, Usage usage, int angleIndex, out Vector3 pos, out Quaternion rot)
    {
        pos = b.center + Vector3.Scale(b.extents, (Vector3)d);

        Vector3 outward = ((Vector3)d).normalized;
        Vector3 up = usage == Usage.Rest ? -outward : outward;   // 놓기: 물체 쪽이 위 / 걸기: 바깥쪽이 위

        // 기본 앞 방향: 물건의 앞(+Z). 위와 겹치면 물건의 아래(-Y) 방향 (눕힌 책의 아래 모서리가 앞)
        Vector3 fwd = Vector3.ProjectOnPlane(Vector3.forward, up);
        if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.ProjectOnPlane(Vector3.down, up);
        fwd.Normalize();
        fwd = Quaternion.AngleAxis(90f * angleIndex, up) * fwd;

        rot = Quaternion.LookRotation(fwd, up);
    }

    static void CreateOrMoveMarker(PuzzleItem item, Vector3Int d, Bounds b, PlacementStyle? style)
    {
        ComputePose(b, d, usage, angleIndex, out var pos, out var rot);

        Transform marker = FindReusableMarker(item, style);
        if (marker == null)
        {
            string baseName = style.HasValue ? $"Marker_{style.Value}" : "Marker_Default";
            var go = new GameObject(baseName);
            Undo.RegisterCreatedObjectUndo(go, "Create Marker");
            Undo.SetTransformParent(go.transform, item.transform, "Create Marker");
            go.name = GameObjectUtility.GetUniqueNameForSibling(item.transform, baseName);
            marker = go.transform;
        }
        else
        {
            Undo.RecordObject(marker, "Move Marker");
        }

        marker.localPosition = pos;
        marker.localRotation = rot;
        marker.localScale = Vector3.one;

        Undo.RecordObject(item, "Assign Marker");
        if (style.HasValue)
        {
            var pose = item.poses.Find(p => p != null && p.style == style.Value);
            if (pose == null)
            {
                pose = new PlacementPose { style = style.Value };
                item.poses.Add(pose);
            }
            pose.marker = marker;
        }
        else
        {
            item.defaultMarker = marker;
        }

        PrefabUtility.RecordPrefabInstancePropertyModifications(marker);
        PrefabUtility.RecordPrefabInstancePropertyModifications(item);
        EditorUtility.SetDirty(item);
    }

    // 같은 대상의 기존 마커가 "물건 바로 아래 자식"이고 다른 곳과 공유되지 않으면 그걸 옮겨서 재사용
    static Transform FindReusableMarker(PuzzleItem item, PlacementStyle? style)
    {
        Transform current = null;
        if (style.HasValue)
        {
            var pose = item.poses.Find(p => p != null && p.style == style.Value);
            current = pose?.marker;
        }
        else current = item.defaultMarker;

        if (current == null || current.parent != item.transform) return null;

        int uses = item.defaultMarker == current ? 1 : 0;
        foreach (var p in item.poses) if (p != null && p.marker == current) uses++;
        return uses == 1 ? current : null;
    }

    static string[] Labels(List<BoxPoint> list)
    {
        var arr = new string[list.Count];
        for (int i = 0; i < list.Count; i++) arr[i] = list[i].label;
        return arr;
    }

    static void BuildPoints()
    {
        if (all != null) return;
        faces = new List<BoxPoint>();
        all = new List<BoxPoint>();

        // 면 중심 (자주 쓰는 순서)
        Vector3Int[] faceDirs =
        {
            new(0, -1, 0), new(0, 1, 0), new(0, 0, 1), new(0, 0, -1), new(-1, 0, 0), new(1, 0, 0),
        };
        foreach (var d in faceDirs)
        {
            var p = new BoxPoint { d = d, label = $"{Words(d)} 면 중심" };
            faces.Add(p); all.Add(p);
        }

        // 모서리 중점, 꼭짓점
        var edges = new List<BoxPoint>();
        var corners = new List<BoxPoint>();
        for (int y = -1; y <= 1; y++)
        for (int z = 1; z >= -1; z--)
        for (int x = -1; x <= 1; x++)
        {
            var d = new Vector3Int(x, y, z);
            int nz = (x != 0 ? 1 : 0) + (y != 0 ? 1 : 0) + (z != 0 ? 1 : 0);
            if (nz == 2) edges.Add(new BoxPoint { d = d, label = $"{Words(d)} 모서리 중점" });
            else if (nz == 3) corners.Add(new BoxPoint { d = d, label = $"{Words(d)} 꼭짓점" });
        }
        all.AddRange(edges);
        all.AddRange(corners);
    }

    // 방향 이름: 위/아래, 앞/뒤, 왼쪽/오른쪽 순
    static string Words(Vector3Int d)
    {
        var parts = new List<string>();
        if (d.y != 0) parts.Add(d.y > 0 ? "위" : "아래");
        if (d.z != 0) parts.Add(d.z > 0 ? "앞" : "뒤");
        if (d.x != 0) parts.Add(d.x > 0 ? "오른쪽" : "왼쪽");
        return string.Join("-", parts);
    }
}
