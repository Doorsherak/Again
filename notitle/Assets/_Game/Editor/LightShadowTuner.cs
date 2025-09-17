// Assets/Editor/LightShadowTuner.cs
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;

public static class LightShadowTuner
{
    const float RangeThreshold = 12f;   // "����" ����(�ʿ�� ����)

    [MenuItem("Tools/Lighting/���� ���������� ���� ����Ʈ �׸��� Low��")]
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
        Debug.Log($"[LightShadowTuner] ���� {changed}�� (������{RangeThreshold})");
    }
}
