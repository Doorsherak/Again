// Assets/_Game/Editor/Scripts/Level/SocketTools.cs
using UnityEngine;
using UnityEditor;
using System.IO;

static class SocketTools
{
    const float LENGTH = 4f; // 모듈 길이(미터) - 필요시 변경

    // 1) 프리팹 일괄 정규화
    [MenuItem("Tools/Level/Normalize Corridor Sockets (Batch)")]
    static void NormalizeSelectedPrefabs()
    {
        var guids = Selection.assetGUIDs;
        if (guids == null || guids.Length == 0)
        {
            EditorUtility.DisplayDialog("Socket Tools", "Project 창에서 프리팹을 선택하세요.", "OK");
            return;
        }

        int fixedCount = 0;
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path) || !path.EndsWith(".prefab")) continue;

            // Prefab을 고립된 씬으로 로드 → 수정 → 저장 → 언로드 (공식 권장 루틴)
            // LoadPrefabContents / SaveAsPrefabAsset / UnloadPrefabContents
            var root = PrefabUtility.LoadPrefabContents(path);                         // :contentReference[oaicite:2]{index=2}
            try
            {
                var tIn = root.transform.Find("Socket_In");
                var tOut = root.transform.Find("Socket_Out");
                if (tIn == null || tOut == null) { Debug.LogWarning($"No sockets in {path}"); continue; }

                // 루트 클린상태로
                root.transform.position = Vector3.zero;
                root.transform.rotation = Quaternion.identity;
                root.transform.localScale = Vector3.one;

                // 표준 로컬 포즈
                tIn.localPosition = Vector3.zero;
                tIn.localRotation = Quaternion.identity;

                tOut.localPosition = new Vector3(0f, 0f, LENGTH);

                // TurnL/TurnR는 이름으로 회전 추정(규칙에 맞게 수정 가능)
                var name = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                if (name.Contains("turnl")) tOut.localRotation = Quaternion.Euler(0f, -90f, 0f);
                else if (name.Contains("turnr")) tOut.localRotation = Quaternion.Euler(0f, +90f, 0f);
                else tOut.localRotation = Quaternion.identity;

                PrefabUtility.SaveAsPrefabAsset(root, path);                            // :contentReference[oaicite:3]{index=3}
                fixedCount++;
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }                      // :contentReference[oaicite:4]{index=4}
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"[SocketTools] normalized {fixedCount} prefab(s).");
    }

    // 2) 씬에서 두 모듈 정밀 스냅(선택 순서: [이전 모듈, 다음 모듈])
    [MenuItem("Tools/Level/Snap Selected Module To Previous Out (Scene)")]
    static void SnapSelectionToPrevOut()
    {
        if (Selection.transforms.Length < 2) { Debug.LogWarning("Select [Prev, Next]"); return; }
        var prev = Selection.transforms[0];
        var next = Selection.transforms[1];

        var prevOut = prev.Find("Socket_Out");
        var nextIn = next.Find("Socket_In");
        if (prevOut == null || nextIn == null) { Debug.LogWarning("Sockets missing."); return; }

        // 위치+회전을 '동시에' 세팅 → 더 정확/효율적
        next.SetPositionAndRotation(prevOut.position, prevOut.rotation);               // :contentReference[oaicite:5]{index=5}
        // In의 로컬 오프셋/회전 상쇄 → 월드에서 Out과 정확히 일치
        next.rotation *= Quaternion.Inverse(nextIn.localRotation);
        next.position -= next.TransformVector(nextIn.localPosition);                   // 방향 변환(스케일 無) 주의점 :contentReference[oaicite:6]{index=6}
    }
}
