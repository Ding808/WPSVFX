# wt-powerfx

> Windows Terminal native enhancement  Keystroke Sounds  Particle FX  CRT Shader  Window Shake
>
> Windows Terminal 原生增强包  按键音效  粒子特效  CRT Shader  窗口抖动

[![npm version](https://img.shields.io/npm/v/wt-powerfx)](https://www.npmjs.com/package/wt-powerfx)
[![platform](https://img.shields.io/badge/platform-Windows%2010%2F11-blue)]()
[![license](https://img.shields.io/badge/license-MIT-green)]()

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

### Step 1  Check prerequisites

| Requirement | How to get it |
|-------------|--------------|
| **Windows 10 1903+ / Windows 11** |  |
| **Windows Terminal** | [Microsoft Store](https://aka.ms/terminal)  [GitHub Releases](https://github.com/microsoft/terminal/releases)  `winget install Microsoft.WindowsTerminal` |
| **Node.js  18** | [nodejs.org](https://nodejs.org)  `winget install OpenJS.NodeJS` |

> The .NET 8 runtime is **already bundled** inside the package  you do not need to install it separately.

Verify Node.js is installed by opening any terminal and running:
```powershell
node --version   # should print v18.x.x or higher
npm --version
```

### Step 2  Install the package

Open **PowerShell** or **Windows Terminal** and run:

```powershell
npm install -g wt-powerfx
```

### Step 3  Run the installer

```powershell
wt-powerfx install
```

This single command will:
1. Detect your Windows Terminal installation
2. Back up your existing `settings.json`
3. Copy the shader and audio assets to `%APPDATA%\wt-powerfx\`
4. Patch `settings.json` to enable the CRT shader
5. Start the helper process in the background

### Step 4  Restart Windows Terminal

Close **all** Windows Terminal windows and reopen it. The CRT shader activates on startup.

> **Toggle the shader at any time** with **Ctrl + Alt + P**

---

##  Verify it works

```powershell
wt-powerfx status
```

Expected output:
```
 Helper process : running  (PID 12345)
 Shader         : enabled
 Assets         : installed
```

If something looks wrong, run the full diagnostic:

```powershell
wt-powerfx doctor
```

---

##  Update

```powershell
npm update -g wt-powerfx
wt-powerfx uninstall --keep-assets
wt-powerfx install
```

---

##  Uninstall

```powershell
wt-powerfx uninstall
```

This stops the helper, restores your original `settings.json`, and removes installed assets.  
To keep the assets (shader/audio files) while removing everything else:

```powershell
wt-powerfx uninstall --keep-assets
```

---

##  CLI Reference

```
wt-powerfx <command> [options]

Commands:
  install       Full install (detect  backup  copy assets  patch settings  start)
  uninstall     Stop helper  restore settings.json  (optionally) remove assets
  start         Start the helper background process
  stop          Stop the helper background process
  status        Show whether the helper is running and features are active
  doctor        Run a full diagnostic check of the installation

install flags:
  -f, --force         Overwrite existing asset files
  --no-start          Skip auto-starting the helper after install
  --no-shader         Skip CRT shader configuration

uninstall flags:
  --keep-assets       Keep shader and audio files on disk
  --keep-settings     Do not restore settings.json
```

---

##  Audio Files

Sound files are **not bundled** (licensing). Place your own WAV files here  
after install: `%APPDATA%\wt-powerfx\audio\`

| Filename | Trigger |
|----------|---------|
| `key.wav` | Any regular keypress |
| `backspace.wav` | Backspace key |
| `delete.wav` | Delete key |
| `select.wav` | Text selection / Ctrl+A |

Free CC0 sounds: [Kenney.nl Interface Sounds](https://kenney.nl/assets/interface-sounds)

---

##  Building from Source

<details>
<summary>Click to expand</summary>

**Requirements:** [.NET 8 SDK](https://dotnet.microsoft.com/download), Node.js  18

```powershell
# 1. Clone the repo
git clone https://github.com/your-org/wt-powerfx
cd wt-powerfx

# 2. Build the C# helper (~100 MB self-contained exe)
.\scripts\build-helper.ps1

# 3. Build the CLI
cd cli
npm install
npm run build
cd ..

# 4. Or do everything in one command
npm run build:all
```

To publish to npm:
```powershell
.\scripts\publish-helper.ps1          # dry run first:
.\scripts\publish-helper.ps1 -DryRun
```
</details>

---

##  Known Limitations

- The helper process (~5080 MB RAM) requires a WPF message loop
- Self-contained exe is ~100 MB; use framework-dependent publish for ~5 MB
- Sound playback has an 80 ms throttle  some keys may be silent during very fast typing
- The CRT shader is static (no time-based animation)

---

##  License

MIT  wt-powerfx contributors

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

### 第一步  确认前置条件

| 前置条件 | 获取方式 |
|----------|---------|
| **Windows 10 1903+ / Windows 11** |  |
| **Windows Terminal** | [Microsoft Store](https://aka.ms/terminal)  [GitHub Releases](https://github.com/microsoft/terminal/releases)  `winget install Microsoft.WindowsTerminal` |
| **Node.js  18** | [nodejs.org](https://nodejs.org)  `winget install OpenJS.NodeJS` |

> .NET 8 运行时已**内嵌**于本包中，**无需单独安装**。

打开任意终端，验证 Node.js 已正确安装：
```powershell
node --version   # 应输出 v18.x.x 或更高版本
npm --version
```

### 第二步  安装 npm 包

打开 **PowerShell** 或 **Windows Terminal**，执行：

```powershell
npm install -g wt-powerfx
```

### 第三步  执行安装程序

```powershell
wt-powerfx install
```

这条命令将自动完成以下步骤：
1. 检测 Windows Terminal 的安装路径
2. 备份现有的 `settings.json`
3. 将 shader 和音频资源复制到 `%APPDATA%\wt-powerfx\`
4. 修改 `settings.json` 以启用 CRT shader
5. 在后台启动 helper 进程

### 第四步  重启 Windows Terminal

关闭**所有** Windows Terminal 窗口后重新打开。CRT shader 会在启动时自动生效。

> **随时按 Ctrl + Alt + P 切换 shader 开关**

---

##  验证是否正常运行

```powershell
wt-powerfx status
```

正常输出如下：
```
 Helper 进程 : 运行中  (PID 12345)
 Shader      : 已启用
 资源文件    : 已安装
```

如有异常，执行完整诊断：

```powershell
wt-powerfx doctor
```

---

##  更新

```powershell
npm update -g wt-powerfx
wt-powerfx uninstall --keep-assets
wt-powerfx install
```

---

##  卸载

```powershell
wt-powerfx uninstall
```

将停止 helper 进程、还原 `settings.json`、删除已安装资源。  
如需保留 shader 和音频文件，请使用：

```powershell
wt-powerfx uninstall --keep-assets
```

---

##  命令参考

```
wt-powerfx <command> [options]

命令：
  install     完整安装（检测  备份  复制资源  修改配置  启动）
  uninstall   停止 helper  还原 settings.json  （可选）删除资源
  start       启动 helper 后台进程
  stop        停止 helper 后台进程
  status      查看 helper 运行状态及各功能是否已激活
  doctor      全项诊断，检查安装状态

install 选项：
  -f, --force         强制覆盖已有资源文件
  --no-start          安装后不自动启动 helper
  --no-shader         跳过 CRT shader 相关配置

uninstall 选项：
  --keep-assets       保留已安装的 shader / 音频文件
  --keep-settings     不还原 settings.json
```

---

##  音效文件

音效文件因版权原因**未随包发布**。请将 WAV 文件放至安装后的目录：  
`%APPDATA%\wt-powerfx\audio\`

| 文件名 | 触发时机 |
|--------|---------|
| `key.wav` | 普通按键 |
| `backspace.wav` | Backspace 键 |
| `delete.wav` | Delete 键 |
| `select.wav` | 文字选区 / Ctrl+A |

推荐免版权音效：[Kenney.nl Interface Sounds](https://kenney.nl/assets/interface-sounds)（CC0）

---

##  从源码构建

<details>
<summary>点击展开</summary>

**需要：** [.NET 8 SDK](https://dotnet.microsoft.com/download)、Node.js  18

```powershell
# 1. 克隆仓库
git clone https://github.com/your-org/wt-powerfx
cd wt-powerfx

# 2. 编译 C# helper（~100 MB 自包含单文件）
.\scripts\build-helper.ps1

# 3. 编译 CLI
cd cli
npm install
npm run build
cd ..

# 4. 或一键全量构建
npm run build:all
```

发布至 npm：
```powershell
.\scripts\publish-helper.ps1          # 先干跑验证：
.\scripts\publish-helper.ps1 -DryRun
```
</details>

---

##  已知限制

- Helper 进程需要消息循环（WPF），最低内存占用约 50~80 MB
- 自包含单文件 exe 体积约 100 MB；改用 framework-dependent 发布可缩减至 ~5 MB
- 音效节流默认 80ms，极快速打字时部分按键可能无声
- CRT shader 为静态效果（无时间动画）

---

##  开源协议

MIT  wt-powerfx contributors

</details>