## Preview


https://github.com/user-attachments/assets/ac397a4f-95e6-4562-bfac-bfe8ae2a17cf


## Core Technology

- **Ray Marching Technique**  
  Uses screen-space ray marching to simulate light propagation in 2D space.

- **Temporal Accumulation**  
  Employs temporal accumulation to reduce noise and provide smoother results by blending new and previous frame data.

- **Jump Flood Algorithm**  
  Efficiently generates distance fields for silhouettes which are used for ray marching lighting calculations.

- **URP Integration**  
  Implemented as a custom render feature for Unity's Universal Render Pipeline.

---

## Rendering Pipeline

1. **Silhouette Generation**  
   Captures silhouettes of objects in the scene.

2. **Jump Flood Pass**  
   Uses Jump Flood Algorithm to create accurate distance fields from object silhouettes.

3. **Ray Marching Pass**  
   Casts rays from each pixel to simulate light propagation.

4. **Temporal Accumulation**  
   Blends current frame with previous frames to reduce noise.

5. **Post-Processing**  
   Applies variable Gaussian blur for smoother results on lower end specs.
