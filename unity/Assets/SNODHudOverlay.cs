using UnityEngine;

// SNOD · LIVE —— 右上角实时数据角标 (HUD overlay)
// 读取 ForestGrowthController.knobValue (0..1) 驱动一个虚构机构的直播监控界面。
// 用法：把本脚本挂在场景里任意一个常驻物体上（比如挂在 ForestGrowthController 同一个物体上即可），
//      然后在 Inspector 里把 "Forest Controller" 拖进去。可选：拖一个等宽字体进 "Monospace Font"。
public class SNODHudOverlay : MonoBehaviour
{
    [Header("数据来源")]
    [Tooltip("拖入场景里的 ForestGrowthController，用它的 knobValue 驱动 HUD。")]
    public ForestGrowthController forestController;

    [Tooltip("没绑定控制器时，用这个手动值预览效果 (0..1)。")]
    [Range(0f, 1f)]
    public float previewKnob = 0.5f;

    [Header("外观")]
    [Tooltip("台标 Logo 图片。拖入 Assets/SNOD_Logo。留空则回退显示 SNO · LIVE 文字。")]
    public Texture2D logo;

    [Tooltip("Logo 高度（像素，会按屏幕高度自动缩放）。宽度按原图比例。")]
    public float logoHeight = 46f;

    [Tooltip("等宽字体（可选）。留空则用系统默认字体。建议拖 Courier / 任意 monospace 字体。")]
    public Font monospaceFont;

    [Tooltip("HUD 基础字号（会按屏幕高度自动缩放）。")]
    public int baseFontSize = 30;

    [Tooltip("整体不透明度。")]
    [Range(0f, 1f)]
    public float opacity = 0.75f;

    [Tooltip("是否显示 FOREST COVERAGE（森林覆盖率）那一行。")]
    public bool showForestCoverage = true;

    [Tooltip("距离屏幕左上角的边距（像素，按屏幕缩放）。")]
    public float margin = 24f;

    [Tooltip("文字主色（会乘上 opacity）。")]
    public Color textColor = new Color(0.85f, 0.95f, 0.9f, 1f);

    [Header("动画")]
    [Tooltip("ACTIVE NODES 数字追赶目标值的速度。越大越快。")]
    public float nodeLerpSpeed = 4f;

    // --- 数值区间 ---
    private const int NODES_MIN = 12;
    private const int NODES_MAX = 847;
    private const float COVERAGE_MIN = 6.4f;    // 森林覆盖率下限 %
    private const float COVERAGE_MAX = 98.6f;   // 森林覆盖率上限 %

    // --- 运行时状态 ---
    private float displayNodes = NODES_MIN;   // 平滑过渡后的当前显示值
    private float displayCoverage = COVERAGE_MIN;
    private bool rising = false;               // 是否正在增长（决定 ▲ 是否显示）
    private float flicker = 1f;                // 闪烁系数
    private float nextFlickerTime = 0f;

    private GUIStyle bodyStyle;
    private GUIStyle titleStyle;

    private float CurrentKnob
    {
        get
        {
            if (forestController != null) return Mathf.Clamp01(forestController.knobValue);
            return Mathf.Clamp01(previewKnob);
        }
    }

    void Update()
    {
        float knob = CurrentKnob;

        // 目标值
        float targetNodes = Mathf.Lerp(NODES_MIN, NODES_MAX, knob);
        float targetCoverage = Mathf.Lerp(COVERAGE_MIN, COVERAGE_MAX, knob);

        // 平滑追赶（用 unscaledDeltaTime，避免 timeScale 影响 HUD）
        float t = 1f - Mathf.Exp(-nodeLerpSpeed * Time.unscaledDeltaTime);
        float prevNodes = displayNodes;
        displayNodes = Mathf.Lerp(displayNodes, targetNodes, t);
        displayCoverage = Mathf.Lerp(displayCoverage, targetCoverage, t);

        // 判定是否在增长（带一点死区，避免抖动误判）
        if (displayNodes - prevNodes > 0.02f) rising = true;
        else if (prevNodes - displayNodes > 0.02f) rising = false;

        // 监控界面的轻微闪烁 / 更新感
        if (Time.unscaledTime >= nextFlickerTime)
        {
            flicker = Random.Range(0.82f, 1f);
            nextFlickerTime = Time.unscaledTime + Random.Range(0.06f, 0.22f);
        }
    }

