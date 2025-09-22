// Assets/Editor/FindLightsReport.cs
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Text;
public class FindLightsReport
{
    [MenuItem("Tools/Lighting/라이트 위치 리포트")]
    static void Run()
    {
        var sb = new StringBuilder();
        // 열려 있는 씬들
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var sc = SceneManager.GetSceneAt(i);
            foreach (var go in sc.GetRootGameObjects())
                foreach (var l in go.GetComponentsInChildren<Light>(true))
                    sb.AppendLine($"SCENE,{sc.name},{GetPath(l.transform)},Shadows={l.shadows},Res={l.shadowResolution}");
        }
        // 프리팹 자산
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var p = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!p) continue;
            foreach (var l in p.GetComponentsInChildren<Light>(true))
                sb.AppendLine($"PREFAB,{path},{GetPath(l.transform)},Shadows={l.shadows},Res={l.shadowResolution}");
        }
        System.IO.File.WriteAllText("Assets/LightReport.csv", sb.ToString());
        AssetDatabase.Refresh();
        Debug.Log("LightReport.csv 생성 완료 (Assets 폴더)");
        string GetPath(Transform t) { var s = t.name; while (t.parent) { t = t.parent; s = t.name + "/" + s; } return s; }
    }
}
