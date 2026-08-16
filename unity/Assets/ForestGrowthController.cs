using UnityEngine;
using System.IO;
using System.IO.Ports;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

[ExecuteAlways]
public class ForestGrowthController : MonoBehaviour
{
    [Header("绑定你的地形")]
    public Terrain targetTerrain;

    [Header("【核心保护】一键备份满级森林")]
    public bool saveCurrentForestAsMax = false;

    [Header("天空盒动态控制")]
    public Material skyboxMaterial;
    public float skyboxRotationSpeed = 1.5f;
    public Color normalSkyTint = new Color(147f / 255f, 147f / 255f, 147f / 255f, 1f);
    public Color overloadSkyTint = new Color(94f / 255f, 94f / 255f, 94f / 255f, 1f);

    [Header("绑定你的环境光")]
    public Light directionalLight; 
    public Color normalColor = new Color(0.584f, 0.576f, 0.192f); 
    public Color overloadColor = new Color(0.584f, 0.192f, 0.200f);

    [Header("光影动态旋转")]
    public bool enableLightRotation = true;
    public Vector3 lightRotationSpeed = new Vector3(0f, 5f, 0f); 

    [Header("树叶材质变色")]
    public Material treeLeavesMaterial;
    [ColorUsage(true, true)]
    public Color normalTopColor = new Color(18f / 255f, 72f / 255f, 42f / 255f, 1f);
    [ColorUsage(true, true)]
    public Color overloadTopColor = new Color(29f / 255f, 7f / 255f, 6f / 255f, 1f);

    [Header("控制模式切换")]
    public bool useHardware = true;

    [Header("串口设置")]
    public string portName = "COM3";
    public int baudRate = 9600;

    [Tooltip("勾选后：进入运行时先弹出端口选择面板，让你现场挑 COM 口（打包后也能用）。")]
    public bool showPortDialogOnStart = true;

    [Header("转发给TD (UDP)")]
    public string tdIP = "127.0.0.1";
    public int tdPort = 7000;

    [Header("核心控制滑块")]
    [Range(0f, 1f)]
    public float knobValue = 0f; 

    [SerializeField, HideInInspector]
    private TreeInstance[] maxTreesBackup;

    private SerialPort stream;
    private TerrainData terrainData;
    private float lastKnobValue = -1f;
    private float initialSkyboxRotation = 0f; 
    private Color initialSkyTint = Color.gray; 

    private UdpClient udpClient;
    private IPEndPoint tdEndPoint;

    // --- 启动端口选择面板状态 ---
    private bool awaitingPort = false;
    private string[] availablePorts = new string[0];
    private string portInput = "";
    private string portStatus = "";
    private Vector2 portScroll = Vector2.zero;

    void Start()
    {
        if (targetTerrain == null) targetTerrain = Terrain.activeTerrain;
        if (targetTerrain != null) terrainData = targetTerrain.terrainData;

        if (skyboxMaterial != null)
        {
            initialSkyboxRotation = skyboxMaterial.GetFloat("_Rotation");
            if (skyboxMaterial.HasProperty("_Tint"))
            {
                initialSkyTint = skyboxMaterial.GetColor("_Tint");
            }
        }

        if (Application.isPlaying && useHardware)
        {
            if (showPortDialogOnStart)
            {
                // 弹出端口选择面板，先不连，等用户挑好再连
                awaitingPort = true;
                portInput = portName;
                RefreshPortList();
            }
            else
            {
                TryOpenPort(portName);
            }
        }

        if (Application.isPlaying)
        {
            udpClient = new UdpClient();
            tdEndPoint = new IPEndPoint(IPAddress.Parse(tdIP), tdPort);
        }
        
        // 【安全修正】只有在真正运行游戏时，才在初始化时刷新地形
        if (Application.isPlaying) 
        {
            ForceRefresh();
        }
    }

    void Update()
    {
        // ==================== 全局快捷键 ====================
        if (Application.isPlaying)
        {
            // ESC 退出程序
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Application.Quit();
            }
            // "." 键切换全屏
            if (Input.GetKeyDown(KeyCode.Period))
            {
                Screen.fullScreen = !Screen.fullScreen;
            }
        }

        // 核心保护：一键备份功能在【编辑模式】和【播放模式】下都可以安全使用
        if (saveCurrentForestAsMax)
        {
            if (terrainData != null) 
            {
                maxTreesBackup = terrainData.treeInstances;
                Debug.Log("【Second Nature】满级森林已成功永久备份！当前树木数量: " + maxTreesBackup.Length);
                
                #if UNITY_EDITOR
                // 强制通知 Unity 资产已改变，确保备份数据被真正写入场景文件中保存
                UnityEditor.EditorUtility.SetDirty(this);
                #endif
            }
            saveCurrentForestAsMax = false; 
        }

