using UnityEngine;

public class FloatingDebris : MonoBehaviour
{
    [Header("위아래 부유 (Vertical Floating)")]
    [Tooltip("위아래로 움직이는 높이 범위")]
    public float floatAmplitude = 0.2f; // 움직이는 높이 (0.1 ~ 0.3 추천)
    [Tooltip("위아래 움직임의 속도")]
    public float floatSpeed = 1.0f;     // 위아래 속도

    [Header("좌우 맴돌기 (Horizontal Sway) - 선택")]
    [Tooltip("좌우로 미세하게 맴도는 범위")]
    public float swayAmplitude = 0.1f;  // 좌우 맴도는 범위
    [Tooltip("좌우 맴도는 속도")]
    public float swaySpeed = 0.7f;      // 좌우 속도

    [Header("회전 연출 (Rotation) - 선택")]
    [Tooltip("잔해가 물속에서 천천히 회전할 축, 속도")]
    public Vector3 rotateSpeed = new Vector3(0.5f, 1.0f, 0.2f); // 아주 천천히 회전

    private Vector3 startPosition;
    private float randomOffset;

    void Start()
    {
        // 시작 위치 저장
        startPosition = transform.position;

        // 여러 잔해가 똑같이 춤추지 않도록 무작위 오프셋(시간 차이) 부여
        randomOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        float time = Time.time + randomOffset;

        // 1. 위아래 삼각함수(Sin) 운동
        float newY = Mathf.Sin(time * floatSpeed) * floatAmplitude;

        // 2. 좌우/앞뒤 맴돌기 삼각함수(Cos/Sin) 운동
        float newX = Mathf.Cos(time * swaySpeed) * swayAmplitude;
        float newZ = Mathf.Sin(time * swaySpeed * 0.8f) * (swayAmplitude * 0.5f);

        // 오프셋 적용된 위치 업데이트
        transform.position = startPosition + new Vector3(newX, newY, newZ);

        // 3. 아주 미세하고 천천히 회전
        transform.Rotate(rotateSpeed * Time.deltaTime);
    }
}