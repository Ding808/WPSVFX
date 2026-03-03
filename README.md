# wt-powerfx

> Windows Terminal 原生增强包 — **按键音效 · 粒子特效 · CRT Shader · 窗口抖动**

[![npm version](https://img.shields.io/npm/v/wt-powerfx)](https://www.npmjs.com/package/wt-powerfx)
[![platform](https://img.shields.io/badge/platform-Windows%2010%2B-blue)]()
[![license](https://img.shields.io/badge/license-MIT-green)]()

---

## 功能一览

| 功能 | 说明 |
|------|------|
| 🎵 按键音效 | 普通按键 / Backspace / Delete / Enter / 选区 各有独立音效 |
| ✨ 粒子特效 | 每次击键在终端窗口上方弹出彩色粒子爆炸 |
| 📺 CRT Shader | 扫描线 + 色差 + 暗角，注入 WT experimental.pixelShaderPath |
| 🫨 窗口抖动 | Enter 触发强抖，普通键触发轻抖，精确恢复原位不漂移 |
| 🖱️ 选区拖尾 | 拖选时发射蓝紫色粒子拖尾（鼠标坐标近似实现） |

---

## 快速开始

### 前置条件

- Windows 10 1903+ 或 Windows 11
- [Windows Terminal](https://aka.ms/terminal) (Store 版 / Preview 版 / Scoop 均支持)
- Node.js ≥ 18
- .NET 8 SDK（仅编译 helper 时需要）

### 安装

```powershell
npm i -g wt-powerfx
wt-powerfx install
```

安装完成后：
- 重启 Windows Terminal 使 shader 生效
- 按 **Ctrl+Alt+P** 切换 shader 开关

---

## 命令参考

```
wt-powerfx <command> [options]

命令：
  install     安装：检测 WT 路径 → 备份设置 → 复制资源 → 修改 settings.json → 启动 helper
  uninstall   卸载：停止 helper → 还原 settings.json → （可选）删除资源
  start       启动 helper 进程
  stop        停止 helper 进程
  status      查看 helper 是否在运行
  doctor      诊断：全项检查安装状态

install 选项：
  -f, --force       强制覆盖已有资源文件
  --no-start        安装后不自动启动 helper
  --no-shader       跳过 shader 相关配置

uninstall 选项：
  --keep-assets     保留安装的 shader/音频文件
  --keep-settings   不还原 settings.json
```

---

## 项目结构

```
wt-powerfx/
├── cli/            # TypeScript CLI（commander + jsonc-parser）
├── helper/         # C# .NET 8 WPF 后台 helper
├── assets/
│   ├── shaders/    # HLSL pixel shader
│   └── audio/      # WAV 音效（需自备，见 assets/audio/README.md）
└── scripts/        # 构建/发布脚本
```

---

## 构建说明

### 1. 编译 Helper（C# exe）

```powershell
# 需要 .NET 8 SDK
.\scripts\build-helper.ps1
```

输出至 `helper/bin/PowerFx.Helper.exe`（自包含单文件，~100MB）。

### 2. 编译 CLI（TypeScript）

```powershell
cd cli
npm install
npm run build
```

输出至 `cli/dist/`。

### 3. 全量构建

```powershell
npm run build:all
```

---

## 音效文件

音效需自备，放入 `assets/audio/`（或安装后放入 `%APPDATA%\wt-powerfx\audio\`）：

- `key.wav` — 普通按键
- `backspace.wav` — Backspace
- `delete.wav` — Delete
- `select.wav` — 选区/Ctrl+A

推荐来源：[Kenney.nl Interface Sounds](https://kenney.nl/assets/interface-sounds)（CC0 免版权）

---

## 技术实现说明

### ⚠️ 近似实现标注

| 功能 | 实现方式 | 精确度 |
|------|----------|--------|
| 粒子位置 | 在 overlay 中央区域随机偏移 | 近似（无法得知字符光标精确坐标） |
| 选区拖尾 | 通过鼠标坐标近似判断 | 近似（无法知道选区左上/右下字符边界） |
| Helper 检测 WT | 轮询 GetForegroundWindow + 进程名/窗口类名 | 可靠 |
| Overlay 跟随 | 50ms 间隔同步 SetWindowPos | 跟随延迟 ≤50ms |

### 技术约束遵守情况

- ✅ 不修改 WindowsTerminal.exe 本体
- ✅ 不注入 DLL，不 Hook 进程内部函数
- ✅ 只使用公开 Win32 API + 外部 overlay + settings.json 修改
- ✅ Overlay 透明 + 置顶 + 点击穿透（WS_EX_TRANSPARENT）
- ✅ 窗口抖动后精确恢复原位（Mutex + SemaphoreSlim 序列化）
- ✅ 粒子对象池（ConcurrentBag），避免高频 GC

---

## 已知限制

- Helper 需要消息循环（WPF Application），最低内存占用约 50~80 MB
- 单文件自包含 exe 较大（~100 MB），可改用 framework-dependent 发布缩减至 ~5 MB
- 音效节流默认 80ms，在极快速打字时部分按键无音效
- CRT shader 效果为静态（无时间动画），如需动态效果可在 HLSL 中使用 `Time` cbuffer

---

## License

MIT © wt-powerfx contributors
