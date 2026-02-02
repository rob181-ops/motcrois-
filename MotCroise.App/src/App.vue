<template>
  <v-app :class="['app', `screen-${screen}`]">
    <v-app-bar class="topbar" flat height="64">
      <v-btn icon variant="text" @click="showMenu = true" aria-label="Menu">
        <v-icon icon="mdi-menu" />
      </v-btn>
      <v-app-bar-title class="title">
        <span class="title-mark">Mot</span><span class="title-mark alt">Croisé</span>
      </v-app-bar-title>
      <div class="topbar-actions">
        <v-btn icon variant="text" @click="showRewardedAd" aria-label="Voir une pub (gagner un joker)">
          <v-icon icon="mdi-video-outline" />
        </v-btn>
        <v-btn icon variant="text" @click="showSettings = true" aria-label="Options">
          <v-icon icon="mdi-cog-outline" />
        </v-btn>
      </div>
    </v-app-bar>

    <v-main class="main">
      <v-container class="content" fluid>
        <v-fade-transition mode="out-in">
          <div class="screen" :key="screen">
            <section v-if="screen === 'select'" class="panel">
              <v-card class="panel-card">
                <v-card-text class="panel-inner">
                  <div class="hero">
                    <div class="hero-kicker">Jeu de lettres</div>
                    <h2 class="hero-title">Choisir une grille</h2>
                    <div class="hero-subtitle">Sélectionne une taille et un niveau, puis lance la partie.</div>
                  </div>

                  <div class="choice">
                    <div class="choice-label">Taille</div>
                    <div class="choice-row">
                      <v-chip
                        v-for="value in AVAILABLE_SIZES"
                        :key="value"
                        class="chip"
                        :color="size === value ? 'primary' : undefined"
                        :variant="size === value ? 'elevated' : 'tonal'"
                        @click="size = value"
                      >
                        {{ value }} × {{ value }}
                      </v-chip>
                    </div>
                  </div>

                  <div class="choice">
                    <div class="choice-label">Difficulté</div>
                    <div class="choice-row">
                      <v-chip
                        v-for="value in DIFFICULTIES"
                        :key="value"
                        class="chip"
                        :color="difficulty === value ? 'secondary' : undefined"
                        :variant="difficulty === value ? 'elevated' : 'tonal'"
                        @click="difficulty = value"
                      >
                        {{ value }}
                      </v-chip>
                    </div>
                  </div>

                  <v-btn class="cta" color="primary" size="x-large" @click="handleStart">
                    <v-icon start icon="mdi-play" />
                    Jouer
                  </v-btn>
                </v-card-text>
              </v-card>
            </section>

            <section v-else-if="screen === 'list'" class="panel">
              <v-card class="panel-card">
                <v-card-text class="panel-inner">
                  <div class="list-header">
                    <h2 class="panel-title">Grilles disponibles</h2>
                    <v-chip class="progress-chip" color="info" variant="tonal">
                      <v-icon start icon="mdi-check-decagram" />
                      {{ lastCompleted }} / {{ grids.length }}
                    </v-chip>
                  </div>

                  <div class="next-info" v-if="nextIndex > 0">
                    Prochaine grille jouable :
                    <strong>{{ grids[nextIndex - 1]?.id || '---' }}</strong>
                  </div>

                  <div class="grid-list">
                    <v-btn
                      v-for="(grid, idx) in grids"
                      :key="grid.id"
                      class="grid-card"
                      :class="{
                        completed: idx + 1 <= lastCompleted,
                        next: idx + 1 === nextIndex,
                        locked: idx + 1 > nextIndex,
                      }"
                      :disabled="idx + 1 !== nextIndex"
                      variant="flat"
                      @click="handleSelectGrid(grid, idx + 1)"
                    >
                      <div class="grid-card-inner">
                        <div class="grid-card-id">{{ grid.id }}</div>
                        <div class="grid-card-meta">
                          <span v-if="idx + 1 === nextIndex" class="badge">Jouable</span>
                          <span v-else-if="idx + 1 <= lastCompleted" class="badge ok">OK</span>
                          <span v-else class="badge lock">Verrouillé</span>
                        </div>
                      </div>
                    </v-btn>
                  </div>

                  <div class="list-actions">
                    <v-btn
                      class="cta"
                      color="primary"
                      size="x-large"
                      :disabled="!grids[nextIndex - 1]"
                      @click="handleSelectGrid(grids[nextIndex - 1], nextIndex)"
                    >
                      <v-icon start icon="mdi-play" />
                      Jouer
                    </v-btn>
                    <v-btn class="ghost" variant="tonal" @click="screen = 'select'">
                      <v-icon start icon="mdi-arrow-left" />
                      Retour
                    </v-btn>
                  </div>
                </v-card-text>
              </v-card>
            </section>

            <section v-else-if="screen === 'game' && activeGrid" class="game">
              <v-card class="game-card">
                <v-card-text class="game-inner">
                  <div class="game-header">
                    <v-chip class="meta" color="secondary" variant="tonal">
                      <v-icon start icon="mdi-grid" />
                      Grille {{ activeGrid.id }}
                    </v-chip>
                    <v-chip class="meta" color="info" variant="tonal">
                      <v-icon start icon="mdi-check" />
                      {{ solvedSet.size }} / {{ totalClues }}
                    </v-chip>
                  </div>

                  <div class="board-wrap">
                    <div class="board-shell">
                      <div :class="['board', { celebrate: celebrating }]" :style="boardStyle">
                        <div v-for="(row, r) in gridState" :key="`row-${r}`" class="row">
                          <div
                            v-for="(cell, c) in row"
                            :key="`${r}-${c}`"
                            class="cell"
                            :class="{
                              black: cell.value === '#',
                              fixed: cell.fixed,
                              highlight: selectedCells.has(`${r}-${c}`),
                            }"
                            :style="celebrating ? { animationDelay: `${(r * activeGrid.size + c) * 10}ms` } : {}"
                          >
                            {{ cell.value === '#' ? '' : cell.value }}
                          </div>
                        </div>
                      </div>
                    </div>
                  </div>

                  <div class="answer answer-bar">
                    <v-text-field
                      v-model="inputValue"
                      density="comfortable"
                      variant="solo"
                      hide-details
                      :placeholder="selectedClue ? `Mot (${selectedClue.answer.length} lettres)` : 'Sélectionner une définition'"
                      :disabled="!selectedClue"
                      :maxlength="selectedClue?.answer.length || 32"
                    />
                    <div class="answer-actions">
                      <v-btn class="cta" color="primary" size="large" :disabled="!selectedClue" @click="handleSubmit">
                        <v-icon start icon="mdi-check-bold" />
                        Valider
                      </v-btn>
                      <v-badge :content="String(hints)" :model-value="hints > 0" color="secondary" floating>
                        <v-btn
                          class="hint-btn"
                          icon
                          variant="tonal"
                          color="secondary"
                          :disabled="false"
                          @click="handleHintButton"
                          :aria-label="hints > 0 ? 'Utiliser un joker' : 'Boutique (obtenir des jokers)'"
                        >
                          <v-icon icon="mdi-lightbulb-on-outline" />
                        </v-btn>
                      </v-badge>
                    </div>
                  </div>

                  <div class="clues">
                    <v-tabs v-model="clueTab" class="clue-tabs" color="primary" align-tabs="center">
                      <v-tab value="across">
                        <v-icon start icon="mdi-arrow-right-bold" />
                        Horizontal
                      </v-tab>
                      <v-tab value="down">
                        <v-icon start icon="mdi-arrow-down-bold" />
                        Vertical
                      </v-tab>
                    </v-tabs>

                    <v-window v-model="clueTab" class="clue-window">
                      <v-window-item value="across">
                        <div class="clue-list">
                          <v-btn
                            v-for="clue in activeGrid.clues.across"
                            :key="`across-${clue.id}`"
                            class="clue"
                            :class="{
                              active: selectedClue?.id === clue.id && selectedClue?.dir === 'across',
                              solved: solvedSet.has(`across-${clue.id}`),
                            }"
                            variant="tonal"
                            block
                            @click="selectClue({ ...clue, dir: 'across' })"
                          >
                            <span class="clue-id">{{ clue.id }}.</span>
                            <span class="clue-text">{{ clue.clue }}</span>
                          </v-btn>
                        </div>
                      </v-window-item>

                      <v-window-item value="down">
                        <div class="clue-list">
                          <v-btn
                            v-for="clue in activeGrid.clues.down"
                            :key="`down-${clue.id}`"
                            class="clue"
                            :class="{
                              active: selectedClue?.id === clue.id && selectedClue?.dir === 'down',
                              solved: solvedSet.has(`down-${clue.id}`),
                            }"
                            variant="tonal"
                            block
                            @click="selectClue({ ...clue, dir: 'down' })"
                          >
                            <span class="clue-id">{{ clue.id }}.</span>
                            <span class="clue-text">{{ clue.clue }}</span>
                          </v-btn>
                        </div>
                      </v-window-item>
                    </v-window>
                  </div>

                  <v-btn v-if="isSolved" class="next" color="primary" @click="handleNextGrid">
                    <v-icon start icon="mdi-skip-next" />
                    Grille suivante
                  </v-btn>
                </v-card-text>
              </v-card>
            </section>
          </div>
        </v-fade-transition>
      </v-container>
    </v-main>

    <v-overlay v-model="loadingGrid" class="align-center justify-center" persistent>
      <v-progress-circular indeterminate color="primary" size="56" />
      <div class="overlay-text">Chargement des définitions…</div>
    </v-overlay>

    <v-dialog v-model="showSettings" max-width="360" content-class="safe-dialog-content">
      <v-card class="modal">
        <v-card-title>Options</v-card-title>
        <v-card-text>
          <div class="choice-row">
            <v-chip
              v-for="value in DIFFICULTIES"
              :key="value"
              class="chip"
              :color="difficulty === value ? 'primary' : undefined"
              @click="difficulty = value"
            >
              {{ value }}
            </v-chip>
          </div>
          <v-switch v-model="soundEnabled" label="Son activé" color="primary" inset />
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="showSettings = false">Fermer</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="showMenu" max-width="320" content-class="safe-dialog-content">
      <v-card class="modal">
        <v-card-title>Menu</v-card-title>
        <v-card-text class="menu-actions">
          <v-btn prepend-icon="mdi-ruler-square" variant="text" @click="goSelect">Choix taille</v-btn>
          <v-btn prepend-icon="mdi-format-list-bulleted" variant="text" @click="goList">Liste des grilles</v-btn>
          <v-btn prepend-icon="mdi-crown-outline" variant="text" @click="showPremiumDialog = true">
            Pass premium (5 €)
            <v-chip v-if="premium" class="premium-chip" color="secondary" variant="tonal" size="x-small">Actif</v-chip>
          </v-btn>
          <v-btn prepend-icon="mdi-exit-to-app" variant="text" @click="handleQuit">Quitter</v-btn>
        </v-card-text>
      </v-card>
    </v-dialog>

    <v-dialog v-model="showAdDialog" max-width="420" persistent content-class="safe-dialog-content">
      <v-card class="modal">
        <v-card-title>{{ adTitle }}</v-card-title>
        <v-card-text class="ad-body">
          <v-progress-linear indeterminate color="primary" />
          <div class="ad-copy">{{ adCopy }}</div>
          <div class="ad-note">Sur Android, cette fenêtre sera remplacée par une vraie publicité (rewarded/interstitielle).</div>
        </v-card-text>
      </v-card>
    </v-dialog>

    <v-dialog v-model="showShopDialog" max-width="420" content-class="safe-dialog-content">
      <v-card class="modal">
        <v-card-title class="shop-title">
          <div class="shop-title-left">
            Boutique
            <v-chip class="shop-balance" color="secondary" variant="tonal" size="small">
              <v-icon start icon="mdi-lightbulb-on-outline" />
              {{ hints }}
            </v-chip>
          </div>
          <v-btn icon variant="text" @click="showShopDialog = false" aria-label="Fermer">
            <v-icon icon="mdi-close" />
          </v-btn>
        </v-card-title>
        <v-card-text class="shop-body">
          <v-btn class="shop-reward" color="primary" variant="tonal" block @click="handleShopWatchVideo">
            <v-icon start icon="mdi-video-outline" />
            Regarder une vidéo (+1 joker)
          </v-btn>

          <div class="shop-grid">
            <v-btn class="shop-pack" variant="tonal" block @click="purchaseHints(2, 2)">
              <div class="pack-main">2 jokers</div>
              <div class="pack-sub">2 €</div>
            </v-btn>
            <v-btn class="shop-pack" variant="tonal" block @click="purchaseHints(6, 5)">
              <div class="pack-main">6 jokers</div>
              <div class="pack-sub">5 €</div>
            </v-btn>
            <v-btn class="shop-pack" variant="tonal" block @click="purchaseHints(15, 10)">
              <div class="pack-main">15 jokers</div>
              <div class="pack-sub">10 €</div>
            </v-btn>
            <v-btn class="shop-pack" variant="tonal" block @click="purchaseHints(30, 18)">
              <div class="pack-main">30 jokers</div>
              <div class="pack-sub">18 €</div>
            </v-btn>
          </div>

          <div class="shop-note">
            Sur Android, ces achats seront reliés à Google Play Billing. Ici, c'est une simulation (ajout direct des jokers).
          </div>
        </v-card-text>
      </v-card>
    </v-dialog>

    <v-dialog v-model="showPremiumDialog" max-width="420" content-class="safe-dialog-content">
      <v-card class="modal">
        <v-card-title class="shop-title">
          <div class="shop-title-left">
            Pass premium
            <v-chip class="shop-balance" color="secondary" variant="tonal" size="small">
              <v-icon start icon="mdi-crown-outline" />
              {{ premium ? 'Actif' : 'Inactif' }}
            </v-chip>
          </div>
          <v-btn icon variant="text" @click="showPremiumDialog = false" aria-label="Fermer">
            <v-icon icon="mdi-close" />
          </v-btn>
        </v-card-title>
        <v-card-text class="shop-body">
          <div class="shop-note">
            Le premium est un achat “à vie” pour l’utilisateur : il supprime les publicités affichées automatiquement au début de chaque grille. Les pubs “rewarded” restent disponibles si tu veux gagner des jokers.
          </div>
          <v-btn :disabled="premium" color="primary" block @click="purchasePremium">
            <v-icon start icon="mdi-crown" />
            Acheter premium — 5 €
          </v-btn>
        </v-card-text>
      </v-card>
    </v-dialog>

    <v-snackbar v-model="snackbar.show" :timeout="snackbar.timeout" color="secondary" rounded="lg">
      {{ snackbar.text }}
    </v-snackbar>
  </v-app>
