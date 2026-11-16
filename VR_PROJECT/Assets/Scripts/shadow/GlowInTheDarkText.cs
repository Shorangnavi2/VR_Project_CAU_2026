using UnityEngine;
using TMPro;

public class GlowInTheDarkText : MonoBehaviour
{
    [Header("연결 설정")]
    public Light roomLight; // 방의 메인 조명 (이 조명이 꺼지면 글자가 보임)
    public TMP_Text hiddenText; // 숨겨진 글자 (TextMeshPro)

    [Header("세부 설정")]
    public float lightThreshold = 0.1f; // 이 밝기 수치 이하로 떨어지면 암전으로 판정
    public float fadeSpeed = 3f; // 글자가 나타나고 사라지는 부드러운 속도

    void Update()
    {
        if (hiddenText == null) return;

        // 1. 방 조명이 꺼져 있거나, 밝기(Intensity)가 임계치 이하인지 수학적으로 판정
        bool isDark = (roomLight == null || !roomLight.gameObject.activeInHierarchy || roomLight.intensity <= lightThreshold);

        // 2. 목표 투명도 설정 (어두우면 1, 밝으면 0)
        float targetAlpha = isDark ? 1f : 0f;

        // 3. 선형 보간(Lerp)을 활용해 야광 도료처럼 부드럽게 나타나고 사라지는 연출
        Color currentColor = hiddenText.color;
        currentColor.a = Mathf.Lerp(currentColor.a, targetAlpha, Time.deltaTime * fadeSpeed);
        hiddenText.color = currentColor;
    }
}