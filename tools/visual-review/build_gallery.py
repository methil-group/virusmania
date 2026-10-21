"""Build the offline review from the original Unity PNGs. No image transformations."""
from pathlib import Path
import hashlib
import html
import json
import struct

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "doc/gameplay/visual-review"

# id, section, title, observation, capture preparation
SCREENS = [
    ("00-splash", "Menus", "Splash animé", "Identité visuelle immédiate ; capture d'une image de l'animation.", "Scène SplashScreen, animation en cours."),
    ("01-menu", "Menus", "Menu principal", "Hiérarchie simple : Play, Options, Quit.", "Scène MainMenu."),
    ("02-choix-tutoriel", "Menus", "Choix du tutoriel", "Le choix est clair. Les libellés Yes / Skip seraient plus précis que OK / No.", "Ouverture du panneau par sa méthode publique."),
    ("03-options-audio", "Menus", "Réglages audio", "Trois volumes distincts. Ajouter une valeur numérique aiderait au réglage fin.", "Options, onglet audio ; rendu 1920×1080 exporté en 1280×720."),
    ("04-options-commandes", "Menus", "Commandes", "Touches ZQSD et interaction visibles ; l'appui long pourrait être expliqué ici.", "Options, onglet clavier ; rendu 1920×1080 exporté en 1280×720."),
    ("06-reaffectation", "Menus", "Réaffectation d'une touche", "État d'écoute du clavier capturé puis annulé ; aucune touche sauvegardée.", "Bouton de réaffectation activé par script, puis annulation."),
    ("05-chargement", "Menus", "Transition / chargement", "Transition identifiable, cohérente avec le vert et les rayures du menu.", "Vraie transition MainMenu → Game ; capture pendant le fondu."),
    ("60-tutoriel-recettes", "Tutoriel", "1 · Consulter la bibliothèque", "Titre et paragraphe séparés après correction de la mise en page.", "Entrée normale dans OnBoarding."),
    ("61-tutoriel-bibliotheque", "Tutoriel", "Recettes du tutoriel", "Une fusion et une cuisson : bon périmètre d'apprentissage.", "Ouverture de la bibliothèque du tutoriel."),
    ("62-tutoriel-achats", "Tutoriel", "2 · Acheter les microbes", "Le sol de prototype diffère fortement du laboratoire final.", "Étape préparée : index du tutoriel réglé par script, postes activés."),
    ("63-tutoriel-cuisson", "Tutoriel", "3 · Cuire le Nanoclostre", "Action concrète et courte ; un indicateur de durée serait utile.", "Étape préparée par script, sans achat manuel préalable."),
    ("64-tutoriel-melange", "Tutoriel", "4 · Fusionner", "Le texte tient ; il reste long à lire. Une suite de trois petites consignes serait plus légère.", "Étape préparée par script, sans fabrication manuelle préalable."),
    ("65-tutoriel-envoi", "Tutoriel", "5 · Expédier", "Uniformiser les noms des postes : Merge Space, treadmill, bain-marie.", "Étape préparée par script."),
    ("66-tutoriel-recuperation", "Tutoriel", "Récupération après erreur", "Le remplacement gratuit évite de bloquer le joueur qui expérimente.", "RecoverLostItem appelé ; échantillon ajouté au chariot."),
    ("40-brief-poliovirus", "Mission", "Brief · Gut & Neuro", "Le titre, l'histoire et la récompense disposent chacun de leur espace.", "Premier brief de Game, avant acceptation."),
    ("41-brief-herpes", "Mission", "Brief · Latency Threat", "La description courte et l'illustration restent lisibles.", "Deuxième brief affiché par SetupNewBriefShow ; progression non jouée."),
    ("42-brief-influenza", "Mission", "Brief · Mutate & Bones", "Vérification du texte plus long et du troisième virus.", "Troisième brief affiché par SetupNewBriefShow ; progression non jouée."),
    ("43-laboratoire", "Mission", "Laboratoire et HUD", "Brief courant, temps et argent sont séparés. Le rappel du brief reste assez dense.", "Brief accepté ; capture avant arrêt du chrono pour l'inspection suivante."),
    ("53-objet-porte", "Fabrication", "Porter un ingrédient", "Le nom et l'icône de l'objet porté permettent de suivre ce que l'on manipule.", "Ingrédient injecté via GiveItem, sans achat manuel."),
    ("54-cuisson", "Fabrication", "Cuisson en cours", "Animation et vapeur visibles ; la progression est peu explicite à distance.", "Entrée valide ajoutée au bain-marie ; capture après une seconde."),
    ("55-melange", "Fabrication", "Fusion et appui long", "La jauge et les aides d'interaction matérialisent l'action maintenue.", "Deux ingrédients valides ajoutés, joueur déplacé près du poste, maintien simulé par appels par frame."),
    ("57-expedition-refusee", "Fabrication", "Mauvais échantillon envoyé", "Le voyant rouge signale le refus. Une raison écrite aiderait à corriger l'erreur.", "Mauvais ingrédient ajouté puis envoyé par le vrai SendItem ; capture pendant l'alarme."),
    ("56-expedition-validee", "Fabrication", "Bon échantillon envoyé", "Le voyant vert signale la réussite ; argent et brief suivant sont ensuite déclenchés.", "Virus demandé injecté puis envoyé par SendItem ; fabrication préalable non jouée."),
    ("45-boutique", "Fenêtres", "Boutique · panier vide", "Les cinq ingrédients sont visibles. Le timer continue pendant les achats.", "Fenêtre ouverte par API ; chrono arrêté pour l'inspection prolongée."),
    ("46-panier", "Fenêtres", "Boutique · panier rempli", "Total 28 $ pour 20 $ disponibles ; il manque une indication de dépassement avant l'achat.", "Une unité de chaque ingrédient ajoutée via les vrais boutons / méthodes."),
    ("47-fonds-insuffisants", "Fenêtres", "Alerte de budget", "Message visible, mais bref ; garder l'alerte près du total aiderait à comprendre.", "TriggerNoMoney déclenché pour capturer cet état transitoire."),
    ("48-recettes-debut", "Fenêtres", "Recettes · haut", "Première recette entière, en-tête séparé de la liste et consigne de défilement en bas.", "Bibliothèque complète ; retour au début après l'animation d'ouverture."),
    ("49-recettes-milieu", "Fenêtres", "Recettes · milieu", "Les ingrédients n'ont pas de nom dans la formule ; la reconnaissance repose sur l'icône.", "ScrollRect positionné à 50 % par script."),
    ("50-recettes-cuisson", "Fenêtres", "Recettes · cuissons", "Les trois transformations de cuisson sont accessibles en bas de liste.", "ScrollRect positionné en bas par script."),
    ("44-analyse", "Fenêtres", "Microscope / analyse", "Bon virus, notes de l'échantillon, statut de la commande et absence de mesures explicitement signalée. Présentation harmonisée avec les autres fenêtres.", "ShowAnalysis reçoit Influenza A, qui correspond à la commande active ; aucune mesure de menace inventée."),
    ("73-analyse-poliovirus", "Fenêtres", "Analyse · changement vers Poliovirus", "L'illustration et les notes changent ensemble. L'analyse signale que ce virus diffère de la commande Influenza A.", "Changement d'échantillon dans le même panneau, sans fermer ni recréer l'interface."),
    ("74-analyse-herpes", "Fenêtres", "Analyse · changement vers Herpes", "Troisième virus, troisième illustration : aucun visuel du sample précédent conservé.", "Changement d'échantillon dans le même panneau ; commande active inchangée."),
    ("51-pause", "Fenêtres", "Pause", "Reprendre, options et quitter. La pause fige effectivement le temps.", "PauseMenu.OpenPanel appelé."),
    ("52-pause-options", "Fenêtres", "Réglages pendant la pause", "Commandes et sons regroupés. Cette présentation diffère des onglets du menu principal.", "Sous-panneau des réglages activé pendant la pause."),
    ("58-victoire", "Fins", "Victoire", "Un bilan de partie et un bouton Rejouer donneraient envie de recommencer.", "Écran final ouvert directement après acceptation d'un brief ; les trois missions n'ont pas été jouées pour cette capture."),
    ("59-defaite", "Fins", "Défaite", "Le ton est taquin, mais le message ne dit ni pourquoi on a perdu ni comment progresser.", "Écran final ouvert directement ; expiration du chrono couverte par la logique, non attendue ici."),
    ("70-brief-1080p", "Formats", "Brief · 1920×1080", "Texte et boutons distincts au format 16:9 natif.", "Vraie résolution du Game View réglée à 1920×1080."),
    ("71-brief-4-3", "Formats", "Brief · 1024×768", "Le brief reste dans le cadre en 4:3. Le laboratoire est davantage recadré.", "Vraie résolution du Game View réglée à 1024×768, sans étirement de l'image."),
    ("72-menu-4-3", "Formats", "Menu · 1024×768", "Colonne du menu adaptée à la largeur : logo et boutons restent visibles en 4:3.", "Résolution native 1024×768."),
    ("75-analyse-1080p", "Formats", "Analyse · 1920×1080", "Contrôle de la nouvelle mise en page à sa résolution native.", "Game View réellement réglé en 1920×1080 ; Influenza A analysé."),
    ("76-analyse-4-3", "Formats", "Analyse · 1024×768", "Les deux colonnes, le titre et la sortie restent dans le cadre en 4:3.", "Game View réellement réglé en 1024×768, sans étirement."),
    ("77-boutique-1080p", "Formats", "Boutique · 1920×1080", "En-tête commun et bouton Exit standard ; le panier reste accessible.", "Game View natif 1920×1080, boutique ouverte sans achat."),
    ("78-boutique-4-3", "Formats", "Boutique · 1024×768", "Titre, sortie, produits et panier contrôlés en 4:3.", "Game View natif 1024×768, sans redimensionnement de l'image."),
    ("79-recettes-1080p", "Formats", "Recettes · 1920×1080", "Le titre reste au-dessus du contenu ; la liste commence par la première recette.", "Game View natif 1920×1080 ; ouverture normale de la bibliothèque."),
    ("80-recettes-4-3", "Formats", "Recettes · 1024×768", "En-tête fixe, liste masquée dans sa propre zone et indication de défilement.", "Game View natif 1024×768, sans redimensionnement de l'image."),
    ("81-menu-1080p", "Formats", "Menu · 1920×1080", "Logo et actions dans la même colonne au format 16:9 natif.", "Game View natif 1920×1080 ; animation du logo en cours."),
    ("10-brief-avant", "Avant", "Avant · brief superposé", "État reproduisant le chevauchement visible dans la capture fournie.", "Avant modification des groupes de mise en page."),
    ("16-analyse", "Avant", "Avant · titres de l'analyse", "Deux titres se superposaient ; comparer avec la nouvelle capture de l'analyse.", "Avant modification du panneau d'analyse."),
    ("17-analyse-intermediaire", "Avant", "Avant · mauvaise identification", "Influenza A affichait l'image du Poliovirus et l'absence de données n'était pas expliquée.", "Version avant correction de l'illustration et reconstruction du panneau."),
    ("21-tutoriel-recettes", "Avant", "Avant · consigne tronquée", "Le paragraphe dépassait le panneau et recouvrait le titre Hint.", "Avant correction du panneau du tutoriel."),
]


