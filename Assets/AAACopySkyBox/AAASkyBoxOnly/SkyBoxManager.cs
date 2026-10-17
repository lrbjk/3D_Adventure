using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Camera))]
public class SkyBoxManager : MonoBehaviour
{
    public ComputeShader ScatteringComputeShader;
    public Shader shader;
    
    //--------------------------------------------------------------------------------------
    [Space(10)]
    [Header("Light")]
    public Light Sun;
    public Color _sunColor;
    public RenderTexture _particleDensityLUT = null;
    public Texture2D _randomVectorsLUT = null;
    public RenderTexture _lightColorTexture;
    public const int LightLUTSize = 128;
    public Texture2D _lightColorTextureTemp;
    [Space(5)]
    [ColorUsage(false, true, 0, 15, 0, 15)]
    public Color[] _directionalLightLUT;
    [ColorUsage(false, true, 0, 10, 0, 10)]
    public Color[] _ambientLightLUT;
    
    //--------------------------------------------------------------------------------------
    [Space(10)]
    [Header("Skybox")]
    public Vector3 _skyboxLUTSize = new Vector3(32, 128, 32);
    public RenderTexture _skyboxLUT;
    
    //--------------------------------------------------------------------------------------
    [Space(10)]
    [Header("Calculate")]
    [Range(1, 64)]
    [ColorUsage(false, true, 0, 10, 0, 10)]
    public Color IncomingLight = new Color(4, 4, 4, 4);
    [Range(0, 10.0f)]
    public float RayleighScatterCoef = 1;
    [Range(0, 10.0f)]
    public float RayleighExtinctionCoef = 1;
    [Range(0, 10.0f)]
    public float MieScatterCoef = 1;
    [Range(0, 10.0f)]
    public float MieExtinctionCoef = 1;
    [Range(0.0f, 0.999f)]
    public float MieG = 0.76f;
    public float DistanceScale = 1;
    [Range(0.5f, 3.0f)]
    public float LightColorIntensity = 1.0f;
    [Range(0.5f, 3.0f)]
    public float AmbientColorIntensity = 1.0f;
    
    //--------------------------------------------------------------------------------------
    private Material _material;
    private Camera _camera;
    private const float AtmosphereHeight = 80000.0f;
    private const float PlanetRadius = 6371000.0f;
    private readonly Vector4 DensityScale = new Vector4(7994.0f, 1200.0f, 0, 0);
    private readonly Vector4 RayleighSct = new Vector4(5.8f, 13.5f, 33.1f, 0.0f) * 0.000001f;
    private readonly Vector4 MieSct = new Vector4(2.0f, 2.0f, 2.0f, 0.0f) * 0.00001f;
    private Vector4[] _FrustumCorners = new Vector4[4];

    void Start()
    {
        if (shader == null)
        {
            shader = Shader.Find("AtmosphereInscatter/Calculate");
            if (shader == null)
                throw new Exception("Critical Error: \"AtmosphereInscatter/Calculate\" shader is missing. Make sure it is included in \"Always Included Shaders\" in ProjectSettings/Graphics.");
        }
        _material = new Material(shader);
        _camera = GetComponent<Camera>();
        
        UpdateMaterialParameters(_material);
        InitialzieRandomVectorsLUT();
        PrecomputeParticleDensity();
        CalculateLightLUTs();
    }
    
    void Update()
    {
        _sunColor = ComputeLightColor();
        UpdateDirectionalLightColor(_sunColor);
        UpdateAmbientLightColor(ComputeAmbientColor());
    }
    void OnEnable() {
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
    }
    void OnDisable() {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
    }
    
    public void OnDestroy()
    {
        Destroy(_material);
    }
    
    private void UpdateMaterialParameters(Material material)
    {
        material.SetFloat("_AtmosphereHeight", AtmosphereHeight);
        material.SetFloat("_PlanetRadius", PlanetRadius);
        material.SetVector("_DensityScaleHeight", DensityScale);
        Vector4 scatteringR = new Vector4(5.8f, 13.5f, 33.1f, 0.0f) * 0.000001f;
        Vector4 scatteringM = new Vector4(2.0f, 2.0f, 2.0f, 0.0f) * 0.00001f;
        material.SetVector("_ScatteringR", RayleighSct * RayleighScatterCoef);
        material.SetVector("_ScatteringM", MieSct * MieScatterCoef);
        material.SetVector("_ExtinctionR", RayleighSct * RayleighExtinctionCoef);
        material.SetVector("_ExtinctionM", MieSct * MieExtinctionCoef);
        material.SetColor("_IncomingLight", IncomingLight);
        material.SetFloat("_MieG", MieG);
        material.SetFloat("_DistanceScale", DistanceScale);
        material.SetColor("_SunColor", _sunColor);

        //---------------------------------------------------
        material.SetVector("_LightDir", new Vector4(Sun.transform.forward.x, Sun.transform.forward.y, Sun.transform.forward.z, 1.0f / (Sun.range * Sun.range)));
        material.SetVector("_LightColor", Sun.color * Sun.intensity);
        material.SetTexture("_ParticleDensityLUT", _particleDensityLUT);
        material.SetTexture("_SkyboxLUT", _skyboxLUT);
    }
    
