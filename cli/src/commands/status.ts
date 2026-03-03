import chalk from 'chalk';
import { isHelperRunning, readPid } from '../core/helperProcess';

/**
 * status 命令：查询 helper 进程当前状态。
 */
export async function runStatus(): Promise<void> {
  const running = isHelperRunning();
  const pid = readPid();

  if (running && pid !== null) {
    console.log(chalk.green(`● helper 正在运行（PID=${pid}）`));
  } else if (!running && pid !== null) {
    console.log(chalk.yellow(`○ helper 未运行（PID 文件残留 pid=${pid}，进程已消失）`));
  } else {
    console.log(chalk.gray('○ helper 未运行'));
  }
}
