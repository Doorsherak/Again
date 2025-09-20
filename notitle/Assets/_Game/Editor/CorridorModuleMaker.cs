#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class CorridorModule : MonoBehaviour
{
    public Transform socketIn;
    public Transform socketOut;
    public float length = 4f; // +Z�� ����
    private void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        // +Z ���� ȭ��ǥ
        Gizmos.DrawLine(Vector3.zero, new Vector3(0, 0, length));
        Gizmos.DrawSphere(Vector3.zero, 0.05f);
        Gizmos.DrawSphere(new Vector3(0, 0, length), 0.05f);
    }
}

public static class CorridorModuleMaker
{
    [MenuItem("Tools/Corridor/Wrap Selection As Module")]
    public static void WrapSelection()
    {
        var sel = Selection.transforms;
        if (sel == null || sel.Length == 0) { Debug.LogWarning("Select meshes first."); return; }

        // ���� �ٿ��� ���
        bool has = false; Bounds b = new Bounds();
        foreach (var t in sel)
        {
            foreach (var r in t.GetComponentsInChildren<Renderer>())
            {
                if (!has) { b = r.bounds; has = true; } else b.Encapsulate(r.bounds);
            }
        }
        if (!has) { Debug.LogWarning("No renderers in selection."); return; }

        // �� �θ�: �ٴ�-�Ա� �߾�(=minZ��), +Z ���� ����
        Vector3 pivot = new Vector3(b.center.x, b.min.y, b.min.z);
        var parent = new GameObject("Corr_Module");
        Undo.RegisterCreatedObjectUndo(parent, "Create Corr_Module");
        parent.transform.position = pivot;
        parent.transform.rotation = Quaternion.identity;
        parent.isStatic = true;

        // ���� ���з��� (���� ��ġ ����)
        foreach (var t in sel) Undo.SetTransformParent(t, parent.transform, "Reparent To Corr_Module");

        // ������Ʈ & ����
        var cm = Undo.AddComponent<CorridorModule>(parent);
        float length = Mathf.Round((b.size.z) * 1000f) / 1000f; // �״�� ����
        cm.length = length;

        Transform sIn = new GameObject("Socket_In").transform;
        Transform sOut = new GameObject("Socket_Out").transform;
        sIn.SetParent(parent.transform, false);
        sOut.SetParent(parent.transform, false);
        sIn.localPosition = Vector3.zero;
        sOut.localPosition = new Vector3(0, 0, cm.length);
        cm.socketIn = sIn;
        cm.socketOut = sOut;

        Debug.Log($"Corridor module created. length?{cm.length:F3} (Z). Pivot at floor/entrance.");
    }
}
#endif
