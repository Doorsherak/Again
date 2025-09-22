#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class AlignBottomToY
{
    [MenuItem("Tools/Align/Bottom To Y=0")]
    public static void BottomToZero() => BottomToY(0f);

    [MenuItem("Tools/Align/Bottom To Y...")]
    public static void BottomToYPrompt()
    {
        string s = EditorUtility.DisplayDialogComplex("Bottom To Y", "Align selected objects' bottom to target Y?", "0", "Cancel", "Custom") switch
        {
            0 => "0",
            2 => EditorUtility.DisplayDialog("Hint", "Enter a custom Y in the console.", "OK") is true ? "0" : "0", // fallback
            _ => null
        };
        if (s == null) return;
        BottomToY(float.Parse(s));
    }

    static void BottomToY(float targetY)
    {
        foreach (var t in Selection.transforms)
        {
            var rends = t.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0) continue;

            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);

            float dy = targetY - b.min.y;
            Undo.RecordObject(t, "Align Bottom To Y");
            t.position += new Vector3(0f, dy, 0f);
        }
    }
}
#endif
