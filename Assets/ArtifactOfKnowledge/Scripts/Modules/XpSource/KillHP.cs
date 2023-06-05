using RoR2;
using TILER2;

namespace ThinkInvisible.ArtifactOfKnowledge.XpSources {
	[AutoConfigRoOInfoOverrides(typeof(ArtifactOfKnowledgePlugin), modGuid = "com.ThinkInvisible.ArtifactOfKnowledge.XpSources", modName = "ArtifactOfKnowledge.XpSources")]
	public class KillHP : XpSource<KillHP> {

		////// Config //////

		public override ScalingType XpScalingType { get; internal set; } = ScalingType.Linear;
		public override float StartingXp { get; internal set; } = 8f;
		public override float LinearXpScaling { get; internal set; } = 0.5f;

		[AutoConfig("If true, only base HP with Elite modifiers will be used (will not scale with levels/time nor other items), not including shield other than that from Overloading elites. If false, the post-calculation max HP stat is used, including shield.", AutoConfigFlags.PreventNetMismatch)]
		[AutoConfigRoOCheckbox()]
		public bool BaseHPOnly { get; internal set; } = true;

		public enum SharingMethod {
			Always, AlwaysSplit, LastHit//, MostDamage //TODO
        }

		[AutoConfig("Determines how kill XP is shared. Always grants the base amount to all players, while AlwaysSplit divides by playercount.", AutoConfigFlags.PreventNetMismatch)]
		[AutoConfigRoOChoice()]
		public SharingMethod Sharing { get; internal set; } = SharingMethod.Always;


		////// Other Fields/Properties //////




		////// TILER2 Module Setup //////

		public override void SetupAttributes() {
			base.SetupAttributes();
		}

		public override void Install() {
			base.Install();
			GlobalEventManager.onCharacterDeathGlobal += GlobalEventManager_onCharacterDeathGlobal;
		}

		public override void Uninstall() {
			base.Uninstall();
			GlobalEventManager.onCharacterDeathGlobal -= GlobalEventManager_onCharacterDeathGlobal;
		}



		////// Hooks //////


		private void GlobalEventManager_onCharacterDeathGlobal(DamageReport obj) {
			if(CanGrant() && obj.attackerTeamIndex == TeamIndex.Player && obj.victimBody) {
				float xp;
				if(BaseHPOnly) {
					xp = obj.victimBody.baseMaxHealth / 50f;
					if(obj.victimBody.inventory) {
						var eliteBoost = obj.victimBody.inventory.GetItemCount(RoR2Content.Items.BoostHp);
						var swarmsCut = obj.victimBody.inventory.GetItemCount(RoR2Content.Items.CutHp);
						xp *= 1f + eliteBoost * 0.1f;
						xp /= 1f + swarmsCut;
					}
				} else {
					xp = obj.victimBody.maxHealth + obj.victimBody.maxShield;
                }
				CharacterMaster singleTarget = null;
				if(Sharing == SharingMethod.LastHit) {
					singleTarget = obj.attackerMaster;
				}
				if(Sharing == SharingMethod.AlwaysSplit)
					xp /= Run.instance.participatingPlayerCount;
				Grant(xp, singleTarget);
			}
		}
	}
}
