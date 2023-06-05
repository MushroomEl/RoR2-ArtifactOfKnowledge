using RoR2;
using TILER2;
using UnityEngine;
using UnityEngine.Networking;

namespace ThinkInvisible.ArtifactOfKnowledge.XpSources {
	public class Purchase : XpSource<Purchase> {

		////// Config //////

		public override ScalingType XpScalingType { get; internal set; } = ScalingType.Linear;
		public override float StartingXp { get; internal set; } = 25f;
		public override float LinearXpScaling { get; internal set; } = 3f;

		[AutoConfigRoOCheckbox()]
		[AutoConfig("If true, purchase cost will scale with time, stacking with other scaling options.", AutoConfigFlags.PreventNetMismatch)]
		public bool ScaleWithTime { get; private set; } = true;


		////// Other Fields/Properties //////

		const float _UI_UPDATE_INTERVAL = 5f;
		float _stopwatch = 0f;


        ////// TILER2 Module Setup //////

        public override void SetupAttributes() {
			base.SetupAttributes();
		}

		public override void Install() {
			base.Install();
            KnowledgeCharacterManager.ModifyUpgradePanel += KnowledgeCharacterManager_ModifyUpgradePanel;
            KnowledgeCharacterManager.OnCustomUpgradeAction += KnowledgeCharacterManager_OnCustomUpgradeAction;
            On.RoR2.Run.FixedUpdate += Run_FixedUpdate;
		}

        public override void Uninstall() {
			base.Uninstall();
			KnowledgeCharacterManager.ModifyUpgradePanel -= KnowledgeCharacterManager_ModifyUpgradePanel;
			KnowledgeCharacterManager.OnCustomUpgradeAction -= KnowledgeCharacterManager_OnCustomUpgradeAction;
			On.RoR2.Run.FixedUpdate -= Run_FixedUpdate;
		}



		////// Public API //////

		public int CalculateNextPurchaseCost(KnowledgeCharacterManager sender) {
			var xpRemaining = (1f - sender.xp);
			var totalCost = StartingXp;
			if(XpScalingType == ScalingType.Exponential)
				totalCost *= Mathf.Pow(ExponentialXpScaling, sender.level);
			else
				totalCost += LinearXpScaling * sender.level;
			if(ScaleWithTime)
				totalCost = Run.instance.GetDifficultyScaledCost(Mathf.CeilToInt(totalCost));
			return Mathf.CeilToInt(totalCost * xpRemaining);
        }

		////// Hooks //////

		private void Run_FixedUpdate(On.RoR2.Run.orig_FixedUpdate orig, Run self) {
			orig(self);
			if(ScaleWithTime && NetworkServer.active) {
				_stopwatch -= UnityEngine.Time.fixedDeltaTime;
				if(_stopwatch < _UI_UPDATE_INTERVAL) {
					_stopwatch = _UI_UPDATE_INTERVAL;
					foreach(var kcm in KnowledgeCharacterManager.readOnlyInstances) {
						kcm.RpcForceUpdateUI(true);
                    }
                }
            }
		}

		private void KnowledgeCharacterManager_ModifyUpgradePanel(KnowledgeCharacterManager sender, KnowledgePickerPanel panel, bool isInitialization) {
			if(!CanGrant()) return;
			if(isInitialization) {
				panel.AddActionButton("Purchase", "AKNOW_PURCHASE", () => sender.CmdCustomUpgradeAction("Purchase"));
			} else {
				panel.SetActionButtonFormatArgs("Purchase", CalculateNextPurchaseCost(sender));
            }
		}

		private void KnowledgeCharacterManager_OnCustomUpgradeAction(KnowledgeCharacterManager sender, string ident) {
			if(!CanGrant() || ident != "Purchase") return;
			var cost = CalculateNextPurchaseCost(sender);
			var master = sender.targetMasterObject.GetComponent<CharacterMaster>();
			if(master.money < cost) {
				sender.RpcDisplayError(KnowledgeCharacterManager.UpgradeActionCode.CustomAction);
				return;
            }
			master.money -= (uint)cost;
			sender.ServerAddXp(1f - sender.xp);
		}
	}
}
