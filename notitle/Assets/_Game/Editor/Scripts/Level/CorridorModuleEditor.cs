#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CorridorModule))]
public class CorridorModuleEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        var cm = (CorridorModule)target;

        EditorGUILayout.Space();
        if (GUILayout.Button("Auto-Assign Sockets (by name)"))
        {
            if (!cm.socketIn) cm.socketIn = cm.transform.Find("Socket_In");
            if (!cm.socketOut) cm.socketOut = cm.transform.Find("Socket_Out");

            if (!cm.socketIn || !cm.socketOut)
            {
                foreach (var t in cm.GetComponentsInChildren<Transform>(true))
                {
                    string n = t.name.ToLower();
                    if (!cm.socketIn && (n == "in" || n.Contains("socket_in"))) cm.socketIn = t;
                    if (!cm.socketOut && (n == "out" || n.Contains("socket_out"))) cm.socketOut = t;
                }
            }
            EditorUtility.SetDirty(cm);
        }

        EditorGUILayout.BeginHorizontal();
        GUI.enabled = cm.socketIn;
        if (GUILayout.Button("Ping In")) EditorGUIUtility.PingObject(cm.socketIn);
        GUI.enabled = cm.socketOut;
        if (GUILayout.Button("Ping Out")) EditorGUIUtility.PingObject(cm.socketOut);
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
    }

    [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
    static void DrawSocketLabels(CorridorModule cm, GizmoType type)
    {
        if (!cm) return;
        var style = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.cyan } };
        if (cm.socketIn) Handles.Label(cm.socketIn.position + Vector3.up * 0.06f, "IN", style);
        if (cm.socketOut) Handles.Label(cm.socketOut.position + Vector3.up * 0.06f, "OUT", style);
    }
}
#endif