    private void UpdateCommonComputeShaderParameters(int kernel)
    {
        ScatteringComputeShader.SetTexture(kernel, "_ParticleDensityLUT", _particleDensityLUT);
        ScatteringComputeShader.SetFloat("_AtmosphereHeight", AtmosphereHeight);
        ScatteringComputeShader.SetFloat("_PlanetRadius", PlanetRadius);
        ScatteringComputeShader.SetVector("_DensityScaleHeight", DensityScale);

        ScatteringComputeShader.SetVector("_ScatteringR", RayleighSct * RayleighScatterCoef);
        ScatteringComputeShader.SetVector("_ScatteringM", MieSct * MieScatterCoef);
        ScatteringComputeShader.SetVector("_ExtinctionR", RayleighSct * RayleighExtinctionCoef);
        ScatteringComputeShader.SetVector("_ExtinctionM", MieSct * MieExtinctionCoef);

        ScatteringComputeShader.SetVector("_IncomingLight", IncomingLight);
        ScatteringComputeShader.SetFloat("_MieG", MieG);
    }

    
    /// <summary>
    /// 注册随机贴图
    /// </summary>
    /// <param name="randomVectorsLUT">随机贴图</param>
    /// <param name="randomSeed">随机种子</param>
    private void InitialzieRandomVectorsLUT()
    {
        _randomVectorsLUT = new Texture2D(256, 1, TextureFormat.RGBAHalf, false, true);
        _randomVectorsLUT.name = "RandomVectorsLUT";
        Color[] colors = new Color[256];
        UnityEngine.Random.seed = 1234567890;
        for (int i = 0; i < colors.Length; ++i)
        {
            Vector3 vector = UnityEngine.Random.onUnitSphere;
            colors[i] = new Color(vector.x, vector.y, vector.z, 1);
        }
        _randomVectorsLUT.SetPixels(colors);
        _randomVectorsLUT.Apply();
    }
    
    /// <summary>
    /// 注册粒子密度
    /// </summary>
    private void PrecomputeParticleDensity()
    {
        if (_particleDensityLUT == null)
        {
            _particleDensityLUT = new RenderTexture(1024, 1024, 0, RenderTextureFormat.RGFloat, RenderTextureReadWrite.Linear);
            _particleDensityLUT.name = "_particleDensityLUT";
            _particleDensityLUT.filterMode = FilterMode.Bilinear;
            _particleDensityLUT.Create();
        }
        Texture nullTexture = null;
        Graphics.Blit(nullTexture, _particleDensityLUT, _material, 0);
        _material.SetTexture("_ParticleDensityLUT", _particleDensityLUT);
    }
    
