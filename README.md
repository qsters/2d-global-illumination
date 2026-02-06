# 2D Global Illumination

A real-time **2D global illumination** prototype built in **Unity 2022.3 LTS** using **URP 14 (2D Renderer)**.

It generates a **screen-space distance field each frame** (via **Jump Flood**) and computes indirect lighting with **stochastic, distance-field–guided ray marching** plus **temporal accumulation** for denoising. The result is exposed as a global texture: **`_CustomLightMapTexture`**.

## Preview

https://github.com/user-attachments/assets/ac397a4f-95e6-4562-bfac-bfe8ae2a17cf

## Technical Highlights

* **URP integration:** implemented as a `ScriptableRendererFeature` with two render passes (Jump Flood + ray-marched lighting).
* **Per-frame distance field (JFA):** silhouettes → seed buffer → jump flood propagation → distance transform used for stepping.
* **Distance-field–guided ray marching (sphere tracing):** rays advance by sampled distance-to-occluder until hit/bailout.
* **Stochastic sampling:** multi-ray sampling per pixel using a **golden-angle spiral** + per-pixel rotation to reduce structured noise.
* **Temporal accumulation:** exponential moving average across frames (tunable).
* **Adaptive sampling:** optional dynamic sample budget to target a frame rate.
* **Optional denoise blur:** separable Gaussian blur (2-pass) to smooth residual noise at low sample counts / downscaled buffers.
* **Two-camera workflow:** a dedicated “lighting camera” computes the light map; the main camera renders normally and materials sample the light map in screen space.

## Pipeline Overview

Each frame, the lighting camera produces a screen-space light map:

1. **Silhouette seeds**

   * Renders silhouettes from a configurable `LayerMask` into a seed buffer (screen-position seeds).
2. **Jump Flood**

   * Ping-pong iterations from large → small step sizes (3×3 neighborhood) to propagate nearest seed coordinates.
3. **Distance field**

   * Converts nearest-seed coordinate → scalar distance; exposed globally as **`_DistanceFieldTexture`**.
4. **Emission capture**

   * Renders only an `EmissionRendering` pass into **`_EmissionTexture`** (emissive sprites come from the custom lit sprite shader).
5. **Lighting pass**

   * For each pixel: cast multiple rays, sphere-trace against the distance field, return first emissive hit (otherwise ambient).
   * Blend with previous frame (temporal accumulation).
6. **Post blur (optional)**

   * Separable Gaussian blur for smoothing/denoise.
7. **Bind global texture**

   * Final output bound as **`_CustomLightMapTexture`** for materials to sample.

**Frame Debug Render**: See these steps rendered step by step here:

https://github.com/user-attachments/assets/19b93e76-498b-48e4-bc5c-16e866d396d8

## Implementation Notes

* **JFA iteration count:** `ceil(log2(width + 1))` (logarithmic in render target size).
* **Render formats:**

  * Seed/JFA buffers: `GraphicsFormat.R16G16B16A16_SFloat` (seed coord storage).
  * Lighting accumulation: `GraphicsFormat.R32G32B32A32_SFloat` (stable temporal accumulation).
* **Predictable cost:** ray marcher caps max steps (15 in shader).
* **Skip work on fully-opaque non-emissive pixels:** avoids marching where lighting can’t contribute (useful when large regions are solid).
* **Primary quality/perf knobs:** downscale, samples/adaptive samples, time constant, blur, ambient, and layer mask.

## Where to Look in the Code

**Core C#**

* `Assets/RayMarchedLighting/RayMarchingRenderFeature.cs` — render feature wiring + settings.
* `Assets/RayMarchedLighting/Passes/JumpFloodPass.cs` — silhouette render, JFA ping-pong, distance field output.
* `Assets/RayMarchedLighting/Passes/RayMarchLightingPass.cs` — emission capture, ray march, temporal accumulation, blur, global bindings.
* `Assets/RayMarchedLighting/Camera/LightCamera.cs` — keeps lighting camera ortho size matched to a reference camera.

**Core Shaders**

* `Assets/RayMarchedLighting/Shaders/JumpFloodShader.shader` — `UVSILHOUETTE`, `JUMPFLOOD`, `DISTANCEFIELD`.
* `Assets/RayMarchedLighting/Shaders/RayMarchLightingShader.shader` — ray marching, temporal accumulation, blur passes.
* `Assets/RayMarchedLighting/Shaders/CustomLitSpriteShader.shader` — emits via `EmissionRendering` pass and samples `_CustomLightMapTexture`.

## Run Locally

**Requirements**

* Unity **2022.3.62f3** (`ProjectSettings/ProjectVersion.txt`)
* URP **14.0.12** (`Packages/manifest.json`)
* GPU/API supporting shader model used here (`#pragma target 4.5` for Jump Flood)

**Steps**

1. Open the project in Unity.
2. Open: `Assets/DemoAssets/DemoScene.unity`
3. Press Play.

## Tuning

Settings live on the `RayMarchingRenderFeature` in:

* `Assets/Settings/Renderer2DLighting.asset`

Key parameters:

* `downscaleAmount` — GI buffer resolution scale.
* `samples`, `useAdaptiveSamples`, `targetFrameRate` — ray budget (fixed or adaptive).
* `timeSpan` — temporal accumulation time constant (higher = smoother, slower response).
* `blurStrength`, `fancyBlur` — optional denoise blur.
* `ambientColor` — fallback when rays miss.
* `layerMask` — which layers contribute silhouettes/emission.

## Demo Setup

The demo scene uses:

* A **lighting camera** (`m_RendererIndex: 1`, renderer = `Renderer2DLighting`) with culling mask `0` so it doesn’t render the scene normally.
* A **reference/main camera** that renders the scene using materials sampling `_CustomLightMapTexture`.

## Limitations / Tradeoffs

* **Screen-space only:** off-screen emitters/occluders don’t contribute.
* **Single-hit model:** rays return the first emissive hit (no multi-bounce).
* **Blur can bleed:** Gaussian blur is pragmatic and can leak across thin occluders.
* **Unsigned distance transform:** good for stepping, but doesn’t encode inside/outside.

## License
See `LISENCE`