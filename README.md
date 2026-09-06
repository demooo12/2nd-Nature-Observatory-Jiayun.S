# SNOD

🎬 **Video / 作品视频:** https://youtu.be/SgXzkfbPTCM

> An interactive installation about signal, broadcast, and second nature.
> 一个关于「信号」与「第二自然」的交互装置。

The audience turns a single physical knob. A forest inside the screen grows or fades, a background video sharpens from snowy noise into a clear image, and a `SNO · LIVE` monitoring overlay in the corner ticks through resolution, node count, and forest coverage — as if watching a fictional broadcaster live-streaming "nature."

观众转动一个实体旋钮，画面里的森林随之生长或消退，背景视频从雪花噪点逐渐变得清晰，屏幕角落的 `SNO · LIVE` 监控角标实时跳动着分辨率、节点数与森林覆盖率——像是在收看一个虚构机构对「自然」的信号直播。

---

## Concept · 作品概念

A single potentiometer knob is the installation's **only input**. Its value (0–1) is mapped simultaneously onto three layers:

一个电位器旋钮是整件作品**唯一的输入**。它的值（0–1）被同时映射到三层画面上：

| Knob up · 旋钮越往上拧 | Result · 效果 |
|---|---|
| **Unity forest · 森林** | More trees; sky, lighting and leaf color shift toward "overload" · 树越多，天空/光影/树叶向「过载」偏移 |
| **TouchDesigner video · 视频** | Signal sharpens from noise → clear (clarity comes late) · 从模糊/噪点逐渐变清晰（越往后越通透） |
| **HUD overlay · 角标** | `RESOLUTION` 360p→4K · `ACTIVE NODES` 12→847 · `FOREST COVERAGE` 6%→98% |

When the knob returns to the low end, the servo push-rod stops and re-centers. The fuller the digital world, the prettier the "second nature" metrics.

旋钮到底时，同步驱动的舵机推杆停摆复位；数字世界拧得越满，「第二自然」的指标也越漂亮。

---

## System · 系统架构

```
┌──────────────┐   USB serial  ┌──────────────┐   UDP 127.0.0.1:7000   ┌──────────────────┐
│   Arduino    │ ────────────▶ │    Unity     │ ─────────────────────▶ │  TouchDesigner   │
│ pot + servo  │   0–1023      │ forest + HUD │      forwards raw       │  video clarity   │
└──────────────┘               └──────────────┘                        └──────────────────┘
```

Arduino reads the potentiometer and sends an integer `0–1023` over serial to Unity. Unity normalizes it to `0–1` to drive the forest and HUD, and forwards the raw value over UDP to TouchDesigner, which drives the background video's clarity / glitch.

Arduino 读电位器，串口发 `0–1023` 给 Unity；Unity 归一化成 `0–1` 驱动森林与 HUD，并把原始值经 UDP 转发给 TouchDesigner 控制视频清晰度 / glitch。

---

## Folder Structure · 文件夹结构

```
SNOD/
├── SNO_arduinocode/
│   └── SNO_arduinocode.ino        # Arduino: pot read + servo push-rod + serial send · 电位器读值 + 舵机 + 串口发送
├── unity/                         # Unity project · Unity 工程
│   └── Assets/
│       ├── ForestGrowthController.cs   # Core: serial, forest density, UDP forward, port picker · 串口/森林密度/转发/端口面板
│       ├── SNODHudOverlay.cs           # Top-left SNO·LIVE HUD · 左上角监控角标
│       ├── SNOD_Logo.png / _Black.png  # Station logo · 台标
│       └── Scenes / 树Polytope Studio / ...
├── videoController.7.toe          # TouchDesigner project · TD 工程（视频清晰度控制）
├── SNO.app/                       # Built Unity macOS app · 打包好的 App
├── documentary.mp4                # Background footage · 背景影像素材
├── logo design.png                # Logo source · 台标源文件
└── 声音.mp3                        # Audio · 音频素材
```

---

## Hardware · 硬件清单

- Arduino (Uno / Nano, etc.) × 1
- Rotary potentiometer × 1 → pin `A0` · 旋转电位器接 `A0`
- Servo (push-rod) × 1 → signal on `D9`, separate power + common ground · 舵机信号接 `D9`，独立供电并共地
- USB cable from Arduino to the Unity machine · USB 线连 Arduino 与 Unity 电脑
- macOS machine × 1 (or two, see *Exhibition Tips*) · macOS 电脑一台（或分两台）
- Optional: iPad as a Sidecar second display · 可选：iPad 作 Sidecar 副屏

---

## Running · 运行说明

### 1. Arduino (knob + servo · 旋钮 + 舵机)

Open `SNO_arduinocode/SNO_arduinocode.ino` in the Arduino IDE and upload. **Close the Serial Monitor after uploading** — otherwise it holds the port and Unity can't connect.

用 Arduino IDE 打开 `SNO_arduinocode/SNO_arduinocode.ino` 并上传。**上传后关闭串口监视器**——否则占用串口，Unity 连不上。

