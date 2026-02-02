import fs from "node:fs";
import path from "node:path";

const repoRoot = process.cwd();

const targets = [
  "node_modules/@capacitor/app/android/build.gradle",
  "node_modules/@capacitor/device/android/build.gradle",
  "node_modules/@capacitor/preferences/android/build.gradle",
  "node_modules/@capacitor/splash-screen/android/build.gradle",
  "node_modules/@capacitor/status-bar/android/build.gradle",
  "node_modules/@capacitor-community/admob/android/build.gradle",
  "node_modules/@revenuecat/purchases-capacitor/android/build.gradle",
];

const from = "getDefaultProguardFile('proguard-android.txt')";
const to = "getDefaultProguardFile('proguard-android-optimize.txt')";

let changed = 0;
let missing = 0;

for (const rel of targets) {
  const filePath = path.resolve(repoRoot, rel);
  if (!fs.existsSync(filePath)) {
    missing += 1;
    continue;
  }

  const before = fs.readFileSync(filePath, "utf8");
  if (!before.includes(from)) continue;

  const after = before.replaceAll(from, to);
  fs.writeFileSync(filePath, after, "utf8");
  changed += 1;
}

// eslint-disable-next-line no-console
console.log(`[fix-android-proguard] changed=${changed} missing=${missing}`);

