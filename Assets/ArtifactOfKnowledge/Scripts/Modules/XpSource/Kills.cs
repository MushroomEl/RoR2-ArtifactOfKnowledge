using RoR2;
using TILER2;

namespace ThinkInvisible.ArtifactOfKnowledge.XpSources {
	[AutoConfigRoOInfoOverrides(typeof(ArtifactOfKnowledgePlugin), modGuid = "com.ThinkInvisible.ArtifactOfKnowledge.XpSources", modName = "ArtifactOfKnowledge.XpSources")]
	public class Kills : XpSource<Kills> {

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
			RoR2.GlobalEventManager.onCharacterDeathGlobal += GlobalEventManager_onCharacterDeathGlobal;
		}

		public override void Uninstall() {
			base.Uninstall();
			RoR2.GlobalEventManager.onCharacterDeathGlobal -= GlobalEventManager_onCharacterDeathGlobal;
		}



		////// Hooks //////


		private void GlobalEventManager_onCharacterDeathGlobal(DamageReport obj) {
			if(CanGrant() && obj.attackerTeamIndex == TeamIndex.Player) {
				CharacterMaster singleTarget = null;
				if(Sharing == SharingMethod.LastHit) {
					singleTarget = obj.attackerMaster;
                }
				var xp = 1f;
				if(Sharing == SharingMethod.AlwaysSplit)
					xp /= Run.instance.participatingPlayerCount;
				Grant(xp, singleTarget);
			}
		}
	}
}
