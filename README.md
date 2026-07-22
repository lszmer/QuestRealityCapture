# QuestRealityCapture — Fog of War Coverage Visualization

<p align="center">
  <img src="docs/overview.png" alt="QuestRealityCapture" width="320"/>
</p>

**A Meta Quest 3 scanning app that adds a real-time "Fog of War" HUD on top of multimodal capture (stereo passthrough, depth, and 6-DoF poses), so the user can see which parts of the room have already been scanned.**

---

## 📖 Overview

This fork extends the open-source [`QuestRealityCapture`](https://github.com/t-34400/QuestRealityCapture) framework with a **Fog of War (FoW) coverage visualization**.

The base app records synchronized real-world data on a Meta Quest 3 — headset and controller poses, stereo passthrough images, camera characteristics, and depth maps — but gives the user **no feedback on what they have already scanned**. Missed regions become holes in the offline reconstruction that are only found after the session.

The **Fog of War** fixes this: the world starts covered in fog, and the fog clears in the direction the user looks. Whatever is still foggy has not been observed yet, guiding the user toward complete scene coverage during the scan.

> 👉 **The main contribution of this fork is documented in [docs/FogOfWar.md](docs/FogOfWar.md).**

For **data parsing, visualization, and reconstruction** of the captured data, refer to the companion project:
**[Meta Quest 3D Reconstruction](https://github.com/t-34400/metaquest-3d-reconstrucion)**

---

## Fog of War

The FoW is a heads-up display driven by a **two-stage GPU pipeline**:

1. **Mask generation (compute shader)** — a persistent equirectangular mask texture records every direction the user has looked. Each frame, the head's forward vector is "stamped" into the mask with a configurable, soft-edged brush ([`FogMaskStamp.compute`](Assets/RealityLog/Shaders/FogMaskStamp.compute), [`FogSphereController.cs`](Assets/RealityLog/Scripts/Runtime/UI/Coverage/FogSphereController.cs)).
2. **Rendering (fragment shader)** — a semi-transparent sphere around the head samples the mask; observed regions become transparent, unobserved regions stay foggy ([`FogSphere.shader`](Assets/RealityLog/Shaders/FogSphere.shader)).

Coverage resets automatically when a recording session begins, and two scenes (`Fog.unity` / `No_Fog.unity`) allow A/B comparison. See [docs/FogOfWar.md](docs/FogOfWar.md) for the full design.

---

## ✅ Base Capture Features (inherited through the fork)

* Records HMD and controller poses (in Unity coordinate system)
* Captures **YUV passthrough images** from **both left and right cameras**
* Logs **Camera2 API characteristics** and image format information
* Saves **depth maps** and **depth descriptors** from both cameras
* Automatically organizes logs into timestamped folders on internal storage

---

## 📢 NOTICE (v1.1.0)

Starting with version **1.1.0**, the **camera pose values** stored in `left_camera_characteristics.json` and `right_camera_characteristics.json` are now saved as **raw pose values directly obtained from the Android Camera2 API**.

### Migration Guide for Older Logs (v1.0.x and earlier)

In versions **prior to 1.1.0**, the camera poses were preprocessed into Unity coordinate space. To convert these older poses to match the new raw format convention, apply the following transformation:

* **Translation (position)**:

  ```
  (x, y, z) → (x, y, -z)
  ```

* **Rotation (quaternion)**:

  ```
  (x, y, z, w) → (-x, -y, z, w)
  ```

This conversion aligns the preprocessed Unity pose with the raw Android pose representation now used in version 1.1.0 and later.

---

## 🧾 Data Structure

Each time you start recording, a new folder is created under:

```
/sdcard/Android/data/com.t34400.QuestRealityCapture/files
```

Example structure:

```
/sdcard/Android/data/com.t34400.QuestRealityCapture/files
└── YYYYMMDD_hhmmss/
    ├── hmd_poses.csv
    ├── left_controller_poses.csv
    ├── right_controller_poses.csv
    │
    ├── left_camera_raw/
    │   ├── <unixtimeMs>.yuv
    │   └── ...
    ├── right_camera_raw/
    │   ├── <unixtimeMs>.yuv
    │   └── ...
    │
    ├── left_camera_image_format.json
    ├── right_camera_image_format.json
    ├── left_camera_characteristics.json
    ├── right_camera_characteristics.json
    │
    ├── left_depth/
    │   ├── <unixtimeMs>.raw
    │   └── ...
    ├── right_depth/
    │   ├── <unixtimeMs>.raw
    │   └── ...
    │
    ├── left_depth_descriptors.csv
    └── right_depth_descriptors.csv
```

---

## 📄 Data Format Details

### Pose CSV

* Files: `hmd_poses.csv`, `left_controller_poses.csv`, `right_controller_poses.csv`
* Format:

  ```
  unix_time,ovr_timestamp,pos_x,pos_y,pos_z,rot_x,rot_y,rot_z,rot_w
  ```

### Camera Characteristics (JSON)

* Obtained via Android Camera2 API
* Includes pose, intrinsics (fx, fy, cx, cy), sensor info, etc.

### Image Format (JSON)

* Includes resolution, format (e.g., `YUV_420_888`), per-plane buffer info
* Contains baseMonoTimeNs and baseUnixTimeMs for timestamp alignment

### Passthrough Camera (Raw YUV)
- Raw YUV frames are stored as `.yuv` files under `left_camera_raw/` and `right_camera_raw/`.
- Image format and buffer information are provided in the accompanying `*_camera_image_format.json` files.

To convert passthrough YUV (YUV_420_888) images to RGB for visualization or reconstruction, see: [Meta Quest 3D Reconstruction](https://github.com/t-34400/metaquest-3d-reconstrucion)

### Depth Map Descriptor CSV

* Format:

  ```
  timestamp_ms,ovr_timestamp,create_pose_location_x, ..., create_pose_rotation_w,
  fov_left_angle_tangent,fov_right_angle_tangent,fov_top_angle_tangent,fov_down_angle_tangent,
  near_z,far_z,width,height
  ```

### Depth Map

* Raw `.float32` depth images (1D float per pixel)

To convert raw depth maps into linear or 3D form, refer to the companion project: [Meta Quest 3D Reconstruction](https://github.com/t-34400/metaquest-3d-reconstrucion)

---

## 🚀 Building the APK from Unity

This fork is a Unity project — there is no prebuilt APK for the Fog of War version. You build it yourself from source and deploy it to the headset. (For the **vanilla** app, a prebuilt APK is available from the [upstream releases](https://github.com/t-34400/QuestRealityCapture/releases); see the upstream README for that path.)

### 1. Install the correct Unity version

This project **must** be opened with **Unity 6000.2.9f1** (see [`ProjectSettings/ProjectVersion.txt`](ProjectSettings/ProjectVersion.txt)). Opening it with a different version may trigger asset/package upgrades.

1. Install [Unity Hub](https://unity.com/download).
2. In Unity Hub → **Installs** → **Install Editor** → **Archive**, install version **6000.2.9f1**.
3. When prompted for modules, check **Android Build Support** (this also installs the **Android SDK & NDK Tools** and **OpenJDK** sub-modules — all three are required).

### 2. Open the project

1. In Unity Hub → **Projects** → **Add** → select this repository's root folder.
2. Open it with **6000.2.9f1**. The first import may take several minutes.

### 3. Configure the build target

1. **File → Build Profiles** (Unity 6). Select **Android** (or the **Meta Quest** platform, available in Unity 6.1+) and click **Switch Platform** if it isn't already active.
2. Confirm the **Scene List** contains `Assets/RealityLog/Scenes/Fog.unity` (the Fog of War scene — enabled by default). Swap in `No_Fog.unity` for the original, fog-free capture behavior.
3. The key settings are already configured in the project and shouldn't need changing:
   * Scripting backend: **IL2CPP**, target architecture: **ARM64**
   * Min/Target Android SDK: **32**
   * Application ID: `com.CHL.Fog_RealityLog`
   * **Project Settings → XR Plug-in Management → Android**: **OpenXR** enabled with the **Oculus Touch Controller Profile**

### 4. Enable Developer Mode on the Quest

1. In the **Meta Horizon** mobile app, pair your headset and enable **Developer Mode** (requires a [Meta developer account / verified organization](https://developer.oculus.com/manage/)).
2. Connect the Quest to your computer via USB-C and put on the headset to **Allow USB debugging** when prompted.

### 5. Build & deploy

**Option A — Build and Run (recommended):** With the headset connected, in **Build Profiles** click **Build And Run**. Unity builds the APK and installs it directly onto the headset.

**Option B — Build the APK, then install manually:** Click **Build**, choose an output path (e.g. `Fog_RealityLog.apk`), then install it with ADB:

```bash
adb install -r Fog_RealityLog.apk
```

Alternatively, drag the APK onto the device using **[Meta Quest Developer Hub (MQDH)](https://developers.meta.com/horizon/documentation/unity/ts-odh/)** or **SideQuest**.

### 6. Run it

1. Launch the app on the **Meta Quest 3 or 3s** (firmware **v74+** required) — it appears under **Unknown Sources** in the app library.
2. When the green instruction panel appears, press the **menu button on the left controller** to dismiss it and start logging.
3. The **Fog of War** sphere appears around you — look around to clear the fog and see which regions still need scanning. A controller button toggles / resets the fog, and it resets automatically when recording begins.
4. Data is saved under the session folder as described above.

Required permissions (camera/scene access) are requested automatically at runtime.

> For details on the underlying vanilla capture app (data parsing, the prebuilt APK, and the reconstruction pipeline), see the [upstream QuestRealityCapture README](https://github.com/t-34400/QuestRealityCapture).

---

## 🛠 Environment

* Unity **6000.2.9f1** (required — this fork; the upstream vanilla app targets an older 6000.0.x)
* Android Build Support (with Android SDK/NDK + OpenJDK modules)
* Meta OpenXR SDK
* Device: Meta Quest 3 or 3s only
* Approx. recording frame rate: \~25 FPS (camera & depth)

---

## 📝 License

This project is licensed under the **[MIT License](LICENSE)**.

This project uses Meta’s OpenXR SDK — please ensure compliance with its license when redistributing.

---

## 🙏 Acknowledgments

The multimodal capture backend is built on the open-source [`QuestRealityCapture`](https://github.com/t-34400/QuestRealityCapture) framework by [t-34400](https://github.com/t-34400). This fork adds the **Fog of War coverage visualization** on top of it.
