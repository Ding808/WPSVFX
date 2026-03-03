#!/usr/bin/env node
/**
 * wt-powerfx CLI 入口
 * 使用 commander 注册所有子命令
 */

import { Command } from 'commander';
import { runInstall } from './commands/install';
import { runUninstall } from './commands/uninstall';
import { runStart } from './commands/start';
import { runStop } from './commands/stop';
import { runStatus } from './commands/status';
import { runDoctor } from './commands/doctor';

const program = new Command();

program
  .name('wt-powerfx')
  .description('Windows Terminal 增强包：按键音效 / 粒子特效 / 窗口抖动 / pixel shader')
  .version('1.0.0');

// ── install ──────────────────────────────────────────────────────────────────
program
  .command('install')
  .description('安装 wt-powerfx（复制资源、修改 settings.json、启动 helper）')
  .option('-f, --force', '强制覆盖已有资源文件', false)
  .option('--no-start', '安装后不自动启动 helper')
  .option('--no-shader', '跳过 shader 相关配置（仅安装音效/粒子）')
  .action(async (opts) => {
    await runInstall({
      force: opts.force as boolean,
      noStart: !opts.start as boolean,
      noShader: !opts.shader as boolean
    });
  });

// ── uninstall ─────────────────────────────────────────────────────────────────
program
  .command('uninstall')
  .description('卸载 wt-powerfx（还原 settings.json、停止 helper）')
  .option('--keep-assets', '保留安装的 shader/音频文件')
  .option('--keep-settings', '不还原 settings.json')
  .action(async (opts) => {
    await runUninstall({
      keepAssets: opts.keepAssets as boolean,
      keepSettings: opts.keepSettings as boolean
    });
  });

// ── start ─────────────────────────────────────────────────────────────────────
program
  .command('start')
  .description('启动 helper 进程')
  .action(async () => {
    await runStart();
  });

// ── stop ──────────────────────────────────────────────────────────────────────
program
  .command('stop')
  .description('停止 helper 进程')
  .action(async () => {
    await runStop();
  });

// ── status ────────────────────────────────────────────────────────────────────
program
  .command('status')
  .description('查询 helper 进程状态')
  .action(async () => {
    await runStatus();
  });

// ── doctor ────────────────────────────────────────────────────────────────────
program
  .command('doctor')
  .description('全项检查安装状态并输出诊断报告')
  .action(async () => {
    await runDoctor();
  });

// 解析参数（异常统一兜底）
program.parseAsync(process.argv).catch((err: unknown) => {
  console.error('[wt-powerfx] 未处理的错误:', err);
  process.exit(1);
});