</template>

<script setup>
import { computed, onMounted, ref, watch } from 'vue';
import { buildGridList, getAvailableSizes } from './data/grids.js';
import {
  addHints as nativeAddHints,
  initAds,
  initPurchases,
  isNative,
  isPremium as nativeIsPremium,
  purchaseProduct,
  setPremium as nativeSetPremium,
  showInterstitialAd as nativeShowInterstitialAd,
  showRewardedAd as nativeShowRewardedAd,
} from './monetization.js';

const AVAILABLE_SIZES = computed(() => getAvailableSizes().filter((s) => s <= 10));
const DIFFICULTIES = ['facile', 'moyen', 'difficile'];
const PREFS_KEY = 'motcroise_prefs';
const SESSION_KEY = 'motcroise_session';
const GRID_STATE_PREFIX = 'motcroise_gridstate';
const LAST_IN_PROGRESS_PREFIX = 'motcroise_last_in_progress';
const HINTS_KEY = 'motcroise_hints';
const PREMIUM_KEY = 'motcroise_premium_v1';
const PREMIUM_KEY_BACKUP = 'motcroise_premium_backup_v1';
const PREMIUM_TS_KEY = 'motcroise_premium_ts_v1';

const ADMOB_INTERSTITIAL_ANDROID = import.meta.env.VITE_ADMOB_INTERSTITIAL_ANDROID ?? '';
const ADMOB_REWARDED_ANDROID = import.meta.env.VITE_ADMOB_REWARDED_ANDROID ?? '';
// Default to Google test ad units when env vars are not provided (so ads work out-of-the-box on Android).
// Replace with your own Ad Unit IDs before publishing.
const ADMOB_INTERSTITIAL_ANDROID_TEST = 'ca-app-pub-3940256099942544/1033173712';
const ADMOB_REWARDED_ANDROID_TEST = 'ca-app-pub-3940256099942544/5224354917';
const REVENUECAT_APIKEY_ANDROID = import.meta.env.VITE_REVENUECAT_APIKEY_ANDROID ?? '';
const RC_PRODUCT_PREMIUM = import.meta.env.VITE_RC_PRODUCT_PREMIUM ?? 'premium_lifetime';
const RC_PRODUCT_HINTS_2 = import.meta.env.VITE_RC_PRODUCT_HINTS_2 ?? 'hints_2';
const RC_PRODUCT_HINTS_6 = import.meta.env.VITE_RC_PRODUCT_HINTS_6 ?? 'hints_6';
const RC_PRODUCT_HINTS_15 = import.meta.env.VITE_RC_PRODUCT_HINTS_15 ?? 'hints_15';
const RC_PRODUCT_HINTS_30 = import.meta.env.VITE_RC_PRODUCT_HINTS_30 ?? 'hints_30';

