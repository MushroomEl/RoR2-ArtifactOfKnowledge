# Artifact of Knowledge Changelog

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
		- Purchase, which adds a purchase button to the upgrade panel; and (optionally) scales over time using the same formula applied to interactables.
	- The Kills XP source, and several of the new XP sources, now allow XP sharing to be turned off. In this case, XP will be granted to the most relevant player (e.g. last-hit for kills). The Purchase XP source cannot be shared.
- Selection config rework:
	- Selection configs are now auto-populated by tier, with special handling remaining for Void tiers. Other mods that add new item tiers should be compatible, and Boss-tier items are now configurable.
	- Selection configs are now displayed in a new config super-category in Risk of Options.
- Added an option to grant multiple copies of an item at once dependent on tier, disabled by default.
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