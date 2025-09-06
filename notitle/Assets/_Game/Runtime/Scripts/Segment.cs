using UnityEngine;

[DisallowMultipleComponent]
public class Segment : MonoBehaviour
{
    [Header("Connectors")]
    public Transform entry;
    public Transform exit;

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (entry) { Gizmos.color = Color.cyan; Gizmos.DrawSphere(entry.position, 0.1f); Gizmos.DrawRay(entry.position, entry.forward * 0.5f); }
        if (exit) { Gizmos.color = Color.magenta; Gizmos.DrawSphere(exit.position, 0.1f); Gizmos.DrawRay(exit.position, exit.forward * 0.5f); }
    }
#endif
}