const screen = ref('select');
const size = ref(10);
const difficulty = ref('facile');
const activeGrid = ref(null);
const gridState = ref([]);
const selectedClue = ref(null);
const inputValue = ref('');
const soundEnabled = ref(true);
const showSettings = ref(false);
const showMenu = ref(false);
const celebrating = ref(false);
const solvedSet = ref(new Set());
const lastCompleted = ref(0);
const loadingGrid = ref(false);
const clueTab = ref('across');
const hints = ref(0);
const premium = ref(false);

const showAdDialog = ref(false);
const adTitle = ref('Publicité');
const adCopy = ref('');
const showShopDialog = ref(false);
const showPremiumDialog = ref(false);
const snackbar = ref({ show: false, text: '', timeout: 1600 });

const grids = computed(() => buildGridList({ size: size.value, difficulty: difficulty.value, count: 999 }));
const nextIndex = computed(() => Math.min(lastCompleted.value + 1, grids.value.length));
const boardStyle = computed(() => {
  if (!activeGrid.value) return {};
  const gridSize = activeGrid.value.size;
  return {
    '--grid-size': gridSize,
  };
});

const allClues = computed(() => {
  if (!activeGrid.value) return [];
  return [
    ...activeGrid.value.clues.across.map((c) => ({ ...c, dir: 'across' })),
    ...activeGrid.value.clues.down.map((c) => ({ ...c, dir: 'down' })),
  ];
});

