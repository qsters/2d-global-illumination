using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering;

namespace CustomLighting 
{
    public class JumpFloodPass : ScriptableRenderPass
    {
        private RayMarchingRenderFeature.PassSettings _settings;

        // Material For FullScreen Blit, Debugging Textures
        private Material _debugMaterial;
        
        // Main Material that Contains Shaders For Jump Flood
        private Material _jumpFloodMaterial;
        
        // Jump Flood Passes
        private static readonly int SILHOUETTE_DRAWER_PASS = 0;
        private static readonly int JUMPFLOOD_PASS = 1;
        private static readonly int DISTANCE_FIELD_PASS = 2;
        
        // Texture Names, The ID's in Shaders
        private static readonly String JUMPFLOOD_TEXTURE_NAME = "_JumpFloodTexture";
        private static readonly String JUMPFLOOD_PP_TEXTURE_NAME = "_JumpFloodPPTexture"; // PP - Ping Pong
        private static readonly String DISTANCE_FIELD_TEXTURE_NAME = "_DistanceFieldTexture";
        
        //Profiler Names for Compartmentalizing the Rendering
        private static readonly String RENDER_SILHOUETTES_PROFILER = "Jump Flood Pass/Render Silhouettes";
        private static readonly String JUMP_FLOOD_PROFILER = "Jump Flood Pass/Jump Flood";
        private static readonly String DISTANCE_FIELD_PROFILER = "Jump Flood Pass/Generate";
        private static readonly String DEBUG_SCREEN_PROFILER = "Jump Flood Pass/Debug Blit";
        
        // RTHandles for storing our rendering data
        private RTHandle _CameraColorBuffer;
        private RTHandle _jumpFloodTexture;
        private RTHandle _jumpFloodPPTexture;
        private RTHandle _distanceFieldTexture;
        
        // List of Shader Tags to include for rendering
        private List<ShaderTagId> _shaderTagIdList = new();
        
        // Filter setting to determine what to render
        private FilteringSettings _filteringSettings;
        
        // Count of how many times to run the jump flood pass
        private int _jumpFloodPassCount;
        
        public JumpFloodPass(RayMarchingRenderFeature.PassSettings settings, Material debugMaterial, Material jumpFloodMaterial)
        {
            // Initializing Data
            _settings = settings;
            _debugMaterial = debugMaterial;
            _jumpFloodMaterial = jumpFloodMaterial;
            renderPassEvent = settings.renderPassEvent;
            
            // Initializing Shader Tags
            _shaderTagIdList.Add(new ShaderTagId("SRPDefaultUnlit"));
            _shaderTagIdList.Add(new ShaderTagId("UniversalForward"));
            _shaderTagIdList.Add(new ShaderTagId("UniversalForwardOnly"));
            _shaderTagIdList.Add(new ShaderTagId("Universal2D"));

            // Initializing Filtering Settings
            _filteringSettings = new FilteringSettings(RenderQueueRange.transparent, _settings.layerMask);
        }

        // Main Execution of the JUMP FLOOD PASS
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            
            // Silhouette Pass
            using (new ProfilingScope(cmd, new ProfilingSampler(RENDER_SILHOUETTES_PROFILER)))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                
                // Setting Data for 2d Renderer and render
                SortingCriteria sortingCriteria = SortingCriteria.CommonTransparent;
                DrawingSettings drawingSettings = CreateDrawingSettings(_shaderTagIdList, ref renderingData, sortingCriteria);
                
                // Setting material and pass to render the objects with
                drawingSettings.overrideMaterialPassIndex = SILHOUETTE_DRAWER_PASS;
                drawingSettings.overrideMaterial = _jumpFloodMaterial;
                
