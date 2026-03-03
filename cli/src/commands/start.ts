import chalk from 'chalk';
import { startHelper, isHelperRunning } from '../core/helperProcess';

/**
 * start 命令：启动 helper 进程（若未在运行）。
 */
export async function runStart(): Promise<void> {
  if (isHelperRunning()) {
    console.log(chalk.yellow('⚠ helper 已在运行，无需重复启动。使用 wt-powerfx status 查看详情。'));
    return;
  }

  try {
    const pid = startHelper();
    console.log(chalk.green(`✔ helper 已启动（PID=${pid}）`));
  } catch (err) {
    console.error(chalk.red(`✖ 启动失败: ${(err as Error).message}`));
    process.exit(1);
  }
}
