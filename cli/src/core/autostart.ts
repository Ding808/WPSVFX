import { execSync } from 'child_process';
import { resolveHelperExePath } from './constants';

const REG_KEY = 'HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Run';
const REG_VALUE_NAME = 'WtPowerFxHelper';

/**
 * 将 helper.exe 注册到 Windows 开机自启（当前用户，HKCU）。
 */
export function registerAutostart(): void {
  const exePath = resolveHelperExePath();
  const safeExePath = `"${exePath}"`;

  try {
    execSync(
      `reg add "${REG_KEY}" /v "${REG_VALUE_NAME}" /t REG_SZ /d ${safeExePath} /f`,
      { stdio: 'pipe' }
    );
    console.log(`[autostart] 已注册开机自启: ${exePath}`);
  } catch (err) {
    console.warn(`[autostart] 注册开机自启失败: ${(err as Error).message}`);
    throw err;
  }
}

/**
 * 删除开机自启注册表项。
 */
export function unregisterAutostart(): void {
  try {
    execSync(
      `reg delete "${REG_KEY}" /v "${REG_VALUE_NAME}" /f`,
      { stdio: 'pipe' }
    );
    console.log('[autostart] 已取消开机自启');
  } catch (err: unknown) {
    const msg = (err as Error).message ?? '';
    // 注册表项不存在时忽略
    if (msg.includes('无法找到') || msg.includes('not found') || msg.includes('系统找不到')) {
      console.log('[autostart] 开机自启项不存在，无需删除。');
    } else {
      throw err;
    }
  }
}

/**
 * 检查是否已注册开机自启。
 */
export function isAutostartRegistered(): boolean {
  try {
    const out = execSync(
      `reg query "${REG_KEY}" /v "${REG_VALUE_NAME}"`,
      { stdio: 'pipe', encoding: 'utf-8' }
    );
    return out.includes(REG_VALUE_NAME);
  } catch {
    return false;
  }
}