                // Render to texture set in OnCameraSetup
                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref _filteringSettings);
            }
            
            using (new ProfilingScope(cmd, new ProfilingSampler(JUMP_FLOOD_PROFILER)))
            {
                // Number of passes through the jump flood texture
                int numIter = _jumpFloodPassCount - 1;
                cmd.SetGlobalVector("_AspectRatio", new Vector4(Screen.width / (float)Screen.height, 1, 0, 0));
                
                for (int i = numIter; i >= 0; i--)
                {
                    // Setting the step length for the shader
                    cmd.SetGlobalFloat("_StepLength", Mathf.Pow(2, i) + 0.5f);
                    
                    // Ping Ponging the textures
                    if (i % 2 == 0)
                    {
                        cmd.Blit( _jumpFloodPPTexture, _jumpFloodTexture, _jumpFloodMaterial, JUMPFLOOD_PASS);
                    }
                    else
                    {
                        cmd.Blit( _jumpFloodTexture, _jumpFloodPPTexture, _jumpFloodMaterial, JUMPFLOOD_PASS);
                    }
                }
            }
            
            using (new ProfilingScope(cmd, new ProfilingSampler(DISTANCE_FIELD_PROFILER)))
            {
                // Blit Texture for generating Distance Field 
                cmd.Blit(_jumpFloodTexture, _distanceFieldTexture, _jumpFloodMaterial, DISTANCE_FIELD_PASS);
                cmd.SetGlobalTexture(DISTANCE_FIELD_TEXTURE_NAME, _distanceFieldTexture);
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }
        
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            renderingData.cameraData.camera.SetReplacementShader(_jumpFloodMaterial.shader, "");
            
            // Getting Camera Descriptor setting depth to 0 to show that it is a color buffer
            RenderTextureDescriptor resizedCameraDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            resizedCameraDescriptor.depthBufferBits = 0; // Setting to 0 to show that it is a color buffer
            resizedCameraDescriptor.width = Mathf.Max(1, resizedCameraDescriptor.width >> _settings.downscaleAmount);
            resizedCameraDescriptor.height = Mathf.Max(1, resizedCameraDescriptor.height >> _settings.downscaleAmount);
            resizedCameraDescriptor.graphicsFormat = GraphicsFormat.R16G16B16A16_SFloat;
            
            // Allocating the textures
            RenderingUtils.ReAllocateIfNeeded(ref _jumpFloodTexture, resizedCameraDescriptor, filterMode: FilterMode.Point, name: JUMPFLOOD_TEXTURE_NAME);
            RenderingUtils.ReAllocateIfNeeded(ref _jumpFloodPPTexture, resizedCameraDescriptor,filterMode: FilterMode.Point, name: JUMPFLOOD_PP_TEXTURE_NAME);
            RenderingUtils.ReAllocateIfNeeded(ref _distanceFieldTexture, resizedCameraDescriptor,filterMode: FilterMode.Point, name: DISTANCE_FIELD_TEXTURE_NAME);
            
            // Using Camera Depth Tex
            // RTHandle rtCameraDepth = renderingData.cameraData.renderer.cameraDepthTargetHandle;
            
            // Updating pass count, assuming that the screen is wider than it is tall
            uint width = (uint)Screen.width;
            width >>= _settings.downscaleAmount;
            _jumpFloodPassCount = Mathf.CeilToInt(Mathf.Log(width + 1.0f, 2f));
            
            // Starting texture to make sure we end on the correct texture after ping ponging
            RTHandle initialTexture = (_jumpFloodPassCount - 1) % 2 == 1 ? _jumpFloodTexture : _jumpFloodPPTexture;
            
            // Setting Rendering target
            ConfigureTarget(initialTexture);
            
            ConfigureClear(ClearFlag.Color, new Color(0, 0, 0, 0));
        }
        
        // Updating camera buffer to blit to camera
        public void SetCameraBuffers(RTHandle cameraColorTargetHandle)
        {
            _CameraColorBuffer = cameraColorTargetHandle;
        }

        // Disposing of Textures
        public void Dispose()
        {
            _jumpFloodTexture?.Release();
            _jumpFloodPPTexture?.Release();
            _distanceFieldTexture?.Release();
        }
    }
}