        // ==================== 以下所有动态修改逻辑，必须在 Play 模式下才允许运行 ====================
        if (Application.isPlaying)
        {
            // 1. 硬件串口数据读取
            if (useHardware && stream != null && stream.IsOpen)
            {
                bool hasNewData = false;
                string latestRawValue = "";
                while (stream.BytesToRead > 0)
                {
                    try {
                        latestRawValue = stream.ReadLine();
                        hasNewData = true; 
                    } catch (Exception) { break; }
                }

                if (hasNewData && !string.IsNullOrEmpty(latestRawValue))
                {
                    try {
                        int parsedValue = int.Parse(latestRawValue);

                        // 合理性校验：Arduino物理上不可能发出超过0-1023的数值
                        if (parsedValue < 0 || parsedValue > 1023)
                        {
                            // 异常值，丢弃
                        }
                        else
                        {
                            float tempValue = Mathf.Clamp01((float)parsedValue / 1023f);
                            if (Mathf.Abs(tempValue - knobValue) > 0.01f) knobValue = tempValue;

                            SendToTD(parsedValue); // 转发原始数值给TD
                        }
                    } catch (Exception) { }
                }
            }

            // 2. 只有滑块真正变化时，才刷新地形（避免了编辑器加载时 -1f 导致的误刷）
            if (Mathf.Abs(knobValue - lastKnobValue) > 0.005f)
            {
                ForceRefresh();
            }

            // 3. 灯光与天空盒的动态旋转
            if (enableLightRotation && directionalLight != null)
            {
                directionalLight.transform.Rotate(lightRotationSpeed * Time.deltaTime, Space.World);
            }

            if (skyboxMaterial != null)
            {
                float currentRot = skyboxMaterial.GetFloat("_Rotation");
                skyboxMaterial.SetFloat("_Rotation", currentRot + skyboxRotationSpeed * Time.deltaTime);
            }
        }
    }

    void RefreshPortList()
    {
        List<string> ports = new List<string>();

        // 1) 标准枚举（Windows 上够用；Mac 上常常列不全，所以下面再补一遍）
        try
        {
            foreach (string p in SerialPort.GetPortNames())
                if (!string.IsNullOrEmpty(p) && !ports.Contains(p)) ports.Add(p);
        }
        catch (Exception) { }

        // 2) Mac/Linux 直接扫 /dev 目录，抓 cu.* 和 tty.usb*（最可靠）
        try
        {
            if (Directory.Exists("/dev"))
            {
                foreach (string f in Directory.GetFiles("/dev"))
                {
                    string name = Path.GetFileName(f);
                    if (name.StartsWith("cu.") || name.StartsWith("tty.usb") || name.StartsWith("tty.wchusb"))
                    {
                        if (!ports.Contains(f)) ports.Add(f);
                    }
                }
            }
        }
        catch (Exception) { }

        availablePorts = ports.ToArray();
    }

    void TryOpenPort(string port)
    {
        // 若已有连接先关掉，避免重复占用
        if (stream != null && stream.IsOpen) { try { stream.Close(); } catch (Exception) { } }

        try
        {
            stream = new SerialPort(port, baudRate);
            stream.ReadTimeout = 1;
            stream.WriteTimeout = 50;
            stream.DtrEnable = true;   // Mac 上很多 USB 串口需要这两个才能通
            stream.RtsEnable = true;
            stream.Open();
            portName = port;
            useHardware = true;
            awaitingPort = false;
            portStatus = "";
            ForceRefresh();
        }
        catch (Exception e)
        {
            portStatus = "连接失败: " + port + "\n" + e.Message;
        }
    }

    // 启动端口选择面板
    void OnGUI()
    {
        if (!awaitingPort) return;

        float sw = Screen.width, sh = Screen.height;

        // 按屏幕高度缩放字号，保证在大分辨率/全屏下也看得清
        float scale = Mathf.Max(1f, sh / 1080f);
        int fsBody = Mathf.RoundToInt(22 * scale);
        int fsTitle = Mathf.RoundToInt(30 * scale);
        int fsBtn = Mathf.RoundToInt(24 * scale);

        GUIStyle title = new GUIStyle(GUI.skin.label) { fontSize = fsTitle, fontStyle = FontStyle.Bold };
        GUIStyle label = new GUIStyle(GUI.skin.label) { fontSize = fsBody, wordWrap = true };
        GUIStyle btn = new GUIStyle(GUI.skin.button) { fontSize = fsBtn };
        GUIStyle portBtn = new GUIStyle(GUI.skin.button) { fontSize = fsBody, alignment = TextAnchor.MiddleLeft };
        GUIStyle field = new GUIStyle(GUI.skin.textField) { fontSize = fsBody };

        // 面板尺寸：占屏幕大半，居中
        float pw = Mathf.Min(900f * scale, sw * 0.85f);
        float ph = Mathf.Min(700f * scale, sh * 0.85f);
        Rect box = new Rect((sw - pw) * 0.5f, (sh - ph) * 0.5f, pw, ph);

        // 半透明背景遮罩
        GUI.color = new Color(0f, 0f, 0f, 0.7f);
        GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
        GUI.color = new Color(0.12f, 0.12f, 0.13f, 1f);
        GUI.DrawTexture(box, Texture2D.whiteTexture);
        GUI.color = Color.white;

        float pad = 26f * scale;
        GUILayout.BeginArea(new Rect(box.x + pad, box.y + pad, box.width - pad * 2, box.height - pad * 2));

        GUILayout.Label("选择 Arduino 串口", title);
        GUILayout.Space(6 * scale);
        GUILayout.Label("点下面对应的串口即可直接连接（选带 usbmodem / usbserial 的那个）：", label);
        GUILayout.Space(8 * scale);

        // 端口列表（可滚动），点一下直接连接
        float listH = ph - pad * 2 - 320 * scale;
        if (listH < 120 * scale) listH = 120 * scale;
        portScroll = GUILayout.BeginScrollView(portScroll, GUILayout.Height(listH));
        if (availablePorts == null || availablePorts.Length == 0)
        {
            GUILayout.Label("  —— 没检测到串口，确认 USB 已插好，然后点“刷新列表” ——", label);
        }
        else
        {
            foreach (string p in availablePorts)
            {
                if (GUILayout.Button(p, portBtn, GUILayout.Height(40 * scale)))
                {
                    portInput = p;
                    TryOpenPort(p.Trim());   // 点端口 = 直接连接
                }
            }
        }
        GUILayout.EndScrollView();

        GUILayout.Space(10 * scale);
        GUILayout.Label("或手动输入端口名：", label);
        portInput = GUILayout.TextField(portInput, field, GUILayout.Height(38 * scale));

        if (!string.IsNullOrEmpty(portStatus))
        {
            GUIStyle err = new GUIStyle(label);
            err.normal.textColor = new Color(1f, 0.55f, 0.45f);
            GUILayout.Label(portStatus, err);
        }

        GUILayout.Space(12 * scale);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("连接并进入", btn, GUILayout.Height(48 * scale)))
        {
            if (!string.IsNullOrEmpty(portInput)) TryOpenPort(portInput.Trim());
        }
        if (GUILayout.Button("刷新列表", btn, GUILayout.Height(48 * scale)))
        {
            RefreshPortList();
        }
        if (GUILayout.Button("跳过(手动模式)", btn, GUILayout.Height(48 * scale)))
        {
            useHardware = false;
            awaitingPort = false;
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(6 * scale);
        GUILayout.Label("提示：ESC 退出程序   ·   “.”键 切换全屏", label);

        GUILayout.EndArea();
    }

    void SendToTD(int value)
    {
        if (udpClient == null) return;
        try
        {
            byte[] data = System.Text.Encoding.UTF8.GetBytes(value.ToString() + "\n");
            udpClient.Send(data, data.Length, tdEndPoint);
        }
        catch (Exception) { }
    }

    void ForceRefresh()
    {
        // 运行时防御性备份
        if (maxTreesBackup == null || maxTreesBackup.Length == 0)
        {
            if (terrainData != null && terrainData.treeInstances.Length > 0)
            {
                maxTreesBackup = terrainData.treeInstances;
            }
        }

        UpdateForestDensity();
        UpdateVisuals();
        lastKnobValue = knobValue;
    }

    void UpdateForestDensity()
    {
        if (maxTreesBackup == null || terrainData == null || maxTreesBackup.Length == 0) return;

        int targetTreeCount = Mathf.RoundToInt(maxTreesBackup.Length * knobValue);
        TreeInstance[] currentTrees = new TreeInstance[targetTreeCount];
        for (int i = 0; i < targetTreeCount; i++) currentTrees[i] = maxTreesBackup[i];

        terrainData.treeInstances = currentTrees;
    }

    void UpdateVisuals()
    {
        if (directionalLight != null)
            directionalLight.color = Color.Lerp(normalColor, overloadColor, knobValue);

        if (treeLeavesMaterial != null)
            treeLeavesMaterial.SetColor("_TopColor", Color.Lerp(normalTopColor, overloadTopColor, knobValue));

        if (skyboxMaterial != null && skyboxMaterial.HasProperty("_Tint"))
        {
            skyboxMaterial.SetColor("_Tint", Color.Lerp(normalSkyTint, overloadSkyTint, knobValue));
        }
    }

    void OnApplicationQuit() { RestoreAssets(); }
    void OnDisable() { RestoreAssets(); }

    void RestoreAssets()
    {
        // 只有在运行结束退出时，才把备份恢复给地形资产，防止污染硬盘实体资产
        if (Application.isPlaying && terrainData != null && maxTreesBackup != null && maxTreesBackup.Length > 0)
        {
            terrainData.treeInstances = maxTreesBackup;
        }

        if (stream != null && stream.IsOpen) stream.Close();
        if (udpClient != null) udpClient.Close();

        if (treeLeavesMaterial != null)
            treeLeavesMaterial.SetColor("_TopColor", normalTopColor);

        if (skyboxMaterial != null)
        {
            skyboxMaterial.SetFloat("_Rotation", initialSkyboxRotation);
            if (skyboxMaterial.HasProperty("_Tint"))
            {
                skyboxMaterial.SetColor("_Tint", initialSkyTint);
            }
        }
    }
}