def make_diagram():
    parts = ['''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1400 1210" role="img" aria-labelledby="title desc">
<title id="title">VirusMania — navigation et boucle de gameplay</title><desc id="desc">Choisir le tutoriel, accepter un brief, consulter la recette, acheter, récupérer, cuire si nécessaire, fusionner, analyser si souhaité et envoyer. Une réussite récompense et mène au brief suivant ou à la victoire. Un mauvais envoi oblige à refabriquer ; le temps écoulé mène à la défaite.</desc>
<defs><marker id="arrow" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="8" markerHeight="8" orient="auto-start-reverse"><path d="M0 0L10 5L0 10Z" fill="#34423c"/></marker></defs>
<style>text{font-family:Arial,sans-serif;fill:#18251e}.title{font-weight:700;font-size:30px}.label{font-weight:700;font-size:21px}.sub{font-size:16px;fill:#485b50}.small{font-size:15px;fill:#485b50}.edge{fill:none;stroke:#34423c;stroke-width:2.5;marker-end:url(#arrow)}.dash{stroke-dasharray:7 6}</style>
<rect width="1400" height="1210" rx="24" fill="#f4f7f1"/><text class="title" x="54" y="56">VIRUSMANIA / BOUCLE DE GAMEPLAY</text><text class="sub" x="54" y="88">État observé sur dev · 20 octobre 2026 · les étapes facultatives sont indiquées</text>
<rect x="42" y="405" width="1314" height="582" rx="22" fill="#e9f0e5" stroke="#cddbc8"/>
<text class="label" x="54" y="136">ENTRER DANS LE JEU</text>''']

    def node(x, y, w, title, lines, fill="#fff", h=94):
        parts.append(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="15" fill="{fill}" stroke="#bac9bd" stroke-width="1.5"/>')
        parts.append(f'<text class="label" x="{x+20}" y="{y+33}">{html.escape(title)}</text>')
        for i, line in enumerate(lines):
            parts.append(f'<text class="sub" x="{x+20}" y="{y+59+i*21}">{html.escape(line)}</text>')

    def arrow(path, dashed=False):
        parts.append(f'<path class="edge {"dash" if dashed else ""}" d="{path}"/>')

    node(54, 160, 250, "Splash → Menu", ["Play · Options · Quit"])
    node(394, 160, 260, "Choisir son départ", ["Avec ou sans tutoriel"])
    node(744, 160, 600, "Tutoriel facultatif · 5 étapes", ["Recettes → Acheter → Cuire → Fusionner → Expédier", "Une erreur récupérable rend l'échantillon au chariot."], "#e4f8d9", 114)
    arrow("M304 207 H384")
    arrow("M654 207 H734")
    arrow("M524 254 V335 H164 V485")
    parts.append('<text class="small" x="322" y="324">Passer le tutoriel</text>')
    arrow("M1044 274 V370 H164 V485")
    parts.append('<text class="small" x="756" y="359">Fin du tutoriel → partie normale</text>')
    parts.append('<text class="label" x="66" y="449">UNE MISSION · 120 SECONDES APRÈS ACCEPTATION</text>')
    node(66, 495, 230, "1 · Accepter", ["Brief · cible · gain"], "#c7ffad")
    node(406, 495, 230, "2 · Recette", ["Consulter la formule"])
    node(746, 495, 230, "3 · Acheter", ["Dépenser son budget"])
    node(1086, 495, 230, "4 · Récupérer", ["Livraison au chariot"])
    arrow("M296 542 H396")
    arrow("M636 542 H736")
    arrow("M976 542 H1076")
    node(1086, 690, 230, "5 · Cuire si besoin", ["Bain-marie · 8 s"])
    node(746, 690, 230, "6 · Fusionner", ["Maintenir · 5 s"])
    node(406, 690, 230, "7 · Analyser", ["Facultatif · 2 s"], "#edf0ff")
    node(66, 690, 230, "8 · Envoyer", ["Échantillon + levier"])
    arrow("M1201 589 V680")
    arrow("M1086 737 H986")
    arrow("M746 737 H646")
    arrow("M406 737 H306")
    node(66, 876, 230, "Vérifier la cible", ["Voyant vert ou rouge"])
    node(406, 876, 230, "Bon échantillon", ["Gain : 35 / 50 / 80 $"], "#c7ffad")
    node(746, 876, 230, "Brief suivant", ["S'il reste une mission"], "#c7ffad")
    node(1086, 876, 230, "Victoire", ["Après les 3 briefs"], "#c7ffad")
    arrow("M181 784 V866")
    arrow("M296 923 H396")
    arrow("M636 923 H736")
    arrow("M521 970 V1000 H1201 V980")
    parts.append('<text class="small" x="832" y="1022">Dernier brief réussi</text>')
    arrow("M861 876 V839 H24 V542 H56", True)
    parts.append('<text class="small" x="318" y="826">Nouvelle commande : même boucle</text>')
    node(66, 1060, 340, "Mauvais envoi", ["Échantillon perdu ; refabriquer.", "Le chrono continue."], "#ffe3d9", 106)
    arrow("M181 970 V1050")
    node(466, 1060, 390, "Pause / Échap", ["Fige le temps. Ferme d'abord", "la fenêtre ouverte avant de pauser."], "#edf0ff", 106)
    node(916, 1060, 400, "Chrono écoulé → Défaite", ["Le temps continue dans les fenêtres", "boutique, recettes et analyse."], "#ffe3d9", 106)
    parts.append('</svg>')
    return "\n".join(parts)


def main():
    captures = []
    for ident, group, title, note, preparation in SCREENS:
        path = OUT / "captures" / f"{ident}.png"
        data = path.read_bytes()
        width, height = struct.unpack(">II", data[16:24])
        digest = hashlib.sha256(data).hexdigest()
        captures.append(dict(id=ident, group=group, title=title, note=note, preparation=preparation,
                             file=f"captures/{ident}.png", width=width, height=height,
                             url=f"captures/{ident}.png?v={digest[:12]}", bytes=len(data), sha256=digest))
    (OUT / "captures.json").write_text(json.dumps(dict(date="2026-10-20", branch="dev", unity="6000.6.6f1", captures=captures), ensure_ascii=False, indent=2), encoding="utf-8")
    (OUT / "boucle-gameplay.svg").write_text(make_diagram(), encoding="utf-8")
    verification = json.loads((OUT / "verification.json").read_text(encoding="utf-8")) if (OUT / "verification.json").exists() else {"summary": "Vérification automatisée en cours"}
    template = '''<!doctype html><html lang="fr"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>VirusMania — revue visuelle</title>
<style>*{box-sizing:border-box}html{scroll-behavior:smooth}body{margin:0;background:#f6f7f2;color:#17251e;font:16px/1.55 system-ui,sans-serif}a{color:#24652e}header,main,footer{max-width:1360px;margin:auto;padding:24px 32px}header{display:flex;justify-content:space-between;border-bottom:1px solid #d8dfd3}header a{margin-left:24px;text-decoration:none}h1{font-size:clamp(34px,5vw,64px);line-height:1.1;letter-spacing:-2px;margin:14px 0 20px;max-width:920px}h2{font-size:30px;margin:0 0 12px}h3{font-size:19px;margin:0 0 8px}p{margin:0 0 14px}.eyebrow{font-size:13px;letter-spacing:1.6px;font-weight:750;text-transform:uppercase;color:#526154}.lead{max-width:870px;font-size:19px;color:#46564b}.hero{padding:36px 0}.tag{display:inline-block;border:1px solid #bdc9b6;border-radius:30px;padding:4px 12px;font-size:13px;background:#fff}.good{background:#d2f9bd}.warn{background:#ffe4c7}.stats{display:flex;gap:36px;margin:26px 0}.stats strong{display:block;font-size:30px;line-height:1.2}.stats span{font-size:13px;color:#58685b}.summary{display:grid;grid-template-columns:1fr 1fr;gap:18px}.box{background:#fff;border:1px solid #d7dfd1;border-radius:16px;padding:24px}.box ul{padding-left:20px;margin:12px 0 0}.box li{margin:8px 0}section{margin:48px 0}.diagram{background:#fff;border-radius:20px;border:1px solid #d7dfd1;padding:12px}.diagram img{display:block;width:100%;height:auto}.tools{display:flex;gap:10px;flex-wrap:wrap;margin:20px 0}button,input{font:inherit;border:1px solid #bccab5;border-radius:10px;padding:9px 15px;background:#fff;color:#17251e}button{cursor:pointer}button:hover,button:focus-visible{border-color:#28652e;outline:2px solid #9df27a}button.active{background:#c7ffad;border-color:#297b2e}input{margin-left:auto;min-width:220px}.grid{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:20px}.card{background:#fff;border:1px solid #d7dfd1;border-radius:14px;overflow:hidden}.image{border:0;border-radius:0;background:#101311;display:block;padding:0;width:100%;aspect-ratio:16/9}.image img{display:block;width:100%;height:100%;object-fit:contain}.content{padding:18px}.content p{font-size:14px;color:#46564b}.content .tag{font-size:11px;margin-bottom:10px}.meta{font-size:12px;color:#697566}.count{font-size:14px;color:#526154}details{font-size:13px;color:#526154;margin-top:14px}summary{cursor:pointer}.notes{display:grid;grid-template-columns:repeat(3,1fr);gap:18px}.notes b{display:block;margin-bottom:8px}dialog{border:1px solid #60745b;border-radius:16px;padding:16px;background:#151b17;color:white;width:min(1500px,97vw);max-height:95vh}dialog::backdrop{background:#000d}dialog img{display:block;width:100%;max-height:74vh;object-fit:contain}dialog .bar{display:flex;align-items:center;gap:12px;margin-top:12px}dialog p{margin:0;flex:1}dialog small{color:#d0dbc9}footer{font-size:13px;color:#5b695e;border-top:1px solid #d8dfd3}code{background:#e8eee3;padding:2px 5px;border-radius:4px}@media(max-width:950px){.grid,.notes{grid-template-columns:repeat(2,1fr)}header{display:block}header a{margin:0 18px 0 0}.summary{grid-template-columns:1fr}}@media(max-width:600px){header,main,footer{padding:20px}.grid,.notes{grid-template-columns:1fr}.stats{gap:24px}input{width:100%;margin:0}dialog .bar{flex-wrap:wrap}dialog p{min-width:100%}}@media print{header nav,.tools,dialog{display:none}.grid{grid-template-columns:repeat(2,1fr)}.card{break-inside:avoid}.hero{padding:0}section{margin:24px 0}}</style>
<header><b>VIRUSMANIA <span class="tag good">dev</span></b><nav><a href="#boucle">Boucle de gameplay</a><a href="#galerie">Captures</a><a href="#suite">À améliorer</a></nav></header><main>
<div class="hero"><div class="eyebrow">Revue visuelle · 20 octobre 2026</div><h1>Le labo, écran par écran.</h1><p class="lead">Un parcours documenté du menu aux fins de partie, avec les étapes de fabrication, les fenêtres et les comparaisons avant/après. L'objectif : garder le côté cartoon et rendre les actions plus lisibles.</p><span class="tag warn">Encore des points à peaufiner</span><div class="stats"><div><strong>4</strong><span>scènes parcourues</span></div><div><strong>__COUNT__</strong><span>captures classées</span></div><div><strong>3</strong><span>résolutions vérifiées</span></div></div><p class="meta">Unity 6000.6.6f1 · branche dev · captures du Game View avec UI · __TESTS__</p></div>
<div class="summary"><div class="box"><h3>Ce qui fonctionne bien</h3><ul><li>Une identité cartoon reconnaissable : contours, typographie, objets et boutons.</li><li>Une boucle concrète : commander, manipuler, transformer, expédier.</li><li>Un tutoriel qui introduit les postes progressivement et permet de récupérer après une erreur.</li></ul></div><div class="box"><h3>Corrigé pendant la revue</h3><ul><li>Brief : titre et description séparés, hauteur adaptée au contenu.</li><li>Tutoriel : paragraphes contenus dans le panneau, sans recouvrir Hint.</li><li>Analyse : bon virus, notes, statut de la commande et état sans mesures ; panneau reconstruit et sortie harmonisée.</li><li>Contours : initialisation du masque avant son utilisation par les renderers.</li></ul></div></div>
<section id="boucle"><div class="eyebrow">01 / Parcours</div><h2>La boucle de gameplay</h2><p>Les durées viennent des valeurs du jeu : 120 s par brief, cuisson de 8 s, fusion maintenue de 5 s, analyse maintenue de 2 s. L'analyse est facultative. Les achats et les fenêtres consomment du temps ; la pause le fige.</p><div class="diagram"><a href="boucle-gameplay.svg" target="_blank"><img src="boucle-gameplay.svg" alt="Schéma de la navigation et de la boucle de gameplay"></a></div><p class="meta"><a href="boucle-gameplay.svg">Ouvrir le SVG</a> · <a href="boucle-gameplay.mmd">Source Mermaid modifiable</a> · <a href="captures.json">Inventaire des captures et empreintes SHA-256</a></p></section>
<section id="galerie"><div class="eyebrow">02 / Galerie</div><h2>Les écrans et leurs états</h2><p>Cliquer une capture pour l'agrandir. Les flèches du clavier parcourent les images ; Échap ferme l'aperçu. Le filtre « Avant » conserve les défauts de départ.</p><div class="box"><b>Portée de la vérification</b><p style="margin-top:8px">Les images viennent de l'Éditeur Unity. Les interfaces sont pilotées par leurs méthodes et les étapes de tutoriel sont préparées par script. Des ingrédients ont été injectés pour observer les postes, le chrono a été arrêté pendant une partie de l'inspection, et les écrans de fin ont été ouverts directement. Ces captures ne constituent donc pas une partie complète jouée au clavier ou à la manette.</p><p class="meta">Les captures en 4:3 viennent d'une vraie modification du Game View, sans étirement. Le détail de préparation est indiqué sous chaque image. Le build Windows de la passe précédente n'a pas été remplacé par cette revue.</p></div><div class="tools" id="filters"></div><p class="count" id="count"></p><div class="grid" id="grid"></div><noscript>Activer JavaScript pour filtrer la galerie, ou ouvrir captures.json et le dossier captures.</noscript></section>
<section id="suite"><div class="eyebrow">03 / Prochaines améliorations</div><h2>Garder le fun, enlever les frictions</h2><div class="notes"><div class="box"><b>1 · Définir le rôle des menaces</b>Le microscope identifie maintenant correctement le sample et sa correspondance avec la commande. Les profils de menace restent à définir dans les données et le game design.</div><div class="box"><b>2 · Aider à trouver la recette</b>Surligner la commande courante, nommer les ingrédients et indiquer le défilement. Le défi doit rester la fabrication.</div><div class="box"><b>3 · Clarifier les erreurs</b>Un mauvais envoi mérite une raison courte et un indice. À la défaite, proposer Rejouer et un bilan encourageant.</div><div class="box"><b>4 · Donner du retour aux postes</b>Afficher la durée ou la progression de cuisson et mieux distinguer cuisson, prêt à prendre et fusion en cours.</div><div class="box"><b>5 · Harmoniser le tutoriel</b>Remplacer le sol de prototype, raccourcir les consignes et employer les mêmes noms pour les postes partout.</div><div class="box"><b>6 · Finir les formats et réglages</b>Le logo du menu déborde en 4:3. Harmoniser les options entre menu et pause, expliquer l'appui long et rendre le coût du panier plus explicite.</div></div></section>
</main><footer>Sources : scènes et données du repository local, captures Unity du 20/10/2026. Aucun changement d'équilibrage ni ajout de données de menace dans cette revue. <a href="rapport.adoc">Compte rendu technique</a>.</footer>
<dialog id="viewer" aria-label="Capture agrandie"><img id="large" alt=""><div class="bar"><button id="prev" aria-label="Capture précédente">←</button><button id="next" aria-label="Capture suivante">→</button><p><b id="caption"></b><br><small id="detail"></small></p><button id="close">Fermer ×</button></div></dialog>
<script>const screens=__DATA__;let group='Tous',query='',visible=[],current=0;const grid=document.getElementById('grid'),filters=document.getElementById('filters'),viewer=document.getElementById('viewer');const groups=['Tous',...new Set(screens.map(x=>x.group))];for(const name of groups){const b=document.createElement('button');b.textContent=name;b.dataset.group=name;b.addEventListener('click',()=>{group=name;render()});filters.append(b)}const search=document.createElement('input');search.type='search';search.placeholder='Chercher un écran…';search.setAttribute('aria-label','Chercher un écran');search.addEventListener('input',()=>{query=search.value.toLocaleLowerCase('fr');render()});filters.append(search);function render(){visible=screens.filter(x=>(group==='Tous'||x.group===group)&&(x.title+' '+x.note).toLocaleLowerCase('fr').includes(query));for(const b of filters.querySelectorAll('button')){b.classList.toggle('active',b.dataset.group===group);b.setAttribute('aria-pressed',b.dataset.group===group)}document.getElementById('count').textContent=visible.length+' captures affichées sur '+screens.length;grid.replaceChildren();visible.forEach((x,i)=>{const article=document.createElement('article');article.className='card';const button=document.createElement('button');button.className='image';button.setAttribute('aria-label','Agrandir : '+x.title);const img=document.createElement('img');img.src=x.file;img.alt=x.title;img.loading='lazy';button.append(img);button.addEventListener('click',()=>open(i));article.append(button);const box=document.createElement('div');box.className='content';const tag=document.createElement('span');tag.className='tag'+(x.group==='Avant'?' warn':'');tag.textContent=x.group;const h=document.createElement('h3');h.textContent=x.title;const p=document.createElement('p');p.textContent=x.note;const meta=document.createElement('div');meta.className='meta';meta.textContent=x.width+' × '+x.height+' · '+x.id;const details=document.createElement('details');const summary=document.createElement('summary');summary.textContent='Comment cet état a été obtenu';const context=document.createElement('p');context.textContent=x.preparation;details.append(summary,context);box.append(tag,h,p,meta,details);article.append(box);grid.append(article)})}function show(){const x=visible[current];document.getElementById('large').src=x.file;document.getElementById('large').alt=x.title;document.getElementById('caption').textContent=x.title;document.getElementById('detail').textContent=(current+1)+' / '+visible.length+' · '+x.note}function open(i){current=i;show();viewer.showModal()}function move(n){if(!visible.length)return;current=(current+n+visible.length)%visible.length;show()}document.getElementById('prev').onclick=()=>move(-1);document.getElementById('next').onclick=()=>move(1);document.getElementById('close').onclick=()=>viewer.close();viewer.addEventListener('keydown',e=>{if(e.key==='ArrowLeft')move(-1);if(e.key==='ArrowRight')move(1)});viewer.addEventListener('click',e=>{if(e.target===viewer)viewer.close()});render();</script></html>'''
    motion = '''<section id="analyse-animee"><div class="eyebrow">Microscope / présentation revue</div><h2>L'analyse en mouvement</h2><p>Capture réelle du Game View : ouverture, survol de la sortie, changement de virus et fermeture. Titre aligné sur la colonne du contenu, bouton Exit standard partagé avec la boutique et les recettes, vert du brief et contours ondulés. Le texte reste stable après son apparition.</p><video controls playsinline preload="metadata" poster="captures/44-analyse.png" style="display:block;width:100%;border-radius:16px;background:#151515"><source src="analysis-motion.mp4" type="video/mp4"></video><p class="meta">6,2 secondes · 1280 × 720 · états préparés par script, chrono arrêté. Vidéo encodée à 25 i/s depuis environ 22 captures/s ; elle ne mesure pas les performances du jeu. <a href="analysis-motion.mp4">Ouvrir la vidéo</a>.</p></section>'''
    template = template.replace('<section id="boucle">', motion + '<section id="boucle">')
    template = template.replace('panneau reconstruit et sortie harmonisée.', 'titre aligné et bouton Exit standard partagé avec la boutique et les recettes.')
    template = template.replace('Le logo du menu déborde en 4:3. Harmoniser les options entre menu et pause, expliquer l\'appui long et rendre le coût du panier plus explicite.', 'Le menu tient désormais en 4:3. Harmoniser les options entre menu et pause, expliquer l\'appui long et rendre le coût du panier plus explicite.')
    template = template.replace("img.src=x.file", "img.src=x.url||x.file").replace(".src=x.file", ".src=x.url||x.file")
    poster = next(x for x in captures if x['id'] == '44-analyse')['url']
    video_digest = hashlib.sha256((OUT / 'analysis-motion-petri.mp4').read_bytes()).hexdigest()[:12]
    template = template.replace('poster="captures/44-analyse.png"', f'poster="{poster}"')
    template = template.replace('src="analysis-motion.mp4"', f'src="analysis-motion.mp4?v={video_digest}"')
    template = template.replace('href="analysis-motion.mp4"', f'href="analysis-motion.mp4?v={video_digest}"')
    template = template.replace('Revue visuelle · 20 octobre 2026', 'Revue visuelle · 20 octobre 2026 · titre, sortie et voile noir')
    template = template.replace('Titre aligné sur la colonne du contenu, bouton Exit standard partagé avec la boutique et les recettes, vert du brief et contours ondulés.', 'Titre ANALYSIS incliné de trois degrés et rapproché du bord du panneau, marge gauche de 112 unités, bouton Exit de la boutique et voile noir à 60 % derrière la fenêtre.')
    template = template.replace('analysis-motion.mp4', 'analysis-motion-petri.mp4')
    template = template.replace('voile noir à 60 % derrière la fenêtre.', 'voile noir à 60 % derrière la fenêtre. La boîte de Petri garde son outline d’origine et son remplissage gris ne laisse plus passer le fond vert.')
    page = template.replace("__COUNT__", str(len(captures))).replace("__DATA__", json.dumps(captures, ensure_ascii=False).replace("<", "\\u003c")).replace("__TESTS__", html.escape(verification["summary"]))
    (OUT / "index.html").write_text(page, encoding="utf-8")
    (OUT / "revision-voile-noir.html").write_text(page, encoding="utf-8")
    (OUT / "revision-petri.html").write_text(page, encoding="utf-8")
    (OUT / "revision-titre-incline.html").write_text(page, encoding="utf-8")
    print(json.dumps({"captures": len(captures), "bytes": sum(x["bytes"] for x in captures), "gallery": str(OUT / "index.html")}, ensure_ascii=False))


if __name__ == "__main__":
    main()