    /// <summary>
    /// 计算光照颜色,计算天空盒颜色
    /// </summary>
    public void CalculateLightLUTs()
    {
        if (_lightColorTexture == null)
        {
            _lightColorTexture = new RenderTexture(LightLUTSize, 1, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            _lightColorTexture.name = "LightColorTexture";
            _lightColorTexture.Create();
        }
        if (_lightColorTextureTemp == null)
        {
            _lightColorTextureTemp = new Texture2D(LightLUTSize, 1, TextureFormat.RGBAHalf, false, true);
            _lightColorTextureTemp.name = "LightColorTextureTemp";
            _lightColorTextureTemp.Apply();
        }

        // ambient LUT
        Texture nullTexture = null;
        _material.SetTexture("_RandomVectors", _randomVectorsLUT);
        Graphics.Blit(nullTexture, _lightColorTexture, _material, 1);
        _lightColorTextureTemp.ReadPixels(new Rect(0, 0, LightLUTSize, 1), 0, 0);
        _ambientLightLUT = _lightColorTextureTemp.GetPixels(0, 0, LightLUTSize, 1);
        // directional LUT
        Graphics.Blit(nullTexture, _lightColorTexture, _material, 2);
        _lightColorTextureTemp.ReadPixels(new Rect(0, 0, LightLUTSize, 1), 0, 0);
        _directionalLightLUT = _lightColorTextureTemp.GetPixels(0, 0, LightLUTSize, 1);
        
        PrecomputeSkyboxLUT();
    }
    
    private void PrecomputeSkyboxLUT()
    {
        if (_skyboxLUT == null)
        {
            _skyboxLUT = new RenderTexture((int)_skyboxLUTSize.x, (int)_skyboxLUTSize.y, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            _skyboxLUT.volumeDepth = (int)_skyboxLUTSize.z;
            _skyboxLUT.isVolume = true;
            _skyboxLUT.enableRandomWrite = true;
            _skyboxLUT.name = "SkyboxLUT";
            _skyboxLUT.Create();
        }
        int kernel = ScatteringComputeShader.FindKernel("SkyboxLUT");
        ScatteringComputeShader.SetTexture(kernel, "_SkyboxLUT", _skyboxLUT);
        UpdateCommonComputeShaderParameters(kernel);
        ScatteringComputeShader.Dispatch(kernel, (int)_skyboxLUTSize.x, (int)_skyboxLUTSize.y, (int)_skyboxLUTSize.z);
    }
    
    private Color ComputeLightColor()
    {
        float cosAngle = Vector3.Dot(Vector3.up, -Sun.transform.forward);
        float u = cosAngle * 0.5f + 0.5f;// * 0.5f + 0.5f;

        u = u * LightLUTSize;
        int index0 = Mathf.FloorToInt(u);
        float weight1 = u - index0;
        int index1 = index0 + 1;
        float weight0 = 1 - weight1;

        index0 = Mathf.Clamp(index0, 0, LightLUTSize - 1);
        index1 = Mathf.Clamp(index1, 0, LightLUTSize - 1);

        Color c = _directionalLightLUT[index0] * weight0 + _directionalLightLUT[index1] * weight1;
        return c.gamma;
    }
    
    private void UpdateDirectionalLightColor(Color c)
    {
        Vector3 color = new Vector3(c.r, c.g, c.b);
        float length = color.magnitude;
        color /= length;

        Sun.color = new Color(Mathf.Max(color.x, 0.01f), Mathf.Max(color.y, 0.01f), Mathf.Max(color.z, 0.01f), 1);
        Sun.intensity = Mathf.Max(length, 0.01f) * LightColorIntensity; // make sure unity doesn't disable this light
    }
    
    private Color ComputeAmbientColor()
    {
        float cosAngle = Vector3.Dot(Vector3.up, -Sun.transform.forward);
        float u = (cosAngle + 0.1f) / 1.1f;// * 0.5f + 0.5f;

        u = u * LightLUTSize;
        int index0 = Mathf.FloorToInt(u);
        float weight1 = u - index0;
        int index1 = index0 + 1;
        float weight0 = 1 - weight1;

        index0 = Mathf.Clamp(index0, 0, LightLUTSize - 1);
        index1 = Mathf.Clamp(index1, 0, LightLUTSize - 1);

        Color c = _ambientLightLUT[index0] * weight0 + _ambientLightLUT[index1] * weight1;
        return c.gamma;
    }
    
    private void UpdateAmbientLightColor(Color c)
    {
        RenderSettings.ambientLight = c * AmbientColorIntensity;
    }
    
    void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera) {
        // get four corners of camera frustom in world space
        // bottom left
        _FrustumCorners[0] = camera.ViewportToWorldPoint(new Vector3(0, 0, camera.farClipPlane));        
        // top left
        _FrustumCorners[1] = camera.ViewportToWorldPoint(new Vector3(0, 1, camera.farClipPlane));
        // top right
        _FrustumCorners[2] = camera.ViewportToWorldPoint(new Vector3(1, 1, camera.farClipPlane));
        // bottom right
        _FrustumCorners[3] = camera.ViewportToWorldPoint(new Vector3(1, 0, camera.farClipPlane));

        // update parameters
        UpdateSkyBoxParameters();
        UpdateLightScatteringParameters();
    }
    
    private void UpdateSkyBoxParameters()
    {
        if (RenderSettings.skybox != null)
        {
            RenderSettings.skybox.SetVector("_CameraPos", _camera.transform.position);
            UpdateMaterialParameters(RenderSettings.skybox);
            RenderSettings.skybox.SetFloat("_SunIntensity", 1);
        }
    }
    
    private void UpdateLightScatteringParameters()
    {
        UpdateMaterialParameters(_material);
        _material.SetVectorArray("_FrustumCorners", _FrustumCorners);
        _material.SetFloat("_SunIntensity", 1);


    }
}