const totalClues = computed(() => allClues.value.length);
const isSolved = computed(() => totalClues.value > 0 && solvedSet.value.size >= totalClues.value);

const acrossCount = computed(() => activeGrid.value?.clues?.across?.length ?? 0);
const downCount = computed(() => activeGrid.value?.clues?.down?.length ?? 0);

const selectedCells = computed(() => {
  if (!selectedClue.value) return new Set();
  return new Set(selectedClue.value.cells.map(([r, c]) => `${r}-${c}`));
});

function getProgressKey() {
  return `motcroise_progress_${size.value}_${difficulty.value}`;
}

function loadProgress() {
  const raw = localStorage.getItem(getProgressKey());
  const value = Number.parseInt(raw ?? '0', 10);
  lastCompleted.value = Number.isNaN(value) ? 0 : value;
}

function saveProgress(last) {
  localStorage.setItem(getProgressKey(), String(last));
  lastCompleted.value = last;
}

function savePrefs() {
  localStorage.setItem(PREFS_KEY, JSON.stringify({ size: size.value, difficulty: difficulty.value }));
}

function loadPrefs() {
  try {
    const raw = localStorage.getItem(PREFS_KEY);
    if (!raw) return;
    const data = JSON.parse(raw);
    if (data?.size) size.value = data.size;
    if (data?.difficulty) difficulty.value = data.difficulty;
  } catch {
    // ignore
  }
}

