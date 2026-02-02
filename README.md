# MotCroise - generateur de grille

## But final
Generer une grille de mots croises 20x20 dense, sans theme impose, avec :
- tous les mots laisses vides (a deviner) mais avec des definitions
- beaucoup de mots croises (densite elevee, peu de cases noires)
- definitions humaines exploitables (pas "definition indisponible")
- export PDF final

## Ce qui a ete mis en place et pourquoi
Le flux a ete durci pour obtenir une grille remplie et definissable:

1) Dictionnaire
   - Base sur le SQLite de definitions (fr-extract.jsonl).
   - Fallback Hunspell FR si SQLite indisponible.
   - Objectif: maximiser la densite avec un pool large et definissable.

2) Definitions
   - Recuperation depuis le cache local si present.
   - Sinon fallback sur definitions internes.
   - Sinon requetes Wiktionary (REST + WiktionaryNET).
   - Prefetch des definitions manquantes une fois la grille trouvee.
   - Generation PDF 100% offline (reseau coupe apres prefetch).
   - Stockage offline en SQLite pour stabiliser les relances.
   - Option cache-only: filtrage defs pendant l'expansion sans reseau.
   - Le solveur n'utilise que les mots presents dans SQLite.
   - Mode sans definitions: generation de grille + sauvegarde SQLite resultat.
   - Objectif: avoir une definition pour chaque entree.

3) Placement libre
   - Aucun mot impose, la grille est entierement libre.

4) Pattern de cases noires
   - Plusieurs variantes de pattern sont essayees (pas + offset).
   - L'objectif est une grille de type "journal": longueurs variees, peu de blocs morts.

5) Solveur incremental (defaut)
   - Remplit la grille ligne par ligne.
   - Ajoute les cases noires en fin de mot.
   - Valide les mots verticaux au fil de l'eau.
   - Tentatives aleatoires pour varier la grille.

5bis) Solveur pattern (legacy)
   - Backtracking avec choix du slot le plus contraint.
   - Recherche par ensembles de cases noires additionnelles.

6) Retries sur definitions
   - Si des mots sans definition apparaissent, on les retire et on relance.
   - Plusieurs passes pour converger vers un resultat definissable.

## Performance et parallelisation
- Expansion Hunspell multithread.
- Variants de pattern en parallele (nouveau).
- Solveur en parallele sur les premiers branchements.
- Limiteurs de connexions HTTP pour Wiktionary.
- Prefetch definitions parallele.

## Observabilite
- Stats defs: hits cache, hits sqlite, hits fallback, requetes reseau, echecs.
- Top mots sans definition pour diagnostiquer les blocages.
- Temps par phase affiche pendant l'execution.

## Cache
Pour accelerer les relances:
- Dictionnaire Hunspell normalise: `cache/hunspell/hunspell.fr_FR.txt`
- Definitions JSON: `cache/definitions.fr.json`
- Definitions SQLite: `cache/definitions.fr.sqlite`

## Variables utiles (principales)
- MOTCROISE_ALLOW_FILLER: active l'expansion Hunspell (si utilisee).
- MOTCROISE_PATTERN_VARIANTS: nombre de variantes de pattern.
- MOTCROISE_REQUIRED_CANDIDATES / MOTCROISE_REQUIRED_COMBOS: taille de recherche pour les mots imposes.
- MOTCROISE_SOLVER_PARALLEL: parallele du solveur.
- MOTCROISE_PATTERN_VARIANT_PARALLEL: parallele des variantes de pattern.
- MOTCROISE_HUNSPELL_*: taille d'expansion Hunspell (si utilisee).
- MOTCROISE_DEF_PASSES: nombre de passes pour filtrer les mots sans definition.
- MOTCROISE_FILTER_DEFS: filtrer les mots sans definition pendant l'expansion.
- MOTCROISE_DEF_CACHE_ONLY_EXPAND: expansion sans reseau (cache only).
- MOTCROISE_PREFETCH_ALL: prefetch defs global (remplit SQLite).
- MOTCROISE_PREFETCH_ONLY: arrete apres prefetch.
- MOTCROISE_IGNORE_DEFS: ignore les definitions (grille seule).
- MOTCROISE_MODE: `grid` (grille seule), `pdf` (PDF depuis SQLite), `all` (par defaut).
- MOTCROISE_SOLVER: `incremental` (defaut) ou `pattern` (ancien solveur).
- MOTCROISE_STOP_ON_FIRST: arrete la recherche des qu'une grille valide est trouvee.
- MOTCROISE_PATTERN_LOGS: logs des patterns/tentatives (0 pour couper).
- MOTCROISE_STEP3_PROGRESS: log global 10/20/.../90% sur la phase 3.
- MOTCROISE_CACHE: active ou non le cache.
- MOTCROISE_POOL_PER_LENGTH: limite de mots par longueur (pool no-theme).
- MOTCROISE_POOL_MAX: limite globale du pool (0 = illimite).
- MOTCROISE_INCREMENTAL_ATTEMPTS: nombre de tentatives aleatoires du solveur incremental.
- MOTCROISE_INCREMENTAL_CANDIDATES: max candidats par slot (solveur incremental).
- MOTCROISE_INCREMENTAL_PARALLEL: parallele pour les tentatives incrementales.
- MOTCROISE_INCREMENTAL_PROGRESS_EVERY: cadence des logs nodes/s (incremental).
- MOTCROISE_INCREMENTAL_MIN_FIRST: longueur minimale du premier mot (defaut 14).
- MOTCROISE_INCREMENTAL_MAX_NODES: plafond nodes par tentative (restart si depasse).
- MOTCROISE_INCREMENTAL_VALIDATE_VERTICALS: valide les mots verticaux (0 = moins de contraintes).
- MOTCROISE_DEFS_DB: chemin SQLite pour les definitions.
- MOTCROISE_RESULT_DB: chemin SQLite pour le resultat de grille.

## Resultat attendu
- Un PDF final avec la grille 20x20.
- Les mots imposes visibles.
- Les autres mots masques mais definis (horizontal + vertical).
- Une grille dense, proche d'un mot croise de journal.

## Mode deux etapes (sans definitions puis definitions)
1) Generation de grille depuis SQLite definitions: `MOTCROISE_MODE=grid`.
2) Projet `MotCroise.Definitions` qui lit le resultat et telecharge les definitions dans SQLite.
3) Generation du PDF offline: `MOTCROISE_MODE=pdf`.

Variables clefs:
- MOTCROISE_RESULT_DB: SQLite de resultat (grille + placements).
- MOTCROISE_DEFS_DB: SQLite definitions (mots avec definition).
- MOTCROISE_JSONL: chemin vers fr-extract.jsonl pour import offline.

## Remarques
- Plus la recherche est large, plus c'est long.
- Si c'est trop lent, reduire PATTERN_VARIANTS, REQUIRED_COMBOS, DEF_PASSES.
- Si ca ne trouve rien, augmenter REQUIRED_CANDIDATES/COMBOS ou HUNSPELL_FILLER.
