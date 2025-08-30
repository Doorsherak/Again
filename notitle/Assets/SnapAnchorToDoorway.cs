using UnityEngine;

public class SnapAnchorToDoorway : MonoBehaviour
{
    public Transform Entry;  // Entry ��ü
    public Transform Exit;   // Exit ��ü

    void Start()
    {
        // Entry�� ��ġ�� �ٴڿ� �°� ����
        Vector3 entryPosition = Entry.position;
        entryPosition.y = 0;  // Y ��ǥ�� 0���� ����
        Entry.position = entryPosition;

        // Exit�� ��ġ�� �ٴڿ� �°� ����
        Vector3 exitPosition = Exit.position;
        exitPosition.y = 0;  // Y ��ǥ�� 0���� ����
        Exit.position = exitPosition;
    }
}
