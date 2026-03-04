/**
 * prepack.js — runs automatically before `npm pack` / `npm publish`
 *
 * Copies the built helper binary and assets from outside the cli/ package root
 * into cli/vendor/, so they are included in the npm tarball.
 *
 * Input  (must exist before running):
 *   <repo>/helper/bin/PowerFx.Helper.exe   ← built by scripts/build-helper.ps1
 *   <repo>/assets/                          ← shaders + audio
 *
 * Output (committed to .gitignore, regenerated on every pack):
 *   cli/vendor/bin/PowerFx.Helper.exe
 *   cli/vendor/assets/shaders/powerfx.hlsl
 *   cli/vendor/assets/audio/...
 */

'use strict';

const fs   = require('fs');
const path = require('path');

const cliDir  = path.resolve(__dirname, '..');   // …/cli/
const repoRoot = path.resolve(cliDir, '..');      // …/wt-powerfx/

function copyDir(src, dst) {
  if (!fs.existsSync(src)) {
    console.warn(`[prepack] ⚠  source not found, skipping: ${src}`);
    return;
  }
  fs.mkdirSync(dst, { recursive: true });
  for (const entry of fs.readdirSync(src, { withFileTypes: true })) {
    const srcPath = path.join(src, entry.name);
    const dstPath = path.join(dst, entry.name);
    if (entry.isDirectory()) {
      copyDir(srcPath, dstPath);
    } else {
      fs.copyFileSync(srcPath, dstPath);
    }
  }
}

// ── 1. Helper binary ────────────────────────────────────────────────────────
const helperSrc = path.join(repoRoot, 'helper', 'bin');
const helperDst = path.join(cliDir,  'vendor', 'bin');
console.log(`[prepack] helper : ${helperSrc}`);
console.log(`            →     ${helperDst}`);
copyDir(helperSrc, helperDst);

// ── 2. Assets (shader + audio) ──────────────────────────────────────────────
const assetsSrc = path.join(repoRoot, 'assets');
const assetsDst = path.join(cliDir,  'vendor', 'assets');
console.log(`[prepack] assets : ${assetsSrc}`);
console.log(`            →     ${assetsDst}`);
copyDir(assetsSrc, assetsDst);

console.log('[prepack] ✔ vendor/ populated — ready to pack.');
