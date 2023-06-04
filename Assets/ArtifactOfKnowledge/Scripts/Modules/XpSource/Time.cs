using RoR2;

namespace ThinkInvisible.ArtifactOfKnowledge.XpSources {
	public class Time : XpSource<Time> {

		////// Config //////

		public override ScalingType XpScalingType { get; internal set; } = ScalingType.Linear;
		public override float StartingXp { get; internal set; } = 20f;
		public override float LinearXpScaling { get; internal set; } = 2f;



		////// Other Fields/Properties //////

		float stopwatch = 0f;




        ////// TILER2 Module Setup //////

        public override void SetupAttributes() {
			base.SetupAttributes();
		}

		public override void Install() {
			base.Install();
            On.RoR2.Run.FixedUpdate += Run_FixedUpdate;
		}

        public override void Uninstall() {
			base.Uninstall();
			On.RoR2.Run.FixedUpdate -= Run_FixedUpdate;
		}



		////// Hooks //////

		private void Run_FixedUpdate(On.RoR2.Run.orig_FixedUpdate orig, Run self) {
			if(CanGrant() && !Run.instance.isRunStopwatchPaused) {
				stopwatch -= UnityEngine.Time.fixedDeltaTime;
				if(stopwatch <= 0f) {
					stopwatch = 1f;
					Grant(1f);
				}
			}
		}
	}
}
