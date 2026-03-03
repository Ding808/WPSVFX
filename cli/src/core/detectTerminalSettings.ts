import * as fs from 'fs';
import { WT_SETTINGS_CANDIDATES } from './constants';

export interface TerminalSettingsResult {
  label: string;
  settingsPath: string;
}

/**
 * 按优先级扫描 Windows Terminal settings.json 的所有已知位置，
 * 返回第一个实际存在的路径。
 *
 * @throws Error 如果所有候选路径均不存在
 */
export function detectTerminalSettings(): TerminalSettingsResult {
  for (const candidate of WT_SETTINGS_CANDIDATES) {
    try {
      if (fs.existsSync(candidate.path)) {
        return { label: candidate.label, settingsPath: candidate.path };
      }
    } catch {
      // 权限异常等，跳过
    }
  }

  const tried = WT_SETTINGS_CANDIDATES.map(c => `  [${c.label}] ${c.path}`).join('\n');
  throw new Error(
    `未找到 Windows Terminal settings.json。已尝试以下路径：\n${tried}\n\n` +
    '请确认已安装 Windows Terminal（Store 或 Scoop 版本）。'
  );
}

/**
 * 列出所有已检测到的（存在的）settings.json 路径。
 */
export function listAllTerminalSettings(): TerminalSettingsResult[] {
  return WT_SETTINGS_CANDIDATES.filter(c => {
    try {
      return fs.existsSync(c.path);
    } catch {
      return false;
    }
  }).map(c => ({ label: c.label, settingsPath: c.path }));
}
