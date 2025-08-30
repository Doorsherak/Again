using UnityEngine;

public class SnapAnchorToDoorway : MonoBehaviour
{
    public Transform Entry;  // Entry 객체
    public Transform Exit;   // Exit 객체

    void Start()
    {
        // Entry의 위치를 바닥에 맞게 수정
        Vector3 entryPosition = Entry.position;
        entryPosition.y = 0;  // Y 좌표를 0으로 설정
        Entry.position = entryPosition;

        // Exit의 위치를 바닥에 맞게 수정
        Vector3 exitPosition = Exit.position;
        exitPosition.y = 0;  // Y 좌표를 0으로 설정
        Exit.position = exitPosition;
    }
}
