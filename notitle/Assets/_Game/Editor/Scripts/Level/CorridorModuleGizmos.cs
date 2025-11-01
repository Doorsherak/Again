// Assets/_Game/Editor/Scripts/Level/CorridorModuleGizmos.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CorridorModule))]
public class CorridorModuleGizmos : Editor
{
    void OnSceneGUI()
    {
        var cm = (CorridorModule)target;
        DrawSocket(cm.socketIn, Color.green);
        DrawSocket(cm.socketOut, Color.red);
    }

    static void DrawSocket(Transform t, Color c)
    {
        if (!t) return;
        using (new Handles.DrawingScope(c, t.localToWorldMatrix)) // 로컬→월드 매트릭스 적용
        {
            // 소켓 로컬 +Z가 '출구' 방향이라는 가정
            var h = 2f; var w = 1f; var z = 0f;
            var verts = new Vector3[] {
                new(-w/2, 0, z), new(w/2, 0, z), new(w/2, h, z), new(-w/2, h, z)
            };
            Handles.DrawSolidRectangleWithOutline(verts, new Color(c.r, c.g, c.b, 0.08f), c);
            Handles.ArrowHandleCap(0, Vector3.zero, Quaternion.identity, 0.6f, EventType.Repaint); // 방향 표시
        }
        // 장면 보기 강제 갱신(보통은 필요 없지만 안전망)
        SceneView.RepaintAll();
    }
}
#endif
