import * as fs from 'fs';
import chalk from 'chalk';
import { detectTerminalSettings, listAllTerminalSettings } from '../core/detectTerminalSettings';
import { readSettingsJsonc } from '../core/patchSettingsJsonc';
import { isHelperRunning, readPid } from '../core/helperProcess';
import {
  SHADER_INSTALL_PATH, WT_SHADER_KEY, WT_TOGGLE_ACTION,
  resolveHelperExePath
} from '../core/constants';

interface CheckResult {
  label: string;
  pass: boolean;
  detail?: string;
}

function ok(label: string, detail?: string): CheckResult {
  return { label, pass: true, detail };
}

function fail(label: string, detail?: string): CheckResult {
  return { label, pass: false, detail };
}

/**
 * doctor 命令：逐项诊断安装状态，打印结构化报告。
 */
export async function runDoctor(): Promise<void> {
  console.log(chalk.cyan('\n[wt-powerfx] 诊断报告\n'));

  const results: CheckResult[] = [];

  // 1. settings.json 存在检查
  const allSettings = listAllTerminalSettings();
  if (allSettings.length > 0) {
    allSettings.forEach(s => {
      results.push(ok(`settings.json (${s.label})`, s.settingsPath));
    });
  } else {
    results.push(fail('settings.json', '未找到任何 Windows Terminal settings.json'));
  }

  // 2. Shader 文件检查
  if (fs.existsSync(SHADER_INSTALL_PATH)) {
    results.push(ok('Shader 文件', SHADER_INSTALL_PATH));
  } else {
    results.push(fail('Shader 文件', `未找到 ${SHADER_INSTALL_PATH}，请执行 wt-powerfx install`));
  }

  // 3. Helper exe 检查
  const helperExe = resolveHelperExePath();
  if (fs.existsSync(helperExe)) {
    results.push(ok('helper.exe', helperExe));
  } else {
    results.push(fail('helper.exe', `未找到 ${helperExe}，请先执行 npm run build:helper`));
  }

  // 4. Helper 进程检查
  const helperRunning = isHelperRunning();
  const pid = readPid();
  if (helperRunning) {
    results.push(ok(`helper 进程`, `运行中 PID=${pid}`));
  } else {
    results.push(fail('helper 进程', 'helper 未运行，执行 wt-powerfx start'));
  }

  // 5. settings.json 配置注入检查
  let settingsPath: string | undefined;
  try {
    settingsPath = detectTerminalSettings().settingsPath;
  } catch {
    /* noop */
  }

  if (settingsPath) {
    try {
      const parsed = readSettingsJsonc(settingsPath);

      // 5a. shader 路径
      if (Object.prototype.hasOwnProperty.call(parsed, WT_SHADER_KEY)) {
        results.push(ok(`settings.${WT_SHADER_KEY}`, String(parsed[WT_SHADER_KEY])));
      } else {
        results.push(fail(`settings.${WT_SHADER_KEY}`, '未注入，执行 wt-powerfx install'));
      }

      // 5b. toggle action
      const actions: Array<Record<string, unknown>> =
        (parsed['actions'] as Array<Record<string, unknown>>) ?? [];
      const hasAction = actions.some(a => a['command'] === WT_TOGGLE_ACTION);
      if (hasAction) {
        results.push(ok(`settings.actions.${WT_TOGGLE_ACTION}`, '已注册 Ctrl+Alt+P'));
      } else {
        results.push(fail(`settings.actions.${WT_TOGGLE_ACTION}`, '未注入'));
      }
    } catch (err) {
      results.push(fail('settings.json 解析', (err as Error).message));
    }
  }

  // 打印结果
  let passed = 0;
  let failed = 0;
  for (const r of results) {
    if (r.pass) {
      console.log(chalk.green(`  ✔ ${r.label}`) + (r.detail ? chalk.gray(` — ${r.detail}`) : ''));
      passed++;
    } else {
      console.log(chalk.red(`  ✖ ${r.label}`) + (r.detail ? chalk.yellow(` — ${r.detail}`) : ''));
      failed++;
    }
  }

  console.log('');
  if (failed === 0) {
    console.log(chalk.green(`所有检查通过（${passed}/${passed + failed}）\n`));
  } else {
    console.log(chalk.red(`${failed} 项检查未通过，共 ${passed + failed} 项。\n`));
  }
}
