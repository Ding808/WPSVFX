import * as fs from 'fs';
import * as path from 'path';
import { AUDIO_INSTALL_DIR, USER_DATA_DIR, SHADER_INSTALL_PATH } from './constants';

/**
 * 将包内自带的 shader 和音频资源复制到用户目录。
 * 如果文件已存在则跳过（除非 force=true）。
 */
export function installAssets(force = false): void {
  const pkgAssetsDir = resolvePackageAssetsDir();

  // 安装 shader
  const srcShader = path.join(pkgAssetsDir, 'shaders', 'powerfx.hlsl');
  const dstShaderDir = path.dirname(SHADER_INSTALL_PATH);
  fs.mkdirSync(dstShaderDir, { recursive: true });

  if (force || !fs.existsSync(SHADER_INSTALL_PATH)) {
    if (fs.existsSync(srcShader)) {
      fs.copyFileSync(srcShader, SHADER_INSTALL_PATH);
      console.log(`[assets] shader 已安装 → ${SHADER_INSTALL_PATH}`);
    } else {
      console.warn(`[assets] 未找到 shader 源文件：${srcShader}（跳过）`);
    }
  } else {
    console.log(`[assets] shader 已存在，跳过（使用 --force 覆盖）`);
  }

  // 安装音频文件
  fs.mkdirSync(AUDIO_INSTALL_DIR, { recursive: true });
  const audioFiles = ['key.wav', 'backspace.wav', 'delete.wav', 'select.wav'];

  for (const audioFile of audioFiles) {
    const src = path.join(pkgAssetsDir, 'audio', audioFile);
    const dst = path.join(AUDIO_INSTALL_DIR, audioFile);

    if (force || !fs.existsSync(dst)) {
      if (fs.existsSync(src)) {
        fs.copyFileSync(src, dst);
        console.log(`[assets] 音频已安装 → ${dst}`);
      } else {
        console.warn(`[assets] 未找到音频源文件：${src}（跳过）`);
      }
    } else {
      console.log(`[assets] ${audioFile} 已存在，跳过`);
    }
  }
}

/**
 * 删除用户目录下安装的所有资源（卸载时使用）。
 */
export function removeInstalledAssets(): void {
  const targets = [
    SHADER_INSTALL_PATH,
    path.join(AUDIO_INSTALL_DIR, 'key.wav'),
    path.join(AUDIO_INSTALL_DIR, 'backspace.wav'),
    path.join(AUDIO_INSTALL_DIR, 'delete.wav'),
    path.join(AUDIO_INSTALL_DIR, 'select.wav')
  ];

  for (const t of targets) {
    try {
      if (fs.existsSync(t)) {
        fs.unlinkSync(t);
        console.log(`[assets] 已删除 ${t}`);
      }
    } catch (err) {
      console.warn(`[assets] 删除失败 ${t}: ${(err as Error).message}`);
    }
  }
}

/** 定位包内 assets 目录（相对于编译后的 dist/core） */
function resolvePackageAssetsDir(): string {
  // dist/core/ → ../../assets/
  return path.resolve(__dirname, '..', '..', '..', 'assets');
}
