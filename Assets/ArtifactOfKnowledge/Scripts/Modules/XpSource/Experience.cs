using RoR2;

namespace ThinkInvisible.ArtifactOfKnowledge.XpSources {
	public class Experience : XpSource<Experience> {

		////// Config //////

		public override bool Active { get; internal set; } = true;




        ////// Other Fields/Properties //////




        ////// TILER2 Module Setup //////

        public override void SetupAttributes() {
			base.SetupAttributes();
		}

		public override void Install() {
			base.Install();
			On.RoR2.TeamManager.GiveTeamExperience += TeamManager_GiveTeamExperience;
		}

		public override void Uninstall() {
			base.Uninstall();
			On.RoR2.TeamManager.GiveTeamExperience -= TeamManager_GiveTeamExperience;
		}



		////// Hooks //////
		
		private void TeamManager_GiveTeamExperience(On.RoR2.TeamManager.orig_GiveTeamExperience orig, TeamManager self, TeamIndex teamIndex, ulong experience) {
			orig(self, teamIndex, experience);
			if(CanGrant() && teamIndex == TeamIndex.Player) {
				Grant((float)experience);
			}
		}
	}
}
