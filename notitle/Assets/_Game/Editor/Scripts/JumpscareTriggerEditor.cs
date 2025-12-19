using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(JumpscareTrigger))]
public class JumpscareTriggerEditor : Editor
{
    void OnSceneGUI()
    {
        var trigger = (JumpscareTrigger)target;
        if (trigger == null) return;
        if (!trigger.showPlacementPreview) return;
        if (!trigger.placeMonsterAtCamera || trigger.monster == null) return;

        Transform camTr = ResolveCameraTransform(trigger);
        if (camTr == null) return;

        Vector3 worldPos = camTr.TransformPoint(trigger.monsterCameraLocalPos);
        Quaternion handleRot = camTr.rotation;

        EditorGUI.BeginChangeCheck();
        Vector3 newWorldPos = Handles.PositionHandle(worldPos, handleRot);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(trigger, "Move Jumpscare Monster Position");
            trigger.monsterCameraLocalPos = camTr.InverseTransformPoint(newWorldPos);
            EditorUtility.SetDirty(trigger);
        }
    }

    static Transform ResolveCameraTransform(JumpscareTrigger trigger)
    {
        if (trigger.cameraTransform != null) return trigger.cameraTransform;
        Camera main = Camera.main;
        if (main != null) return main.transform;
        if (SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.camera != null)
            return SceneView.lastActiveSceneView.camera.transform;
        return null;
    }
}
