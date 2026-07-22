# Fog of War — Coverage Visualization for Scanning

This document describes the **Fog of War (FoW)** coverage visualization added to
`QuestRealityCapture`. It is the main contribution of this fork.

## Why

The base `QuestRealityCapture` app records synchronized stereo RGB, depth maps,
and 6-DoF poses on a Meta Quest 3, but gives the user **no feedback on which parts
of the room they have already scanned**. Missed regions produce holes in the
reconstruction that are only discovered offline, after the session.

The Fog of War is a **heads-up display (HUD)** that gives the user immediate
spatial feedback during scanning: the world starts covered in fog, and the fog
clears in the direction the user looks. What is still foggy has not been observed
yet, so the user knows where to keep scanning until sufficient data density is
reached.

## Design in one picture

```
Head pose (forward vector)
        │
        ▼
┌───────────────────────┐    stamps      ┌──────────────────────┐
│ FogMaskStamp.compute   │  ───────────▶  │  Mask RenderTexture   │
│ (CSMain kernel)        │   alpha=1      │  (equirect opacity)   │
└───────────────────────┘                └──────────┬───────────┘
                                                     │ sampled
                                                     ▼
                                          ┌──────────────────────┐
                                          │  FogSphere.shader      │
                                          │  (fragment shader)     │
                                          │  fog where mask == 0    │
                                          └──────────────────────┘
                                                     │
                                                     ▼
                                     Semi-transparent sphere around head
```

A large, semi-transparent sphere is centered on the user's head. Its opacity is
driven by a **persistent mask texture** that records everywhere the user has
looked. The pipeline has two GPU stages.

## Stage 1 — Mask generation (compute shader)

**Files:** [`FogSphereController.cs`](../Assets/RealityLog/Scripts/Runtime/UI/Coverage/FogSphereController.cs),
[`FogMaskStamp.compute`](../Assets/RealityLog/Shaders/FogMaskStamp.compute)

- The controller keeps one `RenderTexture` (default `256 x 128`, `R8`) that acts
  as a persistent **opacity mask** in equirectangular (longitude/latitude) space.
- Each frame (throttled to `maskUpdateInterval`, ~33 ms), `FogSphereController`
  takes the head's forward vector and dispatches the `CSMain` kernel.
- For every texel, the kernel reconstructs the world direction it represents,
  takes the dot product with the current view direction, and **stamps** the texel
  toward `1.0` if it falls inside the brush cone. The brush has a hard inner cone
  (`brushAngleDegrees`) and a feathered outer edge (`brushFeatherDegrees`) for a
  soft border.
- Stamping is **write-max** (`if (stampValue > previous)`), so cleared regions
  stay cleared — the mask only ever accumulates coverage. It is reset explicitly
  via `ResetFog()`.
- Optional `top`/`bottom` clear latitudes let the ceiling be pre-cleared and the
  floor be marked as an out-of-scope zone.

## Stage 2 — Rendering (fragment shader)

**File:** [`FogSphere.shader`](../Assets/RealityLog/Shaders/FogSphere.shader)

- A custom unlit URP shader draws the inside of the sphere (`Cull Front`,
  transparent blend, `ZWrite Off`).
- For each fragment it computes its own longitude/latitude from world-space
  geometry (not mesh UVs, to avoid pole distortion), samples the mask, and sets
  `fogFactor = 1 - mask`. So **observed regions become transparent** and
  unobserved regions stay foggy.
- A subtle noise texture breaks up the flat fog color; the bottom band can render
  a light red overlay to mark the un-scanned floor zone.

## Runtime behavior & controls

**Files:** [`FogSphereToggleBinding.cs`](../Assets/RealityLog/Scripts/Input/FogSphereToggleBinding.cs),
[`FogSphereResetBinding.cs`](../Assets/RealityLog/Scripts/Input/FogSphereResetBinding.cs),
[`CoverageRecordingStartHandler.cs`](../Assets/RealityLog/Scripts/Runtime/UI/Coverage/CoverageRecordingStartHandler.cs)

- The sphere **follows the head position but stays axis-aligned to the world**, so
  fog is anchored to real directions rather than rotating with the head.
- A controller button toggles / resets the fog. When a **recording session
  starts**, `CoverageRecordingStartHandler` resets the fog and force-locks the
  sphere visible, so every capture begins with a fresh coverage map that cannot be
  accidentally hidden mid-scan.
- Two scenes are provided: `Fog.unity` (with the HUD) and `No_Fog.unity` (the
  original capture behavior, for A/B comparison).

## Mapping to the paper

| Paper term | Implementation |
| --- | --- |
| HUD "Fog of War" visualization | `FogSphereController` + fog scenes |
| Mask Generation (Compute Shader) | `FogMaskStamp.compute`, `CSMain` kernel, write-max stamping into a persistent `RenderTexture` |
| Rendering (Fragment Shader) | `FogSphere.shader`, samples mask, `alpha = 1 - mask` |
| Two-stage GPU pipeline | stamp → mask texture → sample in fragment shader |
| Configurable brush size / softness | `brushAngleDegrees`, `brushFeatherDegrees` |

## Parameters worth knowing

| Parameter | Meaning |
| --- | --- |
| `sphereRadius` | Radius of the fog sphere around the head |
| `maskResolution` | Resolution of the equirectangular mask texture |
| `brushAngleDegrees` | Angular radius of the clearing brush |
| `brushFeatherDegrees` | Soft-edge width of the brush |
| `maskUpdateInterval` | Minimum time between mask stamps |
| `top/bottomClearAngleDegrees` | Pre-cleared ceiling / out-of-scope floor bands |
</content>