Tunable (top of the .ino) · 可调参数（.ino 顶部）: `SERVO_AMPLITUDE` (swing amplitude · 摆动幅度), `REST_THRESHOLD` (re-center threshold · 停摆复位阈值).

### 2. Unity (forest + HUD · 森林 + HUD 角标)

Run the build by double-clicking `SNO.app` (right-click → Open if Gatekeeper blocks it).

直接运行成品：双击 `SNO.app`（首次被 Gatekeeper 拦截时右键 → 打开）。

To rebuild from the project: open `unity/` in **Unity 2022.3.x**; set **Player Settings → Api Compatibility Level = `.NET Framework`** (required for serial); build for **macOS** with the scene added to Build Settings. On launch a serial-port picker appears — choose the `/dev/cu.usbmodem…` entry.

从工程重新打包：用 **Unity 2022.3.x** 打开 `unity/`；把 **Player Settings → Api Compatibility Level 设为 `.NET Framework`**（串口必须）；目标 **macOS** 并把场景加入 Build Settings。运行后弹出串口选择面板，选 `/dev/cu.usbmodem…` 那个。

### 3. TouchDesigner (video · 视频信号)

Open `videoController.7.toe` in **TouchDesigner 2025.x**, make sure `udpin1` listens on **port 7000**, and use **Perform Mode** for final output (`Esc` to exit).

用 **TouchDesigner 2025.x** 打开 `videoController.7.toe`，确认 `udpin1` 监听 **端口 7000**，正式输出用 **Perform Mode**（`Esc` 退出）。

---

## Data Flow & Ports · 数据流与端口

| Link · 环节 | Protocol · 协议 | Address · 地址 | Range · 数值 |
|---|---|---|---|
| Arduino → Unity | USB serial | `/dev/cu.usbmodem*` @ 9600 | `0–1023` |
| Unity → TouchDesigner | UDP | `127.0.0.1:7000` | `0–1023` |

Two machines: change the **Td IP** field on Unity's `ForestGrowthController` from `127.0.0.1` to the TD machine's LAN IP; put both on the same network (wired recommended).

两台电脑分开跑时：把 Unity 里 `ForestGrowthController` 的 **Td IP** 从 `127.0.0.1` 改成 TD 那台的局域网 IP，两机接同一网络（建议网线）。

---

## Live Shortcuts · 现场操作快捷键

`Esc` — quit · 退出程序 &nbsp;|&nbsp; `.` (period) — toggle fullscreen · 切换全屏

The Unity startup panel lets you pick / type the serial port on site, no code change needed.

Unity 启动面板可现场选择/手输串口，无需回工程改代码。

---

## Exhibition Tips · 布展提示

An M1 Pro / 16 GB usually runs Unity + TD together fine; if it stutters or memory pressure is high, split onto two machines (just change the UDP target IP). Keep TD video at 1080p and a hardware-friendly codec (H.264 or HAP). Give the exhibition machine a fixed LAN IP, hide the cursor (TD `window1` → `Cursor Visible = Off`), and watch thermals over long runs.

M1 Pro / 16G 一台机同时跑 Unity + TD 一般够用；若卡顿/内存压力偏高，可分两台（改 UDP 目标 IP 即可）。TD 视频尽量用 1080p + 硬件友好编码（H.264 或 HAP）。展览机建议设固定局域网 IP、隐藏鼠标（TD `window1` 的 `Cursor Visible` 设 `Off`），长时间运行注意散热。

---

## Credits

Interaction / installation design & development · 交互 / 装置设计与开发: **Jiayun.S 小红书：鼠岛SHUDIO**

Engines · 引擎: Unity 2022.3, TouchDesigner 2025, Arduino

Third-party assets · 第三方资源: Polytope Studio (low-poly trees), Darkbringer Shader, Glitched and Corrupted UI SFX, etc. — see subfolders in `unity/Assets/`.

---

