using RoR2;
using System;
using TILER2;
using UnityEngine;
using UnityEngine.Networking;
using XpSource = ThinkInvisible.ArtifactOfKnowledge.ArtifactOfKnowledgePlugin.XpScalingConfig.XpSource;

namespace ThinkInvisible.ArtifactOfKnowledge {
    public class KnowledgeArtifact : Artifact<KnowledgeArtifact> {

        public override bool managedEnable => false;

        ////// Config //////



        ////// Other Fields/Properties //////



        ////// TILER2 Module Setup //////

        public KnowledgeArtifact() {
            iconResource = ArtifactOfKnowledgePlugin.resources.LoadAsset<Sprite>("Assets/ArtifactOfKnowledge/Textures/knowledge_on.png");
            iconResourceDisabled = ArtifactOfKnowledgePlugin.resources.LoadAsset<Sprite>("Assets/ArtifactOfKnowledge/Textures/knowledge_off.png");
        }

        public override void SetupAttributes() {
            base.SetupAttributes();
        }

        public override void Install() {
            base.Install();

            On.RoR2.Run.OnServerCharacterBodySpawned += Run_OnServerCharacterBodySpawned;
            RoR2.Run.onRunDestroyGlobal += Run_onRunDestroyGlobal;
            RoR2.TeleporterInteraction.onTeleporterChargedGlobal += TeleporterInteraction_onTeleporterChargedGlobal;
            SceneDirector.onPrePopulateSceneServer += OnPrePopulateSceneServer;
            SceneDirector.onGenerateInteractableCardSelection += OnGenerateInteractableCardSelection;
            DirectorCardCategorySelection.calcCardWeight += CalcCardWeight;
            On.RoR2.UI.HUD.Awake += HUD_Awake;
            On.RoR2.TeamManager.GiveTeamExperience += TeamManager_GiveTeamExperience;
            RoR2.GlobalEventManager.onCharacterDeathGlobal += GlobalEventManager_onCharacterDeathGlobal;
            On.RoR2.BossGroup.DropRewards += BossGroup_DropRewards;
        }

        public override void Uninstall() {
            base.Uninstall();

            On.RoR2.Run.OnServerCharacterBodySpawned -= Run_OnServerCharacterBodySpawned;
            RoR2.Run.onRunDestroyGlobal -= Run_onRunDestroyGlobal;
            RoR2.TeleporterInteraction.onTeleporterChargedGlobal -= TeleporterInteraction_onTeleporterChargedGlobal;
            SceneDirector.onPrePopulateSceneServer -= OnPrePopulateSceneServer;
            SceneDirector.onGenerateInteractableCardSelection -= OnGenerateInteractableCardSelection;
            DirectorCardCategorySelection.calcCardWeight -= CalcCardWeight;
            On.RoR2.UI.HUD.Awake -= HUD_Awake;
            On.RoR2.TeamManager.GiveTeamExperience -= TeamManager_GiveTeamExperience;
            RoR2.GlobalEventManager.onCharacterDeathGlobal -= GlobalEventManager_onCharacterDeathGlobal;
            On.RoR2.BossGroup.DropRewards -= BossGroup_DropRewards;
        }



        ////// Hooks //////

        private void HUD_Awake(On.RoR2.UI.HUD.orig_Awake orig, RoR2.UI.HUD self) {
            orig(self);
            if(IsActiveAndEnabled())
                KnowledgeXpBar.ModifyHud(self);
        }

        private void Run_OnServerCharacterBodySpawned(On.RoR2.Run.orig_OnServerCharacterBodySpawned orig, Run self, CharacterBody characterBody) {
            orig(self, characterBody);
            if(!IsActiveAndEnabled() || !NetworkServer.active || !characterBody || !characterBody.master || characterBody.teamComponent.teamIndex != TeamIndex.Player || !characterBody.isPlayerControlled) return;
            var master = characterBody.master;
            if(KnowledgeCharacterManager.readOnlyInstancesByTarget.ContainsKey(master.gameObject)) return;
            var kcm = GameObject.Instantiate(KnowledgeCharacterManagerModule.instance.managerPrefab);
            var cao = master.GetComponent<NetworkIdentity>().clientAuthorityOwner;
            if(cao == null) {
                NetworkServer.Spawn(kcm);
            } else {
                NetworkServer.SpawnWithClientAuthority(kcm, cao);
            }
            var kcmCpt = kcm.GetComponent<KnowledgeCharacterManager>();
            kcmCpt.ServerGrantRerolls(ArtifactOfKnowledgePlugin.serverConfig.StartingRerolls);
            kcmCpt.ServerAssignAndStart(master.gameObject);
        }

