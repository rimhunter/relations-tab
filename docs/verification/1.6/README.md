# Verification screenshots, RimWorld 1.6.4871

Taken windowed on 1.6 by the `*-without-clicks` scenarios (`OnlyOn = "1.6"`) and the mod-list scenario, on a 3840x2160 window. The runner's click helper exists only on 1.7, so these open the Relations tab with `MainTabsRoot.SetCurrentTab` and set the page, graph mode, selected colonist or faction through the tab's own properties. The same file names as `docs/verification/1.7/`. Full images are 1152 pixels wide; `-details`, `-graph` and `-bar` files are native-resolution crops. Each scenario asserts what the game drew (node labels, details text, bar buttons) as well.

Not taken on 1.6, because it needs a click: `faction-page-change-leader-choice` (the menu opened by clicking the pencil). The pencil button itself is shown and counted.

| File | What it shows |
|---|---|
| `colonist-page-dynamic-layout` (+ `-details`, `-graph`) | Six colonists in the force layout, the pinned colonist's relations and opinions listed, red, green and blue lines |
| `colonist-page-circle-layout` | The same six on a circle |
| `colonist-page-40-colonists` (+ `-graph`) | 40 colonists spread over the graph, every portrait and name separate |
| `faction-page-30-factions-settled` (+ `-graph`) | 31 factions spread over the graph area, labels separate; the dense red web is the mutual hostility |
| `faction-page-leaderless-faction` (+ `-details`, `-graph`) | A leaderless faction drawn with its faction icon in the faction color; details read "Prime councilor: Nobody" |
| `faction-page-change-leader-button` (+ `-details`, `-graph`) | Colony details with the pencil change-leader button inside the panel |
| `bottom-bar-relations-replaces-factions` (+ `-bar`) | Default bar: Relations icon in the Factions slot |
| `bottom-bar-relations-beside-factions` (+ `-bar`) | Keep-vanilla setting on: Factions and Relations icons both in the bar |
| `colonist-page-german` (+ `-details`, `-graph`) | Colonist page in German ("Kolonisten", "Beziehungen", "Interaktionen") |
| `faction-page-german` (+ `-graph`) | Factions page in German, the page button reads "Fraktionen" |
| `01-mod-list-incompatibility-warning` | Mod list: "Incompatible with: Relations Tab" on our mod; game versions 1.6, 1.7 listed |
