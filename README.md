# LucasJobert\_GameAndWatch

Jobert Lucas – Game \& Watch
One Page Document
Pitch
Vous êtes un scientifique zombie chargé de mener une opération d’infection sur le dernier patient humain immunisé, directement à l’intérieur de son corps.
Pour cela, vous devez contrôler votre virus à travers 3 étapes afin de vaincre son immunité.

1ère étape : Le cœur
Type : Game \& Watch
But : Atteindre le cœur pour marquer des points en évitant les globules blancs à chaque battement.

2ème étape : Les vaisseaux sanguins
Type : Shoot ’em up
Mécanique :
Récolter l’énergie laissée par les ennemis éliminés afin de charger un puissant laser capable d’anéantir plusieurs ennemis en même temps.

3ème étape : Le cerveau
Pas encore défini.

Jeu bonus (si possible)
Incarner l’humain devenu zombie une fois l’infection réussie.

État actuel du projet
•	Menu principal fonctionnel pour lancer le Game \& Watch et choisir la difficulté.
•	D’autres menus sont prévus (scoreboard, etc.).
•	Ajout de feedbacks visuels prévu pour apporter plus de “juice”.
Game \& Watch
•	Fonctionnel
•	3 modes de difficulté
•	Tutoriel prévu au début de la partie pour expliquer le fonctionnement

Contenu majeur manquant
•	Pas de système de sauvegarde
•	Pas de son

Note personnelle
Je me suis lancé le défi de réaliser l’intégralité des sprites moi-même.
Cela m’a malheureusement coûté du temps, ce qui a entraîné un manque de finition sur certains éléments du projet.



NOTE README RENDU FINAL :



Le Shoot em up prévu a été passé en 3ème jeu dans la zone du cerveau.

Mécanique simple : appuyer pour tirer et se déplacer. Lorsque l'on tue des ennemis, ils drop des globule que l'on attire vers soi automatiquement. Ces globules chargent le joueur en énergie. Une fois suffisamment chargé, si le joueur relâche son doigt, le personnage tire un gros laser touchant tout les ennemis en dégâts de zone.

Run infini, système de phase scriptées qui tournent en boucle en amplifiant des variables de difficulté tel que le nombre d'ennemi, leur vitesse de déplacement ou de shoot.



Le mini jeu numéro 2 se déroule dans les vaisseaux sanguin.



C'est un genre de runner en scroll horizontal.

La mécanique est encore une fois simple : le virus reste fixe sur un seul axe. des projectiles peuvent arriver du centre, du haut, ou du bas. lorsque le joueur effectue un "tap", le virus se scinde en deux partie. L'une allant en haut l'autre en bas. 

Le but est donc d'éviter les projectiles qui arrivent en jouant sur cette mécanique.



Structure en pattern de projectile. l'aléatoire est présent mais maitrisé dans sa structure avec des chances plus ou moins élevées de spawn selon la difficulté du pattern.



Entre temps les sons et musiques ont été ajoutées.



Il manque un système de leaderboard ainsi qu'un menu settings pour régler les sons.



Mes prévision d'améliorations si j'en ai envie par la suite seraient d'ajouter un collectible général dans les mini jeu qui formerait comme une monnaie pour acheter des skins. Et apporter d'autres petits éléments de lore avec le zombie du début par exemple

