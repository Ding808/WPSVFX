import * as fs from 'fs';
import * as path from 'path';
import { spawn } from 'child_process';
import { PID_FILE_PATH, USER_DATA_DIR } from './constants';
import { resolveHelperExePath } from './constants';

/**
 * 启动 helper.exe 进程（后台分离）。
 * 将 PID 写入 PID_FILE_PATH。
 *
 * @throws 如果 helper.exe 不存在
 */
export function startHelper(): number {
  const exePath = resolveHelperExePath();

  if (!fs.existsSync(exePath)) {
    throw new Error(
      `helper 可执行文件不存在：${exePath}\n` +
      '请先执行 npm run build:helper 或参考 README 手动编译。'
    );
  }

  fs.mkdirSync(USER_DATA_DIR, { recursive: true });

  const logFile = path.join(USER_DATA_DIR, 'helper.log');

  const child = spawn(exePath, [], {
    detached: true,
    stdio: ['ignore', 'pipe', 'pipe'],
    windowsHide: false
  });

  // 将 helper stdout/stderr 追加写入日志
  const logStream = fs.createWriteStream(logFile, { flags: 'a' });
  child.stdout?.pipe(logStream);
  child.stderr?.pipe(logStream);

  child.unref();

  if (child.pid === undefined) {
    throw new Error('启动 helper 进程失败：未获取到 PID。');
  }

  writePid(child.pid);
  console.log(`[helper] 已启动，PID=${child.pid}，日志→ ${logFile}`);
  return child.pid;
}

/**
 * 停止 helper 进程（通过 PID 文件）。
 *
 * @returns true 表示成功终止，false 表示进程未找到
 */
export function stopHelper(): boolean {
  const pid = readPid();
  if (pid === null) {
    console.log('[helper] 未找到 PID 文件，helper 可能未在运行。');
    return false;
  }

  try {
    process.kill(pid, 'SIGTERM');
    removePid();
    console.log(`[helper] 已发送终止信号给 PID=${pid}`);
    return true;
  } catch (err: unknown) {
    const code = (err as NodeJS.ErrnoException).code;
    if (code === 'ESRCH') {
      console.log(`[helper] PID=${pid} 进程已不存在，清理 PID 文件。`);
      removePid();
      return false;
    }
    throw err;
  }
}

/**
 * 检查 helper 是否正在运行。
 */
export function isHelperRunning(): boolean {
  const pid = readPid();
  if (pid === null) return false;

  try {
    process.kill(pid, 0); // 信号 0：只检查进程是否存在，不发送信号
    return true;
  } catch {
    return false;
  }
}

/** 获取当前 helper PID（若存在），否则返回 null */
export function readPid(): number | null {
  try {
    if (!fs.existsSync(PID_FILE_PATH)) return null;
    const raw = fs.readFileSync(PID_FILE_PATH, 'utf-8').trim();
    const pid = parseInt(raw, 10);
    return isNaN(pid) ? null : pid;
  } catch {
    return null;
  }
}

function writePid(pid: number): void {
  fs.mkdirSync(path.dirname(PID_FILE_PATH), { recursive: true });
  fs.writeFileSync(PID_FILE_PATH, String(pid), 'utf-8');
}

function removePid(): void {
  try {
    fs.unlinkSync(PID_FILE_PATH);
  } catch {
    // 忽略
  }
}
