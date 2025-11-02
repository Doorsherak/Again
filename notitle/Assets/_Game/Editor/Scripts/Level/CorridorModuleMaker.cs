#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
// CorridorModule가 정의된 네임스페이스가 있다면 using 문을 추가하세요.
// 예시: using MyGame.Level; 

public static class CorridorModuleMaker
{
    enum ForwardAxis { Z, X }

    [MenuItem("Tools/Corridor/Wrap Selection As Module (+Z)")]
    public static void WrapZ() => WrapSelection(ForwardAxis.Z);

    [MenuItem("Tools/Corridor/Wrap Selection As Module (+X)")]
    public static void WrapX() => WrapSelection(ForwardAxis.X);

    [MenuItem("Tools/Corridor/Fix All Modules In Scene")]
    public static void FixAll()
    {
        foreach (var cm in Object.FindObjectsByType<CorridorModule>(FindObjectsSortMode.None))
        {
            Undo.RecordObject(cm, "Fix CorridorModule");
            cm.EnsureAndFixSocketsContextMenu();
            EditorUtility.SetDirty(cm);
        }
        SceneView.RepaintAll();
    }

    static void WrapSelection(ForwardAxis axis)
    {
        var trs = Selection.transforms;
        if (trs == null || trs.Length == 0)
        {
            EditorUtility.DisplayDialog("Corridor", "먼저 메시(렌더러)가 있는 오브젝트를 선택하세요.", "확인");
            return;
        }

        // 1) 바운즈 산출
        var bounds = CalcWorldBounds(trs);
        // 2) 부모 모듈 생성
        var go = new GameObject("Corr_Straight_4m");
        Undo.RegisterCreatedObjectUndo(go, "Create Corridor Module");
        var cm = Undo.AddComponent<CorridorModule>(go);

        // 3) 선택 오브젝트를 모듈 하위로 이동(상대 유지)
        var pivot = bounds.center;
        go.transform.position = pivot;

        foreach (var t in trs)
            Undo.SetTransformParent(t, go.transform, "Wrap Selection");

        // 4) 길이/축 설정 (셀 스냅: 0.5m 단위)
        float rawLen = (axis == ForwardAxis.Z) ? bounds.size.z : bounds.size.x;
        cm.length = Mathf.Max(0.5f, Mathf.Round(rawLen * 2f) * 0.5f); // 0.5m 스냅
        cm.kind = CorridorModule.Kind.Straight;
        cm.forwardAxis = (axis == ForwardAxis.Z)
            ? CorridorModule.ForwardAxis.Z
            : CorridorModule.ForwardAxis.X;

        cm.EnsureAndFixSocketsContextMenu();

        Selection.activeObject = go;
        EditorGUIUtility.PingObject(go);
        EditorUtility.SetDirty(go);
        SceneView.RepaintAll();
    }

    static Bounds CalcWorldBounds(Transform[] trs)
    {
        bool inited = false;
        Bounds b = new Bounds(Vector3.zero, Vector3.zero);
        foreach (var t in trs)
        {
            foreach (var r in t.GetComponentsInChildren<Renderer>(true))
            {
                if (!inited) { b = r.bounds; inited = true; }
                else b.Encapsulate(r.bounds);
            }
        }
        if (!inited) b = new Bounds(trs[0].position, Vector3.one * 0.1f);
        return b;
    }
}
#endif
