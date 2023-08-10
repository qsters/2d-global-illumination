using System.Collections;
using System.Collections.Generic;
using CustomLighting;
using UnityEngine;
using UnityEngine.InputSystem;

public class TestDownscale : MonoBehaviour
{
    [SerializeField] private RayMarchingRenderFeature _renderFeature;
    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.hKey.wasPressedThisFrame)
        {
            int newDownscaleAmount = _renderFeature._settings.downscaleAmount == 2? 3 : 2;
            _renderFeature._settings.downscaleAmount = newDownscaleAmount;
        }
    }
}
