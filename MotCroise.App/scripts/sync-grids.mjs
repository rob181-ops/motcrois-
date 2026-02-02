import fs from "node:fs";
import path from "node:path";
import process from "node:process";

const repoRoot = path.resolve(process.cwd(), "..");
const srcRoot = path.resolve(process.cwd(), "src", "data", "generated");

const sizes = [5, 10, 15, 20];

function ensureDir(dirPath) {
  fs.mkdirSync(dirPath, { recursive: true });
}

function copyFile(src, dst) {
  ensureDir(path.dirname(dst));
  fs.copyFileSync(src, dst);
}

function syncSize(size) {
  const fromDir = path.resolve(repoRoot, "GRILLES", `GRILLE ${size}x${size}`);
  const toDir = path.resolve(srcRoot, `${size}x${size}`);

  if (!fs.existsSync(fromDir)) {
    return { size, copied: 0, skipped: 0, missing: true };
  }

  ensureDir(toDir);
  const entries = fs.readdirSync(fromDir, { withFileTypes: true });
  const files = entries
    .filter((e) => e.isFile() && /^grille-\d{3}\.json$/i.test(e.name))
    .map((e) => e.name);

  let copied = 0;
  let skipped = 0;
  for (const name of files) {
    const src = path.resolve(fromDir, name);
    const dst = path.resolve(toDir, name);

    // Skip if destination exists with same size.
    if (fs.existsSync(dst)) {
      const a = fs.statSync(src);
      const b = fs.statSync(dst);
      if (a.size === b.size) {
        skipped += 1;
        continue;
      }
    }

    copyFile(src, dst);
    copied += 1;
  }

  return { size, copied, skipped, missing: false };
}

ensureDir(srcRoot);
const results = sizes.map(syncSize);

for (const r of results) {
  if (r.missing) {
    // eslint-disable-next-line no-console
    console.log(`[sync-grids] ${r.size}x${r.size}: source folder missing`);
  } else {
    // eslint-disable-next-line no-console
    console.log(`[sync-grids] ${r.size}x${r.size}: copied=${r.copied} skipped=${r.skipped}`);
  }
}

