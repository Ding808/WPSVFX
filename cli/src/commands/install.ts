import chalk from 'chalk';
import { detectTerminalSettings } from '../core/detectTerminalSettings';
import { backupSettings } from '../core/backupSettings';
import { installAssets } from '../core/installAssets';
import { patchSettingsValue, ensureToggleShaderAction } from '../core/patchSettingsJsonc';
import { startHelper } from '../core/helperProcess';
import { SHADER_INSTALL_PATH, WT_SHADER_KEY } from '../core/constants';

export interface InstallOptions {
  force: boolean;
  noStart: boolean;
  noShader: boolean;
}

/**
 * install 命令：
 *  1. 检测 settings.json 路径
 *  2. 备份 settings.json
 *  3. 复制 shader / 音频资源到用户目录
 *  4. Patch settings.json（写入 shader 路径 + toggleShaderEffects 动作）
 *  5. （可选）启动 helper 进程
 */
export async function runInstall(options: InstallOptions): Promise<void> {
  console.log(chalk.cyan('\n[wt-powerfx] 开始安装...\n'));

  // Step 1: 检测 settings.json
  let settingsResult;
  try {
    settingsResult = detectTerminalSettings();
    console.log(chalk.green(`✔ 检测到 Windows Terminal (${settingsResult.label})`));
    console.log(`  settings.json: ${settingsResult.settingsPath}`);
  } catch (err) {
    console.error(chalk.red(`✖ ${(err as Error).message}`));
    process.exit(1);
  }

  // Step 2: 备份
  try {
    const backupPath = backupSettings(settingsResult.settingsPath);
    console.log(chalk.green(`✔ settings.json 已备份 → ${backupPath}`));
  } catch (err) {
    console.error(chalk.red(`✖ 备份失败: ${(err as Error).message}`));
    process.exit(1);
  }

  // Step 3: 安装资源
  try {
    installAssets(options.force);
    console.log(chalk.green('✔ Shader 和音频资源已安装'));
  } catch (err) {
    console.error(chalk.red(`✖ 资源安装失败: ${(err as Error).message}`));
    process.exit(1);
  }

  // Step 4: Patch settings.json
  if (!options.noShader) {
    try {
      patchSettingsValue(settingsResult.settingsPath, [WT_SHADER_KEY], SHADER_INSTALL_PATH);
      console.log(chalk.green(`✔ 已写入 ${WT_SHADER_KEY}`));

      ensureToggleShaderAction(settingsResult.settingsPath);
      console.log(chalk.green('✔ 已添加 toggleShaderEffects 快捷键（Ctrl+Alt+P）'));
    } catch (err) {
      console.error(chalk.red(`✖ 修改 settings.json 失败: ${(err as Error).message}`));
      process.exit(1);
    }
  } else {
    console.log(chalk.yellow('  跳过 shader 配置（--no-shader）'));
  }

  // Step 5: 启动 helper
  if (!options.noStart) {
    try {
      const pid = startHelper();
      console.log(chalk.green(`✔ helper 已启动（PID=${pid}）`));
    } catch (err) {
      console.warn(chalk.yellow(`⚠ 启动 helper 失败（可在之后用 wt-powerfx start 手动启动）: ${(err as Error).message}`));
    }
  }

  console.log(chalk.cyan('\n安装完成！重启 Windows Terminal 以使 shader 生效。\n'));
  console.log('  按 Ctrl+Alt+P 可切换 shader 特效开关');
  console.log('  运行 wt-powerfx doctor 检查状态\n');
}
