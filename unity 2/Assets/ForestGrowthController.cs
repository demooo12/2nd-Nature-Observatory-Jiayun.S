using UnityEngine;
using System.IO.Ports;
using System;

// 【核心大招】告诉 Unity，这个脚本不管在运行还是在编辑制作时，都要一直执行 Update！
[ExecuteAlways]
public class ForestGrowthController : MonoBehaviour
{
    [Header("绑定你的地形")]
    public Terrain targetTerrain;

    [Header("控制模式切换")]
    [Tooltip("只有在游戏试运行（Play）且勾选时才会读取硬件。在日常编辑模式下会自动失效，防止卡死。")]
    public bool useHardware = true;

    [Header("串口设置")]
    public string portName = "COM3"; 
    public int baudRate = 9600;

    [Header("核心控制滑块 (随时随地可用)")]
    [Range(0f, 1f)]
    public float knobValue = 0f; 

    private SerialPort stream;
    private TerrainData terrainData;
    private TreeInstance[] maxTreesBackup;

    private float lastKnobValue = -1f;
    private float lastUpdateTime = 0f;
    private const float UPDATE_INTERVAL = 0.05f; 

    void Start()
    {
        if (targetTerrain == null)
            targetTerrain = Terrain.activeTerrain;

        if (targetTerrain != null)
            terrainData = targetTerrain.terrainData;

        // 备份你在编辑器里刷满的森林资产
        if (terrainData != null)
            maxTreesBackup = terrainData.treeInstances;

        // 【安全机制】只有真正点 Play 运行游戏时，才去碰 Arduino 硬件，平时做游戏时不干扰串口
        if (Application.isPlaying && useHardware)
        {
            stream = new SerialPort(portName, baudRate);
            stream.ReadTimeout = 1; 
            try {
                stream.Open();
                Debug.Log("【硬件模式】成功连接到 Arduino！");
            }
            catch (Exception e) {
                Debug.LogWarning("未检测到硬件，已降级为纯面板控制: " + e.Message);
                useHardware = false; 
            }
        }
        
        UpdateForestDensity();
    }

    void Update()
    {
        // 轨道一：只有在【点Play试运行】状态下，才去读硬件数据
        if (Application.isPlaying && useHardware && stream != null && stream.IsOpen)
        {
            bool hasNewData = false;
            string latestRawValue = "";

            while (stream.BytesToRead > 0)
            {
                try {
                    latestRawValue = stream.ReadLine();
                    hasNewData = true; 
                }
                catch (Exception) { break; }
            }

            if (hasNewData && !string.IsNullOrEmpty(latestRawValue))
            {
                try {
                    int parsedValue = int.Parse(latestRawValue);
                    float tempValue = Mathf.Clamp01((float)parsedValue / 1023f);
                    if (Mathf.Abs(tempValue - knobValue) > 0.01f)
                    {
                        knobValue = tempValue;
                    }
                }
                catch (Exception) { }
            }
        }

        // 轨道二：无论游戏有没有运行，只要滑块动了，就立刻刷新！
        // 在编辑模式下，为了让你拖动更跟手，我们去掉了时间间隔限制
        if (Mathf.Abs(knobValue - lastKnobValue) > 0.005f)
        {
            // 在编辑模式下，如果中途 maxTreesBackup 丢了，重新抓一次
            if (maxTreesBackup == null || maxTreesBackup.Length == 0)
            {
                if (terrainData != null) maxTreesBackup = terrainData.treeInstances;
            }

            // 如果游戏是在编辑模式（没有点Play），直接高频刷新
            if (!Application.isPlaying)
            {
                UpdateForestDensity();
                lastKnobValue = knobValue;
            }
            // 如果游戏是在点Play运行模式，走降频优化逻辑
            else if ((Time.time - lastUpdateTime) > UPDATE_INTERVAL)
            {
                UpdateForestDensity();
                lastKnobValue = knobValue;
                lastUpdateTime = Time.time;
            }
        }
    }

    void UpdateForestDensity()
    {
        if (maxTreesBackup == null || terrainData == null || maxTreesBackup.Length == 0) return;

        int targetTreeCount = Mathf.RoundToInt(maxTreesBackup.Length * knobValue);
        TreeInstance[] currentTrees = new TreeInstance[targetTreeCount];

        for (int i = 0; i < targetTreeCount; i++)
        {
            currentTrees[i] = maxTreesBackup[i];
        }

        terrainData.treeInstances = currentTrees;
    }

    void OnApplicationQuit()
    {
        // 游戏彻底关闭时，确保把原始满树林还给地形
        RestoreTrees();
    }

    // 额外增加一个安全机制：如果脚本被意外禁用，也要还原树木
    void OnDisable()
    {
        RestoreTrees();
    }

    void RestoreTrees()
    {
        if (terrainData != null && maxTreesBackup != null && maxTreesBackup.Length > 0)
        {
            terrainData.treeInstances = maxTreesBackup;
        }
        if (stream != null && stream.IsOpen)
        {
            stream.Close();
        }
    }
}