// Generated grids live under src/data/generated/{size}x{size}/grille-XXX.json
// Use import.meta.glob so Vite bundles them automatically.
const generatedModules = import.meta.glob('./generated/*x*/grille-*.json', { eager: true });

function toDefault(mod) {
  return mod && typeof mod === 'object' && 'default' in mod ? mod.default : mod;
}

function buildGeneratedIndex() {
  const bySize = new Map();
  for (const [key, mod] of Object.entries(generatedModules)) {
    const data = toDefault(mod);
    if (!data || !data.size || !data.id) continue;
    const size = Number(data.size);
    if (!bySize.has(size)) bySize.set(size, new Map());
    bySize.get(size).set(String(data.id).padStart(3, '0'), data);
  }
  return bySize;
}

const GENERATED = buildGeneratedIndex();

function listGenerated(size, count) {
  const map = GENERATED.get(size);
  if (!map) return [];
  const ids = Array.from(map.keys()).sort();
  return ids.slice(0, count).map((id) => map.get(id));
}

export function getAvailableSizes() {
  return Array.from(GENERATED.keys()).sort((a, b) => a - b);
}

export function buildGridList({ size, difficulty, count = 20 }) {
  const fromGenerated = listGenerated(size, count);
  return fromGenerated.map((g) => ({
    ...g,
    // Difficulty affects gameplay rules; grid content stays the same.
    difficulty,
    size,
  }));
}
