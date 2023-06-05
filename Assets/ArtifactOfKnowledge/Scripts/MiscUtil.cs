using RoR2;

namespace ThinkInvisible.ArtifactOfKnowledge {
    internal static class MiscUtil {
        internal static bool IsVoid(this ItemTier tier) {
            return tier == ItemTier.VoidTier1 || tier == ItemTier.VoidTier2 || tier == ItemTier.VoidTier3 || tier == ItemTier.VoidBoss;
        }

        internal static bool HasVoidEquivalent(this ItemTier self, ItemTier other) {
            return (self == ItemTier.Tier1 && other == ItemTier.VoidTier1)
                || (self == ItemTier.Tier2 && other == ItemTier.VoidTier2)
                || (self == ItemTier.Tier3 && other == ItemTier.VoidTier3)
                || (self == ItemTier.Boss && other == ItemTier.VoidBoss);
        }

        internal static bool IsVoidEquivalent(this ItemTier self, ItemTier other) {
            return (other == ItemTier.Tier1 && self == ItemTier.VoidTier1)
                || (other == ItemTier.Tier2 && self == ItemTier.VoidTier2)
                || (other == ItemTier.Tier3 && self == ItemTier.VoidTier3)
                || (other == ItemTier.Boss && self == ItemTier.VoidBoss);
        }
    }
}
