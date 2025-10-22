using UnityEngine;
#if UNITY_EDITOR
using UnityEditor; // PrefabUtility, DestroyImmediate
#endif

/// <summary>
/// Corridor 모듈 빌더:
///  - 슬롯에 CorridorModule 프리팹 5종(직선/좌/우/문/막다른)을 할당
///  - layout: 'F'(직진), 'L'(좌회전), 'R'(우회전), 'D'(문), 'E'(막다른) 문자로 구성
///  - 에디터: 인스펙터 우클릭 메뉴 [Build Now]로 즉시 생성
///  - 플레이: Build On Awake 켜면 시작 시 자동 생성
/// </summary>
public class CorridorBuilder : MonoBehaviour
{
    [Header("Module prefabs")]
    public CorridorModule straight;
    public CorridorModule turnL;
    public CorridorModule turnR;
    public CorridorModule door;
    public CorridorModule deadEnd;

    [Header("Layout")]
    [TextArea] public string layout = "FFRFFLFFDFE";

    [Header("Build options")]
    public bool clearBeforeBuild = true;
    public bool buildOnAwake = false;
    [Tooltip("이음새 미세 겹침(미터). 0~2mm 권장")]
    public float joinBias = 0.001f;

    [Header("Naming")]
    public bool autoNameInstances = true;
    [Tooltip("{index:000} {cmd} {prefab} 토큰 사용 가능")]
    public string nameFormat = "{index:000}_{cmd}_{prefab}";

    // 내부 상태
    Transform lastOut; // 직전 모듈의 socketOut
    int index;

    void Awake()
    {
        // 플레이 진입 시 자동 빌드 옵션
        if (buildOnAwake) Build();
    }

    /// <summary>에디터에서 우클릭 메뉴로 수동 실행</summary>
    [ContextMenu("Build Now")]
    public void Build()
    {
        // 0) 초기화/정리
        if (clearBeforeBuild)
        {
            for (int i = transform.childCount - 1; i >= 0; --i)
            {
                var child = transform.GetChild(i).gameObject;
                if (Application.isPlaying)
                    Destroy(child);                  // 런타임
                else
                {
#if UNITY_EDITOR
                    DestroyImmediate(child);         // 에디터 즉시 삭제
#else
                    Destroy(child);
#endif
                }
            }
        }
        lastOut = null;
        index = 0;

        if (string.IsNullOrWhiteSpace(layout))
        {
            Debug.LogWarning("[CorridorBuilder] layout 이 비어 있습니다.", this);
            return;
        }

        // 1) 레이아웃 파싱 & 배치
        foreach (char raw in layout.Trim())
        {
            char c = char.ToUpperInvariant(raw);
            if (c == ' ' || c == ',') continue;

            CorridorModule prefab = c switch
            {
                'F' => straight,
                'L' => turnL,
                'R' => turnR,
                'D' => (door ? door : straight), // 문 프리팹이 없으면 직선으로 대체
                'E' => deadEnd,
                _ => null
            };

            if (!prefab)
            {
                Debug.LogError($"[CorridorBuilder] '{c}' 프리팹이 비어 있습니다.", this);
                return;
            }

            var placed = Place(prefab);       // 소켓 정렬 배치
            if (!placed) { Debug.LogError("[CorridorBuilder] 배치 실패", this); return; }

            // 2) 자동 네이밍
            if (autoNameInstances)
            {
                placed.name = nameFormat
                    .Replace("{index:000}", index.ToString("000"))
                    .Replace("{cmd}", c.ToString())
                    .Replace("{prefab}", prefab.name);
            }

            // 3) 이음새 미세 겹침(빛샘 방지)
            if (joinBias > 0f)
                placed.transform.position -= placed.transform.forward * joinBias;

            lastOut = placed.socketOut;
            index++;

            if (c == 'E') break; // DeadEnd는 여기서 종료
        }

        Debug.Log($"[CorridorBuilder] Done. {index} modules placed.", this);
    }

    /// <summary>프리팹을 인스턴스화하고 socketIn/out 기준으로 맞춰 배치</summary>
    CorridorModule Place(CorridorModule prefab)
    {
        GameObject go;

        if (Application.isPlaying)
        {
            // 런타임 표준 인스턴스화(프리팹 연결 불필요)
            go = Instantiate(prefab.gameObject, transform);
        }
        else
        {
#if UNITY_EDITOR
            // 에디터에선 프리팹 연결 유지(씬에서 재적용/리네임 편리)
            go = (GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject, transform);
#else
            go = Instantiate(prefab.gameObject, transform);
#endif
        }

        var cm = go.GetComponent<CorridorModule>();
        var t = go.transform;

        if (lastOut == null)
        {
            // 첫 모듈: 빌더의 위치/회전 기준
            t.SetPositionAndRotation(transform.position, transform.rotation);
        }
        else
        {
            // 이후 모듈: 직전 모듈의 socketOut과 현 모듈의 socketIn을 정렬
            t.SetPositionAndRotation(lastOut.position, lastOut.rotation);

            if (cm && cm.socketIn)
            {
                // socketIn의 로컬 변환을 상쇄하여 두 소켓이 정확히 맞닿게 함
                t.rotation *= Quaternion.Inverse(cm.socketIn.localRotation);
                t.position -= t.TransformVector(cm.socketIn.localPosition);
            }
        }

        t.localScale = Vector3.one;
        t.gameObject.isStatic = true;
        return cm;
    }
}
