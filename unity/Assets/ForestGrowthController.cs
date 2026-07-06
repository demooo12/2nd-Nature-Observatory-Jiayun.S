using UnityEngine;
using System.IO.Ports;
using System;

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
            stream = new SerialPort(portName, baudRate);
            stream.ReadTimeout = 1; 
            try {
                stream.Open();
            }
            catch (Exception) {
                useHardware = false; 
            }
        }
        
        // 【安全修正】只有在真正运行游戏时，才在初始化时刷新地形
        if (Application.isPlaying) 
        {
            ForceRefresh();
        }
    }

    void Update()
    {
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
                        float tempValue = Mathf.Clamp01((float)parsedValue / 1023f);
                        if (Mathf.Abs(tempValue - knobValue) > 0.01f) knobValue = tempValue;
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