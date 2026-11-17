# Verification screenshots, RimWorld 1.7.5011

Taken windowed by the `screenshot-*` scenarios (`scripts/run-scenarios --windowed --save-screenshots DIR -s <name>`) on a 3840x2160 window. Each full image is scaled to 1152 pixels wide; `-details`, `-graph` and `-bar` files are native-resolution crops of the same capture. Each scenario also asserts what the game drew (labels, details text, bar buttons) before the image is kept.

| File | Scenario | What it shows |
|---|---|---|
| `colonist-page-dynamic-layout` (+ `-details`, `-graph`) | screenshot-colonist-page-in-both-layouts | Colonist page, force layout: six colonists, the pinned one selected with its relations and opinion numbers, red, green and blue lines between nodes |
| `colonist-page-circle-layout` | same | The same six colonists on a circle with the same lines |
| `colonist-page-40-colonists` (+ `-graph`) | screenshot-graphs-at-40-colonists-and-30-factions | 40 colonists settled in the graph, every portrait and name readable and inside the graph area |
| `faction-page-30-factions-settled` (+ `-graph`) | same | 31 factions spread over the graph area after the spread fix, each portrait and label separate; the dense red web is the mutual hostility between the factions |
| `faction-page-leaderless-faction` (+ `-details`, `-graph`) | screenshot-leaderless-faction-and-change-leader-button | A faction with no leader: node drawn with its faction icon in the faction color, details read "Prime councilor: Nobody" |
| `faction-page-change-leader-button` (+ `-details`) | same | Colony details with the pencil change-leader button drawn inside the panel |
| `faction-page-change-leader-choice` (+ `-details`) | same | The button clicked: the leader choice lists every colonist, with the "Change leader" tooltip |
| `bottom-bar-relations-replaces-factions` (+ `-bar`) | screenshot-bottom-bar-with-and-without-the-vanilla-factions-tab | Default bar: the Relations icon sits in the Factions slot, between History and Menu |
| `bottom-bar-relations-beside-factions` (+ `-bar`) | same | With the keep-vanilla setting on: the vanilla Factions icon and the Relations icon both in the bar |
| `colonist-page-german` (+ `-details`, `-graph`) | screenshot-german-tab | Colonist page with German text: "Kolonisten", "Beziehungen", "Interaktionen", "Aktuelle Auswirkungen auf den sozialen Status" |
| `faction-page-german` (+ `-graph`) | same | Factions page in German: the page button reads "Fraktionen" and no raw key is drawn |
| `01-mod-list-incompatibility-warning` | screenshot-mod-list-warns-when-fluffys-relations-tab-is-also-enabled | Mod list with Fluffy's Relations Tab enabled: this mod shows "Incompatible with: Relations Tab" |
