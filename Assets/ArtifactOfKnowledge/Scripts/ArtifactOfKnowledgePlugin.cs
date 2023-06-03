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

namespace ThinkInvisible.ArtifactOfKnowledge {
    [BepInPlugin(ModGuid, ModName, ModVer)]
    [BepInDependency(R2API.R2API.PluginGUID, R2API.R2API.PluginVersion)]
    [BepInDependency(TILER2Plugin.ModGuid, TILER2Plugin.ModVer)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    public class ArtifactOfKnowledgePlugin : BaseUnityPlugin {
        public const string ModVer = "1.0.0";
        public const string ModName = "ArtifactOfKnowledge";
        public const string ModGuid = "com.ThinkInvisible.ArtifactOfKnowledge";

        private static ConfigFile cfgFile;

        internal static FilingDictionary<T2Module> allModules = new FilingDictionary<T2Module>();

        internal static BepInEx.Logging.ManualLogSource _logger;

        internal static AssetBundle resources;

        T2Module[] earlyLoad;

        public class ClientConfig : AutoConfigContainer {
            [AutoConfig("Press to show the upgrade menu while Artifact of Knowledge is active.", AutoConfigFlags.None)]
            [AutoConfigRoOKeybind()]
            public KeyboardShortcut KeybindShowMenu { get; internal set; } = new KeyboardShortcut(KeyCode.U);

            public enum UICluster { TopCenter, BottomLeft, Nowhere }

            [AutoConfig("Controls where the upgrade experience UI displays.", AutoConfigFlags.None)]
            [AutoConfigRoOChoice()]
            public UICluster XpBarLocation { get; internal set; } = UICluster.BottomLeft;
        }

        public class ServerConfig : AutoConfigContainer {
            [AutoConfig("Number of rerolls required to banish an item. If 0, manual banishment is disabled. NYI!", AutoConfigFlags.PreventNetMismatch, 0, int.MaxValue)]
            [AutoConfigRoOIntSlider("{0:N0}", 0, 10)]
            public int BanishCost { get; internal set; } = 2;

            [AutoConfig("Weight for each offered item to be Common item. May be upgraded to Uncommon/Rare by relevant LevelInterval settings.", AutoConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
            [AutoConfigRoOSlider("{0:P0}", 0f, 1f)]
            public float BaseT1Chance { get; internal set; } = 1f;

            [AutoConfig("Weight for each offered item to be an Uncommon item. Does not apply while UncommonLevelInterval/RareLevelInterval is overriding selection.", AutoConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
            [AutoConfigRoOSlider("{0:P0}", 0f, 1f)]
            public float BaseT2Chance { get; internal set; } = 0f;

            [AutoConfig("Weight for each offered item to be a Rare item. Does not apply while UncommonLevelInterval/RareLevelInterval is overriding selection.", AutoConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
            [AutoConfigRoOSlider("{0:P0}", 0f, 1f)]
            public float BaseT3Chance { get; internal set; } = 0f;

            [AutoConfig("Weight for each offered item to be a Void item. Stacks with BaseT2Chance, UncommonLevelInterval, etc.", AutoConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
            [AutoConfigRoOSlider("{0:P0}", 0f, 1f)]
            public float BaseVoidChance { get; internal set; } = 0.06f;

            [AutoConfig("Weight for each offered item to be a Lunar item.", AutoConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
            [AutoConfigRoOSlider("{0:P0}", 0f, 1f)]
            public float BaseLunarChance { get; internal set; } = 0.04f;

            [AutoConfig("Maximum number of Void items to offer per selection.", AutoConfigFlags.PreventNetMismatch, 0, int.MaxValue)]
            [AutoConfigRoOIntSlider("{0:N0}", 0, 30)]
            public int BaseMaxVoid { get; internal set; } = 2;

            [AutoConfig("Maximum number of Lunar items to offer per selection.", AutoConfigFlags.PreventNetMismatch, 0, int.MaxValue)]
            [AutoConfigRoOIntSlider("{0:N0}", 0, 30)]
            public int BaseMaxLunar { get; internal set; } = 1;

            [AutoConfig("Weight for each offered gear to be an equipment.", AutoConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
            [AutoConfigRoOSlider("{0:P0}", 0f, 1f)]
            public float BaseEquipChance { get; internal set; } = 1f;

            [AutoConfig("Weight for each offered gear to be a Lunar equipment.", AutoConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
            [AutoConfigRoOSlider("{0:P0}", 0f, 1f)]
            public float BaseLunarEquipChance { get; internal set; } = 0.03f;

            [AutoConfig("Levels which are a multiple of this number will offer Uncommon-tier items (default is Common). 0 to disable level-dependent upgrading.", AutoConfigFlags.PreventNetMismatch, 0, int.MaxValue)]
            [AutoConfigRoOIntSlider("{0:N0}", 0, 30)]
            public int UncommonLevelInterval { get; internal set; } = 5;

            [AutoConfig("Levels which are a multiple of this number will offer Rare-tier items (default is Common), taking precedence over UncommonLevelInterval. 0 to disable level-dependent upgrading.", AutoConfigFlags.PreventNetMismatch, 0, int.MaxValue)]
            [AutoConfigRoOIntSlider("{0:N0}", 0, 30)]
            public int RareLevelInterval { get; internal set; } = 15;

            [AutoConfig("Experience required for the first upgrade level. Vanilla level system uses 20.", AutoConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
            [AutoConfigRoOSlider("{0:N0}", 0f, 100f)]
            public float StartingXp { get; internal set; } = 8f;

            [AutoConfig("Experience scaling rate for upgrade levels. Vanilla level system uses 1.55.", AutoConfigFlags.PreventNetMismatch, 1f, float.MaxValue)]
            [AutoConfigRoOSlider("{0:N0}", 1.01f, 10f)]
            public float XpScaling { get; internal set; } = 1.4f;

            [AutoConfig("If true, one each of the offered items will always be Damage-, Utility-, and Healing-related.", AutoConfigFlags.PreventNetMismatch)]
            [AutoConfigRoOCheckbox()]
            public bool GuaranteeCategories { get; internal set; } = true;

            [AutoConfig("How many random items are offered at once by the upgrade menu.", AutoConfigFlags.PreventNetMismatch, 1, int.MaxValue)]
            [AutoConfigRoOIntSlider("{0:N0}", 1, 30)]
            public int SelectionSize { get; internal set; } = 5;

            [AutoConfig("How many random gear swaps are offered at once by the upgrade menu.", AutoConfigFlags.PreventNetMismatch, 1, int.MaxValue)]
            [AutoConfigRoOIntSlider("{0:N0}", 1, 30)]
            public int GearSelectionSize { get; internal set; } = 2;

            [AutoConfig("Number of rerolls granted at the start of a run.", AutoConfigFlags.PreventNetMismatch, 0, int.MaxValue)]
            [AutoConfigRoOIntSlider("{0:N0}", 0, 50)]
            public int StartingRerolls { get; internal set; } = 10;

            [AutoConfig("Number of rerolls granted for every cleared teleporter.", AutoConfigFlags.PreventNetMismatch, 0, int.MaxValue)]
            [AutoConfigRoOIntSlider("{0:N0}", 0, 10)]
            public int RerollsPerStage { get; internal set; } = 3;
        }

        public static ServerConfig serverConfig = new ServerConfig();
        public static ClientConfig clientConfig = new ClientConfig();

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

            serverConfig.BindAll(cfgFile, "Artifact of Knowledge", "Server");
            clientConfig.BindAll(cfgFile, "Artifact of Knowledge", "Client");

            clientConfig.ConfigEntryChanged += (newValueBoxed, eventArgs) => {
                if(eventArgs.target.boundProperty.Name == nameof(ClientConfig.XpBarLocation) && Run.instance && KnowledgeArtifact.instance.IsActiveAndEnabled()) {
                    foreach(var hud in GameObject.FindObjectsOfType<RoR2.UI.HUD>()) {
                        KnowledgeXpBar.ModifyHud(hud);
                    }
                    foreach(var kcm in GameObject.FindObjectsOfType<KnowledgeCharacterManager>()) {
                        kcm.ClientUpdateXpBar();
                    }
                }
            };

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
