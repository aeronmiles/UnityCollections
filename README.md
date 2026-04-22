# AM Unity Collections

A Unity Package Manager (UPM) package of C# extension methods, utility classes, runtime services, editor tools, and shaders — collected and maintained for reuse across Unity projects.

- **Package:** `com.am.unity.collections`
- **Version:** 1.0.1
- **Minimum Unity:** 2019.1 (tested against Unity 2022.3 LTS)
- **Render Pipeline:** Built-in RP primary; many shaders/subgraphs usable under URP via ShaderGraph
- **License:** MIT (© 2020 Aeron Miles)

---

## Table of Contents

1. [Overview](#overview)
2. [Installation](#installation)
3. [Dependencies](#dependencies)
4. [Package Layout](#package-layout)
5. [Runtime](#runtime)
   - [C# Extensions](#c-extensions)
   - [Unity Extensions](#unity-extensions)
   - [Components](#components)
   - [Services](#services)
   - [Types](#types)
   - [Utilities](#utilities)
   - [UI](#ui)
   - [Statistics](#statistics)
   - [Attributes](#attributes)
   - [Native Plugins](#native-plugins)
6. [Editor](#editor)
7. [Shaders](#shaders)
   - [Screen-Space Shaders](#screen-space-shaders)
   - [Standard / Masked Shaders](#standard--masked-shaders)
   - [UI Shaders](#ui-shaders)
   - [Compute Shaders](#compute-shaders)
   - [HLSL Include Libraries](#hlsl-include-libraries)
   - [ShaderGraph Subgraphs](#shadergraph-subgraphs)
8. [Assembly Definitions](#assembly-definitions)
9. [Usage Examples](#usage-examples)
10. [License](#license)

---

## Overview

Unity Collections is a grab-bag of production-tested helpers:

- **C# & Unity extension methods** covering primitives, collections, LINQ, transforms, vectors, textures, meshes, cameras, UI Toolkit, and native arrays.
- **Runtime services** for pooling (`RenderTexturePool`, `Texture2DPool`, `FactoryPool`), coroutine dispatch, main-thread marshalling, analytics, localization, scene loading, and camera capture.
- **MonoBehaviour base classes** including a thread-safe `MonoSingleton<T>` and a scene-scoped variant.
- **Result-type error handling** (`Result<T, E>`) for Rust-style flow without exceptions.
- **GPU tooling** — 3D LUT helpers, distance-field baking, render target management, and global shader settings.
- **Shader collection** — screen-space post effects, masked Standard variants, UI color grading, compute kernels, and 55+ ShaderGraph subgraphs including skin/pigment/pustule nodes used in the vitiligo simulation.

The package is intentionally framework-light: one `.asmdef` per assembly, no third-party runtime dependencies beyond Unity's own Mathematics / Collections / Newtonsoft packages.

---

## Installation

### Via Unity Package Manager (git URL)

```
Window > Package Manager > + > Add package from git URL...
https://github.com/aeronmiles/UnityCollections.git
```

### As a local package

Clone into your project's `Packages/` folder (or add via `Packages/manifest.json`):

```json
{
  "dependencies": {
    "com.am.unity.collections": "file:../path/to/UnityCollections"
  }
}
```

### As an embedded asset

Drop the `UnityCollections/` folder into your project's `Assets/` directory. The included `.asmdef` files isolate it from your code.

---

## Dependencies

Declared in `package.json`:

| Package | Version | Purpose |
|---|---|---|
| `com.unity.nuget.newtonsoft-json` | 3.2.1 | JSON (de)serialization used by `SerializationUtil` |
| `com.unity.mathematics` | 1.2.6 | `float2/3/4`, `float4x4` extensions, SIMD math |
| `com.unity.collections` | 2.1.4 | `NativeArray`, `NativeStack`, native util helpers |

The runtime `.asmdef` also references `Unity.TextMeshPro` and `Unity.Burst` and permits unsafe code.

---

## Package Layout

```
UnityCollections/
├── package.json                     # UPM manifest
├── LICENSE.md                       # MIT
├── README.md
├── Editor/                          # Editor-only tools
│   ├── AM.Unity.Collections.Editor.asmdef
│   ├── SetExecutionOrders.cs
│   ├── Extensions/
│   └── Util/
├── Runtime/
│   ├── AM.Unity.Collections.asmdef
│   ├── Attributes/                  # ReadOnlyAttribute + editor drawer
│   ├── C#.Extensions/               # Pure-C# extension methods
│   ├── Components/                  # MonoBehaviour helpers + singletons
│   ├── Debug/                       # SetVisibility
│   ├── Plugins/iOS/                 # UnityBridge.c native interop
│   ├── Services/                    # Managers, pools, dispatchers
│   ├── Shader/                      # DistanceFieldCompute wrapper
│   ├── Statistics/                  # Statistics aggregator + ext
│   ├── Types/                       # Result, CachedTransform, SerializableMesh, ...
│   ├── Types.Native/                # NativeStack
│   ├── UI/                          # Toggles, touch helpers, input
│   ├── Unity.Extensions/            # Extensions for UnityEngine APIs
│   ├── Util/                        # Application / IO / Texture / Serialization utils
│   └── Util.Native/                 # NativeUtil
└── Shaders/
    ├── *.shader                     # Top-level screen-space effects
    ├── HLSL/                        # Reusable .hlsl include libraries
    ├── Compute/                     # .compute kernels
    ├── Standard/                    # Standard-RP masked variants
    ├── StdMGAO/                     # Standard "Modified GAO" family
    ├── UI/                          # UI-shader color grading
    └── ShaderGraph/                 # .shadergraph / .shadersubgraph
```

---

## Runtime

### C# Extensions

`Runtime/C#.Extensions/` — extensions on pure-C# / BCL types:

| File | Extends |
|---|---|
| `ArrayExt.cs` | `T[]` |
| `ClassExt.cs` | `class` helpers |
| `ColorExt.cs` | `Color`, `Color32` |
| `DateTimeExt.cs` | `DateTime` |
| `DictionaryExt.cs` | `Dictionary<K,V>` |
| `EnumExt.cs` | `Enum` parsing/formatting |
| `FloatExt.cs` | `float` remap/clamp/approx |
| `GenericExt.cs` | generic helpers |
| `IEnumerableExt.cs` | `IEnumerable<T>` |
| `IEnumeratorExt.cs` | `IEnumerator` / coroutines |
| `LinqExt.cs` | LINQ-style extras |
| `ListExt.cs` | `List<T>` |
| `MathExt.cs` | math primitives |
| `ResultExt.cs` | `Result<T, E>` helpers |
| `StringExt.cs` | `string` parsing / formatting |

### Unity Extensions

`Runtime/Unity.Extensions/` — extensions for `UnityEngine` types:

| File | Extends |
|---|---|
| `BoundsExt.cs` | `Bounds` |
| `CameraExt.cs` | `Camera` viewport / projection |
| `CanvasExt.cs` | `Canvas` |
| `ColliderExt.cs` | `Collider` |
| `ComponentExt.cs` | `Component` / `GetOrAdd` |
| `float2Ext.cs`, `float4x4Ext.cs` | `Unity.Mathematics` types |
| `GameObjectExt.cs` | `GameObject` lookup & activation |
| `MeshExt.cs` | `Mesh` |
| `NativeArrayExt.cs` | `NativeArray<T>` |
| `ParticleSystemExt.cs` | `ParticleSystem` |
| `QuaternionExt.cs` | `Quaternion` |
| `RectTransformExt.cs` | `RectTransform` layout |
| `RendererExt.cs` | `Renderer` |
| `RenderTextureExt.cs` | `RenderTexture` |
| `SceneExt.cs` | `Scene` |
| `TextureExt.cs` | `Texture` / `Texture2D` |
| `TransformExt.cs` | `Transform` hierarchy ops |
| `UIToolKitExts.cs`, `VisualElementExt.cs` | UI Toolkit |
| `Vector2Ext.cs`, `Vector3Ext.cs` | `Vector2/3` |
| `WebCamTextureUtil.cs` | `WebCamTexture` helpers |

### Components

`Runtime/Components/` — MonoBehaviour building blocks:

- **`MonoSingleton<T>`** — thread-safe, lazy-initialized singleton base. Optional `DontDestroyOnLoad`, cleanup-aware.
- **`MonoSingletonScene<T>`** — scene-scoped variant; disposes on scene unload.
- **`MonoEvents`** — Unity lifecycle event dispatcher for subscribers that can't inherit MonoBehaviour.
- **`DontDestroyOnLoad`** — attach to persist a GameObject across scenes.
- **`DisableIfNotEditor`** — strip editor-only helper objects from builds.
- **`GameObjectActiveState`**, **`SetRotation`**, **`ResetTransformsOnEnable`**, **`ToUVSpace`** — small transform/state utilities.
- **`ID`** — stable string/int identifier component.
- **`ConstraintInfluence`**, **`JointController`** — animation / constraint helpers.
- **`WebCamImage`** — renders a `WebCamTexture` into a material with rotation/flip handling.

### Services

`Runtime/Services/` — managers and pools registered via `ServiceManager`:

- **`ServiceManager`** + **`ServiceManagerConfig`** — lightweight DI / service locator with a ScriptableObject config.
- **`CoroutineRunner`** — start/stop coroutines from non-MonoBehaviour code.
- **`UnityMainThreadDispatcher`** — enqueue work onto the main thread from background threads.
- **`RenderTargetManager`** — shared `RenderTexture` descriptors with global shader settings.
- **`RenderTexturePool`**, **`Texture2DPool`**, **`FactoryPool<T>`**, **`MemPool`** — typed pooling to avoid hot-path allocations.
- **`DataLoader`**, **`DataCache`** — async resource loading with an in-memory cache.
- **`SceneLoader`** — async scene transitions with progress callbacks.
- **`CameraCapture`** — screen / camera → `Texture2D` capture.
- **`UITouchHandler`** — unified touch routing for UI vs. world-space receivers.
- **`LocalizationManager`** — runtime localization manager.
- **`Analytics`**, **`AppLogger`**, **`LogHandlerFactory`** — diagnostics and event logging.

### Types

`Runtime/Types/` — reusable data types:

- **`Result<T, E>`** — discriminated `Ok` / `Err` container (see `ResultExt` for combinators).
- **`CommonErrors`** — shared error enumerations for `Result`.
- **`CachedTransform`** — frame-cached `Transform` snapshot to skip dirty checks.
- **`SerializableMesh`**, **`PointCacheData`** — mesh snapshots & point caches for persistence.
- **`DataStoreSO`** — generic `ScriptableObject` key/value store.
- **`AnimStateBehaviour`** — base `StateMachineBehaviour` with typed events.
- **`MeanMedianVarMinMax`** — rolling statistic accumulator.
- **`float5x4`**, **`XYZBool`**, **`TypeHashSet`** — small specialized structs / collections.
- **`Interfaces.cs`** — shared interface declarations.
- **`Types.Native/NativeStack.cs`** — burst-friendly stack on top of `NativeList`.

### Utilities

`Runtime/Util/`:

- **`ApplicationUtil`** — platform detection, quit/restart, persistent paths.
- **`IOUtil`** — file read/write, atomic replace, path normalization.
- **`SerializationUtil`** — JSON (Newtonsoft) + binary helpers.
- **`TextureUtil`** — resize, crop, encode, channel swap.
- **`LUT3D`** — 3D LUT load/bake/apply.
- **`EditorUtil`** — runtime-safe checks for editor state.
- **`ARUtil`** — AR session helpers.
- **`Util.Native/NativeUtil`** — unsafe/burst utilities.
- **`Shader/DistanceFieldCompute`** — C# wrapper around `DistanceFieldGenerator.compute` for SDF baking.

### UI

`Runtime/UI/`:

- **`ToggleButton`** + **`ToggleButtonManager`** — radio-group toggle controller.
- **`TouchCreator`** — synthesize touches for editor testing.
- **`InputHelper`** — unified mouse/touch query surface.

### Statistics

`Runtime/Statistics/Statistics.cs` plus `StatisticsExt.cs` — online mean/variance/stddev over streams with matching extension methods.

### Attributes

`Runtime/Attributes/ReadOnlyAttribute.cs` — `[ReadOnly]` inspector decorator, with its companion drawer in `Attributes/Editor/`.

### Native Plugins

`Runtime/Plugins/iOS/UnityBridge.c` — small C shim exposing functions to Unity's iOS plugin system.

---

## Editor

`Editor/` — editor-only tools (compiled into a separate assembly):

- **`SetExecutionOrders.cs`** — declarative script execution order setup; keeps ordering in source rather than `ProjectSettings`.
- **`Extensions/`** — `SerializedProperty`, `Editor`, and inspector utilities.
- **`Util/`** — editor-time helpers (build hooks, mesh inspection, etc.).

---

## Shaders

`Shaders/` — a mix of `.shader`, `.compute`, `.hlsl`, and ShaderGraph assets.

### Screen-Space Shaders

Post/blit effects suitable for `Graphics.Blit`:

| Shader | Purpose |
|---|---|
| `Contrast.shader` | Contrast adjustment |
| `Gamma.shader`, `GlobalGammaInverse.shader` | Gamma encode / inverse |
| `HighlightCompression.shader` | Highlight rolloff / soft-clip |
| `LowPass.shader`, `LowPass_SS.shader` | Low-pass blur (pass & screen-space) |
| `LensBlur.shader` | Disc / bokeh blur |
| `Lanczos2UpscalerCAS.shader` | Lanczos upscaler with contrast-adaptive sharpening |
| `AdaptiveHistogramEqualization.shader` | CLAHE-style histogram equalization |
| `BlitCropped.shader` | Blit with crop/viewport rect |
| `MultiplyTextures.shader`, `MultiplyTexturesToHSV_SS.shader`, `MultiplyTexturesToLab_SS.shader` | Multi-texture compositing in RGB / HSV / CIE Lab |
| `DepthMask.shader` | Depth-only mask writer |
| `Lut3D.shader` | 3D LUT color grading |
| `ReplaceAlpha_ScreenSpace.shader`, `Unlit_ScreenSpace.shader` | Alpha replacement / unlit blit |
| `Simplex.shader` | Simplex noise generator |
| `UV.shader`, `Normals.shader`, `TileRotate.shader` | Debug / utility visualizations |

### Standard / Masked Shaders

`Shaders/Standard/` — Built-in Standard RP variants:

- `StandardMasks.shader`, `StandardMasksTiled.shader` — Standard with extra mask channels.
- `StandardCutoutMasks.shader`, `StandardEmissiveCutout.shader` — cutout + emission variants.

`Shaders/StdMGAO/` — "Modified Ground AO" Standard family with additional double-sided and transparent permutations:

- `StdMGAO.hlsl` shared include.
- `StdMasks.shader`, `StdMasksTiled.shader`, `StdCutoutMasks.shader`, `StdEmissiveCutout.shader`.
- `StdDS.shader`, `StdDSTrans.shader`, `StdDSFrontTrans.shader`, `StdDSBackfaceTrans.shader` — double-sided opaque/transparent permutations.
- `StdFresnelTrans.shader` — fresnel transparency.

### UI Shaders

`Shaders/UI/`:

- `UIGamma.shader` — per-canvas gamma correction.
- `UIGammaLiftGain.shader` — gamma + lift/gain grading for UI.

### Compute Shaders

`Shaders/Compute/`:

- `DistanceFieldGenerator.compute` — SDF baking (driven by `Runtime/Shader/DistanceFieldCompute.cs`).
- `CalculateHSVBoundsSingleT.compute` — HSV min/max analysis over a texture.

### HLSL Include Libraries

`Shaders/HLSL/` — reusable `.hlsl` modules for custom shaders:

| File | Contents |
|---|---|
| `Common.hlsl`, `ShaderUtil.hlsl`, `Structures.hlsl`, `CC.hlsl` | Core helpers & common structs |
| `ColorConversion.hlsl` | RGB ↔ HSV / HSL / Lab / YCbCr |
| `Blending.hlsl` | Photoshop-style blend modes |
| `Masks.hlsl`, `Filters.hlsl` | Mask combinators and filter kernels |
| `Noise.hlsl`, `RidgedMF.hlsl` | Value / simplex / ridged-multifractal noise |
| `Pustules.hlsl` | Vitiligo / pustule procedural functions |
| `ParallaxOcclusion.hlsl` | Parallax occlusion mapping |
| `SmoothSquareWaveGlow.hlsl` | Smooth periodic glow pulse |

### ShaderGraph Subgraphs

`Shaders/ShaderGraph/` — one `Skin Lighting.shadergraph` entry point plus ~50 reusable `.shadersubgraph` nodes, grouped roughly as:

- **Skin / pigment:** `Skin Lighting`, `Skin Mask`, `Face Regions`, `Pigment`, `Pigment Texture`, `Veins`.
- **Vitiligo / pustule:** `Pustule Noise`, `Pustule Application`, `Pustule Normals`, `Pustule Lighting Lookup`.
- **Noise:** `FBM`, `FBM Noise Mask`, `Ridge Noise`, `RidgedMF`, `Seeded FBM Variance Noise`, `Bleed Noise`.
- **Color / HSV:** `HSV Diff`, `HSV Lerp`, `HSV Variance`, `Saturation Lerp RGBA`, `Texture To HSV`, `Grayscale`, `Lookup Luminance`, `Luminance Lerp`, `Luminance Lerp RGBA`, `Luminace Add Lerp`.
- **Math / utility:** `Add 3`, `Add 3 2`, `Add Lerp`, `Multiply 3`, `Maximum 4`, `Remap 2`, `Contrast Offset`, `High Pass Filter`, `Midtone Mask`, `Correct Invalid Pixels`.
- **UV / tiling:** `Tiling Offset Rotation`, `Seeded Offset Rotation`, `UV To XYZ`.

---

## Assembly Definitions

| Assembly | Path | Notes |
|---|---|---|
| `AM.Unity.Collections` | `Runtime/AM.Unity.Collections.asmdef` | Runtime; allows unsafe code; references Unity.Mathematics, Unity.TextMeshPro, Unity.Burst |
| `AM.Unity.Collections.Editor` | `Editor/AM.Unity.Collections.Editor.asmdef` | Editor-only; references the runtime assembly |

---

## Usage Examples

### MonoSingleton

```csharp
using AM.Unity.Collections;

public class AudioService : MonoSingleton<AudioService>
{
    public void Play(AudioClip clip) { /* ... */ }
}

// Anywhere:
AudioService.Instance.Play(clip);
```

### Result<T, E>

```csharp
public Result<Texture2D, string> LoadTexture(string path)
{
    if (!File.Exists(path)) return Result.Err<Texture2D, string>($"missing: {path}");
    var bytes = File.ReadAllBytes(path);
    var tex = new Texture2D(2, 2);
    return tex.LoadImage(bytes)
        ? Result.Ok<Texture2D, string>(tex)
        : Result.Err<Texture2D, string>("decode failed");
}
```

### Main-thread dispatch

```csharp
Task.Run(() =>
{
    var data = FetchRemote();
    UnityMainThreadDispatcher.Instance.Enqueue(() => ApplyOnUI(data));
});
```

### Render texture pooling

```csharp
var rt = RenderTexturePool.Get(width, height, 0, RenderTextureFormat.ARGB32);
Graphics.Blit(src, rt, material);
// ... use rt ...
RenderTexturePool.Release(rt);
```

### Custom shader using the HLSL includes

```hlsl
#include "Packages/com.am.unity.collections/Shaders/HLSL/ColorConversion.hlsl"
#include "Packages/com.am.unity.collections/Shaders/HLSL/Blending.hlsl"

half3 hsv = RGBToHSV(color.rgb);
half3 blended = BlendOverlay(base.rgb, layer.rgb);
```

---

## License

MIT — see [`LICENSE.md`](./LICENSE.md). © 2020 Aeron Miles.
