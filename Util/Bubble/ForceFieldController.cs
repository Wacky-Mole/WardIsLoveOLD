﻿using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using WardIsLove.Extensions;
using WardIsLove.Util;

[HarmonyPatch]
public class ForceFieldController : MonoBehaviour
{
    public enum FFstate
    {
        SingleSpheres,
        MultipleSpheres
    }

    public static float openCloseValue;

    public FFstate forceFieldMode = FFstate.SingleSpheres;

    public int affectorCount = 20;
    [Range(-2, 2)] public float openCloseProgress = 2;
    public bool openAutoAnimation = true;
    public float openSpeed = 0.6f;
    public AnimationCurve openCurve;

    public Material[] materialLayers;

    public bool procedrualGradientEnabled = true;
    public bool procedrualGradientUpdate = true;
    public Gradient procedrualGradientRamp;
    public Color procedrualRampColorTint = Color.white;

    public ParticleSystem controlParticleSystem;
    public GameObject getRenderersInChildren;
    public Renderer[] getRenderersCustom;

    private ParticleSystem.Particle[] controlParticles;
    private Vector4[] controlParticlesPositions;
    private float[] controlParticlesSizes;
    private int numberOfSpheres;
    private int numberOfSpheresOld;
    private float openCloseCurve;
    private ParticleSystem.MainModule psmain;
    private Texture2D rampTexture;
    private readonly List<Material> rendererMaterials = new();

    private Renderer[] renderers;

    private Vector4[] spherePositions;
    private float[] sphereSizes;

    // Use this for initialization
    private void Start()
    {
        psmain = controlParticleSystem.main;

        GetRenderers();
        GetNumberOfSpheres();
        GetSphereArrays();
        ApplyMaterials();

        if (procedrualGradientEnabled) UpdateRampTexture();
    }

    // Update is called once per frame
    private void Update()
    {
        GetNumberOfSpheres();

        if (numberOfSpheres != numberOfSpheresOld)
        {
            GetRenderers();
            ApplyMaterials();
        }

        numberOfSpheresOld = numberOfSpheres;

        GetSphereArrays();
        if (procedrualGradientEnabled)
            if (procedrualGradientUpdate)
                UpdateRampTexture();

        controlParticles = new ParticleSystem.Particle[affectorCount];
        controlParticlesPositions = new Vector4[affectorCount];
        controlParticlesSizes = new float[affectorCount];
        psmain.maxParticles = affectorCount;
        //controlParticleSystem.maxParticles = affectorCount;
        controlParticleSystem.GetParticles(controlParticles);
        for (int i = 0; i < affectorCount; i++)
        {
            controlParticlesPositions[i] = controlParticles[i].position;
            controlParticlesSizes[i] = controlParticles[i].GetCurrentSize(controlParticleSystem) *
                                       controlParticleSystem.transform.lossyScale.x;
        }

        UpdateHitWaves();

        if (openAutoAnimation) OpenCloseProgress();
    }

    // For Better Effects Change in DemoScene
    private void OnEnable()
    {
        psmain = controlParticleSystem.main;

        GetRenderers();
        GetNumberOfSpheres();
        GetSphereArrays();

        controlParticles = new ParticleSystem.Particle[affectorCount];
        controlParticlesPositions = new Vector4[affectorCount];
        controlParticlesSizes = new float[affectorCount];
        psmain.maxParticles = affectorCount;
        //controlParticleSystem.maxParticles = affectorCount;
        controlParticleSystem.GetParticles(controlParticles);
        for (int i = 0; i < affectorCount; i++)
        {
            controlParticlesPositions[i] = controlParticles[i].position;
            controlParticlesSizes[i] = controlParticles[i].GetCurrentSize(controlParticleSystem) *
                                       controlParticleSystem.transform.lossyScale.x;
        }

        OpenCloseProgress();
        UpdateHitWaves();
    }

    private void GetNumberOfSpheres()
    {
        //numberOfSpheres = renderers.Length;
        if (getRenderersCustom.Length > 0)
            numberOfSpheres = getRenderersCustom.Length;
        else
            numberOfSpheres = getRenderersInChildren.transform.childCount;
    }

    private void GetSphereArrays()
    {
        try
        {
            spherePositions = new Vector4[numberOfSpheres];
            sphereSizes = new float[numberOfSpheres];
            for (int i = 0; i < numberOfSpheres; i++)
            {
                spherePositions[i] = renderers[i].gameObject.transform.position;
                sphereSizes[i] = renderers[i].gameObject.transform.lossyScale.x;
            }
        }
        catch
        {
        }
    }

