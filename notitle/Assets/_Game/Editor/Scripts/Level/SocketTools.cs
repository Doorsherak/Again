// Editor/SocketTools.cs
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

static class SocketTools
{
    [MenuItem("Tools/Corridor/Normalize Sockets (Selected)")]
    static void Normalize()
    {
        foreach (var t in Selection.transforms)
        {
            var cm = t.GetComponent<CorridorModule>();
            if (!cm || !t.gameObject.scene.IsValid()) continue;

            Ensure(ref cm.socketIn, "Socket_In", t);
            Ensure(ref cm.socketOut, "Socket_Out", t);

            // 예시: 모듈 중심을 원점, 진행축 +X 라고 가정
            float half = 2.0f; // 길이의 절반 (프로퍼티 있으면 그 값 사용)
            Vector3 dir = Vector3.right;

            Undo.RecordObject(cm.socketIn, "Move Socket");
            Undo.RecordObject(cm.socketOut, "Move Socket");
            cm.socketIn.localPosition = -dir * half;
            cm.socketOut.localPosition = dir * half;
            cm.socketIn.localRotation = Quaternion.LookRotation(dir);
            cm.socketOut.localRotation = Quaternion.LookRotation(-dir);

            EditorUtility.SetDirty(cm);
        }
    }

    static void Ensure(ref Transform tr, string name, Transform parent)
    {
        if (!tr)
        {
            var existing = parent.Find(name);
            tr = existing ? existing : new GameObject(name).transform;
            if (tr.parent != parent) Undo.SetTransformParent(tr, parent, "Parent Socket");
        }
    }
}
#endif
