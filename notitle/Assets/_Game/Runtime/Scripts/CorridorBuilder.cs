using UnityEngine;

    public class CorridorBuilder : MonoBehaviour
    {
        [Header("Module prefabs")]
        public CorridorModule straight, turnL, turnR, door, deadEnd;

        [Header("Layout")]
        public string layout = "FFRFFLFFDFE";

        [Header("Build options")]
        public bool clearBeforeBuild = true;
        public bool buildOnAwake = false;
        public float joinBias = 0.001f;

        // ▼ 추가: 자동 네이밍 옵션
        [Header("Naming")]
        public bool autoNameInstances = true;
        public string nameFormat = "{index:000}_{cmd}_{prefab}"; // 예: 001_F_Straight

        Transform lastOut;
        int index;

        void Awake() { if (buildOnAwake) Build(); }

        [ContextMenu("Build Now")]
        public void Build()
        {
            if (clearBeforeBuild)
            {
                for (int i = transform.childCount - 1; i >= 0; i--)
                {
                    if (Application.isPlaying) Destroy(transform.GetChild(i).gameObject);
                    else DestroyImmediate(transform.GetChild(i).gameObject);
                }
            }
            lastOut = null; index = 0;

            for (int i = 0; i < layout.Length; i++)
            {
                char c = layout[i];
                CorridorModule prefab = c switch
                {
                    'F' => straight,
                    'L' => turnL,
                    'R' => turnR,
                    'D' => (door ? door : straight),
                    'E' => deadEnd,
                    _ => null
                };
                if (!prefab) continue;

                var placed = Place(prefab);
                if (!placed) continue;

                // 자동 네이밍
                if (autoNameInstances)
                {
                    placed.name = nameFormat
                        .Replace("{index:000}", index.ToString("000"))
                        .Replace("{cmd}", c.ToString())
                        .Replace("{prefab}", prefab.name);

                    if (placed.socketIn) placed.socketIn.name = $"Socket_In_{index:000}";
                    if (placed.socketOut) placed.socketOut.name = $"Socket_Out_{index:000}";
                }

                // 이음새 1~2mm 겹치기
                placed.transform.position -= placed.transform.forward * Mathf.Max(0f, joinBias);

                lastOut = placed.socketOut;
                index++;
                if (c == 'E') break;
            }
        }

        CorridorModule Place(CorridorModule prefab)
        {
            var go = Instantiate(prefab.gameObject, transform);
            var cm = go.GetComponent<CorridorModule>();
            var t = cm.transform;

            if (lastOut == null)
            {
                t.SetPositionAndRotation(transform.position, transform.rotation);
            }
            else
            {
                t.SetPositionAndRotation(lastOut.position, lastOut.rotation);
                if (cm.socketIn)
                {
                    t.rotation *= Quaternion.Inverse(cm.socketIn.localRotation);
                    t.position -= t.TransformVector(cm.socketIn.localPosition);
                }
            }
            t.localScale = Vector3.one;
            t.gameObject.isStatic = true;
            return cm;
        }
    }
