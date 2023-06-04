using RoR2;
using TILER2;

namespace ThinkInvisible.ArtifactOfKnowledge.XpSources {
	public class KillBaseHP : XpSource<KillBaseHP> {

		////// Config //////

		public override ScalingType XpScalingType { get; internal set; } = ScalingType.Linear;
		public override float StartingXp { get; internal set; } = 8f;
		public override float LinearXpScaling { get; internal set; } = 0.5f;

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
				var xp = obj.victimBody.baseMaxHealth / 100f;
				if(obj.victimBody.inventory) {
					var eliteBoost = obj.victimBody.inventory.GetItemCount(RoR2Content.Items.BoostHp);
					xp *= 1f + eliteBoost * 0.1f;
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
