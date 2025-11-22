using UnityEngine;

#if UNITY_EDITOR
[ExecuteInEditMode]
#endif
public class MeasureSize : MonoBehaviour
{
    public Vector3 worldSize;   // Renderer 기준 (자식 포함, 월드 단위)
    public Vector3 localSize;   // Mesh 기준 (로컬 단위, 단일 메시에만 정확)

    void Update()
    {
        UpdateSize();
    }

    void OnValidate()
    {
        UpdateSize();
    }

    void UpdateSize()
    {
        var rend = GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            worldSize = rend.bounds.size; // 월드 기준 크기 
        }

        var mf = GetComponentInChildren<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            // Mesh 로컬 bounds 크기 
            localSize = mf.sharedMesh.bounds.size;
        }
    }
}
