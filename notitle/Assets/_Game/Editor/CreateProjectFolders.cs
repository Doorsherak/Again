// Assets/_Game/Editor/CreateProjectFolders.cs
#if UNITY_EDITOR
using UnityEditor;
using System.IO;
using UnityEngine;

public static class CreateProjectFolders
{
    [MenuItem("Tools/Project Setup/Create Folders (Again)")]
    public static void Create()
    {
        // 루트
        Make("Assets/_Game");
        Make("Assets/_ThirdParty"); // 외부 에셋 보관

        // 런타임
        Make("Assets/_Game/Runtime");
        Make("Assets/_Game/Runtime/Scenes");
        Make("Assets/_Game/Runtime/Prefabs");
        Make("Assets/_Game/Runtime/Prefabs/UI");
        Make("Assets/_Game/Runtime/Prefabs/Audio");
        Make("Assets/_Game/Runtime/Scripts");
        Make("Assets/_Game/Runtime/Scripts/UI");
        Make("Assets/_Game/Runtime/Scripts/Audio");
        Make("Assets/_Game/Runtime/Materials");
        Make("Assets/_Game/Runtime/Textures");
        Make("Assets/_Game/Runtime/Animations");
        Make("Assets/_Game/Runtime/ScriptableObjects");
        Make("Assets/_Game/Runtime/Audio");
        Make("Assets/_Game/Runtime/Audio/SFX");
        Make("Assets/_Game/Runtime/Audio/BGM");
        Make("Assets/_Game/Runtime/Shaders");

        // 에디터 & 테스트
        Make("Assets/_Game/Editor");
        Make("Assets/_Game/Tests");

        // .gitkeep로 빈 폴더 보존(선택)
        TouchKeep("Assets/_Game/Runtime/Prefabs/UI/.gitkeep");
        TouchKeep("Assets/_Game/Runtime/Prefabs/Audio/.gitkeep");
        TouchKeep("Assets/_Game/Runtime/Audio/SFX/.gitkeep");
        TouchKeep("Assets/_Game/Runtime/Audio/BGM/.gitkeep");

        // (선택) asmdef 두 개 생성: Runtime/Editor 분리
        CreateAsmDef("Assets/_Game/Runtime/_Game.Runtime.asmdef", "Game.Runtime");
        CreateAsmDef("Assets/_Game/Editor/_Game.Editor.asmdef", "Game.Editor", true);

        AssetDatabase.Refresh();
        Debug.Log("[Project Setup] 폴더/asmdef 생성 완료 ✅");
    }

    static void Make(string path)
    {
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
    }
    static void TouchKeep(string file)
    {
        var dir = Path.GetDirectoryName(file);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        if (!File.Exists(file)) File.WriteAllText(file, "");
    }
    static void CreateAsmDef(string path, string name, bool editorOnly = false)
    {
        if (File.Exists(path)) return;
        var json = editorOnly
            ? $"{{\"name\":\"{name}\",\"includePlatforms\":[\"Editor\"]}}"
            : $"{{\"name\":\"{name}\"}}";
        File.WriteAllText(path, json);
    }
}
#endif
