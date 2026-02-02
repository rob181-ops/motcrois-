# Publication Android (Google Play)

Ce dossier contient une app Vue/Vite empaquetée via **Capacitor** pour Android.

## 1) Pré-requis

- Android Studio + Android SDK
- JDK installé
- Compte Google Play Console (frais unique 25$)

## 2) Local: build + sync Android

Depuis le dossier `MotCroise.App`:

```powershell
npm install
copy .env.example .env
# (optionnel) remplir les variables VITE_* si vous branchez pubs/achats
npm run cap:sync
npm run android:open
```

## 3) Ads / Achats (optionnel)

L’app supporte 2 modes:
- **Web / desktop**: simulation (popups + ajout direct de jokers/premium)
- **Android**: plugins Capacitor (si configurés)

### AdMob (pubs interstitielles + rewarded)

Par défaut, l’app fonctionne **sans configuration** en utilisant les **Ad Units de test Google** (pour que les pubs s’affichent et que l’app ne crash pas).

Pour passer en production:

1. Créer une app AdMob + ad units (Android).
2. Remplir `.env` (vos IDs):
   - `VITE_ADMOB_INTERSTITIAL_ANDROID`
   - `VITE_ADMOB_REWARDED_ANDROID`
3. Remplacer l’App ID AdMob dans `android/app/src/main/res/values/admob.xml` (par défaut c’est un **TEST App ID** pour éviter un crash).
4. Suivre la doc du plugin `@capacitor-community/admob`.

### Premium + Packs de jokers (achats)

Le projet est prêt pour **RevenueCat** (`@revenuecat/purchases-capacitor`) afin d’éviter d’implémenter Google Play Billing à la main.

1. Créer un projet RevenueCat + coller la clé Android dans `.env`:
   - `VITE_REVENUECAT_APIKEY_ANDROID`
2. Créer des products (identifiants) et les mapper:
   - `VITE_RC_PRODUCT_PREMIUM` (achat “à vie”)
   - `VITE_RC_PRODUCT_HINTS_2`, `VITE_RC_PRODUCT_HINTS_6`, `VITE_RC_PRODUCT_HINTS_15`, `VITE_RC_PRODUCT_HINTS_30`
3. Publier ces produits dans Google Play Console (monétisation) et lier RevenueCat.

> Note: côté app, le premium est persistant (stockage local) et désactive les pubs “entre grilles”.

## 4) Générer un AAB signé (Play Store)

Dans Android Studio:

1. `Build` → `Generate Signed Bundle / APK…`
2. Choisir **Android App Bundle (AAB)**.
3. Créer/choisir un **keystore** (à sauvegarder soigneusement).
4. Générer le `.aab`.

## 5) Publier sur Google Play Console

Recommandé: commencer par **Internal testing**.

1. Créer l’application.
2. Remplir:
   - Fiche Play Store (titre, description, screenshots)
   - Classification du contenu
   - Politique de confidentialité (URL obligatoire)
   - “Data safety” (déclarations)
3. Upload du `.aab` dans la piste (Internal / Closed / Production).
4. Soumettre en revue.

## 5bis) Politique de confidentialité (URL)

Google Play demande une URL publique. Une page prête est fournie dans le repo :
- `docs/privacy/index.html` (à publier via GitHub Pages)
- Voir `docs/privacy/README.md` pour activer GitHub Pages.

## 6) Versioning (important)

Pour chaque release:
- Augmenter `versionCode` et `versionName` dans `android/app/build.gradle` (Android Studio).