    // Open Animation Progress
    private void OpenCloseProgress()
    {
        if (!getRenderersInChildren.gameObject.GetComponentInParent<WardMonoscript>().GetBubbleOn())
            openCloseValue =
                0f; // Reversing of the bubble when being turned off is in WardEx.cs SetBubbleOn, was easier there.

        if (openCloseValue < 1f)
            openCloseValue += Time.deltaTime * openSpeed;
        else
            openCloseValue = 1f;

        openCloseCurve = openCurve.Evaluate(openCloseValue);
        openCloseProgress = openCloseCurve;
    }

    // Set Open Value from other Scripts
    public void SetOpenCloseValue(float val)
    {
        if (openAutoAnimation) openCloseValue = val;
    }

    public static void SetOpenCloseValueBubbleOff(WardMonoscript ward)
    {
        openCloseValue -= Time.deltaTime * 0.2f;
        if (openCloseValue <= 0f) ward.m_nview.GetZDO().Set("bubbleOn", false);
    }

    // Generating a texture from gradient variable
    private Texture2D GenerateTextureFromGradient(Gradient grad)
    {
        float width = 256;
        float height = 1;
        Texture2D text = new((int)width, (int)height);
        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        {
            Color col = grad.Evaluate(0 + x / width);
            text.SetPixel(x, y, col);
        }

        text.wrapMode = TextureWrapMode.Clamp;
        text.Apply();
        return text;
    }

    // Applying material layers to objects
    public void ApplyMaterials()
    {
        for (int i = 0; i < materialLayers.Length; i++) materialLayers[i] = new Material(materialLayers[i]);

        foreach (Renderer rend in renderers)
        {
            rendererMaterials.Clear();
            foreach (Material mat in rend.sharedMaterials)
            {
                bool isEffect = false;
                for (int i = 0; i < materialLayers.Length; i++)
                    if (materialLayers[i].name == mat.name)
                        isEffect = true;

                if (isEffect != true) rendererMaterials.Add(mat);
            }

            foreach (Material matt in materialLayers) rendererMaterials.Add(matt);

            rend.materials = rendererMaterials.ToArray();
        }
    }

    // Update procedural ramp textures and applying them to the shaders
    public void UpdateRampTexture()
    {
        rampTexture = GenerateTextureFromGradient(procedrualGradientRamp);
        GetRenderers();

        foreach (Renderer rend in renderers)
        foreach (Material matt in materialLayers)
        {
            matt.SetTexture("_Ramp", rampTexture);
            matt.SetColor("_RampColorTint", procedrualRampColorTint);
        }
    }

    // Getting all renderers for ForceField meshes
    public void GetRenderers()
    {
        if (getRenderersCustom.Length > 0)
            renderers = getRenderersCustom;
        else
            renderers = getRenderersInChildren.GetComponentsInChildren<Renderer>();
    }

    // Update Hit Waves and Control Particles
    public void UpdateHitWaves()
    {
        foreach (Renderer rend in renderers)
            switch (forceFieldMode)
            {
                case FFstate.SingleSpheres:
                    foreach (Material matt in materialLayers)
                    {
                        matt.SetVectorArray("_ControlParticlePosition", controlParticlesPositions);
                        matt.SetFloatArray("_ControlParticleSize", controlParticlesSizes);
                        matt.SetInt("_AffectorCount", affectorCount);
                        matt.SetFloat("_PSLossyScale", controlParticleSystem.transform.lossyScale.x);
                        matt.SetFloat("_MaskAppearProgress", openCloseProgress);
                    }

                    break;
                case FFstate.MultipleSpheres:
                    foreach (Material matt in materialLayers)
                    {
                        matt.SetVectorArray("_ControlParticlePosition", controlParticlesPositions);
                        matt.SetFloatArray("_ControlParticleSize", controlParticlesSizes);
                        matt.SetInt("_AffectorCount", affectorCount);
                        matt.SetFloat("_PSLossyScale", controlParticleSystem.transform.lossyScale.x);
                        matt.SetFloat("_MaskAppearProgress", openCloseProgress);

                        matt.SetVectorArray("_FFSpherePositions", spherePositions);
                        matt.SetFloatArray("_FFSphereSizes", sphereSizes);
                        matt.SetFloat("_FFSphereCount", numberOfSpheres);
                    }

                    break;
            }
    }
}