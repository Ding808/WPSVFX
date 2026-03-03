import * as fs from 'fs';
import * as path from 'path';
import { BACKUP_DIR } from './constants';

/**
 * 将指定的 settings.json 备份到 BACKUP_DIR 目录下，
 * 文件名包含时间戳以避免覆盖。
 *
 * @returns 备份文件的完整路径
 */
export function backupSettings(settingsPath: string): string {
  if (!fs.existsSync(settingsPath)) {
    throw new Error(`settings.json 不存在，无法备份：${settingsPath}`);
  }

  fs.mkdirSync(BACKUP_DIR, { recursive: true });

  const timestamp = new Date()
    .toISOString()
    .replace(/[:.]/g, '-')
    .replace('T', '_')
    .slice(0, 19);

  const backupFile = path.join(BACKUP_DIR, `settings_${timestamp}.json`);
  fs.copyFileSync(settingsPath, backupFile);

  return backupFile;
}

/**
 * 列出所有已存在的备份文件（按时间倒序）。
 */
export function listBackups(): string[] {
  if (!fs.existsSync(BACKUP_DIR)) {
    return [];
  }

  return fs
    .readdirSync(BACKUP_DIR)
    .filter(f => f.startsWith('settings_') && f.endsWith('.json'))
    .map(f => path.join(BACKUP_DIR, f))
    .sort()
    .reverse();
}

/**
 * 从最新备份恢复 settings.json。
 *
 * @returns 被恢复的备份文件路径
 * @throws 如果没有任何备份
 */
export function restoreLatestBackup(settingsPath: string): string {
  const backups = listBackups();
  if (backups.length === 0) {
    throw new Error(`在 ${BACKUP_DIR} 中未找到任何备份文件。`);
  }

  const latest = backups[0]!;
  fs.copyFileSync(latest, settingsPath);
  return latest;
}
