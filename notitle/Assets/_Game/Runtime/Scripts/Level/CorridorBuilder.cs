using System;
using UnityEngine;
using static System.Net.Mime.MediaTypeNames;
// 아래 줄을 추가하여 UnityEngine.Application을 명시적으로 사용
using Application = UnityEngine.Application;
#if UNITY_EDITOR
using UnityEditor; // PrefabUtility, DestroyImmediate
#endif

/// <summary>
/// Corridor  :
///  - Կ CorridorModule  5(////ٸ) Ҵ
///  - layout: 'F'(), 'L'(ȸ), 'R'(ȸ), 'D'(), 'E'(ٸ) ڷ 
///  - : ν Ŭ ޴ [Build Now]  
///  - ÷: Build On Awake Ѹ   ڵ 
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

    [Header("Random layout")]
    [Tooltip("체크하면 layout 대신 랜덤으로 15글자(F/R/L + 마지막 D/E) 생성")]
    public bool useRandomLayout = false;

    [Tooltip("같은 시드를 쓰면 항상 같은 레이아웃이 나옴")]
    public int randomSeed = 42;

#if DEVELOPMENT_BUILD || UNITY_EDITOR
    public static bool UseSeedOverride;
    public static int SeedOverride;
#endif

    [Tooltip("총 길이(문자 수). 기본값 15")]
    public int randomLength = 15;

    [Header("Random layout (Pacing)")]
    [Tooltip("랜덤 레이아웃을 '페이싱' 있게 생성합니다(초반 직선↑, 중반 회전↑, 중간에 문(D) 비트 삽입).")]
    public bool usePacedRandomLayout = true;

    [Tooltip("페이싱 모드에서 삽입할 Door(D) 비트 수")]
    [Range(0, 4)] public int pacedDoorBeats = 2;

    [Tooltip("연속 회전(L/R) 최대 허용 횟수. 초과 시 강제로 F를 배치합니다.")]
    [Range(0, 4)] public int maxConsecutiveTurns = 2;

    [Tooltip("true면 마지막 글자를 항상 E(DeadEnd)로 고정합니다.")]
    public bool forceDeadEndAtEnd = true;



    [Header("Build options")]
    public bool clearBeforeBuild = true;
    public bool buildOnAwake = false;
    [Tooltip(" ̼ ħ(). 0~2mm ")]
    public float joinBias = 0.001f;

    [Header("Naming")]
    public bool autoNameInstances = true;
    [Tooltip("{index:000} {cmd} {prefab} ū  ")]
    public string nameFormat = "{index:000}_{cmd}_{prefab}";

    //  
    Transform lastOut; //   socketOut
    int index;

    void Awake()
    {
        // ÷   ڵ  ɼ
        if (buildOnAwake) Build();
    }

    /// <summary>Ϳ Ŭ ޴  </summary>
    [ContextMenu("Build Now")]
    public void Build()
    {
        // 0) ʱȭ/
        if (clearBeforeBuild)
        {
            for (int i = transform.childCount - 1; i >= 0; --i)
            {
                var child = transform.GetChild(i).gameObject;
                if (Application.isPlaying)
                    Destroy(child);                  // Ÿ
                else
                {
#if UNITY_EDITOR
                    DestroyImmediate(child);         //   
#else
                    Destroy(child);
#endif
                }
            }
        }
        lastOut = null;
        index = 0;

        if (useRandomLayout)
        {
            int seed = randomSeed;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (UseSeedOverride) seed = SeedOverride;
#endif
            layout = usePacedRandomLayout
                ? GeneratePacedRandomLayout(randomLength, seed)
                : GenerateRandomLayout(randomLength, seed);
            UnityEngine.Debug.Log($"[CorridorBuilder] Random layout generated: {layout}", this);
        }

        if (string.IsNullOrWhiteSpace(layout))
        {
            UnityEngine.Debug.LogWarning("[CorridorBuilder] layout   ֽϴ.", this);
            return;
        }

        // 1) ̾ƿ Ľ & ġ
        foreach (char raw in layout.Trim())
        {
            char c = char.ToUpperInvariant(raw);
            if (c == ' ' || c == ',') continue;

            CorridorModule prefab = c switch
            {
                'F' => straight,
                'L' => turnL,
                'R' => turnR,
                'D' => (door ? door : straight), //     ü
                'E' => deadEnd,
                _ => null
            };

            if (!prefab)
            {
                UnityEngine.Debug.LogError($"[CorridorBuilder] '{c}'   ֽϴ.", this);
                return;
            }

            var placed = Place(prefab);

            // 2) ó socketOut    ̴
            if (autoNameInstances)
            {
                placed.name = nameFormat
                    .Replace("{index:000}", index.ToString("000"))
                    .Replace("{cmd}", c.ToString())
                    .Replace("{prefab}", prefab.name);
            }

            // 3)  ̼ ħ( )
            if (joinBias > 0f)
                placed.transform.position -= placed.transform.forward * joinBias;

            lastOut = placed.socketOut;
            index++;

            if (c == 'E') break; // DeadEnd ⼭ 
        }

        UnityEngine.Debug.Log($"[CorridorBuilder] Done. {index} modules placed.", this);
    }

    /// <summary>
    /// FRL에서 앞 N-1 글자를 뽑고, 마지막 글자는 D 또는 E로 고정하는 랜덤 레이아웃 생성
    /// totalLength가 15이면: 앞 14자 = F/R/L, 마지막 1자 = D 또는 E
    /// </summary>
    string GenerateRandomLayout(int totalLength, int seed)
    {
        if (totalLength < 1)
        {
            Debug.LogError("[CorridorBuilder] totalLength 는 1 이상이어야 합니다.");
            totalLength = 1;
        }

        var rng = new System.Random(seed);

        char[] result = new char[totalLength];

        // 앞 totalLength - 1 자리는 F / R / L 중에서 랜덤
        char[] pool = { 'F', 'R', 'L' };
        int bodyLength = Mathf.Max(1, totalLength - 1);

        for (int i = 0; i < bodyLength; i++)
        {
            int idx = rng.Next(0, pool.Length); // [0, pool.Length)
            result[i] = pool[idx];
        }

        // 마지막 글자는 D 또는 E
        result[totalLength - 1] = (rng.Next(0, 2) == 0) ? 'D' : 'E';

        return new string(result);
    }

    string GeneratePacedRandomLayout(int totalLength, int seed)
    {
        if (totalLength < 1)
        {
            Debug.LogError("[CorridorBuilder] totalLength 는 1 이상이어야 합니다.");
            totalLength = 1;
        }

        var rng = new System.Random(seed);
        char[] result = new char[totalLength];
        int bodyLength = Mathf.Max(1, totalLength - 1);

        // Door(D) beats (avoid first/last of body when possible)
        bool[] doorSlots = new bool[bodyLength];
        int doorCount = Mathf.Clamp(pacedDoorBeats, 0, Mathf.Max(0, bodyLength - 1));
        if (doorCount > 0 && bodyLength > 1)
        {
            int minPos = bodyLength > 2 ? 1 : 0;
            int maxPos = bodyLength > 2 ? bodyLength - 2 : bodyLength - 1;
            for (int d = 1; d <= doorCount; d++)
            {
                float t = d / (float)(doorCount + 1);
                int pos = Mathf.RoundToInt((bodyLength - 1) * t);
                pos = Mathf.Clamp(pos, minPos, maxPos);
                doorSlots[pos] = true;
            }
        }

        int consecutiveTurns = 0;
        char prev = '\0';
        char prev2 = '\0';

        for (int i = 0; i < bodyLength; i++)
        {
            if (doorSlots[i])
            {
                result[i] = 'D';
                consecutiveTurns = 0;
                prev2 = prev;
                prev = 'D';
                continue;
            }

            float p = bodyLength <= 1 ? 0f : i / (float)(bodyLength - 1);
            double straightChance = (p < 0.35f) ? 0.72 : (p < 0.75f ? 0.55 : 0.62);

            bool prevTurn = prev == 'L' || prev == 'R';
            bool prev2Turn = prev2 == 'L' || prev2 == 'R';
            if (prevTurn && prev2Turn) straightChance = Math.Max(straightChance, 0.85);

            char c;
            if (maxConsecutiveTurns > 0 && consecutiveTurns >= maxConsecutiveTurns)
            {
                c = 'F';
            }
            else if (rng.NextDouble() < straightChance)
            {
                c = 'F';
            }
            else
            {
                c = (rng.Next(0, 2) == 0) ? 'L' : 'R';
            }

            result[i] = c;
            consecutiveTurns = (c == 'L' || c == 'R') ? (consecutiveTurns + 1) : 0;
            prev2 = prev;
            prev = c;
        }

        result[totalLength - 1] = forceDeadEndAtEnd ? 'E' : ((rng.Next(0, 2) == 0) ? 'D' : 'E');
        return new string(result);
    }

    /// <summary> νϽȭϰ socketIn/out   ġ</summary>
    CorridorModule Place(CorridorModule prefab)
    {
        GameObject go;

        if (Application.isPlaying)
        {
            // Ÿ ǥ νϽȭ(  ʿ)
            go = Instantiate(prefab.gameObject, transform);
        }
        else
        {
#if UNITY_EDITOR
            // Ϳ   ( / )
            go = (GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject, transform);
#else
            go = Instantiate(prefab.gameObject, transform);
#endif
        }

        var cm = go.GetComponent<CorridorModule>();
        if (!cm)
        {
            Debug.LogError("[CorridorBuilder] CorridorModule ϵ ϴ.", prefab);
            return null;
        }

        var t = go.transform;

        if (!lastOut)
        {
            // ù :  ġ/ȸ 
            t.SetPositionAndRotation(transform.position, transform.rotation);
        }
        else
        {
            //  : socketOut socketIn ġ
            t.SetPositionAndRotation(lastOut.position, lastOut.rotation);
            t.localScale = lastOut.lossyScale;

            if (cm && cm.socketIn)
            {
                // socketIn  ȯ Ͽ   Ȯ ´ 
                t.rotation *= Quaternion.Inverse(cm.socketIn.localRotation);
                t.position -= t.TransformVector(cm.socketIn.localPosition);
            }
        }

        t.localScale = Vector3.one;
        t.gameObject.isStatic = true;
        return cm;
    }
}
