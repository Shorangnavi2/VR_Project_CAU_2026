using UnityEngine;

[ExecuteInEditMode]
public class STAIN : MonoBehaviour
{
    [Header("필수 연결")]
    [SerializeField] Material Mat;        // 숨겨진 글자 머티리얼
    [SerializeField] Light SpotLight;     // 손전등 스팟 라이트 (색상 변경 안 함)
    [SerializeField] Transform GlassPane; // 빛을 통과시킬 유리판 오브젝트

    void Update()
    {
        if (Mat != null && SpotLight != null)
        {
            Mat.SetVector("_LightPos", SpotLight.transform.position);
            Mat.SetVector("_LightDir", SpotLight.transform.forward);
            Mat.SetFloat("_LightAngle", SpotLight.spotAngle);

            if (GlassPane != null)
            {
                // 유리판의 월드 변환 행렬(위치, 회전, 크기)과 색상을 셰이더로 전송
                Mat.SetMatrix("_GlassWorldToLocal", GlassPane.worldToLocalMatrix);
                Renderer r = GlassPane.GetComponent<Renderer>();
                if (r != null && r.sharedMaterial != null)
                {
                    Mat.SetColor("_GlassColor", r.sharedMaterial.color);
                }
            }
        }
    }
}