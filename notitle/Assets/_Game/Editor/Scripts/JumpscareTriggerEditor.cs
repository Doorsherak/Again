using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(JumpscareTrigger))]
public class JumpscareTriggerEditor : Editor
{
    const string PreviewLockKey = "JumpscareTrigger_PreviewLock_";
    const string PreviewFovKey = "JumpscareTrigger_PreviewFov_";

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var trigger = (JumpscareTrigger)target;
        if (trigger == null) return;

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

        Transform camTr = ResolveCameraTransform(trigger);
        using (new EditorGUI.DisabledScope(camTr == null))
        {
            string lockKey = PreviewLockKey + target.GetInstanceID();
            string fovKey = PreviewFovKey + target.GetInstanceID();

            bool lockView = SessionState.GetBool(lockKey, false);
            bool newLock = EditorGUILayout.ToggleLeft("Lock Scene View To Camera", lockView);
            if (newLock != lockView)
            {
                SessionState.SetBool(lockKey, newLock);
                SceneView.RepaintAll();
            }

            bool matchFov = SessionState.GetBool(fovKey, true);
            bool newMatch = EditorGUILayout.ToggleLeft("Match Camera FOV", matchFov);
            if (newMatch != matchFov)
                SessionState.SetBool(fovKey, newMatch);

            if (GUILayout.Button("Align Scene View Now"))
                AlignSceneView(trigger, camTr, newMatch);
        }

        if (camTr == null)
            EditorGUILayout.HelpBox("Assign Camera Transform/Target Camera or a Player Controller camera to enable preview.", MessageType.Info);
    }

    void OnSceneGUI()
    {
        var trigger = (JumpscareTrigger)target;
        if (trigger == null) return;

        Transform camTr = ResolveCameraTransform(trigger);
        if (camTr == null) return;

        string lockKey = PreviewLockKey + target.GetInstanceID();
        string fovKey = PreviewFovKey + target.GetInstanceID();
        if (SessionState.GetBool(lockKey, false))
            AlignSceneView(trigger, camTr, SessionState.GetBool(fovKey, true));

        if (trigger.showPlacementPreview && trigger.placeMonsterAtCamera && trigger.monster != null)
        {
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

        if (trigger.showKeyLightPreview)
        {
            Vector3 lightWorldPos = camTr.TransformPoint(trigger.keyLightCameraLocalPos);
            Quaternion handleRot = camTr.rotation;

            Handles.color = trigger.keyLightPreviewColor;
            EditorGUI.BeginChangeCheck();
            Vector3 newLightWorldPos = Handles.PositionHandle(lightWorldPos, handleRot);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(trigger, "Move Jumpscare Key Light");
                trigger.keyLightCameraLocalPos = camTr.InverseTransformPoint(newLightWorldPos);
                EditorUtility.SetDirty(trigger);
            }
        }
    }

    static Transform ResolveCameraTransform(JumpscareTrigger trigger)
    {
        if (trigger.cameraTransform != null) return trigger.cameraTransform;
        if (trigger.preferTargetCameraForAutoResolve && trigger.targetCamera != null)
            return trigger.targetCamera.transform;
        if (trigger.allowPlayerControllerCameraSearch && trigger.playerController != null)
        {
            var cam = trigger.playerController.GetComponentInChildren<Camera>(true);
            if (cam != null) return cam.transform;
        }
        if (!trigger.preferTargetCameraForAutoResolve && trigger.targetCamera != null)
            return trigger.targetCamera.transform;
        Camera main = Camera.main;
        if (main != null) return main.transform;
        if (SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.camera != null)
            return SceneView.lastActiveSceneView.camera.transform;
        return null;
    }

    static void AlignSceneView(JumpscareTrigger trigger, Transform camTr, bool matchFov)
    {
        if (camTr == null) return;
        var view = SceneView.lastActiveSceneView;
        if (view == null) return;

        view.orthographic = false;
        view.pivot = camTr.position;
        view.rotation = camTr.rotation;
        view.size = 0.01f;

        if (matchFov)
        {
            var cam = camTr.GetComponent<Camera>();
            if (cam != null)
            {
                var settings = view.cameraSettings;
                float fov = cam.fieldOfView;
                if (trigger != null && trigger.previewUseFovKick && trigger.useFovKick)
                    fov = Mathf.Clamp(fov + trigger.fovKick, 10f, 170f);
                settings.fieldOfView = fov;
                view.cameraSettings = settings;
            }
        }

        view.Repaint();
    }
}
