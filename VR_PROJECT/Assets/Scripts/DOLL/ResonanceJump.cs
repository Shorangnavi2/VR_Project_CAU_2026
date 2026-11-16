using UnityEngine;
using System.Collections;

public class ResonanceJump : MonoBehaviour
{
    [Header("점프 설정")]
    public float jumpHeight = 0.5f; // 뛰어오르는 높이 (미터 단위)
    public float jumpDuration = 1.0f; // 공중에 떠 있는 체공 시간 (초)

    private Vector3 originalPosition;
    private bool isJumping = false;

    void Start()
    {
        // 게임 시작 시 물체의 원래 위치를 기억해둠
        originalPosition = transform.position;
    }

    // 굽쇠가 작동했을 때 외부에서 호출될 함수
    public void TriggerJump()
    {
        // 이미 뛰고 있는 중이 아닐 때만 점프 실행
        if (!isJumping)
        {
            StartCoroutine(JumpRoutine());
        }
    }

    private IEnumerator JumpRoutine()
    {
        isJumping = true;
        float elapsed = 0f;

        // 물체가 잡혀서 이동했을 수도 있으므로, 점프 직전의 현재 위치를 기준점으로 갱신
        Vector3 startPos = transform.position;

        while (elapsed < jumpDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / jumpDuration; // 0.0 에서 1.0 사이의 진행률

            // 사인(Sin) 그래프 곡선을 이용해 위로 솟아올랐다 떨어지는 부드러운 포물선 높이 계산
            float height = Mathf.Sin(t * Mathf.PI) * jumpHeight;

            // 물체의 위치를 실시간으로 위아래로 움직임
            transform.position = startPos + Vector3.up * height;

            yield return null;
        }

        // 점프가 끝나면 오차가 생기지 않도록 원래 위치로 완벽하게 고정
        transform.position = startPos;
        isJumping = false;
    }
}