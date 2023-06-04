using RoR2;
using System;
using TILER2;
using UnityEngine;
using UnityEngine.Networking;

namespace ThinkInvisible.ArtifactOfKnowledge {
	public abstract class XpSource<T> : XpSource where T : XpSource<T> {
		public static T instance { get; private set; }

		public XpSource() {
			if(instance != null) throw new InvalidOperationException("Singleton class \"" + typeof(T).Name + "\" inheriting T2Module/XpSource was instantiated twice");
			instance = this as T;
		}
	}

	public abstract class XpSource : T2Module {

		public enum ScalingType { Exponential, Linear }

		////// Config //////

		public override string configCategoryPrefix => "XpSources.";

		[AutoConfig("Determines how the StartingXp and XpScaling options apply.\r\n - Exponential: each level takes *XpScaling more than the last (XP granted is equal to BaseXp/(XpScaling^Level)). Matches scaling of the vanilla experience system.\r\n - Linear: each level takes +XpScaling more than the last (XP granted is equal to BaseXp/(1+XpScaling*Level)). Much shallower scaling compared to Exponential, more suitable for Time/Kills sources.", AutoConfigFlags.None)]
		[AutoConfigRoOChoice()]
		public virtual ScalingType XpScalingType { get; internal set; } = ScalingType.Exponential;

		[AutoConfig("Experience, kills, seconds, etc. required for the first upgrade level. Vanilla level system uses 20 xp.", AutoConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
		[AutoConfigRoOSlider("{0:N0}", 0f, 100f)]
		public virtual float StartingXp { get; internal set; } = 8f;

		[AutoConfig("Experience scaling rate for upgrade levels, if XpScalingType is Exponential. Vanilla level system uses 1.55.", AutoConfigFlags.PreventNetMismatch, 1f, float.MaxValue)]
		[AutoConfigRoOSlider("{0:P0}", 1.01f, 3f)]
		public virtual float ExponentialXpScaling { get; internal set; } = 1.4f;

		[AutoConfig("Experience scaling rate for upgrade levels, if XpScalingType is Linear.", AutoConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
		[AutoConfigRoOSlider("{0:N1}", 0f, 50f)]
		public virtual float LinearXpScaling { get; internal set; } = 5f;

		[AutoConfig("Controls whether this XpSource is applied.", AutoConfigFlags.PreventNetMismatch)]
		[AutoConfigRoOCheckbox()]
		public virtual bool Active { get; internal set; } = false;



		////// Other Fields/Properties //////




		////// TILER2 Module Setup //////

		public override void SetupAttributes() {
			base.SetupAttributes();
		}

		public override void Install() {
			base.Install();
		}

		public override void Uninstall() {
			base.Uninstall();
		}



		////// Private API //////

		const int MAX_ITERS = 100;
		protected void Grant(float baseAmount, CharacterMaster singleTarget = null) {
			foreach(var kcm in GameObject.FindObjectsOfType<KnowledgeCharacterManager>()) {
				if(singleTarget && kcm.targetMasterObject != singleTarget.gameObject) continue;

				int iters = 0;
				float xpSimAdd = 0f;
				float xpSimCurrent = kcm.xp;
				float remainingBase = baseAmount;
				while(remainingBase > 0f && iters <= MAX_ITERS) {
					iters++;
					//translate to scaled
					var scaledAmount = remainingBase / StartingXp;
					if(XpScalingType == ScalingType.Exponential)
						scaledAmount /= Mathf.Pow(ExponentialXpScaling, kcm.level);
					else
						scaledAmount /= 1f + LinearXpScaling * kcm.level;
					//apply
					var amountToNextLevel = 1f - xpSimCurrent;
					var amountToApply = Mathf.Min(scaledAmount, amountToNextLevel);
					xpSimAdd += amountToApply;
					scaledAmount -= amountToApply;
					xpSimCurrent = (xpSimCurrent + amountToApply) % 1f;
					//translate to unscaled and deduct
					if(XpScalingType == ScalingType.Exponential)
						scaledAmount *= Mathf.Pow(ExponentialXpScaling, kcm.level);
					else
						scaledAmount *= 1f + LinearXpScaling * kcm.level;
					remainingBase = scaledAmount * StartingXp;
				}

				if(remainingBase < 0f)
					ArtifactOfKnowledgePlugin._logger.LogWarning($"XpSource.Grant in type {this.GetType()} made a bookkeeping error and granted extra XP (remaining baseAmount is {remainingBase}, {iters} iters, granting {xpSimAdd}). Please report this as a bug!");

				if(iters >= MAX_ITERS)
					ArtifactOfKnowledgePlugin._logger.LogWarning("XpSource.Grant tried to grant too many levels at once. Halting early to prevent lag, some XP will be lost. Check your scaling settings.");

				kcm.ServerAddXp(xpSimAdd);
			}
        }

		protected bool CanGrant() {
			return Active && NetworkServer.active && KnowledgeArtifact.instance.IsActiveAndEnabled();
		}



		////// Hooks //////

	}
}
