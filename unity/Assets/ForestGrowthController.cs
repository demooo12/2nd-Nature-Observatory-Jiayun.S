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
    [Tooltip("把你截图里的 PT_Skybox_mat 材质球拖到这里")]
    public Material skyboxMaterial;
    [Tooltip("天空旋转速度，数值越大转得越快")]
    public float skyboxRotationSpeed = 1.5f;
    
    [Tooltip("低网速：天空明亮灰色 (对应十六进制 #939393)")]
    public Color normalSkyTint = new Color(147f / 255f, 147f / 255f, 147f / 255f, 1f);
    
    [Tooltip("高网速：过载压抑深灰 (对应十六进制 #5E5E5E)")]
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
    private Color initialSkyTint = Color.gray; // 记录天空盒初始色彩

    void Start()
    {
        if (targetTerrain == null) targetTerrain = Terrain.activeTerrain;
        if (targetTerrain != null) terrainData = targetTerrain.terrainData;

        // 记录天空盒的初始状态，方便退出时复原资产
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
        
        if (Application.isPlaying) 
        {
            ForceRefresh();
        }
    }

    void Update()
    {
        if (saveCurrentForestAsMax)
        {
            if (terrainData != null) 
            {
                maxTreesBackup = terrainData.treeInstances;
                Debug.Log("【Second Nature】满级森林已成功永久备份！");
            }
            saveCurrentForestAsMax = false; 
        }

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

        if (Mathf.Abs(knobValue - lastKnobValue) > 0.005f)
        {
            ForceRefresh();
        }

        if (Application.isPlaying)
        {
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

    // 视觉核心同步更新
    void UpdateVisuals()
    {
        // 1. 环境光变色 (绿 -> 红)
        if (directionalLight != null)
            directionalLight.color = Color.Lerp(normalColor, overloadColor, knobValue);

        // 2. 树叶材质 HDR 变色 (绿 -> 红)
        if (treeLeavesMaterial != null)
            treeLeavesMaterial.SetColor("_TopColor", Color.Lerp(normalTopColor, overloadTopColor, knobValue));

        // 3. 天空盒 Tint 变色 (浅灰 #939393 -> 深灰 #5E5E5E) (NEW)
        if (skyboxMaterial != null && skyboxMaterial.HasProperty("_Tint"))
        {
            skyboxMaterial.SetColor("_Tint", Color.Lerp(normalSkyTint, overloadSkyTint, knobValue));
        }
    }

    void OnApplicationQuit() { RestoreAssets(); }
    void OnDisable() { RestoreAssets(); }

    void RestoreAssets()
    {
        if (terrainData != null && maxTreesBackup != null && maxTreesBackup.Length > 0)
        {
            terrainData.treeInstances = maxTreesBackup;
        }

        if (stream != null && stream.IsOpen) stream.Close();

        if (treeLeavesMaterial != null)
            treeLeavesMaterial.SetColor("_TopColor", normalTopColor);

        // 彻底还原天空盒的所有初始状态，防止资产永久受损
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