<div align="center">

# ⚡ Chromium CrispVideo Patcher

**Fix blurry, soft, and washed-out YouTube / HTML5 videos on Windows 10 & 11**  
*(The infamous "Video becomes sharper only when right-clicking or hovering UI" bug)*

[![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011%20(64--bit)-0078D6?style=for-the-badge&logo=windows)](https://github.com/)
[![Browsers](https://img.shields.io/badge/Browsers-Chrome%20%7C%20Edge%20%7C%20Brave%20%7C%20Opera%20%7C%20Vivaldi-58A6FF?style=for-the-badge&logo=googlechrome)](https://github.com/)
[![Size](https://img.shields.io/badge/Binary%20Size-~35%20KB%20(Zero%20Deps)-2ea043?style=for-the-badge)](https://github.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=for-the-badge)](LICENSE)

</div>

---

## 📖 Overview

Have you ever noticed on **Google Chrome, Microsoft Edge, or Brave** that while watching a YouTube or HTML5 `<video>`, the image looks slightly **blurry, soft, or washed-out**—but the exact moment you **right-click on the video** (bringing up the context menu), the video suddenly snaps into **crystal-clear sharpness and deep, accurate colors**, only to turn blurry again as soon as you close the menu?

**Chromium CrispVideo Patcher** is a lightweight, standalone Windows GUI utility (~35 KB, zero external dependencies) that permanently resolves this hardware overlay & video processor bug across all Chromium-based browsers.

---

## 🔬 Technical Root Cause Analysis

### 1. Hardware Overlay Promotion (`NV12` / `YUY2` SwapChain) vs. Direct RGB Compositing
By default on Windows 10/11, Chromium's **Viz Display Compositor** (`ui/gl/swap_chain_presenter.cc`) attempts to save power by promoting `<video>` elements into a dedicated **DirectComposition Hardware Overlay / Multiplane Overlay (MPO)** decoded swap chain.

- **Normal Playback (No UI overlapping the video):**
  Chromium hands the video frame over to `ID3D11VideoContext::VideoProcessorBlt`. On many **NVIDIA + Intel Hybrid GPU** setups, high-refresh-rate monitors, or standard SDR displays, the GPU driver's Video Processor applies:
  - Chroma subsampling degradation (`NV12` 4:2:0) and soft bilinear scaling.
  - Limited YCbCr color range (`16–235` instead of Full `0–255` RGB) or aggressive driver post-processing filters.
  - Cross-adapter DMA copy overhead when the browser renders on the iGPU (Intel/AMD) while the monitor is wired to the dGPU (NVIDIA RTX).
- **When You Right-Click the Video:**
  Because a native/DOM context menu now overlaps the video quad, Chromium's compositor **immediately aborts Hardware Overlay promotion** and falls back to **Direct RGB Shader Compositing** (`Full Range 0–255` RGB). The video instantly looks sharper and richer. Closing the context menu re-promotes the video back into the blurry Hardware Overlay plane.

### 2. Why Old Tutorials Don't Work Anymore (Chromium 130–156+ Changes)
Older guides tell users to pass `--disable-direct-composition-video-overlays` (with hyphens `-`) or search `chrome://flags` for *"Hardware Overlay"*.
- In modern Chromium builds (**Chrome 130 through 156+**), Google **removed the hyphenated command-line switch** from being forwarded to `--type=gpu-process`.
- Instead, the actual flags recognized by the GPU process are internal **GPU Driver Bug Workarounds** using **underscores (`_`)** with explicit `=1` values:
  ```text
  --disable_direct_composition_video_overlays=1
  --disable_vp_scaling=1
  --disable_vp_super_resolution=1
  --disable-features=DirectCompositionVideoOverlays,DirectCompositionSoftwareOverlays
  ```

---

## ✨ Key Features

- 🔍 **5-Stage Dynamic Browser Scanner:**
  Automatically locates **Google Chrome, Microsoft Edge, Brave, Coc Coc, Vivaldi, and Opera/Opera GX** regardless of which drive (`C:\`, `D:\`, `E:\`, etc.) or custom directory they are installed in by scanning:
  1. Active running processes in RAM (`Process.MainModule`).
  2. Windows Registry `App Paths` (`HKLM` & `HKCU`).
  3. Registry `StartMenuInternet` & `Classes\*\shell\open\command` handlers.
  4. All fixed physical drives (`DriveInfo.GetDrives()`).
  5. Reverse-scanning all `.lnk` shortcuts on Desktop, Start Menu, and Pinned Taskbar by inspecting their internal `TargetPath`.
- ➕ **Custom Chromium Browser Support:**
  Includes an **"Add Custom Chromium Browser (.exe)..."** button to patch portable browsers or niche forks (*Thorium, Ungoogled Chromium, CentBrowser, Supermium, Arc, etc.*).
- 🛡️ **4-Layer Deep Fix:**
  1. **DirectX High-Performance GPU Binding (`GpuPreference=2`):** Binds the browser executable to your discrete GPU in `HKCU\Software\Microsoft\DirectX\UserGpuPreferences`, eliminating iGPU-to-dGPU cross-adapter blur.
  2. **Windows DWM MPO Fix (`OverlayTestMode = 5`):** Sets `HKLM\SOFTWARE\Microsoft\Windows\Dwm\OverlayTestMode = 5` to prevent Windows Desktop Window Manager Multiplane Overlay flickering and color shifts.
  3. **`Local State` Experiment Injection:** Configures `"overlay-strategies@1"` (`Select HW overlay strategies = None`) directly inside the browser's `User Data\Local State` JSON.
  4. **GPU Process Workaround Injection:** Patches Desktop, Start Menu, Taskbar shortcuts, and Registry shell open commands with `--disable_direct_composition_video_overlays=1 --disable_vp_scaling=1 --disable_vp_super_resolution=1`.
- 🔄 **Zero-Downtime Session Restore:**
  Automatically restarts patched browsers with `--restore-last-session` so all your open tabs are preserved.
- ↩️ **1-Click Revert:**
  Want to go back to stock settings? Click **`↩️ REVERT TO DEFAULT`** to cleanly remove all flags, registry keys, and restore stock behavior in 1 second.

---

## 🚀 How to Use

1. Download **`ChromiumCrispVideoPatcher.exe`** from the [Releases](../../releases) page (or run it directly from the repository root).
2. Launch **`ChromiumCrispVideoPatcher.exe`**.
   - *Tip:* Click the **`🛡️ Restart as Admin`** button in the top-right corner if you want the patcher to also apply the system-wide Windows DWM MPO registry fix and update `HKLM` shell launchers.
3. Check the browsers you want to patch (**Google Chrome, Microsoft Edge, Brave**, etc.).
4. Click **`🚀 APPLY CRISP VIDEO PATCH`**.
5. Your browser will automatically restart and restore your open tabs. Open any YouTube video and right-click—the video will now stay **100% crisp and color-accurate at all times!**

---

## ❓ FAQ & Safety

**Q: Does this disable Hardware Acceleration in my browser?**  
**A:** **No!** Hardware Acceleration (GPU rasterization, WebGL, WebGPU, and hardware video decoding via NVDEC) remains **100% enabled**. This patch only stops Chromium from handing the decoded video frame to the legacy D3D11 Hardware Overlay / Video Processor scaler, forcing Chromium to composite the frame directly via high-precision RGB shaders.

**Q: Will this affect my games or other desktop applications?**  
**A:** **Not at all.** The command-line switches and GPU preferences are scoped strictly to the selected browser executables. Your games, FPS, and global NVIDIA/AMD driver settings are untouched.

**Q: Why doesn't Windows SmartScreen or Antivirus complain? How can I verify it's safe?**  
**A:** The entire application is written in ~650 lines of clean, transparent C# ([`src/Program.cs`](src/Program.cs)) using standard .NET WinForms and Registry APIs, and compiles in 1 second using the built-in Windows compiler (`csc.exe`).

---

## 🛠️ Building from Source

You do **not** need Visual Studio or any external SDKs/NuGet packages installed. Every Windows 10/11 machine includes the .NET Framework 4.x C# compiler (`csc.exe`) out of the box.

1. Clone this repository:
   ```cmd
   git clone https://github.com/YOUR_USERNAME/ChromiumCrispVideoPatcher.git
   cd ChromiumCrispVideoPatcher
   ```
2. Run the build script:
   ```cmd
   build.bat
   ```
3. The standalone executable **`ChromiumCrispVideoPatcher.exe`** will be generated in the root folder.

---

## 📂 Project Structure

```text
ChromiumCrispVideoPatcher/
├── src/
│   ├── Program.cs          # Complete C# WinForms Dark UI & 5-stage scanner source code
│   └── app.manifest        # High-DPI awareness & Win32 application manifest
├── ChromiumCrispVideoPatcher.exe  # Prebuilt standalone executable (~35 KB)
├── build.bat               # 1-click compiler script using built-in Windows csc.exe
├── LICENSE                 # MIT License
└── README.md               # Documentation
```

---

## 📄 License

Distributed under the **MIT License**. See [`LICENSE`](LICENSE) for more information.
