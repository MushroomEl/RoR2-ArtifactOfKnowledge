using BepInEx;
using R2API.Utils;
using UnityEngine;
using BepInEx.Configuration;
using Path = System.IO.Path;
using TILER2;
using static TILER2.MiscUtil;
using System.Linq;
using UnityEngine.AddressableAssets;
using System;
using RoR2;
using UnityEngine.Networking;
using System.Collections.Generic;
using static ThinkInvisible.ArtifactOfKnowledge.MiscUtil;

[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace ThinkInvisible.ArtifactOfKnowledge {
    [BepInPlugin(ModGuid, ModName, ModVer)]
    [BepInDependency(R2API.R2API.PluginGUID, R2API.R2API.PluginVersion)]
    [BepInDependency(TILER2Plugin.ModGuid, TILER2Plugin.ModVer)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    public class ArtifactOfKnowledgePlugin : BaseUnityPlugin {
        public const string ModVer = "2.0.1";
        public const string ModName = "ArtifactOfKnowledge";
        public const string ModGuid = "com.ThinkInvisible.ArtifactOfKnowledge";

        private static ConfigFile cfgFile;

        internal static FilingDictionary<T2Module> allModules = new FilingDictionary<T2Module>();

        internal static BepInEx.Logging.ManualLogSource _logger;

        internal static AssetBundle resources;

        T2Module[] earlyLoad;

        public class ClientConfigContainer : AutoConfigContainer {
            [AutoConfig("Press to show the upgrade menu while Artifact of Knowledge is active.", AutoConfigFlags.None)]
            [AutoConfigRoOKeybind()]
            public KeyboardShortcut KeybindShowMenu { get; internal set; } = new KeyboardShortcut(KeyCode.U);

            public enum UICluster { TopCenter, BottomLeft, Nowhere }
            public enum UIVisibility { Visible, Subdued, Hidden }

            [AutoConfig("Controls where the upgrade experience UI displays.", AutoConfigFlags.None)]
            [AutoConfigRoOChoice()]
            public UICluster XpBarLocation { get; internal set; } = UICluster.BottomLeft;

            [AutoConfig("Controls how visible the keybind hint text in the experience bar is.", AutoConfigFlags.None)]
            [AutoConfigRoOChoice()]
            public UIVisibility XpBarHintText { get; internal set; } = UIVisibility.Subdued;

            [AutoConfig("Controls how visible the unspent upgrades animation on the experience bar are.", AutoConfigFlags.None)]
            [AutoConfigRoOChoice()]
            public UIVisibility XpBarUnspentFlashiness { get; internal set; } = UIVisibility.Visible;
        }

        public class ServerConfigContainer : AutoConfigContainer {
            [AutoConfig("Number of rerolls required to banish an item. If 0, manual banishment is disabled. NYI!", AutoConfigFlags.PreventNetMismatch, 0, int.MaxValue)]
            [AutoConfigRoOIntSlider("{0:N0}", 0, 10)]
            public int BanishCost { get; internal set; } = 2;

            [AutoConfig("Number of rerolls granted at the start of a run.", AutoConfigFlags.PreventNetMismatch, 0, int.MaxValue)]
            [AutoConfigRoOIntSlider("{0:N0}", 0, 50)]
            public int StartingRerolls { get; internal set; } = 10;

            [AutoConfig("Number of rerolls granted for every cleared teleporter.", AutoConfigFlags.PreventNetMismatch, 0, int.MaxValue)]
            [AutoConfigRoOIntSlider("{0:N0}", 0, 10)]
            public int RerollsPerStage { get; internal set; } = 3;
        }

        [AutoConfigRoOInfoOverrides(typeof(ArtifactOfKnowledgePlugin), modGuid = "com.ThinkInvisible.ArtifactOfKnowledge.Selection", modName = "ArtifactOfKnowledge.Selection")]
        public class ItemSelectionConfigContainer : AutoConfigContainer {
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
        }

        public static ServerConfigContainer ServerConfig { get; private set; } = new ServerConfigContainer();
        public static ClientConfigContainer ClientConfig { get; private set; } = new ClientConfigContainer();
        public static ItemSelectionConfigContainer ItemSelectionConfig { get; private set; } = new ItemSelectionConfigContainer();

        public static ItemTierDef MetaItemTier { get; private set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity Engine.")]
        private void Awake() {
            _logger = Logger;

            resources = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Info.Location), "artifactofknowledge_assets"));

            try {
                UnstubShaders();
            } catch(Exception ex) {
                _logger.LogError($"Shader unstub failed: {ex} {ex.Message}");
            }

            cfgFile = new ConfigFile(Path.Combine(Paths.ConfigPath, ModGuid + ".cfg"), true);

            ServerConfig.BindAll(cfgFile, "Artifact of Knowledge", "Server Misc.");
            ItemSelectionConfig.BindAll(cfgFile, "AoK Item Selection", "Misc.");
            ClientConfig.BindAll(cfgFile, "Artifact of Knowledge", "Client");

            ClientConfig.ConfigEntryChanged += (newValueBoxed, eventArgs) => {
                if(eventArgs.target.boundProperty.Name == nameof(ClientConfigContainer.XpBarLocation) && Run.instance && KnowledgeArtifact.instance.IsActiveAndEnabled()) {
                    foreach(var hud in GameObject.FindObjectsOfType<RoR2.UI.HUD>()) {
                        KnowledgeXpBar.ModifyHud(hud);
                    }
                    foreach(var kcm in GameObject.FindObjectsOfType<KnowledgeCharacterManager>()) {
                        kcm.ClientUpdateXpBar();
                    }
                }
            };

            ItemSelectionConfig.ConfigEntryChanged += (newValueBoxed, eventArgs) => {
                if(NetworkServer.active && Run.instance && KnowledgeArtifact.instance.IsActiveAndEnabled()) {
                    foreach(var kcm in KnowledgeCharacterManager.readOnlyInstances) {
                        kcm.ServerGenerateSelection();
                    }
                }
            };

            MetaItemTier = resources.LoadAsset<ItemTierDef>("Assets/ArtifactOfKnowledge/ScriptableObjects/MetaItemTier.asset");
            var metaColor = new Color(0.1f, 0.05f, 0.7f);
            var metaColorDark = new Color(0.05f, 0.025f, 0.4f);
            //MetaItemTier.colorIndex = R2API.ColorsAPI.RegisterColor(metaColor);
            //MetaItemTier.darkColorIndex = R2API.ColorsAPI.RegisterColor(metaColorDark);
            MetaItemTier.colorIndex = ColorCatalog.ColorIndex.LunarItem;
            MetaItemTier.darkColorIndex = ColorCatalog.ColorIndex.LunarItemDark;

            ModifyItemTierPrefabs(metaColor);

            R2API.ContentAddition.AddItemTierDef(MetaItemTier);

            var modInfo = new T2Module.ModInfo {
                displayName = "Artifact of Knowledge",
                longIdentifier = "ArtifactOfKnowledge",
                shortIdentifier = "AKNOW",
                mainConfigFile = cfgFile
            };
            allModules = T2Module.InitAll<T2Module>(modInfo);

            earlyLoad = new T2Module[] { };
            T2Module.SetupAll_PluginAwake(earlyLoad);
            T2Module.SetupAll_PluginAwake(allModules.Except(earlyLoad));
        }

        [SystemInitializer(typeof(ItemTierCatalog))]
        static void InitItemTierConfigs() {
            //setup default values
            foreach(var tier in ItemTierCatalog.allItemTierDefs) {
                if(!tier || !tier.isDroppable || tier.tier.IsVoid() || tier == MetaItemTier) continue; //void has special handling
                ItemSelectionConfig.TierMultipliers[tier] = 1; //default values
                ItemSelectionConfig.TierWeights[tier] = 0f;
                ItemSelectionConfig.TierCaps[tier] = 0;
                ItemSelectionConfig.TierUpgradeIntervals[tier] = 0;
            }

            ItemSelectionConfig.TierWeights[ItemTierCatalog.GetItemTierDef(ItemTier.Tier1)] = 1f;
            ItemSelectionConfig.TierWeights[ItemTierCatalog.GetItemTierDef(ItemTier.Lunar)] = 0.04f;

            ItemSelectionConfig.TierCaps[ItemTierCatalog.GetItemTierDef(ItemTier.Lunar)] = 1;

            ItemSelectionConfig.TierUpgradeIntervals[ItemTierCatalog.GetItemTierDef(ItemTier.Tier2)] = 5;
            ItemSelectionConfig.TierUpgradeIntervals[ItemTierCatalog.GetItemTierDef(ItemTier.Tier3)] = 15;

            //bind
            ItemSelectionConfig.Bind(typeof(ItemSelectionConfigContainer).GetPropertyCached(nameof(ItemSelectionConfigContainer.TierMultipliers)), cfgFile, "ArtifactOfKnowledge.Selection", "Multipliers", new AutoConfigAttribute($"<AIC.DictKeyProp.{nameof(ItemTierDef.name)}>", "Items of this tier will grant this many copies when selected in the Upgrade menu.", AutoConfigFlags.BindDict | AutoConfigFlags.PreventNetMismatch, 1, int.MaxValue));
            foreach(var k in ItemSelectionConfig.TierMultipliers.Keys)
                ItemSelectionConfig.BindRoO(ItemSelectionConfig.FindConfig(nameof(ItemSelectionConfig.TierMultipliers), k), new AutoConfigRoOIntSliderAttribute("{0:N0}",1,10));

            ItemSelectionConfig.Bind(typeof(ItemSelectionConfigContainer).GetPropertyCached(nameof(ItemSelectionConfigContainer.TierWeights)), cfgFile, "ArtifactOfKnowledge.Selection", "Weights", new AutoConfigAttribute($"<AIC.DictKeyProp.{nameof(ItemTierDef.name)}>", "Weight for this item tier to be offered (higher = more often, relative to others).", AutoConfigFlags.BindDict | AutoConfigFlags.PreventNetMismatch, 0f, 1f));
            foreach(var k in ItemSelectionConfig.TierWeights.Keys)
                ItemSelectionConfig.BindRoO(ItemSelectionConfig.FindConfig(nameof(ItemSelectionConfig.TierWeights), k), new AutoConfigRoOSliderAttribute("{0:P0}", 0f, 1f));

            ItemSelectionConfig.Bind(typeof(ItemSelectionConfigContainer).GetPropertyCached(nameof(ItemSelectionConfigContainer.TierCaps)), cfgFile, "ArtifactOfKnowledge.Selection", "Caps", new AutoConfigAttribute($"<AIC.DictKeyProp.{nameof(ItemTierDef.name)}>", "Maximum number of this item tier to offer across the entire selection. 0 for unlimited.", AutoConfigFlags.BindDict | AutoConfigFlags.PreventNetMismatch, 0, int.MaxValue));
            foreach(var k in ItemSelectionConfig.TierCaps.Keys)
                ItemSelectionConfig.BindRoO(ItemSelectionConfig.FindConfig(nameof(ItemSelectionConfig.TierCaps), k), new AutoConfigRoOIntSliderAttribute("{0:N0}", 0, 30));

            ItemSelectionConfig.Bind(typeof(ItemSelectionConfigContainer).GetPropertyCached(nameof(ItemSelectionConfigContainer.TierUpgradeIntervals)), cfgFile, "ArtifactOfKnowledge.Selection", "Upgrade Intervals", new AutoConfigAttribute($"<AIC.DictKeyProp.{nameof(ItemTierDef.name)}>", "If nonzero, all offered items will be of this tier after this many levels. Longer intervals take precedence.", AutoConfigFlags.BindDict | AutoConfigFlags.PreventNetMismatch, 0, int.MaxValue));
            foreach(var k in ItemSelectionConfig.TierUpgradeIntervals.Keys)
                ItemSelectionConfig.BindRoO(ItemSelectionConfig.FindConfig(nameof(ItemSelectionConfig.TierUpgradeIntervals), k), new AutoConfigRoOIntSliderAttribute("{0:N0}", 0, 50));
        }

        void ModifyItemTierPrefabs(Color metaColor) {
            var highlight = R2API.PrefabAPI.InstantiateClone(Addressables.LoadAssetAsync<GameObject>("RoR2/Base/UI/HighlightLunarItem.prefab").WaitForCompletion(), "AKNOWTmpSetupPrefab", false);

            highlight.GetComponent<RoR2.UI.HighlightRect>().highlightColor = metaColor;

            highlight = R2API.PrefabAPI.InstantiateClone(highlight, "AKNOWMetaHighlight", false);
            MetaItemTier.highlightPrefab = highlight;


            var droplet = R2API.PrefabAPI.InstantiateClone(Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/LunarOrb.prefab").WaitForCompletion(), "AKNOWTmpSetupPrefab", false);

            var tr = droplet.transform.Find("VFX").GetComponent<TrailRenderer>();
            tr.colorGradient.colorKeys[0] = new GradientColorKey(metaColor, tr.colorGradient.colorKeys[0].time);

            var coreps = droplet.transform.Find("VFX/Core").GetComponent<ParticleSystem>();
            var ccol = coreps.colorOverLifetime;
            var ccolColor = ccol.color;
            ccolColor.gradientMax.colorKeys[0] = new GradientColorKey(metaColor, ccolColor.gradientMax.colorKeys[0].time);
            ccol.color = ccolColor;

            var pl = droplet.transform.Find("VFX/Point light").GetComponent<Light>();
            pl.color = metaColor;

            var pgps = droplet.transform.Find("VFX/PulseGlow (1)").GetComponent<ParticleSystem>();
            ccol = pgps.colorOverLifetime;
            ccolColor = ccol.color;
            ccolColor.gradientMax.colorKeys[0] = new GradientColorKey(metaColor, ccolColor.gradientMax.colorKeys[0].time);
            ccol.color = ccolColor;

            droplet = R2API.PrefabAPI.InstantiateClone(droplet, "AKNOWMetaDroplet", false);
            MetaItemTier.dropletDisplayPrefab = droplet;
        }

        private void UnstubShaders() {
            var materials = resources.LoadAllAssets<Material>();
            foreach(Material material in materials)
                if(material.shader.name.StartsWith("STUB_"))
                    material.shader = Addressables.LoadAssetAsync<Shader>(material.shader.name.Substring(5))
                        .WaitForCompletion();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity Engine.")]
        private void Start() {
            T2Module.SetupAll_PluginStart(earlyLoad, true);
            T2Module.SetupAll_PluginStart(allModules.Except(earlyLoad), true);
        }
    }
}
