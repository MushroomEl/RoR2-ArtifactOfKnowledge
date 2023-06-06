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

        internal static ConfigFile cfgFile;

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

        public static ServerConfigContainer ServerConfig { get; private set; } = new ServerConfigContainer();
        public static ClientConfigContainer ClientConfig { get; private set; } = new ClientConfigContainer();

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
