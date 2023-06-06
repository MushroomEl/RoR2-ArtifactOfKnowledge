using R2API.Utils;
using RoR2;
using System.Collections.Generic;
using TILER2;
using UnityEngine.Networking;

namespace ThinkInvisible.ArtifactOfKnowledge {
    [AutoConfigRoOInfoOverrides(typeof(ArtifactOfKnowledgePlugin), modGuid = "com.ThinkInvisible.ArtifactOfKnowledge.Selection", modName = "ArtifactOfKnowledge.Selection", categoryName = "Selection.Misc")]
    public class ItemSelection : T2Module<ItemSelection> {
        public override bool managedEnable => false;

        //dictionaries have custom impl during awake/catalog-init, instead of direct attributes
        public Dictionary<ItemTierDef, int> TierMultipliers { get; private set; } = new Dictionary<ItemTierDef, int>();
        public Dictionary<ItemTierDef, float> TierWeights { get; private set; } = new Dictionary<ItemTierDef, float>();
        public Dictionary<ItemTierDef, int> TierUpgradeIntervals { get; private set; } = new Dictionary<ItemTierDef, int>();
        public Dictionary<ItemTierDef, int> TierCaps { get; private set; } = new Dictionary<ItemTierDef, int>();

        [AutoConfig("Chance for normal item drops to be converted to Void. Stacks with normal tiers in the Multiplier section.", AutoConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
        [AutoConfigRoOSlider("{0:P0}", 0f, 1f)]
        public float MultVoidWeight { get; internal set; } = 0.06f;

        [AutoConfig("Total cap for Void items of any tier to appear. 0 for unlimited. DOES NOT stack with normal tiers in the Caps section.", AutoConfigFlags.PreventNetMismatch, 0, int.MaxValue)]
        [AutoConfigRoOIntSlider("{0:N0}", 0, 30)]
        public int VoidCap { get; internal set; } = 1;

        [AutoConfig("Weight for each offered gear to be an equipment.", AutoConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
        [AutoConfigRoOSlider("{0:P0}", 0f, 1f)]
        public float BaseEquipChance { get; internal set; } = 1f;

        [AutoConfig("Weight for each offered gear to be a Lunar equipment.", AutoConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
        [AutoConfigRoOSlider("{0:P0}", 0f, 1f)]
        public float BaseLunarEquipChance { get; internal set; } = 0.03f;

        [AutoConfig("If true, one each of the offered items will always be Damage-, Utility-, and Healing-related.", AutoConfigFlags.PreventNetMismatch)]
        [AutoConfigRoOCheckbox()]
        public bool GuaranteeCategories { get; internal set; } = true;

        [AutoConfig("How many random items are offered at once by the upgrade menu.", AutoConfigFlags.PreventNetMismatch, 1, int.MaxValue)]
        [AutoConfigRoOIntSlider("{0:N0}", 1, 30)]
        public int SelectionSize { get; internal set; } = 5;

        [AutoConfig("How many random gear swaps are offered at once by the upgrade menu.", AutoConfigFlags.PreventNetMismatch, 1, int.MaxValue)]
        [AutoConfigRoOIntSlider("{0:N0}", 1, 30)]
        public int GearSelectionSize { get; internal set; } = 2;

        public override void SetupConfig() {
            base.SetupConfig();
            ConfigEntryChanged += (newValueBoxed, eventArgs) => {
                if(NetworkServer.active && Run.instance && KnowledgeArtifact.instance.IsActiveAndEnabled()) {
                    foreach(var kcm in KnowledgeCharacterManager.readOnlyInstances) {
                        kcm.ServerGenerateSelection();
                    }
                }
            };
        }

        [SystemInitializer(typeof(ItemTierCatalog))]
        static void InitItemTierConfigs() {
            //setup default values
            foreach(var tier in ItemTierCatalog.allItemTierDefs) {
                if(!tier || !tier.isDroppable || tier.tier.IsVoid() || tier == ArtifactOfKnowledgePlugin.MetaItemTier) continue; //void has special handling
                instance.TierMultipliers[tier] = 1; //default values
                instance.TierWeights[tier] = 0f;
                instance.TierCaps[tier] = 0;
                instance.TierUpgradeIntervals[tier] = 0;
            }

            instance.TierWeights[ItemTierCatalog.GetItemTierDef(ItemTier.Tier1)] = 1f;
            instance.TierWeights[ItemTierCatalog.GetItemTierDef(ItemTier.Lunar)] = 0.04f;

            instance.TierCaps[ItemTierCatalog.GetItemTierDef(ItemTier.Lunar)] = 1;

            instance.TierUpgradeIntervals[ItemTierCatalog.GetItemTierDef(ItemTier.Tier2)] = 5;
            instance.TierUpgradeIntervals[ItemTierCatalog.GetItemTierDef(ItemTier.Tier3)] = 15;

            //bind
            instance.Bind(typeof(ItemSelection).GetPropertyCached(nameof(ItemSelection.TierMultipliers)), ArtifactOfKnowledgePlugin.cfgFile, "ArtifactOfKnowledge.Selection", "Selection.Multipliers", new AutoConfigAttribute($"<AIC.DictKeyProp.{nameof(ItemTierDef.name)}>", "Items of this tier will grant this many copies when selected in the Upgrade menu.", AutoConfigFlags.BindDict | AutoConfigFlags.PreventNetMismatch, 1, int.MaxValue));
            foreach(var k in instance.TierMultipliers.Keys)
                instance.BindRoO(instance.FindConfig(nameof(instance.TierMultipliers), k), new AutoConfigRoOIntSliderAttribute("{0:N0}", 1, 10));

            instance.Bind(typeof(ItemSelection).GetPropertyCached(nameof(ItemSelection.TierWeights)), ArtifactOfKnowledgePlugin.cfgFile, "ArtifactOfKnowledge.Selection", "Selection.Weights", new AutoConfigAttribute($"<AIC.DictKeyProp.{nameof(ItemTierDef.name)}>", "Weight for this item tier to be offered (higher = more often, relative to others).", AutoConfigFlags.BindDict | AutoConfigFlags.PreventNetMismatch, 0f, 1f));
            foreach(var k in instance.TierWeights.Keys)
                instance.BindRoO(instance.FindConfig(nameof(instance.TierWeights), k), new AutoConfigRoOSliderAttribute("{0:P0}", 0f, 1f));

            instance.Bind(typeof(ItemSelection).GetPropertyCached(nameof(ItemSelection.TierCaps)), ArtifactOfKnowledgePlugin.cfgFile, "ArtifactOfKnowledge.Selection", "Selection.Caps", new AutoConfigAttribute($"<AIC.DictKeyProp.{nameof(ItemTierDef.name)}>", "Maximum number of this item tier to offer across the entire selection. 0 for unlimited.", AutoConfigFlags.BindDict | AutoConfigFlags.PreventNetMismatch, 0, int.MaxValue));
            foreach(var k in instance.TierCaps.Keys)
                instance.BindRoO(instance.FindConfig(nameof(instance.TierCaps), k), new AutoConfigRoOIntSliderAttribute("{0:N0}", 0, 30));

            instance.Bind(typeof(ItemSelection).GetPropertyCached(nameof(ItemSelection.TierUpgradeIntervals)), ArtifactOfKnowledgePlugin.cfgFile, "ArtifactOfKnowledge.Selection", "Selection.UpgradeIntervals", new AutoConfigAttribute($"<AIC.DictKeyProp.{nameof(ItemTierDef.name)}>", "If nonzero, all offered items will be of this tier after this many levels. Longer intervals take precedence.", AutoConfigFlags.BindDict | AutoConfigFlags.PreventNetMismatch, 0, int.MaxValue));
            foreach(var k in instance.TierUpgradeIntervals.Keys)
                instance.BindRoO(instance.FindConfig(nameof(instance.TierUpgradeIntervals), k), new AutoConfigRoOIntSliderAttribute("{0:N0}", 0, 50));
        }
    }
}
