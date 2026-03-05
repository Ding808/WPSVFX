# wt-powerfx

> Windows Terminal native enhancement  Keystroke Sounds  Particle FX  CRT Shader  Window Shake
>
> Windows Terminal 原生增强包  按键音效  粒子特效  CRT Shader  窗口抖动

[![platform](https://img.shields.io/badge/platform-Windows%2010%2F11-blue)]()
[![license](https://img.shields.io/badge/license-MIT-green)]()

https://github.com/user-attachments/assets/abc8cb99-2128-4d55-93ec-01ffb0e57768


---

<!--  -->
<!--                       ENGLISH                             -->
<!--  -->

<details open>
<summary><b> English</b></summary>
<br>

##  Features

| | Feature | Description |
|-|---------|-------------|
|  | Keystroke Sounds | Distinct sounds for regular keys, Backspace, Delete, Enter, and text selection |
|  | Particle Effects | Colorful explosion particles burst from the terminal on every keystroke |
|  | CRT Shader | Scanlines + chromatic aberration + vignette via `experimental.pixelShaderPath` |
|  | Window Shake | Hard shake on Enter, soft shake on regular keys  snaps back to the exact original position |
|  | Selection Trail | Blue-violet particle trail while dragging to select text |

---

##  Installation

### Prerequisites

| Requirement | How to get it |
|-------------|--------------|
| **Windows 10 1903+ / Windows 11** | — |
| **Windows Terminal** | [Microsoft Store](https://aka.ms/terminal) · [GitHub Releases](https://github.com/microsoft/terminal/releases) · `winget install Microsoft.WindowsTerminal` |

> The .NET 8 runtime is **already bundled** — no separate install needed.

---

### 📦 Install from GitHub

**Step 1** — Download the latest release:

👉 [github.com/Ding808/WPSVFX/releases/latest](https://github.com/Ding808/WPSVFX/releases/latest)

Download and extract `wt-powerfx-win-x64.zip`.

**Step 2** — Open PowerShell in the extracted folder and run:
```powershell
.\install.ps1
```

> If you see a security warning, run: `Set-ExecutionPolicy -Scope CurrentUser RemoteSigned`

**Step 3** — Restart Windows Terminal (close all windows and reopen).

> **Toggle the shader at any time** with **Ctrl + Alt + P**

---

### What the installer does

`install.ps1` will automatically:
1. Detect your Windows Terminal installation
2. Back up your existing `settings.json`
3. Copy shader + audio assets to `%APPDATA%\wt-powerfx\`
4. Patch `settings.json` to enable the CRT shader
5. Start the helper process in the background

---

##  Audio Files

Sound files are **bundled** inside the zip — no extra downloads needed.

| Filename | Trigger |
|----------|---------|
| `whoosh.mp3` | Any regular keypress |
| `CinematicBoom.mp3` | Backspace / Delete |
| `Lightning.mp3` | Enter |
| `click.mp3` | Text selection / Ctrl+A |

You can replace any file with your own audio — just drop a same-named MP3 into  
`%APPDATA%\wt-powerfx\audio\` and restart the helper.

---

##  Uninstall

Run in the extracted folder:
```powershell
.\uninstall.ps1
```

This stops the helper, restores your original `settings.json`, and removes installed assets.

---

##  Known Limitations

- The helper process (~50–80 MB RAM) requires a WPF message loop
- Self-contained exe is ~100 MB
- Sound playback has an 80 ms throttle — some keys may be silent during very fast typing
- The CRT shader is static (no time-based animation)

---

##  License

MIT — wt-powerfx contributors

</details>

---

<!--  -->
<!--                         中文                              -->
<!--  -->

<details open>
<summary><b> 中文</b></summary>
<br>

##  功能一览

| | 功能 | 说明 |
|-|------|------|
|  | 按键音效 | 普通按键 / Backspace / Delete / Enter / 选区 各有独立音效 |
|  | 粒子特效 | 每次击键在终端窗口上方弹出彩色粒子爆炸效果 |
|  | CRT Shader | 扫描线 + 色差 + 暗角，通过 `experimental.pixelShaderPath` 注入 |
|  | 窗口抖动 | Enter 触发强抖，普通键触发轻抖，精确恢复原位不漂移 |
|  | 选区拖尾 | 拖选文字时发射蓝紫色粒子拖尾 |

---

##  安装教程

### 前置条件

| 前置条件 | 获取方式 |
|----------|---------|
| **Windows 10 1903+ / Windows 11** | — |
| **Windows Terminal** | [Microsoft Store](https://aka.ms/terminal) · [GitHub Releases](https://github.com/microsoft/terminal/releases) · `winget install Microsoft.WindowsTerminal` |

> .NET 8 运行时已**内嵌**于压缩包中，**无需单独安装**。

---

### 📦 从 GitHub 下载安装

**第一步** — 下载最新发行版：

👉 [github.com/Ding808/WPSVFX/releases/latest](https://github.com/Ding808/WPSVFX/releases/latest)

下载并解压 `wt-powerfx-win-x64.zip`。

**第二步** — 在解压目录中打开 **PowerShell**，执行：

```powershell
.\install.ps1
```

> 若出现安全警告，请先执行：`Set-ExecutionPolicy -Scope CurrentUser RemoteSigned`

**第三步** — 重启 Windows Terminal（关闭所有窗口后重新打开）。

> **随时按 Ctrl + Alt + P 切换 shader 开关**

---

### 安装程序自动完成的步骤

`install.ps1` 将自动执行：
1. 检测 Windows Terminal 的安装路径
2. 备份现有的 `settings.json`
3. 将 shader 和音频资源复制到 `%APPDATA%\wt-powerfx\`
4. 修改 `settings.json` 以启用 CRT shader
5. 在后台启动 helper 进程

---

##  音效文件

音效文件已**内置于压缩包中**，无需另行下载。

| 文件名 | 触发时机 |
|--------|----------|
| `whoosh.mp3` | 普通按键 |
| `CinematicBoom.mp3` | Backspace / Delete |
| `Lightning.mp3` | Enter |
| `click.mp3` | 文字选区 / Ctrl+A |

如需替换为自定义音效，将同名 MP3 放入 `%APPDATA%\wt-powerfx\audio\` 并重启 helper 即可。

---

##  卸载

在解压目录中执行：
```powershell
.\uninstall.ps1
```

将停止 helper 进程、还原 `settings.json`、删除已安装资源。

---

##  已知限制

- Helper 进程需要消息循环（WPF），最低内存占用约 50~80 MB
- 自包含单文件 exe 体积约 100 MB
- 音效节流默认 80ms，极快速打字时部分按键可能无声
- CRT shader 为静态效果（无时间动画）

---

##  开源协议

MIT — wt-powerfx contributors

</details>
