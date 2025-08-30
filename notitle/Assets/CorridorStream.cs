using UnityEngine;

public class CorridorStream : MonoBehaviour
{
    public Transform entry;            // 시작점
    public Transform exit;             // 종료점
    public GameObject segmentPrefab;   // 세그먼트 프리팹
    public int numberOfSegments = 10;  // 세그먼트 개수
    public float corridorLength = 50f; // 전체 길이 (Entry와 Exit 간 거리)

    void Start()
    {
        // 세그먼트 간격을 맞추기 위해 계산된 길이를 기반으로 세그먼트를 생성합니다.
        AdjustCorridorSegments();
    }

    void AdjustCorridorSegments()
    {
        // Entry와 Exit의 위치를 기준으로 벡터 계산
        Vector3 direction = exit.position - entry.position;  // 두 점을 잇는 벡터
        float segmentLength = corridorLength / numberOfSegments;  // 각 세그먼트의 길이
        Vector3 segmentDirection = direction.normalized;  // 방향 벡터를 정규화

        // 세그먼트 생성 및 위치 조정
        for (int i = 0; i < numberOfSegments; i++)
        {
            // 각 세그먼트의 위치 계산
            Vector3 segmentPosition = entry.position + segmentDirection * i * segmentLength;

            // 세그먼트의 Y값을 Entry의 Y값으로 설정하여 높이를 일관되게 유지
            segmentPosition.y = entry.position.y;

            // 세그먼트를 해당 위치에 인스턴스화
            Instantiate(segmentPrefab, segmentPosition, Quaternion.identity);
        }

        // Exit의 위치를 정확하게 설정하기 위해 전체 길이에 맞게 조정
        Vector3 finalExitPosition = entry.position + direction.normalized * corridorLength;
        finalExitPosition.y = entry.position.y;  // Exit의 Y값을 Entry의 Y값으로 맞추기
        exit.position = finalExitPosition;  // Exit 위치 업데이트
    }
}
