import chalk from 'chalk';
import { stopHelper, isHelperRunning, readPid } from '../core/helperProcess';

/**
 * stop 命令：终止 helper 进程。
 */
export async function runStop(): Promise<void> {
  if (!isHelperRunning()) {
    const pid = readPid();
    if (pid !== null) {
      console.log(chalk.yellow(`⚠ PID=${pid} 进程已不存在，清理残留 PID 文件。`));
    } else {
      console.log(chalk.yellow('⚠ helper 未在运行。'));
    }
    stopHelper(); // 会自动清理 PID 文件
    return;
  }

  try {
    const stopped = stopHelper();
    if (stopped) {
      console.log(chalk.green('✔ helper 已停止'));
    } else {
      console.log(chalk.yellow('⚠ helper 进程未找到'));
    }
  } catch (err) {
    console.error(chalk.red(`✖ 停止失败: ${(err as Error).message}`));
    process.exit(1);
  }
}
