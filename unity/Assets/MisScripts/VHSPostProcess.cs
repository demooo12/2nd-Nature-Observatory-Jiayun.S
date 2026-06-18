using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class VHSPostProcess : MonoBehaviour
{
    public Shader shader;

    [Header("绑定滑块控制器")]
    public ForestGrowthController controller;

    [Header("效果细节（可选手动调整）")]
    [Range(0f, 1f)] public float scanlineSpeed = 0.1f;

    private Material m_Material;
    private Texture2D blackTexture;
    float yScanline, xScanline;

    void OnEnable()
    {
        if (shader == null) return;
        m_Material = new Material(shader);
        m_Material.hideFlags = HideFlags.DontSave;
        blackTexture = new Texture2D(2, 2, TextureFormat.RGB24, false);
        blackTexture.hideFlags = HideFlags.DontSave;
        blackTexture.SetPixels(new Color[] { Color.black, Color.black, Color.black, Color.black });
        blackTexture.Apply();
        m_Material.SetTexture("_VHSTex", blackTexture);
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        // 读取 knobValue，计算 glitch 强度（0.5 开始，1.0 最强）
        float knob = controller != null ? controller.knobValue : 0f;
        float glitchAmount = Mathf.Clamp01((knob - 0.5f) * 2f); // 0.5->0, 1.0->1

        if (glitchAmount <= 0f || m_Material == null || shader == null)
        {
            Graphics.Blit(source, destination);
            return;
        }

        yScanline += Time.deltaTime * 0.01f * (scanlineSpeed * 10f);
        xScanline -= Time.deltaTime * scanlineSpeed * glitchAmount;

        if (yScanline >= 1)
            yScanline = Random.value;

        if (xScanline <= 0 || Random.value < 0.05f * glitchAmount)
            xScanline = Random.value;

        m_Material.SetFloat("_yScanline", yScanline);
        m_Material.SetFloat("_xScanline", xScanline * glitchAmount);
        Graphics.Blit(source, destination, m_Material);
    }

    void OnDisable()
    {
        if (m_Material != null) DestroyImmediate(m_Material);
        if (blackTexture != null) DestroyImmediate(blackTexture);
        m_Material = null;
        blackTexture = null;
    }
}
