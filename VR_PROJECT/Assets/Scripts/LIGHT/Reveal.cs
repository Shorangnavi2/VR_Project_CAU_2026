using UnityEngine;

[ExecuteInEditMode]
public class Reveal : MonoBehaviour
{
    [SerializeField] Material Mat;
    [SerializeField] Light SpotLight;

    void Update()
    {
        // 에러 방지를 위해 두 빈칸이 모두 채워졌을 때만 작동
        if (Mat != null && SpotLight != null)
        {
            Mat.SetVector("_LightPos", SpotLight.transform.position);
            Mat.SetVector("_LightDir", SpotLight.transform.forward);
            Mat.SetFloat("_LightAngle", SpotLight.spotAngle);
        }
    }
}