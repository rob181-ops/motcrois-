import { Capacitor } from '@capacitor/core';
import { AdMob, AdmobConsentStatus } from '@capacitor-community/admob';
import { Purchases } from '@revenuecat/purchases-capacitor';

const LS_PREMIUM_KEY = 'motcroise_premium_v1';
const LS_PREMIUM_BACKUP_KEY = 'motcroise_premium_backup_v1';
const LS_HINTS_KEY = 'motcroise_hints';

function safeGet(key) {
  try {
    return localStorage.getItem(key);
  } catch {
    return null;
  }
}

function safeSet(key, value) {
  try {
    localStorage.setItem(key, value);
  } catch {
    // ignore
  }
}

export function isNative() {
  const platform = Capacitor.getPlatform?.() ?? 'web';
  if (platform && platform !== 'web') return true;
  return Capacitor.isNativePlatform?.() ?? false;
}

export function getHints() {
  const raw = safeGet(LS_HINTS_KEY);
  const value = Number.parseInt(raw ?? '0', 10);
  return Number.isNaN(value) ? 0 : Math.max(0, value);
}

export function setHints(value) {
  safeSet(LS_HINTS_KEY, String(Math.max(0, Number(value) || 0)));
}

export function addHints(delta) {
  setHints(getHints() + (Number(delta) || 0));
}

export function isPremium() {
  const p = safeGet(LS_PREMIUM_KEY);
  const b = safeGet(LS_PREMIUM_BACKUP_KEY);
  return p === '1' || b === '1';
}

export function setPremium() {
  safeSet(LS_PREMIUM_KEY, '1');
  safeSet(LS_PREMIUM_BACKUP_KEY, '1');
}

export async function initAds({ appIdAndroid, testMode = false }) {
  if (!isNative()) return;
  try {
    // Consent (EU). In production you should show a consent UI; this is a minimal setup.
    await AdMob.initialize();
    const consent = await AdMob.requestConsentInfo();
    if (consent?.status === AdmobConsentStatus.REQUIRED) {
      await AdMob.showConsentForm();
    }
    if (testMode) {
      await AdMob.setRequestConfiguration({
        testDeviceIds: ['EMULATOR'],
      });
    }
    // App ID is configured in AndroidManifest via AdMob; keep param to document setup.
    void appIdAndroid;
  } catch {
    // ignore (no ads configured yet)
  }
}

export async function showInterstitialAd({ adUnitIdAndroid }) {
  if (!isNative()) return true;
  if (isPremium()) return true;
  try {
    await AdMob.prepareInterstitial({ adId: adUnitIdAndroid });
    await AdMob.showInterstitial();
    return true;
  } catch {
    return false;
  }
}

export async function showRewardedAd({ adUnitIdAndroid }) {
  if (!isNative()) return true;
  try {
    await AdMob.prepareRewardVideoAd({ adId: adUnitIdAndroid });
    await AdMob.showRewardVideoAd();
    return true;
  } catch {
    return false;
  }
}

export async function initPurchases({ apiKeyAndroid, appUserId = null }) {
  if (!isNative()) return;
  try {
    await Purchases.configure({ apiKey: apiKeyAndroid, appUserId });
  } catch {
    // ignore
  }
}

export async function purchaseProduct(productId) {
  if (!isNative()) return { ok: false, reason: 'not-native' };
  try {
    const result = await Purchases.purchaseProduct({ productIdentifier: productId });
    return { ok: true, result };
  } catch (e) {
    return { ok: false, reason: 'failed', error: String(e?.message ?? e) };
  }
}
