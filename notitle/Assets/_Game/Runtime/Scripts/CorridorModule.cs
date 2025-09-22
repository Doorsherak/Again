using UnityEngine;

public class CorridorModule : MonoBehaviour
{
    [Header("Sockets (local)")]
    public Transform socketIn;   // local: pos (0,0,0), rot (0,0,0)
    public Transform socketOut;  // local: pos (0,0,length), rot: Straight=0 / L=-90 / R=+90
    [Header("Standard cell length (m)")]
    public float length = 4f;

    private void OnDrawGizmos()
    {
        // 입구(빨강), 출구(초록) 기준면을 그려서 한눈에 확인
        Gizmos.matrix = transform.localToWorldMatrix;
        float w = 3.0f, h = 2.7f;

        void DrawPlane(float z, Color c)
        {
            Gizmos.color = c;
            Vector3 a = new(-w / 2f, 0f, z), b = new(w / 2f, 0f, z);
            Vector3 c1 = new(w / 2f, h, z), d = new(-w / 2f, h, z);
            Gizmos.DrawLine(a, b); Gizmos.DrawLine(b, c1); Gizmos.DrawLine(c1, d); Gizmos.DrawLine(d, a);
        }

        DrawPlane(0f, Color.red);
        DrawPlane(length, Color.green);

        if (socketOut)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(socketOut.position, socketOut.forward * 0.6f);
        }
    }
}