function createInitialGridState(grid) {
  return grid.map((row) =>
    row.split('').map((ch) => ({
      value: ch === '#' ? '#' : '',
      fixed: false,
    }))
  );
}

function lockAllLetters(state) {
  return state.map((row) => row.map((cell) => (cell.value === '#' ? cell : { ...cell, fixed: true })));
}

function playTone(frequency, durationMs, volume = 0.06) {
  if (!soundEnabled.value) return;
  const context = new (window.AudioContext || window.webkitAudioContext)();
  const osc = context.createOscillator();
  const gain = context.createGain();
  osc.type = 'sine';
  osc.frequency.value = frequency;
  gain.gain.value = volume;
  osc.connect(gain);
  gain.connect(context.destination);
  osc.start();
  setTimeout(() => {
    osc.stop();
    context.close();
  }, durationMs);
}

function playSuccessSound() {
  if (!soundEnabled.value) return;
  const context = new (window.AudioContext || window.webkitAudioContext)();
  const master = context.createGain();
  master.gain.value = 0.0001;
  master.connect(context.destination);

  const now = context.currentTime;
  master.gain.exponentialRampToValueAtTime(0.12, now + 0.02);
  master.gain.exponentialRampToValueAtTime(0.0001, now + 0.6);

  const chord = [523.25, 659.25, 783.99, 987.77]; // C5 E5 G5 B5

  const addVoice = (freq, offset, duration) => {
    const gain = context.createGain();
    gain.gain.value = 0.0001;
    gain.connect(master);
    const oscA = context.createOscillator();
    const oscB = context.createOscillator();
    oscA.type = 'sine';
    oscB.type = 'triangle';
    oscA.frequency.value = freq;
    oscB.frequency.value = freq;
    oscA.detune.value = -6;
    oscB.detune.value = 6;
    oscA.connect(gain);
    oscB.connect(gain);
    const start = now + offset;
    gain.gain.exponentialRampToValueAtTime(0.08, start + 0.02);
    gain.gain.exponentialRampToValueAtTime(0.0001, start + duration);
    oscA.start(start);
    oscB.start(start);
    oscA.stop(start + duration);
    oscB.stop(start + duration);
  };

  chord.forEach((freq, i) => {
    addVoice(freq, i * 0.035, 0.35);
  });

  // sparkle
  addVoice(1567.98, 0.18, 0.2);

  setTimeout(() => context.close(), 800);
}

