using UnityEngine;

public class LevelModule : MonoBehaviour
{
    public Bounds bounds;

    public void RecalcBounds()
    {
        var rends = GetComponentsInChildren<Renderer>(true);
        if (rends.Length == 0) { bounds = new Bounds(transform.position, Vector3.one); return; }

        // 월드 바운즈 합치기
        var w = rends[0].bounds;
        foreach (var r in rends) w.Encapsulate(r.bounds);

        // 월드 → 로컬 변환
        var t = transform;
        var c = t.InverseTransformPoint(w.center);
        var s = t.InverseTransformVector(w.size);

        bounds = new Bounds(c, new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z)));
    }
}
