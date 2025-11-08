// Editor/SocketTools.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Experimental.SceneManagement;
using UnityEngine;

public static class SocketTools
{
    [MenuItem("Corridor/Socket Tools/Normalize Selected")]
    static void NormalizeSelected()
    {
        var modules = Selection.GetFiltered<CorridorModule>(SelectionMode.Editable);
        foreach (var cm in modules)
        {
            // ★ 위치/회전만 보정. SetParent 금지 (프리팹 에셋 오류 방지)
            NormalizeSocketsNoReparent(cm);

            // 더티 마킹
            EditorUtility.SetDirty(cm);
            if (cm.socketIn) EditorUtility.SetDirty(cm.socketIn);
            if (cm.socketOut) EditorUtility.SetDirty(cm.socketOut);
        }

        // 저장: 프리팹 모드 vs 씬
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage != null)
        {
            string path = AssetDatabase.GetAssetPath(stage.prefabContentsRoot);
            PrefabUtility.SaveAsPrefabAsset(stage.prefabContentsRoot, path);
        }
        else
        {
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }
    }

    // 누락된 함수 정의 추가
    static void NormalizeSocketsNoReparent(CorridorModule cm)
    {
        // 기존 NormalizeSockets()와 유사하게 구현하거나, 필요한 위치/회전 보정 로직 작성
        // 예시: 소켓의 위치/회전만 보정 (SetParent 사용 금지)
        if (cm.socketIn != null)
        {
            cm.socketIn.localPosition = Vector3.zero;
            cm.socketIn.localRotation = Quaternion.identity;
        }
        if (cm.socketOut != null)
        {
            cm.socketOut.localPosition = new Vector3(0, 0, cm.length);
            cm.socketOut.localRotation = Quaternion.identity;
        }
    }
}
#endif
