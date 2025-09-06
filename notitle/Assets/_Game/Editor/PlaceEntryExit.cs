using UnityEngine;
using UnityEditor;

// Ensure the Segment class is defined or referenced
public class Segment : MonoBehaviour
{
    public Transform entry;
    public Transform exit;
}

public static class PlaceEntryExit
{
    [MenuItem("Tools/Level/Add Entry & Exit (centered)")]
    static void Run()
    {
        var root = Selection.activeTransform;
        if (!root) { EditorUtility.DisplayDialog("Select one", "���� ��Ʈ(�θ�)�� �����ϼ���.", "OK"); return; }

        var rends = root.GetComponentsInChildren<Renderer>(true);
        if (rends.Length == 0) { EditorUtility.DisplayDialog("No renderers", "�ڽĿ� Renderer�� �����ϴ�.", "OK"); return; }

        // ���� �ռ� �ٿ���
        var b = rends[0].bounds;
        foreach (var r in rends) b.Encapsulate(r.bounds);

        bool alongZ = b.size.z >= b.size.x; // �� �� ���� ����������� ����

        Vector3 center = b.center;
        Vector3 entryPos = center + (alongZ ? new Vector3(0, 0, -b.extents.z) : new Vector3(-b.extents.x, 0, 0));
        Vector3 exitPos = center + (alongZ ? new Vector3(0, 0, b.extents.z) : new Vector3(b.extents.x, 0, 0));

        Transform entry = root.Find("Entry") ?? new GameObject("Entry").transform;
        entry.SetParent(root, true); entry.position = entryPos; entry.forward = alongZ ? Vector3.back : Vector3.left;

        Transform exit = root.Find("Exit") ?? new GameObject("Exit").transform;
        exit.SetParent(root, true); exit.position = exitPos; exit.forward = alongZ ? Vector3.forward : Vector3.right;

        var seg = root.GetComponent<Segment>() ?? root.gameObject.AddComponent<Segment>();
        seg.entry = entry; seg.exit = exit;

        EditorUtility.DisplayDialog("Done", "Entry/Exit �ڵ� ��ġ �Ϸ�", "OK");
    }
}
