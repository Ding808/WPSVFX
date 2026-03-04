import * as path from 'path';
import * as os from 'os';

/** 包名 */
export const PACKAGE_NAME = 'wt-powerfx';

/** Helper 可执行文件名 */
export const HELPER_EXE_NAME = 'PowerFx.Helper.exe';

/** 用户数据目录：%APPDATA%\wt-powerfx */
export const USER_DATA_DIR = path.join(os.homedir(), 'AppData', 'Roaming', 'wt-powerfx');

/** Shader 安装路径 */
export const SHADER_INSTALL_PATH = path.join(USER_DATA_DIR, 'shaders', 'powerfx.hlsl');

/** 音频安装目录 */
export const AUDIO_INSTALL_DIR = path.join(USER_DATA_DIR, 'audio');

/** PID 文件路径（用于追踪 helper 进程） */
export const PID_FILE_PATH = path.join(USER_DATA_DIR, 'helper.pid');

/** 备份 settings.json 的目录 */
export const BACKUP_DIR = path.join(USER_DATA_DIR, 'backups');

/** Helper exe 绝对路径（根据 __dirname 计算）
 *  安装后布局：<pkg>/dist/core/ → <pkg>/vendor/bin/
 */
export function resolveHelperExePath(): string {
  return path.resolve(__dirname, '..', '..', 'vendor', 'bin', HELPER_EXE_NAME);
}

/** Windows Terminal settings.json 可能存在的路径（优先级从高到低）*/
export const WT_SETTINGS_CANDIDATES: Array<{ label: string; path: string }> = [
  {
    label: 'Stable (Store)',
    path: path.join(
      os.homedir(),
      'AppData', 'Local', 'Packages',
      'Microsoft.WindowsTerminal_8wekyb3d8bbwe',
      'LocalState', 'settings.json'
    )
  },
  {
    label: 'Preview (Store)',
    path: path.join(
      os.homedir(),
      'AppData', 'Local', 'Packages',
      'Microsoft.WindowsTerminalPreview_8wekyb3d8bbwe',
      'LocalState', 'settings.json'
    )
  },
  {
    label: 'Unpackaged / Scoop',
    path: path.join(
      os.homedir(),
      'AppData', 'Local', 'Microsoft', 'Windows Terminal', 'settings.json'
    )
  }
];

/** settings.json 中 shader 的 key */
export const WT_SHADER_KEY = 'experimental.pixelShaderPath';

/** toggleShaderEffects 动作名 */
export const WT_TOGGLE_ACTION = 'toggleShaderEffects';