    void BuildStyles()
    {
        if (bodyStyle == null)
        {
            bodyStyle = new GUIStyle();
            bodyStyle.alignment = TextAnchor.UpperLeft;
            bodyStyle.fontStyle = FontStyle.Bold;
            bodyStyle.richText = true;
        }
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle();
            titleStyle.alignment = TextAnchor.UpperLeft;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.richText = true;
        }

        float scale = Mathf.Max(1f, Screen.height / 1080f);
        int fs = Mathf.RoundToInt(baseFontSize * scale);

        bodyStyle.font = monospaceFont;
        bodyStyle.fontSize = fs;
        titleStyle.font = monospaceFont;
        titleStyle.fontSize = Mathf.RoundToInt(fs * 1.05f);
    }

    void OnGUI()
    {
        BuildStyles();

        float knob = CurrentKnob;
        string resolution = ResolutionTier(knob);
        int nodes = Mathf.RoundToInt(displayNodes);
        string arrow = rising ? " ▲" : " ▼";
        string coverage = displayCoverage.ToString("F1");

        float a = opacity * flicker;
        Color col = new Color(textColor.r, textColor.g, textColor.b, a);
        Color dim = new Color(textColor.r, textColor.g, textColor.b, a * 0.6f);

        float scale = Mathf.Max(1f, Screen.height / 1080f);
        float m = margin * scale;
        float lineH = titleStyle.fontSize * 1.5f;
        float width = 560f * scale;
        float x = m;
        float y = m;

        // 标题：优先画 Logo，没有 Logo 时回退成文字
        if (logo != null)
        {
            float h = logoHeight * scale;
            float w = h * ((float)logo.width / logo.height);
            Color prevGui = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, a);
            GUI.DrawTexture(new Rect(x, y, w, h), logo, ScaleMode.ScaleToFit);
            GUI.color = prevGui;

            // Logo 后面接 · LIVE，垂直居中于 Logo
            titleStyle.normal.textColor = col;
            titleStyle.alignment = TextAnchor.MiddleLeft;
            GUI.Label(new Rect(x + w + 10f * scale, y, width, h), "· LIVE", titleStyle);
            titleStyle.alignment = TextAnchor.UpperLeft;

            y += h + lineH * 0.35f;
        }
        else
        {
            titleStyle.normal.textColor = col;
            GUI.Label(new Rect(x, y, width, lineH), "SNO · LIVE", titleStyle);
            y += lineH * 1.15f;
        }

        // 数据行：标签用暗色，数值用主色，营造监控界面层次
        bodyStyle.normal.textColor = col;
        string hexDim = ColorToHex(dim);
        string hexHot = ColorToHex(col);

        GUI.Label(new Rect(x, y, width, lineH),
            Row(hexDim, "RESOLUTION", hexHot, resolution), bodyStyle);
        y += lineH;

        GUI.Label(new Rect(x, y, width, lineH),
            Row(hexDim, "ACTIVE NODES", hexHot, nodes.ToString() + arrow), bodyStyle);
        y += lineH;

        if (showForestCoverage)
        {
            GUI.Label(new Rect(x, y, width, lineH),
                Row(hexDim, "FOREST COVERAGE", hexHot, coverage + "%" + arrow), bodyStyle);
        }
    }

    private static string Row(string labelHex, string label, string valHex, string val)
    {
        return "<color=" + labelHex + ">" + label + ": </color><color=" + valHex + ">" + val + "</color>";
    }

    private static string ResolutionTier(float knob)
    {
        if (knob < 0.25f) return "360p";
        if (knob < 0.5f) return "720p";
        if (knob < 0.75f) return "1080p";
        return "4K";
    }

    private static string ColorToHex(Color c)
    {
        int r = Mathf.RoundToInt(Mathf.Clamp01(c.r) * 255f);
        int g = Mathf.RoundToInt(Mathf.Clamp01(c.g) * 255f);
        int b = Mathf.RoundToInt(Mathf.Clamp01(c.b) * 255f);
        int a = Mathf.RoundToInt(Mathf.Clamp01(c.a) * 255f);
        return string.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", r, g, b, a);
    }
}
