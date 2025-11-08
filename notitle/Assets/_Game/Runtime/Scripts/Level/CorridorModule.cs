using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif


[DisallowMultipleComponent]
public class CorridorModule : MonoBehaviour
{
    // 모듈 종류
    public enum Kind { Straight, TurnL, TurnR, Door, DeadEnd }

    [Header("모듈 규격")]
    public Kind kind = Kind.Straight;

    [Tooltip("셀 길이(m). Socket_Out의 진행축 위치와 동일")]
    public float length = 4f;

    [Tooltip("기즈모(시각화)용 폭/높이 — 메시에는 영향 없음")]
    public float width = 3.0f;
    public float height = 2.7f;

    [Header("Sockets (local)")]
    public Transform socketIn;   // local: pos (0,0,0), rot (0,0,0)
    public Transform socketOut;  // local: pos (0,0,length) 또는 (length,0,0)

    // 진행축 선택: 프로젝트 기준으로 사용 (+Z 또는 +X)
    public enum ForwardAxis { Z, X }
    [Header("진행축 설정")]
    public ForwardAxis forwardAxis = ForwardAxis.Z;

    // 에디터에서 값 바뀔 때마다 소켓 정규화
    void OnValidate()

    {
        {
            if (Application.isPlaying) return;                        // [1] 플레이 진입 시 호출돼도 정렬 금지. OnValidate는 재생 진입에도 호출됨. :contentReference[oaicite:0]{index=0}
#if UNITY_EDITOR
            if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(gameObject)) return;        // [2] 프리팹 '자산'이면 금지. :contentReference[oaicite:1]{index=1}
            if (UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() != null) return; // [3] 프리팹 모드(격리 편집)에서도 금지. :contentReference[oaicite:2]{index=2}
#endif

            // ↓↓↓ 기존 자동 정렬/재배치 코드가 여기부터 실행되도록 유지
        }
        EnsureSockets();
        NormalizeSockets();
        WarnIfNonUniformScale();
    }

    [ContextMenu("Corridor/Create or Fix Sockets")]
    public void EnsureAndFixSocketsContextMenu()
    {
        EnsureSockets();
        NormalizeSockets();
    }

    void EnsureSockets()
    {
        if (!socketIn) socketIn = FindDeep(transform, "Socket_In");
        if (!socketOut) socketOut = FindDeep(transform, "Socket_Out");

        if (!socketIn)
        {
            socketIn = new GameObject("Socket_In").transform;
            socketIn.SetParent(transform, false);
        }
        if (!socketOut)
        {
            socketOut = new GameObject("Socket_Out").transform;
            socketOut.SetParent(transform, false);
        }
    }

    void NormalizeSockets()
    {
        if (socketIn)
        {
            socketIn.SetParent(transform, true);
            socketIn.localPosition = Vector3.zero;
            socketIn.localRotation = Quaternion.identity;
            socketIn.localScale = Vector3.one;
        }

        if (!socketOut) return;

        socketOut.SetParent(transform, true);

        // 진행축에 따른 위치/방향
        float L = Mathf.Max(0.01f, length);
        if (forwardAxis == ForwardAxis.Z)
        {
            socketOut.localPosition = new Vector3(0f, 0f, L);
            float y = (kind == Kind.TurnL) ? -90f : (kind == Kind.TurnR ? 90f : 0f);
            socketOut.localRotation = Quaternion.Euler(0f, y, 0f);
        }
        else // ForwardAxis.X
        {
            socketOut.localPosition = new Vector3(L, 0f, 0f);
            float y = (kind == Kind.TurnL) ? 0f : (kind == Kind.TurnR ? 180f : 90f);
            // +X 진행에서 좌회전/우회전 기준을 유지하도록 보정(90° 오프셋)
            socketOut.localRotation = Quaternion.Euler(0f, y, 0f);
        }

        socketOut.localScale = Vector3.one;
    }

    void WarnIfNonUniformScale()
    {
        var s = transform.lossyScale;
        if (Mathf.Abs(s.x - s.y) > 1e-4f || Mathf.Abs(s.y - s.z) > 1e-4f)
        {
            Debug.LogWarning($"[CorridorModule] 비균등 스케일 감지: {name} (lossyScale={s}). 정합에 문제 가능.", this);
        }
    }

    static Transform FindDeep(Transform root, string targetName)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == targetName) return t;
        return null;
    }
}