        private void Run_onRunDestroyGlobal(Run obj) {
            foreach(var kcm in GameObject.FindObjectsOfType<KnowledgeCharacterManager>()) {
                GameObject.Destroy(kcm.gameObject);
            }
        }

        private void TeleporterInteraction_onTeleporterChargedGlobal(TeleporterInteraction obj) {
            if(!NetworkServer.active) return;
            foreach(var kcm in GameObject.FindObjectsOfType<KnowledgeCharacterManager>()) {
                kcm.ServerGrantRerolls(ArtifactOfKnowledgePlugin.serverConfig.RerollsPerStage);
            }
        }

        private void CalcCardWeight(DirectorCard card, ref float weight) {
            if(!IsActiveAndEnabled() || RunArtifactManager.instance.IsArtifactEnabled(RoR2Content.Artifacts.Sacrifice)) return; //sacrifice performs same code
            var isc = card.spawnCard as InteractableSpawnCard;
            if(isc != null) weight *= isc.weightScalarWhenSacrificeArtifactEnabled;
        }

        private void OnGenerateInteractableCardSelection(SceneDirector sceneDirector, DirectorCardCategorySelection dccs) {
            if(!IsActiveAndEnabled() || RunArtifactManager.instance.IsArtifactEnabled(RoR2Content.Artifacts.Sacrifice)) return; //sacrifice performs same code
            dccs.RemoveCardsThatFailFilter((card) => {
                var isc = card.spawnCard as InteractableSpawnCard;
                return isc == null || !isc.skipSpawnWhenSacrificeArtifactEnabled;
            });
        }

        private void OnPrePopulateSceneServer(SceneDirector sceneDirector) {
            if(!IsActiveAndEnabled() || RunArtifactManager.instance.IsArtifactEnabled(RoR2Content.Artifacts.Sacrifice)) return; //sacrifice performs same code
            sceneDirector.onPopulateCreditMultiplier *= 0.5f;
        }

        private void GlobalEventManager_onCharacterDeathGlobal(DamageReport obj) {
            if(NetworkServer.active && IsActiveAndEnabled() && obj.attackerTeamIndex == TeamIndex.Player && ArtifactOfKnowledgePlugin.xpScalingConfig.Source == XpSource.Kills) {
                foreach(var kcm in GameObject.FindObjectsOfType<KnowledgeCharacterManager>()) {
                    kcm.ServerAddXp(1f);
                }
            }
        }

        private void TeamManager_GiveTeamExperience(On.RoR2.TeamManager.orig_GiveTeamExperience orig, TeamManager self, TeamIndex teamIndex, ulong experience) {
            orig(self, teamIndex, experience);
            if(NetworkServer.active && IsActiveAndEnabled() && teamIndex == TeamIndex.Player && (ArtifactOfKnowledgePlugin.xpScalingConfig.Source == XpSource.LevelXp || !Enum.IsDefined(typeof(XpSource), ArtifactOfKnowledgePlugin.xpScalingConfig))) {
                foreach(var kcm in GameObject.FindObjectsOfType<KnowledgeCharacterManager>()) {
                    kcm.ServerAddXp(experience);
                }
            }
        }

        private void BossGroup_DropRewards(On.RoR2.BossGroup.orig_DropRewards orig, BossGroup self) {
            if(IsActiveAndEnabled() && TeleporterInteraction.instance && TeleporterInteraction.instance.bossGroup == self && ArtifactOfKnowledgePlugin.xpScalingConfig.ConvertTeleporterDrops) {
                var xp = ArtifactOfKnowledgePlugin.xpScalingConfig.TeleporterDropXp * (self.bonusRewardCount + 1) / (self.scaleRewardsByPlayerCount ? 1f : Run.instance.participatingPlayerCount);
                if(xp > 0f)
                    foreach(var kcm in GameObject.FindObjectsOfType<KnowledgeCharacterManager>()) {
                        kcm.ServerAddXp(kcm.xpToNextLevel * xp);
                    }
            } else orig(self);
        }
    }
}