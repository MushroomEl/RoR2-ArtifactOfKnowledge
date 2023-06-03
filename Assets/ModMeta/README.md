# Artifact of Knowledge

## SUPPORT DISCLAIMER

### Use of a mod manager is STRONGLY RECOMMENDED.

Seriously, use a mod manager.

If the versions of Artifact of Knowledge or TILER2 (or possibly any other mods) are different between your game and other players' in multiplayer, things WILL break. If TILER2 is causing kicks for "unspecified reason", it's likely due to a mod version mismatch. Ensure that all players in a server, including the host and/or dedicated server, are using the same mod versions before reporting a bug.

**While reporting a bug, make sure to post a console log** (`path/to/RoR2/BepInEx/LogOutput.log`) from a run of the game where the bug happened; this often provides important information about why the bug is happening. If the bug is multiplayer-only, please try to include logs from both server and client.

## Description

Artifact of Knowledge adds a highly configurable alternate item acquisition system to the game, a la Artifact of Command and/or Sacrifice, which is heavily based on the upgrade system from the bullet-heaven roguelike Nova Drift.

While enabled, the Artifact will add a second level system, upgrade levels, which gives levels slightly faster than the vanilla system and grants one upgrade point per level. Pressing [U] (rebindable in mod config) will open the upgrade menu, presenting a selection of 5 new items and 2 gear swaps (equipment), 1 each of which will have the Damage/Utility/Healing category guaranteed. Only one of these options may be selected; at which point the upgrade point will be spent to obtain that item, then all of the options will be rerolled.

Every 5 levels and 15 levels, the selection of items will be upgraded to uncommon or rare, respectively. Any offered item also has a small chance to be Void and a smaller chance to be Lunar.

## Issues/TODO

- TODO: a new Meta item tier which affects how item selections are chosen (and also displays Rerolls in the item bar for convenience).
- TODO: gear swaps other than Equipment -- survivors, skills, *maybe* other artifacts, etc.
- TODO: visual clientside feedback for invalid actions in the upgrade menu. Currently audio-only.
- TODO: change or remove remaining item spawns untouched by Sacrifice list, e.g. Void pods.
- See the GitHub repo for more!

## Changelog

The 5 latest updates are listed below. For a full changelog, see: https://github.com/ThinkInvis/RoR2-ArtifactOfKnowledge/blob/master/changelog.md

(🌧︎: Involves an accepted GitHub Pull Request or other significant assistance from the community. Thanks for your help!)

**2.0.0**

- Added per-kills and per-time alternate progression modes.
- Added linear alternate progression scaling (default vanilla option is exponential), which may fit kills/time modes better.
- Artifact now prevents Teleporter drops by default, instead granting a scaling sum of upgrade experience for cleared teleporters.
- Split server config category into generic, XP scaling, and item generation categories.
- Fixed drones causing level-up fanfares. *Pipe down, will you?*
	- Prevented KnowledgeCharacterManager from being added to non-player-controlled allies.
	- Also prevented level-up fanfares from playing for KCM instances which don't have a discovered HUD on client, just in case.
- Some config options now have more sensible Risk of Options limits/formatting.
- Fixed StartingXp config not applying to the first level (further levels would properly use it in calculation).
- Changed XP requirement calculation to use the amount of XP required to level, instead of the total XP thresholds at the current and next levels.
- Fixed level-ups not occurring with *exactly* enough XP (could happen very consistently at first level).
- Upgrade menu tooltips now use dark itemtier colors for higher readability.

**1.0.1** *The Pobody's Nerfect Update*

- Fixed hidden and WorldUnique (e.g. scrap) items being inadvertently offered by Artifact of Knowledge.
- Added a custom package icon, which was previously an accidental copy from Tinker's Satchel.

**1.0.0**

- Initial version.
- Adds the Artifact of Knowledge, which presents a selection of 5 items and 2 gear for every level of a secondary level-up system; and prevents any Sacrifice-banned interactables (mostly chests) from spawning.
- Adds an upgrade menu popup, and an upgrade experience bar in the main HUD.