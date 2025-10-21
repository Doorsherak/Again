using UnityEngine;

[DisallowMultipleComponent]
public partial class CorridorModule : MonoBehaviour
{
    // 모듈 종류: 코너는 Out의 Y 회전이 자동 적용됨(좌 -90°, 우 +90°)
    public enum Kind { Straight, TurnL, TurnR, Door, DeadEnd }

    [Header("모듈 규격")]
    public Kind kind = Kind.Straight;

    [Tooltip("셀 길이(m). Socket_Out의 z 위치와 동일")]
    public float length = 4f;

    [Tooltip("기즈모 표시용 크기(메시에는 영향 없음)")]
    public float width = 3.0f;
    public float height = 2.7f;

    [Header("Sockets (local)")]
    public Transform socketIn;   // local: pos (0,0,0), rot (0,0,0)
    public Transform socketOut;  // local: pos (0,0,length), rot: Straight/Door/DeadEnd=0, L=-90, R=+90

    // ──────────────────────────────────────────────────────────────────────────
    // 에디터에서만 호출되는 자동화: 누락 시 소켓을 찾아 채우거나 생성/정규화
    void OnValidate()
    {
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

        if (socketOut)
        {
            socketOut.SetParent(transform, true);
            socketOut.localPosition = new Vector3(0f, 0f, Mathf.Max(0.01f, length));

            float y = 0f;
            if (kind == Kind.TurnL) y = -90f;
            else if (kind == Kind.TurnR) y = 90f;
            socketOut.localRotation = Quaternion.Euler(0f, y, 0f);
            socketOut.localScale = Vector3.one;
        }
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

    // ──────────────────────────────────────────────────────────────────────────
    // 기즈모: 입/출구 단면 및 진행 방향 시각화(씬 작업 편의를 위한 표시)
    void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;

        DrawPlane(0f, new Color(1f, 0.25f, 0.25f));   // In(빨강)
        DrawPlane(length, new Color(0.3f, 1f, 0.3f)); // Out(초록)

        if (socketIn)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(socketIn.localPosition, 0.05f);
        }
        if (socketOut)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(socketOut.localPosition, 0.05f);

            // 진행 방향 화살표(코너는 ±90°)
            Gizmos.color = Color.cyan;
            var p = socketOut.localPosition;
            var dir = (Quaternion.Euler(0f,
                        (kind == Kind.TurnR ? 90f : (kind == Kind.TurnL ? -90f : 0f)), 0f))
                        * Vector3.forward;
            Gizmos.DrawRay(p, dir * 0.6f);
        }
    }

    void DrawPlane(float z, Color c)
    {
        Gizmos.color = c;
        Vector3 a = new Vector3(-width / 2f, 0f, z);
        Vector3 b = new Vector3(width / 2f, 0f, z);
        Vector3 c1 = new Vector3(width / 2f, height, z);
        Vector3 d = new Vector3(-width / 2f, height, z);
        Gizmos.DrawLine(a, b); Gizmos.DrawLine(b, c1); Gizmos.DrawLine(c1, d); Gizmos.DrawLine(d, a);
    }
}
