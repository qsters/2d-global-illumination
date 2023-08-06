using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CustomLighting
{
    public class RayMarchLightingPass : ScriptableRenderPass
    {
        // Lighting Texture Global ID's
        private static readonly string LIGHTMAP_TEXTURE_NAME = "_CustomLightMapTexture";
        private static readonly string EMISSION_TEXTURE_NAME = "_EmissionTexture";
        private static readonly string BLUR_TEXTURE_NAME = "_BlurTexture";
        
        // Profiling Scope Name
        private static readonly string EMISSION_PROFILER = "Raymarch Lighting Pass/Emission";
        private static readonly string LIGHTING_PROFILER = "Raymarch Lighting Pass/Lighting";
        private static readonly string BLUR_PROFILER = "Raymarch Lighting Pass/Blur";
        
        // Final Lighting Texture
        private RTHandle _lightMapTexture;
        private RTHandle _lightMapPPTexture; // Ping Pong Texture
        private RTHandle _emissionTexture;
        private RTHandle _blurTexture;
        private RTHandle _blurPPTexture;
        private RTHandle _cameraColorBuffer;
        
        // Settings
        private RayMarchingRenderFeature.PassSettings _settings;
        
        // Materials   
        private Material _rayMarchLightingMaterial;
        
        private ShaderTagId _emissionShaderTagId;
        
        private Material _debugMaterial;
        
        public RayMarchLightingPass(RayMarchingRenderFeature.PassSettings settings, Material rayMarchLightingMaterial, Material debugMaterial)
        {
            // Initializing Data
            _settings = settings;
            _rayMarchLightingMaterial = rayMarchLightingMaterial;
            renderPassEvent = _settings.renderPassEvent;
            _debugMaterial = debugMaterial;
            
            _emissionShaderTagId = new ShaderTagId("EmissionRendering");
        }
        
        // OnCameraSetup
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            
            // Make a descriptor scaled by the downscale amount
            RenderTextureDescriptor scaledCameraDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            scaledCameraDescriptor.depthBufferBits = 0;
            scaledCameraDescriptor.graphicsFormat = GraphicsFormat.R32G32B32A32_SFloat;
            scaledCameraDescriptor.width = Mathf.Max(1, scaledCameraDescriptor.width >> _settings.downscaleAmount);
            scaledCameraDescriptor.height = Mathf.Max(1, scaledCameraDescriptor.height >> _settings.downscaleAmount);
            
            // Allocate the textures
            RenderingUtils.ReAllocateIfNeeded(ref _lightMapTexture,
                scaledCameraDescriptor, filterMode: FilterMode.Bilinear ,name: LIGHTMAP_TEXTURE_NAME);
            RenderingUtils.ReAllocateIfNeeded(ref _emissionTexture,
                scaledCameraDescriptor, name: EMISSION_TEXTURE_NAME);
            RenderingUtils.ReAllocateIfNeeded(ref _lightMapPPTexture,
                scaledCameraDescriptor, filterMode: FilterMode.Bilinear ,name: LIGHTMAP_TEXTURE_NAME);
            RenderingUtils.ReAllocateIfNeeded(ref _blurPPTexture, scaledCameraDescriptor, filterMode: FilterMode.Bilinear ,name: BLUR_TEXTURE_NAME);
            RenderingUtils.ReAllocateIfNeeded(ref _blurTexture, scaledCameraDescriptor, filterMode: FilterMode.Bilinear ,name: BLUR_TEXTURE_NAME);
            
            // Configure Target to emission Tex and Clear
            ConfigureTarget(_emissionTexture);
            ConfigureClear(ClearFlag.Color, Color.clear);
            }
        
        // Updating camera buffer to blit to camera
        public void SetCameraBuffers(RTHandle cameraColorTargetHandle)
        {
            _cameraColorBuffer = cameraColorTargetHandle;
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            
            // using new profiling scope
            using (new ProfilingScope(cmd, new ProfilingSampler(EMISSION_PROFILER)))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                
                FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.transparent, _settings.layerMask);
                SortingCriteria sortingCriteria = SortingCriteria.CommonTransparent;
                DrawingSettings drawingSettings = CreateDrawingSettings(_emissionShaderTagId, ref renderingData, sortingCriteria);
                
                // render 
                context.DrawRenderers(_settings.createdCullingResults, ref drawingSettings, ref filteringSettings);
                cmd.SetGlobalTexture(EMISSION_TEXTURE_NAME, _emissionTexture);
            }
            
            
            using (new ProfilingScope(cmd, new ProfilingSampler(LIGHTING_PROFILER)))
            {
                cmd.SetGlobalFloat("_frameCount", Time.frameCount);
                cmd.SetGlobalFloat("_time", Time.timeSinceLevelLoad % 10);
                cmd.SetGlobalInt("_samples", _settings.samples);
                cmd.SetGlobalFloat("_OneOverTimeSpan", 1f / _settings.timeSpan);
                
                
                if (Time.frameCount % 2 == 0)
                {
                    cmd.Blit(_lightMapTexture, _lightMapPPTexture, _rayMarchLightingMaterial, 0);
                    
                }
                else
                {
                    cmd.Blit(_lightMapPPTexture, _lightMapTexture, _rayMarchLightingMaterial, 0);
                }
            }

            RTHandle startingTex = Time.frameCount % 2 == 0 ? _lightMapPPTexture : _lightMapTexture;
            
            if (_settings.blurStrength > 0.0)
            {
                using (new ProfilingScope(cmd, new ProfilingSampler(BLUR_PROFILER)))
                {
                    int gridSize = Mathf.CeilToInt(_settings.blurStrength * 3.0f);
            
                    if (gridSize % 2 == 0)
                    {
                        gridSize++;
                    }
                
                    cmd.SetGlobalInt("_gridSize", gridSize);
                    cmd.SetGlobalFloat("_spread", _settings.blurStrength);

                    if (_settings.fancyBlur)
                    {
                        if (Time.frameCount % 2 == 0)
                        {
                            cmd.Blit(_lightMapPPTexture, _lightMapTexture, _rayMarchLightingMaterial, 1);
                            cmd.Blit(_lightMapTexture, _lightMapPPTexture, _rayMarchLightingMaterial, 2);
                            cmd.SetGlobalTexture(LIGHTMAP_TEXTURE_NAME, _lightMapPPTexture);
                        }
                        else
                        {
                            cmd.Blit(_lightMapTexture, _lightMapPPTexture, _rayMarchLightingMaterial, 1);
                            cmd.Blit(_lightMapPPTexture, _lightMapTexture, _rayMarchLightingMaterial, 2);
                            cmd.SetGlobalTexture(LIGHTMAP_TEXTURE_NAME, _lightMapTexture);
                        }
                    }
                    else
                    {
                        cmd.Blit(startingTex, _blurPPTexture, _rayMarchLightingMaterial, 1);
                        cmd.Blit(_blurPPTexture, _blurTexture, _rayMarchLightingMaterial, 2);
                        cmd.SetGlobalTexture(LIGHTMAP_TEXTURE_NAME, _blurTexture);
                    }
                }
            }
            else
            {
                cmd.SetGlobalTexture(LIGHTMAP_TEXTURE_NAME, startingTex);
            }
            
            // Set the CameraScaleOffset
            Shader.SetGlobalFloat("_CameraScaleOffset", 1 / _settings.cameraScaleOffset);
            
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        // Dispose of textures
        public void Dispose()
        {
            _emissionTexture?.Release();
            _lightMapTexture?.Release();
            _lightMapPPTexture?.Release();
            _blurPPTexture?.Release();
            _blurTexture?.Release();
        }
    }
}

