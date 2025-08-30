using UnityEngine;

public class Segment : MonoBehaviour
{
    public Transform entry; // �Ա�
    public Transform exit;  // �ⱸ

    // ������ �ȿ��� Entry/Exit�� �ڵ����� ã�� ä����
    void OnValidate()
    {
        if (!entry) entry = transform.Find("Entry");
        if (!exit) exit = transform.Find("Exit");
    }
}
