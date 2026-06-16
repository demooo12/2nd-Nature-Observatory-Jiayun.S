using UnityEngine;
using System.IO.Ports;
using System;

[ExecuteAlways]
public class ForestGrowthController : MonoBehaviour
{
    [Header("地形")]
    public Terrain targetTerrain;

    [Header("备份森林状态")]
    public bool saveCurrentForestAsMax = false;

    [Header("旋钮控制环境光")]
    public Light directionalLight; 
    public Color normalColor = new Color(0.584f, 0.576f, 0.192f); 
    public Color overloadColor = new Color(0.584f, 0.192f, 0.200f);

    [Header("光影旋转")]
    [Tooltip("是否开启环境光随时间自动旋转")]
    public bool enableLightRotation = true;
    [Tooltip("光线旋转的速度。Y轴控制影子水平移动，X轴控制日夜高低。建议先只给 Y 轴一个数值测试。")]
    public Vector3 lightRotationSpeed = new Vector3(0f, 5f, 0f); 

    [Header("是否Arduino")]
    public bool useHardware = true;

    [Header("串口设置")]
    public string portName = "COM3"; 
    public int baudRate = 9600;

    [Header("手动控制调试")]
    [Range(0f, 1f)]
    public float knobValue = 0f; 

    [SerializeField, HideInInspector]
    private TreeInstance[] maxTreesBackup;

    private SerialPort stream;
    private TerrainData terrainData;
    private float lastKnobValue = -1f;

    void Start()
    {
        if (targetTerrain == null) targetTerrain = Terrain.activeTerrain;
        if (targetTerrain != null) terrainData = targetTerrain.terrainData;

        // 如果开启了硬件且在Play模式，尝试连接
        if (Application.isPlaying && useHardware)
        {
            stream = new SerialPort(portName, baudRate);
            stream.ReadTimeout = 1; 
            try {
                stream.Open();
            }
            catch (Exception) {
                // 没插Arduino时自动降级
                useHardware = false; 
            }
        }
        
        // 确保游戏一运行，立刻根据当前滑块的值刷新一次地形
        if (Application.isPlaying) 
        {
            ForceRefresh();
        }
    }

    void Update()
    {
        // 1. 一键备份逻辑
        if (saveCurrentForestAsMax)
        {
            if (terrainData != null) 
            {
                maxTreesBackup = terrainData.treeInstances;
                Debug.Log("【Second Nature】满级森林已成功永久备份！");
            }
            saveCurrentForestAsMax = false; 
        }

        // 2. Arduino 硬件接收逻辑
        if (Application.isPlaying && useHardware && stream != null && stream.IsOpen)
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
                    float tempValue = Mathf.Clamp01((float)parsedValue / 1023f);
                    if (Mathf.Abs(tempValue - knobValue) > 0.01f) knobValue = tempValue;
                } catch (Exception) { }
            }
        }

        // 3. 核心视觉刷新逻辑 (地形和光线颜色)
        if (Mathf.Abs(knobValue - lastKnobValue) > 0.005f)
        {
            ForceRefresh();
        }

        // 4. 【新增】光影动态旋转逻辑
        // 仅在点 Play 运行游戏时旋转，防止平时在编辑器里做场景时灯光一直自己乱转
        if (Application.isPlaying && enableLightRotation && directionalLight != null)
        {
            // 使用 Time.deltaTime 确保旋转极其丝滑，不受帧率掉帧影响
            // Space.World 确保它是围绕世界坐标轴自转，效果最自然
            directionalLight.transform.Rotate(lightRotationSpeed * Time.deltaTime, Space.World);
        }
    }

    void ForceRefresh()
    {
        if (maxTreesBackup == null || maxTreesBackup.Length == 0)
        {
            if (terrainData != null && terrainData.treeInstances.Length > 0)
            {
                maxTreesBackup = terrainData.treeInstances;
            }
        }

        UpdateForestDensity();
        UpdateLightingColor();
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

    void UpdateLightingColor()
    {
        if (directionalLight != null)
            directionalLight.color = Color.Lerp(normalColor, overloadColor, knobValue);
    }

    void OnApplicationQuit() { RestoreTrees(); }
    void OnDisable() { RestoreTrees(); }

    void RestoreTrees()
    {
        if (terrainData != null && maxTreesBackup != null && maxTreesBackup.Length > 0)
        {
            terrainData.treeInstances = maxTreesBackup;
        }
        if (stream != null && stream.IsOpen) stream.Close();
    }
}