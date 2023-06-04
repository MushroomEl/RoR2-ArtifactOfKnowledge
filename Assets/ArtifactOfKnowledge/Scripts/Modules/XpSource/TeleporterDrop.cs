using RoR2;
using TILER2;

namespace ThinkInvisible.ArtifactOfKnowledge.XpSources {
	public class TeleporterDrop : XpSource<TeleporterDrop> {

		////// Config //////

		[AutoConfig("If true, teleporter drops will not spawn. Takes effect even if Active=False.", AutoConfigFlags.PreventNetMismatch)]
		[AutoConfigRoOCheckbox()]
		public bool Intercept { get; internal set; } = true;

		public override ScalingType XpScalingType { get; internal set; } = ScalingType.Linear;
		public override float StartingXp { get; internal set; } = 2f;
		public override float LinearXpScaling { get; internal set; } = 0f;

		public override bool Active { get; internal set; } = true;



		////// Other Fields/Properties //////




		////// TILER2 Module Setup //////

		public override void SetupAttributes() {
			base.SetupAttributes();
		}

		public override void Install() {
			base.Install();
			On.RoR2.BossGroup.DropRewards += BossGroup_DropRewards;
		}

        public override void Uninstall() {
			base.Uninstall();
			On.RoR2.BossGroup.DropRewards -= BossGroup_DropRewards;
		}



		////// Hooks //////

		private void BossGroup_DropRewards(On.RoR2.BossGroup.orig_DropRewards orig, BossGroup self) {
			bool calledOrig = false;
			if(!Intercept || !KnowledgeArtifact.instance.IsActiveAndEnabled()) {
				orig(self);
				calledOrig = true;
			}
			if(CanGrant() && TeleporterInteraction.instance && TeleporterInteraction.instance.bossGroup == self) {
				var xp = (self.bonusRewardCount + 1) / (self.scaleRewardsByPlayerCount ? 1f : Run.instance.participatingPlayerCount);
				if(xp > 0f)
					Grant(xp);
			} else if(!calledOrig) orig(self);
		}
	}
}
