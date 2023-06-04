using RoR2;
using UnityEngine;
using System.Collections.ObjectModel;
using TILER2;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;

namespace ThinkInvisible.ArtifactOfKnowledge.Items {
    public class TidalPull : Item<TidalPull> {

        ////// Item Data //////

        public override ItemTier itemTier { get {
                return ArtifactOfKnowledgePlugin.MetaItemTier.tier;
            }
        } 
        public override bool itemIsAIBlacklisted { get; protected set; } = true;
        public override ReadOnlyCollection<ItemTag> itemTags => new ReadOnlyCollection<ItemTag>(new ItemTag[] { });

        protected override string[] GetDescStringArgs(string langID = null) => new string[] {
            LunarChanceBonus.ToString("0%")
        };



        ////// Config ///////

        [AutoConfigRoOSlider("{0:N1}%", 0f, 1f)]
        [AutoConfig("Weight for this item to appear in the upgrade menu, per slot.", AutoConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
        public float BaseDropChance { get; private set; } = 0.03f;

        [AutoConfigRoOIntSlider("{0:N0}", 0, 100)]
        [AutoConfig("Maximum count of this item before the upgrade menu will stop offering it. 0 for unlimited.", AutoConfigFlags.PreventNetMismatch, 0, int.MaxValue)]
        public int MaxCount { get; private set; } = 5;

        [AutoConfigRoOCheckbox()]
        [AutoConfig("If true, all Lunar Pod drops will be replaced with this item.", AutoConfigFlags.PreventNetMismatch)]
        public bool OverridePodDrops { get; private set; } = true;

        [AutoConfigRoOSlider("{0:P0}", 0f, 3f)]
        [AutoConfig("Amount of bonus multiplier to Lunar drop rate to apply per stack.", AutoConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
        public float LunarChanceBonus { get; private set; } = 0.5f;


        ////// Other Fields/Properties //////



        ////// TILER2 Module Setup //////
        public TidalPull() {
            modelResource = ArtifactOfKnowledgePlugin.resources.LoadAsset<GameObject>("Assets/ArtifactOfKnowledge/Prefabs/Items/TidalPull.prefab");
            iconResource = ArtifactOfKnowledgePlugin.resources.LoadAsset<Sprite>("Assets/ArtifactOfKnowledge/Textures/ItemIcons/TidalPull.png");
        }

        public override void SetupAttributes() {
            base.SetupAttributes();

            modelResource.transform.Find("TidalPullPlanet").gameObject.GetComponent<MeshRenderer>().material = Addressables.LoadAssetAsync<Material>("RoR2/Base/LunarBadLuck/matStarSeed.mat").WaitForCompletion();
            modelResource.transform.Find("TidalPullPlanet/TidalPullMoon").gameObject.GetComponent<MeshRenderer>().material = Addressables.LoadAssetAsync<Material>("RoR2/Base/bazaar/matBazaarPots.mat").WaitForCompletion();
            modelResource.transform.Find("TidalPullPlanet/TidalPullWater").gameObject.GetComponent<MeshRenderer>().material = Addressables.LoadAssetAsync<Material>("RoR2/Base/bazaar/matBazaarWater.mat").WaitForCompletion();
            modelResource = R2API.PrefabAPI.InstantiateClone(modelResource, "AKNOWTidalPullRendered", false);
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
            On.RoR2.ShopTerminalBehavior.SetPickupIndex += ShopTerminalBehavior_SetPickupIndex;
            RoR2.Inventory.onServerItemGiven += Inventory_onServerItemGiven;
        }

        public override void Uninstall() {
            base.Uninstall();

            KnowledgeCharacterManager.ModifyMaxOfAnyTier -= KnowledgeCharacterManager_ModifyMaxOfAnyTier;
            KnowledgeCharacterManager.ModifyItemTierWeights -= KnowledgeCharacterManager_ModifyItemTierWeights;
            KnowledgeCharacterManager.ModifyItemSuperSelection -= KnowledgeCharacterManager_ModifyItemSuperSelection;
            On.RoR2.ChestBehavior.Roll -= ChestBehavior_Roll;
            On.RoR2.ShopTerminalBehavior.SetPickupIndex -= ShopTerminalBehavior_SetPickupIndex;
            RoR2.Inventory.onServerItemGiven -= Inventory_onServerItemGiven;
        }



        ////// Hooks //////

        private void Inventory_onServerItemGiven(Inventory inv, ItemIndex ind, int count) {
            if(KnowledgeArtifact.instance.IsActiveAndEnabled() && ind == itemDef.itemIndex && KnowledgeCharacterManager.readOnlyInstancesByTarget.TryGetValue(inv.gameObject, out var kcm)) {
                kcm.ServerGrantRerolls(count);
            }
        }

        private void KnowledgeCharacterManager_ModifyItemTierWeights(KnowledgeCharacterManager sender, Dictionary<ItemTier, float> tierWeights) {
            var fac = 1f + LunarChanceBonus * GetCount(sender.targetMasterObject.GetComponent<CharacterMaster>());
            tierWeights[ItemTier.Lunar] *= fac;
        }

        private void KnowledgeCharacterManager_ModifyItemSuperSelection(KnowledgeCharacterManager sender, List<WeightedSelection<PickupIndex>.ChoiceInfo> superSelection) {
            if(MaxCount == 0 || GetCount(sender.targetMasterObject.GetComponent<CharacterMaster>()) < MaxCount)
                superSelection.Add(new WeightedSelection<PickupIndex>.ChoiceInfo { value = pickupIndex, weight = BaseDropChance });
        }

        private void KnowledgeCharacterManager_ModifyMaxOfAnyTier(KnowledgeCharacterManager sender, System.Collections.Generic.Dictionary<ItemTier[], int> maxOfAnyTier) {
            var lunarTierGroup = maxOfAnyTier.Where(kvp => kvp.Key.Contains(ItemTier.Lunar)).First().Key;
            maxOfAnyTier[lunarTierGroup] += GetCount(sender.targetMasterObject.GetComponent<CharacterMaster>());
        }

        private void ChestBehavior_Roll(On.RoR2.ChestBehavior.orig_Roll orig, ChestBehavior self) {
            orig(self);
            if(KnowledgeArtifact.instance.IsActiveAndEnabled() && this.enabled && OverridePodDrops && self.gameObject.name == "LunarChest(Clone)") {
                self.dropPickup = pickupIndex;
            }
        }

        private void ShopTerminalBehavior_SetPickupIndex(On.RoR2.ShopTerminalBehavior.orig_SetPickupIndex orig, ShopTerminalBehavior self, PickupIndex newPickupIndex, bool newHidden) {
            if(KnowledgeArtifact.instance.IsActiveAndEnabled() && this.enabled && OverridePodDrops && self.gameObject.name == "LunarShopTerminal(Clone)" && newPickupIndex != PickupIndex.none) {
                newPickupIndex = pickupIndex;
            }
            orig(self, newPickupIndex, newHidden);
        }
    }
}