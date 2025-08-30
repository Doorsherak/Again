using UnityEngine;

public class Segment : MonoBehaviour
{
    public Transform entry; // 입구
    public Transform exit;  // 출구

    // 프리팹 안에서 Entry/Exit를 자동으로 찾아 채워줌
    void OnValidate()
    {
        if (!entry) entry = transform.Find("Entry");
        if (!exit) exit = transform.Find("Exit");
    }
}
