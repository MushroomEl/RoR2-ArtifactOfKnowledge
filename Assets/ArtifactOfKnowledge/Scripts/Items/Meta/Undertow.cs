using RoR2;
using UnityEngine;
using System.Collections.ObjectModel;
using TILER2;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;

namespace ThinkInvisible.ArtifactOfKnowledge.Items {
    public class Undertow : Item<Undertow> {

        ////// Item Data //////

        public override ItemTier itemTier { get {
                return ArtifactOfKnowledgePlugin.MetaItemTier.tier;
            }
        } 
        public override bool itemIsAIBlacklisted { get; protected set; } = true;
        public override ReadOnlyCollection<ItemTag> itemTags => new ReadOnlyCollection<ItemTag>(new ItemTag[] { });

        protected override string[] GetDescStringArgs(string langID = null) => new string[] {
            VoidChanceBonus.ToString("0%")
        };



        ////// Config ///////

        [AutoConfigRoOSlider("{0:P1}", 0f, 1f)]
        [AutoConfig("Weight for this item to appear in the upgrade menu, per slot.", AutoConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
        public float BaseDropChance { get; private set; } = 0.03f;

        [AutoConfigRoOIntSlider("{0:N0}", 0, 100)]
        [AutoConfig("Maximum count of this item before the upgrade menu will stop offering it. 0 for unlimited.", AutoConfigFlags.PreventNetMismatch, 0, int.MaxValue)]
        public int MaxCount { get; private set; } = 5;

        [AutoConfigRoOCheckbox()]
        [AutoConfig("If true, all Void Chest and Void Triple drops will be replaced with this item.", AutoConfigFlags.PreventNetMismatch)]
        public bool OverridePodDrops { get; private set; } = true;

        [AutoConfigRoOSlider("{0:P0}", 0f, 3f)]
        [AutoConfig("Amount of bonus multiplier to Void drop rate to apply per stack.", AutoConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
        public float VoidChanceBonus { get; private set; } = 0.5f;


        ////// Other Fields/Properties //////



        ////// TILER2 Module Setup //////
        public Undertow() {
            modelResource = ArtifactOfKnowledgePlugin.resources.LoadAsset<GameObject>("Assets/ArtifactOfKnowledge/Prefabs/Items/Undertow.prefab");
            iconResource = ArtifactOfKnowledgePlugin.resources.LoadAsset<Sprite>("Assets/ArtifactOfKnowledge/Textures/ItemIcons/Undertow.png");
        }

        public override void SetupAttributes() {
            base.SetupAttributes();

            modelResource.transform.Find("UndertowModel").gameObject.GetComponent<MeshRenderer>().material = Addressables.LoadAssetAsync<Material>("RoR2/DLC1/matVoidFogEmitterGlass.mat").WaitForCompletion();
            modelResource.transform.Find("UndertowModel/VoidOrb").gameObject.GetComponent<MeshRenderer>().material = Addressables.LoadAssetAsync<Material>("RoR2/DLC1/matVoidFogEmitterMetal.mat").WaitForCompletion();
            modelResource = R2API.PrefabAPI.InstantiateClone(modelResource, "AKNOWUndertowRendered", false);
            /*var orbMesh = Addressables.LoadAssetAsync<Mesh>("RoR2/DLC1/mdlVoidFogEmitter.fbx").WaitForCompletion();
            modelResource.transform.Find("UndertowModel/VoidOrb").gameObject.GetComponent<MeshFilter>().mesh = orbMesh;*/
        }

        public override void SetupModifyItemDef() {
            base.SetupModifyItemDef();
            itemDef._itemTierDef = ArtifactOfKnowledgePlugin.MetaItemTier;
        }

        public override void Install() {
            base.Install();

            KnowledgeCharacterManager.ModifyMaxOfAnyTier += KnowledgeCharacterManager_ModifyMaxOfAnyTier;
            KnowledgeCharacterManager.ModifyItemTierWeights += KnowledgeCharacterManager_ModifyItemTierWeights;
            KnowledgeCharacterManager.ModifyItemSuperSelection += KnowledgeCharacterManager_ModifyItemSuperSelection;
            On.RoR2.ChestBehavior.Roll += ChestBehavior_Roll;
            On.RoR2.OptionChestBehavior.Roll += OptionChestBehavior_Roll;
            RoR2.Inventory.onServerItemGiven += Inventory_onServerItemGiven;
        }

        public override void Uninstall() {
            base.Uninstall();

            KnowledgeCharacterManager.ModifyMaxOfAnyTier -= KnowledgeCharacterManager_ModifyMaxOfAnyTier;
            KnowledgeCharacterManager.ModifyItemTierWeights -= KnowledgeCharacterManager_ModifyItemTierWeights;
            KnowledgeCharacterManager.ModifyItemSuperSelection -= KnowledgeCharacterManager_ModifyItemSuperSelection;
            On.RoR2.ChestBehavior.Roll -= ChestBehavior_Roll;
            On.RoR2.OptionChestBehavior.Roll -= OptionChestBehavior_Roll;
            RoR2.Inventory.onServerItemGiven -= Inventory_onServerItemGiven;
        }



        ////// Hooks //////

        private void Inventory_onServerItemGiven(Inventory inv, ItemIndex ind, int count) {
            if(KnowledgeArtifact.instance.IsActiveAndEnabled() && ind == itemDef.itemIndex && KnowledgeCharacterManager.readOnlyInstancesByTarget.TryGetValue(inv.gameObject, out var kcm)) {
                kcm.ServerGrantRerolls(count);
            }
        }

        private void KnowledgeCharacterManager_ModifyItemTierWeights(KnowledgeCharacterManager sender, Dictionary<ItemTier, float> tierWeights) {
            var fac = 1f + VoidChanceBonus * GetCount(sender.targetMasterObject.GetComponent<CharacterMaster>());
            tierWeights[ItemTier.VoidTier1] *= fac;
            tierWeights[ItemTier.VoidTier2] *= fac;
            tierWeights[ItemTier.VoidTier3] *= fac;
        }

        private void KnowledgeCharacterManager_ModifyItemSuperSelection(KnowledgeCharacterManager sender, List<WeightedSelection<PickupIndex>.ChoiceInfo> superSelection) {
            if(MaxCount == 0 || GetCount(sender.targetMasterObject.GetComponent<CharacterMaster>()) < MaxCount)
                superSelection.Add(new WeightedSelection<PickupIndex>.ChoiceInfo { value = pickupIndex, weight = BaseDropChance });
        }

        private void KnowledgeCharacterManager_ModifyMaxOfAnyTier(KnowledgeCharacterManager sender, System.Collections.Generic.Dictionary<ItemTier[], int> maxOfAnyTier) {
            var voidTierGroup = maxOfAnyTier.Where(kvp => kvp.Key.Contains(ItemTier.VoidTier1)).First().Key;
            maxOfAnyTier[voidTierGroup] += GetCount(sender.targetMasterObject.GetComponent<CharacterMaster>());
        }

        private void ChestBehavior_Roll(On.RoR2.ChestBehavior.orig_Roll orig, ChestBehavior self) {
            orig(self);
            if(KnowledgeArtifact.instance.IsActiveAndEnabled() && this.enabled && OverridePodDrops && self.gameObject.name == "VoidChest(Clone)") {
                self.dropPickup = pickupIndex;
            }
        }

        private void OptionChestBehavior_Roll(On.RoR2.OptionChestBehavior.orig_Roll orig, OptionChestBehavior self) {
            orig(self);
            if(KnowledgeArtifact.instance.IsActiveAndEnabled() && this.enabled && OverridePodDrops && self.gameObject.name == "VoidTriple(Clone)") {
                self.generatedDrops = new PickupIndex[] { pickupIndex };
            }
        }
    }
}