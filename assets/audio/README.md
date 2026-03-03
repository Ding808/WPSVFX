# Audio Assets — wt-powerfx

此目录存放 wt-powerfx 所需的音效文件（WAV 格式，16-bit PCM，44100 Hz）。

## 文件列表

| 文件           | 触发时机                           | 建议音效风格         |
|----------------|------------------------------------|----------------------|
| key.wav        | 普通按键（字母、数字、符号、Enter） | 清脆短促（< 100ms）  |
| backspace.wav  | Backspace 键                       | 略低沉、带回弹感     |
| delete.wav     | Delete 键                          | 类似 backspace       |
| select.wav     | Ctrl+A 或鼠标拖选开始              | 轻柔扫过感           |

## 获取方式

1. **自备**：将以上格式的 WAV 放入此目录即可。
2. **推荐来源**（免版权）：
   - [Kenney.nl — Interface Sounds](https://kenney.nl/assets/interface-sounds)
   - [freesound.org](https://freesound.org) 搜索 "keyboard click"
3. **占位空文件**：缺失 WAV 时，SoundService 会输出警告但程序仍正常运行（静音模式）。

## 替换说明

安装后，音频文件被复制到：

```
%APPDATA%\wt-powerfx\audio\
```

可直接替换此目录中的文件，无需重新运行 `wt-powerfx install`，重启 helper 即可生效：

```
wt-powerfx stop
wt-powerfx start
```
