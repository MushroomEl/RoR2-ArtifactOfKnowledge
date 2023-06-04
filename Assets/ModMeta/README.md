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

## Mod Content

### Upgrade XP Sources

Only Experience and Teleporter Drop are enabled by default. The actual Upgrade XP required for every level is always 1; individual XP Sources are instead scaled down by their XP-to-level requirements (e.g. with default config, Kills grants 1/8 actual Upgrade XP per kill at first level, 1/8.5 at second, etc.).

- **Experience**: The vanilla XP system also grants Upgrade XP. Scales exponentially by default, with slightly faster levels than the vanilla system (8XP x1.4 per level; vanilla is 20XP x1.55 per level).
- **Teleporter Drop**: Gain 1 XP per teleporter drop, divided by participating player count. Default scaling: 2XP, does not scale (clearing a teleporter always grants half a level). By default, prevents actual teleporter drops from spawning.
- **Kills**: Gain 1 XP per kill. Default scaling: 8XP +0.5 per level. May be configured to only grant XP to the player who struck the killing blow.
- **Kill HP**: Gain 1 XP per 100 enemy base health on kill (scales with only Elite and Swarms health modifiers and nothing else). Default scaling: 8XP + 0.5 per level. May be configured to only grant XP to the player who struck the killing blow. May be configured to scale with total modified HP instead of base HP.
- **Time**: Gain 1 XP per second while the run timer is progressing. Default scaling: 20XP +2 per level.

### Items

<table>
	<thead>
		<tr>
			<th>Icon</th>
			<th>Name/Description</th>
		</tr>
	</thead>
	<tbody>
		<tr>
			<td colspan="2" align="center"><h3>Meta Items</h3><br>Meta-tier items only appear while Artifact of Knowledge is enabled. They usually have no direct effect on gameplay, instead changing how Artifact of Knowledge functions within a run.</td>
		</tr>
		<tr>
			<td><img src="https://github.com/ThinkInvis/RoR2-ArtifactOfKnowledge/blob/master/Assets/ArtifactOfKnowledge/Textures/ItemIcons/TidalPull.png?raw=true" width=128></td>
			<td>
				<b>Tidal Pull</b><br>
				Increase chance and limit of Lunar item upgrades.<br>
				May rarely appear in the Upgrade menu. By default, also replaces all drops from Void Chests, and grants a Reroll when picked up.
			</td>
		</tr>
		<tr>
			<td><img src="https://github.com/ThinkInvis/RoR2-ArtifactOfKnowledge/blob/master/Assets/ArtifactOfKnowledge/Textures/ItemIcons/Undertow.png?raw=true" width=128></td>
			<td>
				<b>Tidal Pull</b><br>
				Increase chance and limit of Lunar item upgrades.<br>
				May rarely appear in the Upgrade menu. By default, also replaces all drops from Lunar Chests, and grants a Reroll when picked up.
			</td>
		</tr>
	</tbody>
</table>

## Issues/TODO

- Issue: the upgrade menu cannot be opened while moving.
- TODO: implement Rerolls as a Meta item, to display in the item bar for convenience.
- TODO: gear swaps other than Equipment -- survivors, skills, *maybe* other artifacts, etc.
- TODO: visual clientside feedback for invalid actions in the upgrade menu. Currently audio-only.
- See the GitHub repo for more!

## Changelog

The 5 latest updates are listed below. For a full changelog, see: https://github.com/ThinkInvis/RoR2-ArtifactOfKnowledge/blob/master/changelog.md

(🌧︎: Involves an accepted GitHub Pull Request or other significant assistance from the community. Thanks for your help!)

**3.0.0**

- Added the Meta item tier.
- New items:
	- Tidal Pull (Meta): Increase chance and limit of Lunar item upgrades.
		- Replaces all drops from Lunar pods by default.
	- Undertow (Meta): Increase chance and limit of Void item upgrades.
		- Replaces all drops from Void pods by default.
- XP source rework:
	- XP sources are now implemented as separate modules, meaning multiple may be active at once with different scaling settings.
		- XP to next level is now always 1; individual XP sources scale down per level instead, with severity depending on their settings.
	- Implemented new XP sources:
		- KillHP, which scales based on either enemy base max health (Elite modifiers count, all other items and HP-per-enemy-level do not) or enemy max health stat (affected by all items and levels).
		- TeleporterDrop, migrated from a previously standalone XpScaling setting.
			- Has a separate config option to prevent teleporter drops, even if XP gain from such is disabled.
		- DamageFracDealt, which scales based on percentage of damage stat.
		- DamageDealt, which does not scale down.
		- Gold, which optionally scales over time using the same formula applied to interactables.
	- The Kills XP source, and several of the new XP sources, now allow XP sharing to be turned off. In this case, XP will be granted to the most relevant player (e.g. last-hit for kills).
- Added some accessibility configs:
	- Visibility of keybind text hint in the experience bar: may now be visible (brighter text), subdued (previously only option), or hidden.
	- Flashy animations on the experience bar while the player has unspent upgrades: may now be visible (previously only option), subdued (no aura, only the bar itself pulses), or hidden.

**2.0.1**

- Fixed obsolete and nonfunctional Vanilla XpScalingType option still being present and default.
- Split XpScaling config into LinearXpScaling and ExponentialXpScaling in order to present better RiskOfOptions range/display for each.
- Changing a Server XP Scaling config mid-run now recalculates XP-to-next-level for all players.
- Changing a Server Item Selection config mid-run now forces a free reroll for all players.
- XpScalingType and Source now have default behavior in case of invalid settings.

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