*SNOD — tune in to a live broadcast of nature. · 收看一场关于自然的信号直播。*
│       ├── ForestGrowthController.cs   # Core: serial read, forest density, UDP forward, startup port picker
│       ├── SNODHudOverlay.cs           # Top-left SNO·LIVE monitoring HUD · 监控角标
│       ├── SNOD_Logo.png / _Black.png  # Station logo · 台标
│       └── Scenes / 树Polytope Studio / ...
├── videoController.7.toe          # TouchDesigner project (video clarity control)
├── SNO.app/                       # Built Unity macOS app · 打包好的可执行 App
├── documentary.mp4                # Background / footage · 背景影像素材
├── logo design.png                # Logo source · 台标设计源文件
└── 声音.mp3                        # Audio · 音频素材
```

---

## Hardware · 硬件清单

- Arduino (Uno / Nano, etc.) × 1
- Rotary potentiometer × 1 → pin `A0` · 旋转电位器接 `A0`
- Servo (push-rod) × 1 → signal on `D9`, **separate power recommended, common ground** · 舵机信号接 `D9`，建议独立供电并共地
- USB cable from Arduino to the Unity machine · USB 线连 Arduino 与运行 Unity 的电脑
- macOS machine × 1 (or two machines split, see *Exhibition Tips*) · macOS 电脑一台（或分两台）
- Optional: iPad as a Sidecar second display · 可选：iPad 作 Sidecar 副屏

---

## Running · 运行说明

### 1. Arduino (knob + servo · 旋钮 + 舵机)

1. Open `SNO_arduinocode/SNO_arduinocode.ino` in the Arduino IDE and upload. · 用 Arduino IDE 打开并上传。
2. **Close the Serial Monitor after uploading** — otherwise it holds the port and Unity can't connect. · **上传后关闭串口监视器**，否则占用串口，Unity 连不上。

Tunable (top of the .ino) · 可调参数（.ino 顶部）:
- `SERVO_AMPLITUDE` — servo swing amplitude (currently ±50° → 40°–140°). · 舵机摆动幅度。
- `REST_THRESHOLD` — stop/re-center threshold when the knob is low. · 旋钮回到低端时的停摆/复位阈值。

### 2. Unity (forest + HUD · 森林 + HUD 角标)

**Run the build · 直接运行成品**: double-click `SNO.app` (right-click → Open if Gatekeeper blocks it). · 双击 `SNO.app`（被拦截时右键 → 打开）。

**From the project / rebuild · 从工程运行或重新打包**:
1. Open the `unity/` project in **Unity 2022.3.x**, open the scene (`Assets/Scenes` or `11111.unity`). · 用 Unity 2022.3.x 打开工程与场景。
2. Before building, set **Player Settings → Api Compatibility Level = `.NET Framework`** (required for serial). · 打包前设 Api Compatibility Level 为 `.NET Framework`（串口必须）。
3. Build target **macOS**, and make sure the scene is added to Build Settings. · 目标 macOS，并把场景加入 Build Settings。
4. On launch a **serial-port picker** appears — choose the `/dev/cu.usbmodem…` entry → connect. · 运行后弹出串口选择面板，选 `/dev/cu.usbmodem…` 连接。

### 3. TouchDesigner (video signal · 视频信号)

1. Open `videoController.7.toe` in **TouchDesigner 2025.x**. · 用 TouchDesigner 2025.x 打开。
2. Make sure `udpin1` listens on **port 7000**. · 确认 `udpin1` 监听端口 7000。
3. For final output use **Perform Mode** (`window1` → Open/Close → *Open as Perform Window*), `Esc` to exit. · 正式输出用 Perform Mode，`Esc` 退出。

---

## Data Flow & Ports · 数据流与端口

| Link · 环节 | Protocol · 协议 | Address · 地址/端口 | Range · 数值 |
|---|---|---|---|
| Arduino → Unity | USB serial | `/dev/cu.usbmodem*` @ 9600 | `0–1023` |
| Unity → TouchDesigner | UDP | `127.0.0.1:7000` | `0–1023` |

> **Two machines · 两台电脑分开跑时**: change the **Td IP** field on Unity's `ForestGrowthController` from `127.0.0.1` to the TD machine's LAN IP; put both on the same network (wired recommended). · 把 Unity 里 `ForestGrowthController` 的 **Td IP** 从 `127.0.0.1` 改成 TD 那台的局域网 IP，两机接同一网络（建议网线）。

---

## Live Shortcuts · 现场操作快捷键

Unity app at runtime · Unity App 运行时:

- `Esc` — quit · 退出程序
- `.` (period) — toggle fullscreen · 切换全屏

The Unity startup panel lets you pick / type the serial port on site, no code change needed. · 启动面板可现场选择/手输串口，无需改代码。

---

## Exhibition Tips · 布展提示

- **One machine vs two · 单机 vs 双机** — An M1 Pro / 16 GB usually runs Unity + TD together fine; if it stutters or memory pressure is high, split onto two machines (just change the UDP target IP). · M1 Pro / 16G 一台机一般够；卡顿就分两台，改 UDP 目标 IP 即可。
- **Video specs · 视频规格** — Keep TD video at 1080p and a hardware-friendly codec (H.264 or HAP) to ease memory. · TD 视频尽量 1080p + H.264/HAP，降低内存压力。
- **Static IP · 静态 IP** — Give the exhibition machine a fixed LAN IP so it survives reboots. · 展览机设固定局域网 IP，避免重启后变化。
- **Hide the cursor · 隐藏鼠标** — Set TD `window1` → `Cursor Visible = Off`. · TD `window1` 的 `Cursor Visible` 设 `Off`。
- **Cooling · 散热** — Watch thermals over long runs to avoid throttling / dropped frames. · 长时间运行注意散热，避免降频掉帧。

---

## Credits

- Interaction / installation design & development · 交互 / 装置设计与开发: **Jsong**
- Engines · 引擎: Unity 2022.3, TouchDesigner 2025, Arduino
- Third-party assets · 第三方资源: Polytope Studio (low-poly trees), Darkbringer Shader, Glitched and Corrupted UI SFX, etc. — see subfolders in `unity/Assets/`.

---

*SNOD — tune in to a live broadcast of nature. · 收看一场关于自然的信号直播。*
