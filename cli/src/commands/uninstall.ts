import chalk from 'chalk';
import { detectTerminalSettings } from '../core/detectTerminalSettings';
import { restoreLatestBackup } from '../core/backupSettings';
import { removeInstalledAssets } from '../core/installAssets';
import { stopHelper, isHelperRunning } from '../core/helperProcess';
import { patchSettingsValue, readSettingsJsonc } from '../core/patchSettingsJsonc';
import { WT_SHADER_KEY } from '../core/constants';

export interface UninstallOptions {
  keepAssets: boolean;
  keepSettings: boolean;
}

/**
 * uninstall 命令：
 *  1. 停止 helper
 *  2. 从 settings.json 移除 shader 配置（或恢复备份）
 *  3. 可选删除安装的资源文件
 */
export async function runUninstall(options: UninstallOptions): Promise<void> {
  console.log(chalk.cyan('\n[wt-powerfx] 开始卸载...\n'));

  // Step 1: 停止 helper
  if (isHelperRunning()) {
    try {
      stopHelper();
      console.log(chalk.green('✔ helper 已停止'));
    } catch (err) {
      console.warn(chalk.yellow(`⚠ 停止 helper 失败: ${(err as Error).message}`));
    }
  } else {
    console.log('  helper 未在运行，跳过');
  }

  // Step 2: 恢复/清理 settings.json
  if (!options.keepSettings) {
    let settingsPath: string | undefined;
    try {
      settingsPath = detectTerminalSettings().settingsPath;
    } catch {
      console.warn(chalk.yellow('⚠ 未能找到 settings.json，跳过配置还原'));
    }

    if (settingsPath) {
      // 尝试恢复最新备份
      try {
        const restoredFrom = restoreLatestBackup(settingsPath);
        console.log(chalk.green(`✔ settings.json 已从备份恢复 (← ${restoredFrom})`));
      } catch {
        // 没有备份时，直接移除 shader key
        console.warn(chalk.yellow('  无可用备份，将直接移除 shader 配置项'));
        try {
          const parsed = readSettingsJsonc(settingsPath);
          if (Object.prototype.hasOwnProperty.call(parsed, WT_SHADER_KEY)) {
            patchSettingsValue(settingsPath, [WT_SHADER_KEY], undefined);
            console.log(chalk.green(`✔ 已从 settings.json 移除 ${WT_SHADER_KEY}`));
          }
        } catch (err2) {
          console.error(chalk.red(`✖ 修改 settings.json 失败: ${(err2 as Error).message}`));
        }
      }
    }
  } else {
    console.log('  跳过 settings.json 还原（--keep-settings）');
  }

  // Step 3: 删除安装的资源
  if (!options.keepAssets) {
    removeInstalledAssets();
    console.log(chalk.green('✔ 安装的资源文件已删除'));
  } else {
    console.log('  跳过资源删除（--keep-assets）');
  }

  console.log(chalk.cyan('\n卸载完成。\n'));
}
