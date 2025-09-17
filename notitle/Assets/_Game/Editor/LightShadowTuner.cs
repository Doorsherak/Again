// Assets/Editor/LightShadowTuner.cs
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;

public static class LightShadowTuner
{
    const float RangeThreshold = 12f;   // "소형" 기준(필요시 조정)

    [MenuItem("Tools/Lighting/선택 폴더·씬의 소형 라이트 그림자 Low로")]
    static void LowerShadowRes()
    {
        var lights = Selection.GetFiltered<Light>(
          SelectionMode.Unfiltered | SelectionMode.Deep | SelectionMode.DeepAssets);

        int changed = 0;
        foreach (var l in lights)
        {
            if (!l) continue;
            if ((l.type == LightType.Point || l.type == LightType.Spot) && l.range <= RangeThreshold)
            {
                if (l.shadows == LightShadows.None) continue;
                Undo.RecordObject(l, "Lower light shadow");
                l.shadows = LightShadows.Soft;
                l.shadowResolution = LightShadowResolution.Low;
                EditorUtility.SetDirty(l);
                changed++;
            }
        }
        AssetDatabase.SaveAssets();
        if (changed > 0) EditorSceneManager.MarkAllScenesDirty();
        Debug.Log($"[LightShadowTuner] 변경 {changed}개 (범위≤{RangeThreshold})");
    }
}
