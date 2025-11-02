// Assets/_Game/Editor/Scripts/Level/CorridorModuleGizmos.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class CorridorModuleGizmos
{
    [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
    static void DrawSockets(CorridorModule cm, GizmoType gizmoType)
    {
        if (!cm) return;
        DrawSocket(cm.socketIn,  new Color(0f, 1f, 0f, 0.8f));
        DrawSocket(cm.socketOut, new Color(1f, 0f, 0f, 0.8f));

        void DrawSocket(Transform t, Color c)
        {
            if (!t) return;
            Gizmos.color = c;
            var p = t.position;
            var fwd = t.forward;            // +Z를 소켓 법선으로 사용
            var up  = Vector3.up;
            var right = Vector3.Cross(up, fwd).normalized;
            float w = 1.9f, h = cm.height - 0.1f;

            Vector3 a = p + up*(h*0.5f) - right*(w*0.5f);
            Vector3 b = p + up*(h*0.5f) + right*(w*0.5f);
            Vector3 c2= p - up*(h*0.5f) + right*(w*0.5f);
            Vector3 d = p - up*(h*0.5f) - right*(w*0.5f);

            Gizmos.DrawLine(a,b); Gizmos.DrawLine(b,c2);
            Gizmos.DrawLine(c2,d); Gizmos.DrawLine(d,a);
            Gizmos.DrawRay(p, fwd * 0.8f); // 진행 화살표
        }
    }
}
#endif
