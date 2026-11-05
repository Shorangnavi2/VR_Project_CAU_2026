using UnityEngine;

public enum HighlightState { None, Hover, Valid, Invalid }

public class Highlightable : MonoBehaviour
{
    [Tooltip("켜고 끌 표시물 (예: 자리의 반투명 상자). 비워두면 자기 모델에 색을 입힘")]
    [SerializeField] GameObject highlightVisual;
    [SerializeField] Color hoverColor   = new(1f, 0.9f, 0.3f);
    [SerializeField] Color validColor   = new(0.3f, 1f, 0.4f);
    [SerializeField] Color invalidColor = new(1f, 0.3f, 0.3f);
    [SerializeField, Range(0f, 1f)] float tintStrength = 0.5f;

    static readonly int ColorId     = Shader.PropertyToID("_Color");      // Built-in Standard
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");  // URP Lit (나중 대비)

    Renderer[] renderers;
    Color[] originals;
    MaterialPropertyBlock block;
    HighlightState state = HighlightState.None;

    void Awake()
    {
        block = new MaterialPropertyBlock();
        if (highlightVisual != null) highlightVisual.SetActive(false);
        renderers = highlightVisual != null
            ? highlightVisual.GetComponentsInChildren<Renderer>(true)
            : GetComponentsInChildren<Renderer>(true);
        originals = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            originals[i] = ReadColor(renderers[i].sharedMaterial);
    }

    static Color ReadColor(Material m)
    {
        if (m == null) return Color.white;
        if (m.HasProperty(BaseColorId)) return m.GetColor(BaseColorId);
        if (m.HasProperty(ColorId)) return m.GetColor(ColorId);
        return Color.white;
    }

    public void SetState(HighlightState newState)
    {
        if (newState == state || renderers == null) return;
        state = newState;
        if (highlightVisual != null) highlightVisual.SetActive(state != HighlightState.None);

        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (r == null) continue;
            if (state == HighlightState.None) { r.SetPropertyBlock(null); continue; }

            Color target = state switch
            {
                HighlightState.Valid   => validColor,
                HighlightState.Invalid => invalidColor,
                _                      => hoverColor,
            };
            Color c = highlightVisual != null
                ? new Color(target.r, target.g, target.b, originals[i].a)   // 표시물: 색만 바꾸고 투명도 유지
                : Color.Lerp(originals[i], target, tintStrength);          // 모델: 원래 색에 섞기

            r.GetPropertyBlock(block);
            block.SetColor(ColorId, c);
            block.SetColor(BaseColorId, c);
            r.SetPropertyBlock(block);
        }
    }

    void OnDisable() => SetState(HighlightState.None);   // 인벤토리로 들어가 숨겨질 때 원상복구
}
