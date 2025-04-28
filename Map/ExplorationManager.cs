using UnityEngine;
using Unity.Netcode;
using System.Linq;
using SleepHerd.Game.Player.PlayerController;
using SleepHerd.Game.Player;
using SleepHerd.UI.Utilities;
using SleepHerd.Core;
using System.Collections.Generic;

public class ExplorationManager : MonoBehaviour
{
    [SerializeField] private GameObject _map;

    [SerializeField] private RenderTexture _explorationTexture;
    [SerializeField] private ComputeShader _explorationShader;

    [SerializeField] private int _textureWidth = 256;
    [SerializeField] private int _textureHeight = 256;

    [SerializeField] private PlayerIconUI _playerIcons;

    [SerializeField] private float _explorationRadius = 10f;

    [SerializeField] private Vector2 _minBounds;
    [SerializeField] private Vector2 _maxBounds;

    private uint[] _gpuBufferData;

    private int _kernelHandle;
    private ComputeBuffer _explorationBuffer;

    private bool _bufferNeedsUpdate = false;
    private int _packingFactor;

    private bool _isVisible;

    private void Awake()
    {
        _packingFactor = sizeof(uint) * 8;

        // TODO:
        // We will want this to read from the save data later as part of the progress update
        // The exploration save data should be taken from the host and networked to all clients
        _gpuBufferData = Enumerable.Repeat(uint.MinValue, (_textureWidth * _textureHeight) / _packingFactor).ToArray();

        InitializeComputeShader();
        InitializeIcons();
        PlayerController.Instance.Map.OnToggleMap += ToggleMapVisibility;

        ToggleMapVisibility(false);
    }

    public void OnDestroy()
    {
        _explorationBuffer?.Dispose();
        PlayerController.Instance.Map.OnToggleMap -= ToggleMapVisibility;
    }

    private void Update()
    {     
        if (Core.Instance.CurrentState is NightState)
        {
            UpdateGPUBuffer();
            UpdatePlayerPositions();
        }
    }

    private void ToggleMapVisibility(bool isVisible)
    {
        _isVisible = isVisible;
        _map.SetActive(isVisible);
    }

    private void InitializeComputeShader()
    {
        _kernelHandle = _explorationShader.FindKernel("CSMain");

        _explorationBuffer = new ComputeBuffer((_textureWidth * _textureHeight) / _packingFactor, 4, ComputeBufferType.Default);
        _explorationShader.SetBuffer(_kernelHandle, "ExplorationData", _explorationBuffer);
        _explorationShader.SetTexture(_kernelHandle, "ResultTexture", _explorationTexture);
    }

    private void InitializeIcons()
    {
        _playerIcons.HideAllIcons();

        foreach (var player in PlayerRegistry.Instance.Players)
        {
            var skinIndex = player.Skin.SkinIndex;
            _playerIcons.SetIconVisibility(true, (int)skinIndex, false);
        }
    }

    private void ExploreArea(float normalizedX, float normalizedZ, float radius)
    {
        var intX = Mathf.RoundToInt(normalizedX * _textureWidth);
        var intY = Mathf.RoundToInt(normalizedZ * _textureHeight);
        var intRadius = Mathf.CeilToInt(radius);
        var radiusSqr = radius * radius;

        for (var x = intX - intRadius; x <= intX + intRadius; x++)
        {
            for (var y = intY - intRadius; y <= intY + intRadius; y++)
            {
                if (x < 0 || x >= _textureWidth || y < 0 || y >= _textureHeight) continue;

                float dx = x - intX;
                float dy = y - intY;

                if (dx * dx + dy * dy <= radiusSqr)
                {
                    var index = x + y * _textureWidth;
                    var byteIndex = Mathf.FloorToInt(index / _packingFactor);
                    var bitPosition = index % _packingFactor;
                    var bitMask = (uint)(1 << bitPosition);

                    if (byteIndex >= _gpuBufferData.Length || (_gpuBufferData[byteIndex] & bitMask) != 0) continue;

                    _gpuBufferData[byteIndex] |= bitMask;
                    _bufferNeedsUpdate = true;
                }
            }
        }
    }

    private void UpdateGPUBuffer()
    {
        if (!_bufferNeedsUpdate) return;
        Debug.Log("Updating RenderTexture");

        _explorationBuffer.SetData(_gpuBufferData);
        _explorationShader.Dispatch(_kernelHandle, _textureWidth, _textureHeight, 1);

        _bufferNeedsUpdate = false;
    }

    private void UpdatePlayerPositions()
    {
        foreach (var player in PlayerRegistry.Instance.Players)
        {
            var skinIndex = (int)player.Skin.SkinIndex;
            var playerPosition = player.transform.position;

            var normX = Mathf.InverseLerp(_minBounds.x, _maxBounds.x, playerPosition.x);
            var normZ = Mathf.InverseLerp(_minBounds.y, _maxBounds.y, playerPosition.z);

            ExploreArea(normX, normZ, _explorationRadius);

            if (_isVisible)
            {
                var iconX = Mathf.Lerp(-50, 50, normX);
                var iconY = Mathf.Lerp(-50, 50, normZ);

                var iconPosition = new Vector3(iconX, iconY, 0.0f);

                _playerIcons.SetRectPositon(iconPosition, skinIndex);
            }
        }
    }
}