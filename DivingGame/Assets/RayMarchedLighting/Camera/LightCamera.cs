using CustomLighting;
using UnityEngine;

[ExecuteInEditMode]
public class LightCamera : MonoBehaviour
{
    [SerializeField] private Camera _referenceCamera;
    [SerializeField] private RayMarchingRenderFeature _renderFeature;
    private Camera _lightCamera;
    
    private void Start()
    {
        _lightCamera = GetComponent<Camera>();
    }

    // Update is called once per frame
    void Update()
    {
        _lightCamera.orthographicSize = _referenceCamera.orthographicSize * _renderFeature._settings.cameraScaleOffset;
    }
}
