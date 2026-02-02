# Cahier des charges - appli React mot croise

## Objectif
Construire une application React.js (cible Android) pour jouer a des mots croises, basee sur des grilles en JSON (format issu du generateur .NET).

## UX / Design (priorite haute)
- Application au top de l'UX: parcours fluide, clair, sans friction.
- Transitions et animations de couleurs elegantes et modernes.
- Nuancier riche et harmonieux (palette de haut niveau, pas de design "basique").

## Optimisations solveur (memo)
- Reduire les listes de candidats par slot (indexation par motif/position + cache).
- Limiter le pool de mots (par longueur) pour eviter des ensembles gigantesques.
- Budget temps et budget noeuds pour eviter les boucles sans fin.
- Logs legers de progression seulement (pas de logs par pattern).
- Preferer un dictionnaire filtre (SQLite + defs), eviter Hunspell brut pour le solveur.

## Donnees (format JSON de grille)
Chaque grille expose :
- id ("001", "002", ...)
- size (10, 15, 20)
- difficulty (facile, moyen, difficile)
- grid: tableau de lignes (strings) avec lettres et # pour cases noires
- clues: listes de definitions horizontales et verticales
  - id / number
  - row, col
  - answer
  - clue (texte)
  - cells (liste des coordonnees de lettres)

## Parcours utilisateur
1) Ecran de selection
- choix taille 5x5 / 10x10 / 15x15 / 20x20
- choix difficulte: facile / moyen / difficile
- bouton continuer

2) Ecran liste des grilles
- grilles numerotees 001, 002, 003...
- grilles terminees: contour vert, non selectionnables
- grilles futures: grisees, non selectionnables
- seule la prochaine grille non terminee est selectionnable (rose/violette)

3) Ecran de jeu
- affichage de la grille
- liste des definitions horizontales/verticales
- selection d'une definition -> saisie du mot
- comportement par difficulte:
  - Facile: si mot incorrect, on conserve les lettres correctement placees
  - Moyen: si mot incorrect, rien n'est place
  - Difficile: lettres placees meme si mot incorrect
- si mot correct: lettres placees en vert + son de succes

4) Fin de grille
- si tous les mots sont trouves: animation vague qui colore les lettres en jaune puis vert
- son de victoire
- lettres vertes verrouillees (non effacables)
- bouton "grille suivante" en bas a droite

## UI / Navigation
- icone engrenage (haut droite): menu pour changer difficulte et activer/couper son
- menu burger (haut gauche): retour, quitter, retour au choix de taille

## Donnees de progression
- stocker localement (localStorage)
- la progression determine la prochaine grille selectionnable

## Mode test (grilles de demo)
- grilles generees en JSON avec lettres "A" partout
- cases noires aleatoires
- definition = "a" pour toutes les entrees

## Integration generator (option)
- Pour la taille 5x5, la grille 001 peut etre importee depuis `cache/motcroise.result.sqlite` (sortie du generateur .NET)
- Les definitions sont chargees avant l'entree en jeu (ecran de chargement)

## Contraintes
- application React.js (web) prevue pour etre empaquetee Android (ex: Capacitor)
- doit compiler et afficher les ecrans et grilles de test
- respect du format de grille defini par le generateur
