using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement; // PrefabStageUtility, MarkSceneDirty
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

#if UNITY_EDITOR
    [Header("Editor Snap")]
    [Tooltip("소켓/자기 자신 위치를 이 그리드로 반올림(틈 방지). 0.001 = 1mm")]
    public float snapGrid = 0.001f;
#endif

    // 에디터에서 값 변경시 자동 변형은 하지 않음(되돌림 방지)
    void OnValidate()
    {
#if UNITY_EDITOR
        if (Application.isPlaying) return; // 플레이 중 호출되기도 함. 문서상 여러 시점에서 불림. :contentReference[oaicite:3]{index=3}
        // Prefab 모드에서도 자동 정렬은 수행하지 않음(수동 버튼으로만)
        if (PrefabStageUtility.GetCurrentPrefabStage() != null) return; // Prefab Stage 감지. :contentReference[oaicite:4]{index=4}
#endif
    }

    // === 툴 버튼: 소켓 만들기/정렬/스냅/저장표시 ===
    [ContextMenu("Corridor/Create or Fix Sockets")]
    public void EnsureAndFixSocketsContextMenu()
    {
#if UNITY_EDITOR
        // 프로젝트 창 프리팹 자산에 직접 실행 금지(프리팹은 Prefab 모드에서 수정)
        var stage = PrefabStageUtility.GetCurrentPrefabStage(); // null이면 일반 씬
        if (PrefabUtility.IsPartOfPrefabAsset(gameObject) && stage == null)
        {
            EditorUtility.DisplayDialog(
                "Open Prefab",
                "프로젝트 창 프리팹 자산에서는 실행할 수 없어요.\n더블클릭해 Prefab Mode에서 열거나, 씬 인스턴스를 선택해 주세요.",
                "확인"
            );
            return;
        }
#endif
        EnsureSockets();
        NormalizeSockets();
#if UNITY_EDITOR
        QuantizeSelfAndSockets();
        WarnIfNonUniformScale();
        MarkDirtyForSave();
#endif
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
            // 부모 보장(씬 인스턴스/프리팹 모드에서만 호출됨)
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
            // +X 진행에서 좌/우회전 기준 유지(90° 오프셋)
            socketOut.localRotation = Quaternion.Euler(0f, y, 0f);
        }

        socketOut.localScale = Vector3.one;
    }

#if UNITY_EDITOR
    // === 에디터 유틸 ===

    void WarnIfNonUniformScale()
    {
        var s = transform.lossyScale;
        if (Mathf.Abs(s.x - s.y) > 1e-4f || Mathf.Abs(s.y - s.z) > 1e-4f)
        {
            Debug.LogWarning($"[CorridorModule] 비균등 스케일 감지: {name} (lossyScale={s}). 정합에 문제 가능.", this);
        }
    }

    void QuantizeSelfAndSockets()
    {
        // 자기 자신은 월드 좌표 스냅(원하면 주석)
        transform.position = Round(transform.position, snapGrid);

        // 소켓은 로컬 좌표 스냅(틈 방지 핵심)
        if (socketIn) socketIn.localPosition = Round(socketIn.localPosition, snapGrid);
        if (socketOut) socketOut.localPosition = Round(socketOut.localPosition, snapGrid);
    }

    static Vector3 Round(Vector3 v, float g)
    {
        if (g <= 0f) return v;
        float inv = 1f / g;
        return new Vector3(
            Mathf.Round(v.x * inv) / inv,
            Mathf.Round(v.y * inv) / inv,
            Mathf.Round(v.z * inv) / inv
        );
    }

    void MarkDirtyForSave()
    {
        // 변경 사항 저장 표시(씬/프리팹 모드 모두)
        EditorUtility.SetDirty(this);
        if (socketIn) EditorUtility.SetDirty(socketIn);
        if (socketOut) EditorUtility.SetDirty(socketOut);

        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage != null) EditorSceneManager.MarkSceneDirty(stage.scene);  // Prefab Mode에서의 저장 표시
        else EditorSceneManager.MarkSceneDirty(gameObject.scene); // 씬 인스턴스 저장 표시
        // MarkSceneDirty는 "저장 대상 변경됨" 표시 역할. :contentReference[oaicite:5]{index=5}
    }
#endif

    static Transform FindDeep(Transform root, string targetName)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == targetName) return t;
        return null;
    }
}
