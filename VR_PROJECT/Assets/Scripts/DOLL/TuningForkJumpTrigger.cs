using UnityEngine;

public class TuningForkJumpTrigger : MonoBehaviour
{
    [Header("물리 설정")]
    public float impactThreshold = 1.5f; // 이 속도 이상으로 세게 쳐야 발동

    [Header("VR 미사용자 및 테스트용 설정")]
    public KeyCode activationKey = KeyCode.Space; // 키보드로 강제 작동할 키

    void Update()
    {
        // 에디터에서 Space 바를 누르면 즉시 발동
        if (Input.GetKeyDown(activationKey))
        {
            ActivateFork();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        float impactForce = collision.relativeVelocity.magnitude;

        // 임계값 이상으로 강하게 부딪혔을 때 작동
        if (impactForce >= impactThreshold)
        {
            ActivateFork();
            Debug.Log($"굽쇠 물리적 작동! 충격량: {impactForce:F2}");
        }
    }

    public void ActivateFork()
    {
        // 씬 내의 튀어올라야 할 모든 정답 물체를 찾아 점프 신호 발송
        ResonanceJump[] clues = FindObjectsOfType<ResonanceJump>();
        foreach (var clue in clues)
        {
            clue.TriggerJump();
        }

        Debug.Log("굽쇠 기믹 발동: 정답 물체들이 공명하여 뛰어오릅니다.");
    }
}