function wait(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

async function runAd({ title, copy, durationMs }) {
  adTitle.value = title;
  adCopy.value = copy;
  showAdDialog.value = true;
  await wait(durationMs);
  showAdDialog.value = false;
  await wait(150);
  return true;
}

async function showRewardedAd() {
  if (isNative()) {
    const adUnitIdAndroid = ADMOB_REWARDED_ANDROID || ADMOB_REWARDED_ANDROID_TEST;
    const ok = await nativeShowRewardedAd({ adUnitIdAndroid });
    if (ok) hints.value += 1;
    else toast("Publicité indisponible. Réessaie plus tard.", 2200);
    return;
  }
  const ok = await runAd({
    title: 'Publicité (rewarded)',
    copy: "Regarde cette pub pour gagner un joker. Merci de soutenir l'application.",
    durationMs: 2800,
  });
  if (ok) hints.value += 1;
}

async function showInterstitialAd() {
  if (premium.value) return;
  if (isNative()) {
    const adUnitIdAndroid = ADMOB_INTERSTITIAL_ANDROID || ADMOB_INTERSTITIAL_ANDROID_TEST;
    await nativeShowInterstitialAd({ adUnitIdAndroid });
    return;
  }
  await runAd({
    title: 'Publicité',
    copy: 'Chargement de la grille…',
    durationMs: 1500,
  });
}

function toast(text, timeout = 1600) {
  snackbar.value = { show: true, text, timeout };
}

function getWordFromGridState(clue, state) {
  if (!clue?.cells?.length) return '';
  return clue.cells
    .map(([r, c]) => {
      const cell = state?.[r]?.[c];
      if (!cell || cell.value === '#' || !cell.value) return '';
      return String(cell.value).toUpperCase();
    })
    .join('');
}

function maybeAutoSolveOtherDirection(nextGridState) {
  if (!activeGrid.value) return;
  const grid = nextGridState ?? gridState.value;

  const acrossSolved = acrossCount.value > 0 && Array.from({ length: acrossCount.value }).every((_, i) => {
    const clue = activeGrid.value.clues.across[i];
    return solvedSet.value.has(`across-${clue.id}`);
  });
  const downSolved = downCount.value > 0 && Array.from({ length: downCount.value }).every((_, i) => {
    const clue = activeGrid.value.clues.down[i];
    return solvedSet.value.has(`down-${clue.id}`);
  });

  if (acrossSolved && downSolved) return;

  const shouldFillDown = acrossSolved && !downSolved;
  const shouldFillAcross = downSolved && !acrossSolved;
  if (!shouldFillDown && !shouldFillAcross) return;

  const targetDir = shouldFillDown ? 'down' : 'across';
  const clues = activeGrid.value.clues[targetDir] ?? [];
  if (clues.length === 0) return;

  let mismatches = 0;
  let filled = 0;
  const nextSolved = new Set(solvedSet.value);
  for (const clue of clues) {
    const key = `${targetDir}-${clue.id}`;
    if (nextSolved.has(key)) continue;

    const word = getWordFromGridState(clue, grid);
    if (!word || word.length !== String(clue.answer ?? '').length) {
      mismatches += 1;
      continue;
    }
    if (word === String(clue.answer).toUpperCase()) {
      nextSolved.add(key);
      filled += 1;
    } else {
      mismatches += 1;
    }
  }

  if (filled > 0) solvedSet.value = nextSolved;
  if (mismatches > 0) {
    toast('Certaines définitions de l’autre sens ne correspondent pas à la grille', 2200);
  }
}

async function handleShopWatchVideo() {
  await showRewardedAd();
  showShopDialog.value = false;
}

function purchaseHints(amount, euros) {
  // Android: use RevenueCat purchase when configured; otherwise simulate.
  const map = new Map([
    [2, RC_PRODUCT_HINTS_2],
    [6, RC_PRODUCT_HINTS_6],
    [15, RC_PRODUCT_HINTS_15],
    [30, RC_PRODUCT_HINTS_30],
  ]);

  const productId = map.get(amount) ?? '';
  if (isNative() && REVENUECAT_APIKEY_ANDROID && productId) {
    purchaseProduct(productId).then((res) => {
      if (res.ok) {
        hints.value += amount;
        showShopDialog.value = false;
        toast(`Achat OK : +${amount} jokers`, 2000);
      } else {
        toast('Achat annulé ou indisponible', 2000);
      }
    });
    return;
  }

  hints.value += amount;
  showShopDialog.value = false;
  toast(`Achat simulé : +${amount} jokers (${euros} €)`);
}

function purchasePremium() {
  if (premium.value) return;

  if (isNative() && REVENUECAT_APIKEY_ANDROID && RC_PRODUCT_PREMIUM) {
    purchaseProduct(RC_PRODUCT_PREMIUM).then((res) => {
      if (res.ok) {
        premium.value = true;
        showPremiumDialog.value = false;
        try {
          localStorage.setItem(PREMIUM_KEY_BACKUP, '1');
          localStorage.setItem(PREMIUM_TS_KEY, new Date().toISOString());
        } catch {
          // ignore
        }
        toast('Premium activé : pubs entre grilles désactivées', 2000);
      } else {
        toast('Achat annulé ou indisponible', 2000);
      }
    });
    return;
  }

  // Web / fallback: simulation.
  premium.value = true;
  showPremiumDialog.value = false;
  try {
    localStorage.setItem(PREMIUM_KEY_BACKUP, '1');
    localStorage.setItem(PREMIUM_TS_KEY, new Date().toISOString());
  } catch {
    // ignore
  }
  toast('Premium activé : pubs entre grilles désactivées', 2000);
}

function handleStart() {
  screen.value = 'list';
}

function selectClue(clue) {
  clueTab.value = clue.dir === 'down' ? 'down' : 'across';
  selectedClue.value = clue;
  inputValue.value = '';
}

function applyCorrectAnswerToGrid(clue) {
  const nextGrid = gridState.value.map((row) => row.map((cell) => ({ ...cell })));
  const answer = clue.answer.toUpperCase();
  clue.cells.forEach((cell, idx) => {
    const [r, c] = cell;
    const current = nextGrid[r][c];
    if (current.value === '#') return;
    current.value = answer[idx] ?? '';
    current.fixed = true;
  });
  return nextGrid;
}

function useHint() {
  if (!selectedClue.value || celebrating.value) return;
  if (hints.value <= 0) return;

  hints.value -= 1;
  const nextGrid = applyCorrectAnswerToGrid(selectedClue.value);
  const nextSolved = new Set(solvedSet.value);
  nextSolved.add(`${selectedClue.value.dir}-${selectedClue.value.id}`);
  solvedSet.value = nextSolved;
  gridState.value = nextGrid;
  inputValue.value = '';
  playSuccessSound();
  maybeAutoSolveOtherDirection(nextGrid);

  if (isSolved.value) {
    celebrating.value = true;
    gridState.value = lockAllLetters(nextGrid);
    playTone(523, 200);
    setTimeout(() => playTone(659, 200), 220);
    setTimeout(() => playTone(784, 220), 440);
    persistSession(false);
    saveGridProgress(activeGrid.value, false);
  } else {
    persistSession(true);
    saveGridProgress(activeGrid.value, true);
  }
}

function handleHintButton() {
  if (hints.value <= 0) {
    showShopDialog.value = true;
    return;
  }
  if (!selectedClue.value) {
    toast('Sélectionne une définition');
    return;
  }
  if (solvedSet.value.has(`${selectedClue.value.dir}-${selectedClue.value.id}`)) {
    showShopDialog.value = true;
    return;
  }
  useHint();
}

function handleSelectGrid(grid, index) {
  if (index !== nextIndex.value) return;
  loadingGrid.value = true;
  // Let the UI paint the loader before heavy state updates.
  setTimeout(async () => {
    await showInterstitialAd();
    activeGrid.value = grid;
    const saved = loadGridProgress(grid);
    gridState.value = saved?.gridState ?? createInitialGridState(grid.grid);
    solvedSet.value = new Set(saved?.solved ?? []);
    selectedClue.value = null;
    clueTab.value = 'across';
    inputValue.value = '';
    celebrating.value = false;
    screen.value = 'game';
    persistSession(true);
    saveGridProgress(grid, true);
    loadingGrid.value = false;
  }, 0);
}

function handleSubmit() {
  if (!selectedClue.value || celebrating.value) return;
  const answer = selectedClue.value.answer.toUpperCase();
  const typed = inputValue.value.trim().toUpperCase();
  const isCorrect = typed === answer;
  const nextGrid = gridState.value.map((row) => row.map((cell) => ({ ...cell })));

  selectedClue.value.cells.forEach((cell, idx) => {
    const [r, c] = cell;
    const current = nextGrid[r][c];
    if (current.value === '#') return;
    if (isCorrect) {
      current.value = answer[idx];
      current.fixed = true;
      return;
    }
    if (difficulty.value === 'difficile') {
      if (typed[idx]) current.value = typed[idx];
      return;
    }
    if (difficulty.value === 'facile') {
      if (typed[idx] && typed[idx] === answer[idx]) {
        current.value = typed[idx];
      }
    }
  });

  if (isCorrect) {
    const nextSolved = new Set(solvedSet.value);
    nextSolved.add(`${selectedClue.value.dir}-${selectedClue.value.id}`);
    solvedSet.value = nextSolved;
    playSuccessSound();
  }

  gridState.value = nextGrid;
  inputValue.value = '';
  if (isCorrect) maybeAutoSolveOtherDirection(nextGrid);

  if (isSolved.value) {
    celebrating.value = true;
    gridState.value = lockAllLetters(nextGrid);
    playTone(523, 200);
    setTimeout(() => playTone(659, 200), 220);
    setTimeout(() => playTone(784, 220), 440);
    persistSession(false);
    saveGridProgress(activeGrid.value, false);
  } else {
    persistSession(true);
    saveGridProgress(activeGrid.value, true);
  }
}

function handleNextGrid() {
  const index = Number.parseInt(activeGrid.value?.id ?? '0', 10);
  saveProgress(index);
  clearGridProgress(activeGrid.value);
  clearSession();
  screen.value = 'list';
}

function handleQuit() {
  showMenu.value = false;
  persistSession(true);
  saveGridProgress(activeGrid.value, true);
  screen.value = 'select';
  activeGrid.value = null;
}

function goSelect() {
  showMenu.value = false;
  persistSession(true);
  saveGridProgress(activeGrid.value, true);
  screen.value = 'select';
}

function goList() {
  showMenu.value = false;
  persistSession(true);
  saveGridProgress(activeGrid.value, true);
  screen.value = 'list';
}

function persistSession(inProgress) {
  if (!activeGrid.value) return;
  const payload = {
    size: size.value,
    difficulty: difficulty.value,
    gridId: activeGrid.value.id,
    inProgress,
    celebrating: celebrating.value,
    solved: Array.from(solvedSet.value),
    gridState: gridState.value,
  };
  localStorage.setItem(SESSION_KEY, JSON.stringify(payload));
}

function clearSession() {
  localStorage.removeItem(SESSION_KEY);
}

function gridStateKey(gridSize, gridId) {
  return `${GRID_STATE_PREFIX}_${gridSize}_${String(gridId).padStart(3, '0')}`;
}

function lastInProgressKey(gridSize) {
  return `${LAST_IN_PROGRESS_PREFIX}_${gridSize}`;
}

function saveGridProgress(grid, inProgress) {
  if (!grid?.id) return;
  const key = gridStateKey(grid.size, grid.id);
  const payload = {
    size: grid.size,
    gridId: grid.id,
    inProgress: Boolean(inProgress),
    updated_utc: new Date().toISOString(),
    solved: Array.from(solvedSet.value),
    gridState: gridState.value,
  };
  try {
    localStorage.setItem(key, JSON.stringify(payload));
    if (payload.inProgress) {
      localStorage.setItem(lastInProgressKey(grid.size), String(grid.id).padStart(3, '0'));
    }
  } catch {
    // ignore (storage full / blocked)
  }
}

function loadGridProgress(grid) {
  if (!grid?.id) return null;
  const key = gridStateKey(grid.size, grid.id);
  try {
    const raw = localStorage.getItem(key);
    if (!raw) return null;
    const parsed = JSON.parse(raw);
    if (!parsed || parsed.size !== grid.size || parsed.gridId !== grid.id) return null;
    if (!parsed.inProgress) return null;
    return parsed;
  } catch {
    return null;
  }
}

function clearGridProgress(grid) {
  if (!grid?.id) return;
  try {
    localStorage.removeItem(gridStateKey(grid.size, grid.id));
  } catch {
    // ignore
  }
}

function restoreSession(session) {
  size.value = session.size ?? size.value;
  difficulty.value = session.difficulty ?? difficulty.value;
  const grid = grids.value.find((g) => g.id === session.gridId);
  if (!grid) return false;
  activeGrid.value = grid;
  gridState.value = session.gridState ?? createInitialGridState(grid.grid);
  solvedSet.value = new Set(session.solved ?? []);
  celebrating.value = Boolean(session.celebrating);
  clueTab.value = 'across';
  screen.value = 'game';
  return true;
}

watch([size, difficulty], () => {
  savePrefs();
  loadProgress();
});

watch(
  hints,
  () => {
    try {
      localStorage.setItem(HINTS_KEY, String(hints.value));
    } catch {
      // ignore
    }
  },
  { deep: false }
);

watch(gridState, () => {
  if (screen.value === 'game') {
    persistSession(!isSolved.value);
    saveGridProgress(activeGrid.value, !isSolved.value);
  }
}, { deep: true });

watch(solvedSet, () => {
  if (screen.value === 'game') {
    persistSession(!isSolved.value);
    saveGridProgress(activeGrid.value, !isSolved.value);
  }
}, { deep: true });

onMounted(() => {
  loadPrefs();
  loadProgress();
  try {
    const primary = localStorage.getItem(PREMIUM_KEY);
    const backup = localStorage.getItem(PREMIUM_KEY_BACKUP);
    premium.value = primary === '1' || backup === '1';
    if (premium.value) {
      localStorage.setItem(PREMIUM_KEY, '1');
      localStorage.setItem(PREMIUM_KEY_BACKUP, '1');
    }
  } catch {
    premium.value = false;
  }

  // Native init (ads + purchases). Safe even if not configured yet.
  if (isNative()) {
    const usingTestAds = !ADMOB_INTERSTITIAL_ANDROID || !ADMOB_REWARDED_ANDROID;
    initAds({ appIdAndroid: '', testMode: usingTestAds });
    if (REVENUECAT_APIKEY_ANDROID) initPurchases({ apiKeyAndroid: REVENUECAT_APIKEY_ANDROID, appUserId: null });
  }
  try {
    const raw = localStorage.getItem(HINTS_KEY);
    if (!raw) {
      hints.value = 3;
      localStorage.setItem(HINTS_KEY, '3');
    } else {
      const value = Number.parseInt(raw ?? '0', 10);
      hints.value = Number.isNaN(value) ? 3 : Math.max(0, value);
      if (Number.isNaN(value)) localStorage.setItem(HINTS_KEY, String(hints.value));
    }
  } catch {
    hints.value = 3;
  }

  // If user stored a size that's currently hidden/unavailable, fall back to the first available size.
  if (!AVAILABLE_SIZES.value.includes(size.value)) {
    size.value = AVAILABLE_SIZES.value[0] ?? 10;
  }
  try {
    const raw = localStorage.getItem(SESSION_KEY);
    if (raw) {
      const session = JSON.parse(raw);
      if (session?.inProgress) {
        restoreSession(session);
        saveGridProgress(activeGrid.value, true);
      }
    }
  } catch {
    // ignore
  }

  // If there is no active session but we have a last in-progress grid, resume it.
  if (screen.value !== 'game') {
    try {
      const lastId = localStorage.getItem(lastInProgressKey(size.value));
      if (lastId) {
        const grid = grids.value.find((g) => g.id === lastId);
        const saved = grid ? loadGridProgress(grid) : null;
        if (grid && saved) {
          activeGrid.value = grid;
          gridState.value = saved.gridState ?? createInitialGridState(grid.grid);
          solvedSet.value = new Set(saved.solved ?? []);
          celebrating.value = false;
          selectedClue.value = null;
          clueTab.value = 'across';
          screen.value = 'game';
          persistSession(true);
        }
      }
    } catch {
      // ignore
    }
  }
});

watch(
  premium,
  () => {
    try {
      if (premium.value) {
        localStorage.setItem(PREMIUM_KEY, '1');
        localStorage.setItem(PREMIUM_KEY_BACKUP, '1');
        if (!localStorage.getItem(PREMIUM_TS_KEY)) localStorage.setItem(PREMIUM_TS_KEY, new Date().toISOString());
      }
    } catch {
      // ignore
    }
  },
  { deep: false }
);
</script>
