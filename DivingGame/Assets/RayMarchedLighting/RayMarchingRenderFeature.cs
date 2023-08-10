using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

namespace CustomLighting
{
    public class RayMarchingRenderFeature : ScriptableRendererFeature
    {
        // Which silhouette Generation Mode to use
        public enum SilhouetteMode
        {
            FromCamera,
            FromPass,
        }
        
        [Serializable]
        public class PassSettings
        {
            // Where/when the render pass should be injected during the rendering process.
            // [SerializeField] public SilhouetteMode silhouetteMode = SilhouetteMode.FromPass;
            [SerializeField] public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
            [SerializeField] public LayerMask layerMask = -1;
            [SerializeField] public float cameraScaleOffset = 1.5f;
            [SerializeField] public int targetFrameRate = 30;
            [SerializeField] public bool useAdaptiveSamples = true;
            [SerializeField, Range(0, 3)] public int downscaleAmount = 1;
            [SerializeField, Range(1, 1000)] public int samples = 5;
            [SerializeField, Range(0f, 300f)] public float timeSpan = 2f;
            [SerializeField, Range(0f, 15f)] public float blurStrength = 5f;
            [SerializeField] public bool fancyBlur = true;
            [SerializeField] public Color ambientColor = Color.black;
            [SerializeField] public Color waterColor = Color.blue;
            
            // Used in Passes privately
            [HideInInspector] public CullingResults createdCullingResults;
        }
        
        // Settings for the Passes
        
        [SerializeField] private Material _debugMaterial;
        [SerializeField] private Material _jumpFloodMaterial;
        [SerializeField] private Material _rayMarchLightingMaterial;
        
        [SerializeField] public PassSettings _settings;
        
        


        // Passes
        private JumpFloodPass _jumpFloodPass;
        private RayMarchLightingPass _rayMarchLightingPass;
        
        public override void Create()
        {
            if (!_jumpFloodMaterial || !_jumpFloodMaterial || !_rayMarchLightingMaterial)
            {
                Debug.LogError("Initializing Data is NULL");
            }
            
            _jumpFloodPass = new JumpFloodPass(_settings, _debugMaterial, _jumpFloodMaterial);
            _rayMarchLightingPass = new RayMarchLightingPass(_settings, _rayMarchLightingMaterial, _debugMaterial);
        }
        protected override void Dispose(bool disposing)
        {
            _jumpFloodPass.Dispose();
            _rayMarchLightingPass.Dispose();
        }
        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            _jumpFloodPass.ConfigureInput(ScriptableRenderPassInput.Color); 
            _jumpFloodPass.SetCameraBuffers(renderer.cameraColorTargetHandle);
            
            _rayMarchLightingPass.ConfigureInput(ScriptableRenderPassInput.Color);
            _rayMarchLightingPass.SetCameraBuffers(renderer.cameraColorTargetHandle);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(_jumpFloodPass);
            renderer.EnqueuePass(_rayMarchLightingPass);
        }
    }

}
