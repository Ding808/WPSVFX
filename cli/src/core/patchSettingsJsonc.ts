import * as fs from 'fs';
import * as jsoncParser from 'jsonc-parser';

/**
 * 使用 jsonc-parser 对 settings.json 做无损修改：
 * 保留原始注释和尾逗号，只修改指定的值。
 *
 * @param settingsPath  settings.json 完整路径
 * @param keyPath       要修改的 key 路径（如 ['experimental.pixelShaderPath']）
 *                      注意 WT 的 key 含点号，整体是一个单 key，不是嵌套路径
 * @param value         要写入的值
 */
export function patchSettingsValue(
  settingsPath: string,
  keyPath: string[],
  value: unknown
): void {
  const raw = fs.readFileSync(settingsPath, 'utf-8');

  const edits = jsoncParser.modify(raw, keyPath, value, {
    formattingOptions: { tabSize: 4, insertSpaces: true }
  });

  const patched = jsoncParser.applyEdits(raw, edits);
  fs.writeFileSync(settingsPath, patched, 'utf-8');
}

/**
 * 读取 settings.json（容忍注释/尾逗号），返回解析后的对象。
 */
export function readSettingsJsonc(settingsPath: string): Record<string, unknown> {
  const raw = fs.readFileSync(settingsPath, 'utf-8');
  const errors: jsoncParser.ParseError[] = [];
  const parsed = jsoncParser.parse(raw, errors, { allowTrailingComma: true });

  if (errors.length > 0) {
    const msgs = errors.map(e => `offset=${e.offset} code=${e.error}`).join('; ');
    console.warn(`[warn] settings.json 存在解析警告（将尽力继续）: ${msgs}`);
  }

  return (parsed as Record<string, unknown>) ?? {};
}

/**
 * 确保 toggleShaderEffects 动作已存在于 actions 数组中，
 * 若已存在则跳过，若不存在则追加。
 */
export function ensureToggleShaderAction(settingsPath: string): void {
  const raw = fs.readFileSync(settingsPath, 'utf-8');
  const parsed = readSettingsJsonc(settingsPath);

  // 检查是否已经存在
  const actions: Array<Record<string, unknown>> =
    (parsed['actions'] as Array<Record<string, unknown>>) ?? [];

  const alreadyExists = actions.some(
    a => typeof a['command'] === 'string' && a['command'] === 'toggleShaderEffects'
  );

  if (alreadyExists) {
    return; // 已有，不重复写
  }

  const newAction = {
    command: 'toggleShaderEffects',
    keys: 'ctrl+alt+p'
  };

  const currentActions = parsed['actions'] ?? [];
  const updatedActions = [...(currentActions as unknown[]), newAction];

  const edits = jsoncParser.modify(raw, ['actions'], updatedActions, {
    formattingOptions: { tabSize: 4, insertSpaces: true }
  });

  const patched = jsoncParser.applyEdits(raw, edits);
  fs.writeFileSync(settingsPath, patched, 'utf-8');
}
