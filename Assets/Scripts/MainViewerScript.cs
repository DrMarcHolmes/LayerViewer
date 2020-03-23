﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class MainViewerScript : MonoBehaviour
{
    public enum DisplayMode
    {
        ColorAndHeat,
        FullHeat,
        FullLongevity,
        FlatColor
    }

    public enum ColorMode
    {
        BaseColor,
        Heat,
        Longevity
    }

    public enum HeightMode
    {
        Heat,
        Longevity,
        None
    }
    
    public ColorMode CurrentColorMode;
    public HeightMode CurrentHeightMode;

    public Transform Light;
    
    [Range(0, 1)]
    public float Time;

    [Range(0, 1)]
    public float TreeRingTopTime;

    public bool LoadTreeRingTop;

    [Range(0, 1)]
    public float ColorLerpSpeed;
    
    private string _outputFolder;

    public Material Mat;
    public Material TreeRingTopMat;
    public Material TreeRingBottomMat;

    private TextureLoader _mainTextureLoader;
    private TextureLoader _treeRingTextureLoader;
    
    private const int DispatchGroupSize = 128;

    private string[] _texturePaths;
    
    private float _currentHeatAlpha;
    private float _currentLongevityAlpha;

    private float _currentHeatHeightAlpha;
    private float _currentLongevityHeightAlpha;

    private bool _validFolder;

    void Start()
    {
        if(!File.Exists(BakingScript.OutputPathFile))
        {
            System.Windows.Forms.FolderBrowserDialog outputFolderDialog = new System.Windows.Forms.FolderBrowserDialog();

            outputFolderDialog.Description = "Where is the processed PNG data?";
            outputFolderDialog.ShowDialog();

            _outputFolder = outputFolderDialog.SelectedPath;
            _validFolder = Directory.Exists(_outputFolder);
            if (_validFolder)
            {
                File.WriteAllText(BakingScript.OutputPathFile, _outputFolder);
            }
            else
            {
                return;
            }
        }
        else
        {
            _validFolder = true;
        }
        _outputFolder = File.ReadAllText(BakingScript.OutputPathFile);
        _texturePaths = Directory.GetFiles(_outputFolder);
        _mainTextureLoader = new TextureLoader(Mat, TreeRingBottomMat);
        _treeRingTextureLoader = new TextureLoader(TreeRingTopMat);
    }
    
    private void Update()
    {
        if (!_validFolder)
        {
            return;
        }
        UpdateColorModeProperties();
        UpdateHeightModeProperties();
        _mainTextureLoader.UpdateTexture(Time, _texturePaths);

        TreeRingTopTime = Mathf.Max(Time, TreeRingTopTime);
        if (LoadTreeRingTop)
        {
            _treeRingTextureLoader.UpdateTexture(TreeRingTopTime, _texturePaths);
        }

        Mat.SetVector("_LightPos", Light.position);
    }

    private void UpdateHeightModeProperties()
    {
        float baseHeatHeightTarget = CurrentHeightMode == HeightMode.Heat ? 1 : 0;
        float baseLongevityHeightTarget = CurrentHeightMode == HeightMode.Longevity ? 1 : 0;
        _currentHeatHeightAlpha = Mathf.Lerp(_currentHeatHeightAlpha, baseHeatHeightTarget, ColorLerpSpeed);
        _currentLongevityHeightAlpha = Mathf.Lerp(_currentLongevityHeightAlpha, baseLongevityHeightTarget, ColorLerpSpeed);
        Mat.SetFloat("_HeatHeightAlpha", _currentHeatHeightAlpha);
        Mat.SetFloat("_LongevityHeightAlpha", _currentLongevityHeightAlpha);
    }

    private void UpdateColorModeProperties()
    {
        float heatTarget = CurrentColorMode == ColorMode.Heat ? 1 : 0;
        float longevityTarget = CurrentColorMode == ColorMode.Longevity ? 1 : 0;
        _currentHeatAlpha = Mathf.Lerp(_currentHeatAlpha, heatTarget, ColorLerpSpeed);
        _currentLongevityAlpha = Mathf.Lerp(_currentLongevityAlpha, longevityTarget, ColorLerpSpeed);
        Mat.SetFloat("_HeatAlpha", _currentHeatAlpha);
        Mat.SetFloat("_LongevityAlpha", _currentLongevityAlpha);
    }

    internal void SetDisplayMode(DisplayMode displayMode)
    {
        switch (displayMode)
        {
            case DisplayMode.ColorAndHeat:
                CurrentColorMode = ColorMode.BaseColor;
                CurrentHeightMode = HeightMode.Heat;
                break;
            case DisplayMode.FullHeat:
                CurrentColorMode = ColorMode.Heat;
                CurrentHeightMode = HeightMode.Heat;
                break;
            case DisplayMode.FullLongevity:
                CurrentColorMode = ColorMode.Longevity;
                CurrentHeightMode = HeightMode.Longevity;
                break;
            case DisplayMode.FlatColor:
            default:
                CurrentColorMode = ColorMode.BaseColor;
                CurrentHeightMode = HeightMode.None;
                break;
        }
    }

    class TextureLoader
    {
        private int _lastLoadedTextureIndex;
        
        private Texture2D _inputTexture;
        private byte[] _inputPngData;

        private Material[] _mats;

        public TextureLoader(params Material[] mats)
        {
            _mats = mats;
            _inputTexture = new Texture2D(1024, 1024, TextureFormat.RGB24, false);
            _inputTexture.filterMode = FilterMode.Point;
            _inputTexture.wrapMode = TextureWrapMode.Clamp;
        }

        public void UpdateTexture(float time, string[] texturePaths)
        {
            int textureIndex = (int)Mathf.Min(texturePaths.Length * time, texturePaths.Length - 1);

            if (textureIndex != _lastLoadedTextureIndex)
            {
                LoadTexture(textureIndex, texturePaths);
                _lastLoadedTextureIndex = textureIndex;
            }
        }

        private void LoadTexture(int textureIndex, string[] texturePaths)
        {
            string path = texturePaths[textureIndex];
            _inputPngData = File.ReadAllBytes(path);
            _inputTexture.LoadImage(_inputPngData);

            foreach (Material mat in _mats)
            {
                mat.SetTexture("_MainTex", _inputTexture);
            }
        }
    }
}

