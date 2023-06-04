using RoR2;
using RoR2.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UIVisibility = ThinkInvisible.ArtifactOfKnowledge.ArtifactOfKnowledgePlugin.ClientConfigContainer.UIVisibility;

namespace ThinkInvisible.ArtifactOfKnowledge {
    public class KnowledgeXpBarModule : TILER2.T2Module<KnowledgeXpBarModule> {

        public override bool managedEnable => false;

        public GameObject xpBarPrefab { get; private set; }

        public override void SetupAttributes() {
            base.SetupAttributes();

            xpBarPrefab = ArtifactOfKnowledgePlugin.resources.LoadAsset<GameObject>("Assets/ArtifactOfKnowledge/Prefabs/KnowledgeXpBar.prefab");

            var xpBar = xpBarPrefab.GetComponent<KnowledgeXpBar>();
            xpBar.unspentText.font = LegacyResourcesAPI.Load<TMP_FontAsset>("tmpfonts/misc/tmpsquaresboldhud");
            xpBar.unspentText.material = LegacyResourcesAPI.Load<Material>("tmpfonts/misc/tmpsquaresboldhud");
            xpBar.unspentTextToken.formatArgs = new object[] { 0, ArtifactOfKnowledgePlugin.ClientConfig.KeybindShowMenu.ToString() };
            xpBar.levelText.font = LegacyResourcesAPI.Load<TMP_FontAsset>("tmpfonts/misc/tmpsquaresboldhud");
            xpBar.levelText.material = LegacyResourcesAPI.Load<Material>("tmpfonts/misc/tmpsquaresboldhud");
        }
    }

    public class KnowledgeXpBar : MonoBehaviour {
        public RectTransform fillPanel;
        public AnimateUIAlpha fillPanelPulser;
        public GameObject pulser1;
        public GameObject pulser2;
        public Image capstoneImage;
        public HGTextMeshProUGUI unspentText;
        public LanguageTextMeshController unspentTextToken;
        public HGTextMeshProUGUI levelText;

        RectTransform pulserRtf1;
        Image pulserImage1;
        RectTransform pulserRtf2;
        Image pulserImage2;

        float targetFill = 0f;
        float fill = 0f;
        float fillV = 0f;

        public static void ModifyHud(HUD hud) {
            var existingBar = hud.transform.GetComponentInChildren<KnowledgeXpBar>();
            if(existingBar)
                GameObject.Destroy(existingBar.gameObject);

            if(ArtifactOfKnowledgePlugin.ClientConfig.XpBarLocation == ArtifactOfKnowledgePlugin.ClientConfigContainer.UICluster.Nowhere) return;

            bool isTopCenter = ArtifactOfKnowledgePlugin.ClientConfig.XpBarLocation == ArtifactOfKnowledgePlugin.ClientConfigContainer.UICluster.TopCenter;
            var targetCluster = hud.transform.Find(
                (isTopCenter)
                ? "MainContainer/MainUIArea/SpringCanvas/TopCenterCluster"
                : "MainContainer/MainUIArea/SpringCanvas/BottomLeftCluster/BarRoots");
            var xpBarObj = GameObject.Instantiate(KnowledgeXpBarModule.instance.xpBarPrefab, targetCluster);
            xpBarObj.name = "KnowledgeXpBar";
            var xpBar = xpBarObj.GetComponent<KnowledgeXpBar>();
            if(isTopCenter) {
                xpBarObj.transform.SetSiblingIndex(1);
            } else {
                xpBarObj.transform.SetSiblingIndex(1);
                var layout = xpBarObj.GetComponent<LayoutElement>();
                layout.preferredHeight = 6f;
                layout.minHeight = 4f;
                layout.preferredWidth = 420f;
                xpBar.unspentText.fontSize *= 0.8f;
                xpBar.capstoneImage.rectTransform.localScale = new Vector3(0.8f, 0.8f, 1f);
                xpBar.capstoneImage.rectTransform.localPosition -= new Vector3(4f, 0f, 0f);
                var lg = xpBarObj.transform.parent.GetComponent<VerticalLayoutGroup>();
            }

            switch(ArtifactOfKnowledgePlugin.ClientConfig.XpBarHintText) {
                case UIVisibility.Visible:
                    xpBar.unspentTextToken.token = "AKNOW_UNSPENT_HINT";
                    break;
                case UIVisibility.Subdued:
                    xpBar.unspentTextToken.token = "AKNOW_UNSPENT_HINTSUBDUED";
                    break;
                default:
                    xpBar.unspentTextToken.token = "AKNOW_UNSPENT";
                    break;
            }

            xpBar.SetFill(0, 0, 0);
        }

        void Awake() {
            pulserRtf1 = pulser1.GetComponent<RectTransform>();
            pulserImage1 = pulser1.GetComponent<Image>();
            pulserRtf2 = pulser2.GetComponent<RectTransform>();
            pulserImage2 = pulser2.GetComponent<Image>();
        }

        void Update() {
            fill = Mathf.SmoothDamp(fill, targetFill, ref fillV, 0.2f);

            fillPanel.anchorMin = new Vector2(0f, 0f);
            fillPanel.anchorMax = new Vector2(fill, 1f);
            fillPanel.sizeDelta = new Vector2(1f, 1f);

            if(pulser1.activeSelf) {
                var animProgress1 = (Time.time / 3f) % 1f;
                var animProgress2 = (Time.time / 3f + 0.5f) % 1f;

                pulserRtf1.sizeDelta = new Vector2(60f * animProgress1, 60f * animProgress1);
                pulserImage1.color = new Color(pulserImage1.color.r, pulserImage1.color.g, pulserImage1.color.b, (1f - animProgress1) * 0.2f);
                pulserRtf2.sizeDelta = new Vector2(60f * animProgress2, 60f * animProgress2);
                pulserImage2.color = new Color(pulserImage2.color.r, pulserImage2.color.g, pulserImage2.color.b, (1f - animProgress1) * 0.2f);
            }
        }

        public void SetFill(float frac, int unspent, int spent) {
            frac = Mathf.Clamp01(frac);
            targetFill = frac;

            if(unspent > 0) {
                if(ArtifactOfKnowledgePlugin.ClientConfig.XpBarUnspentFlashiness != UIVisibility.Hidden) {
                    if(ArtifactOfKnowledgePlugin.ClientConfig.XpBarUnspentFlashiness != UIVisibility.Subdued) {
                        pulser1.SetActive(true);
                        pulser2.SetActive(true);
                    }
                    fillPanelPulser.enabled = true;
                }
                unspentText.gameObject.SetActive(true);
                unspentTextToken.formatArgs = new object[] { unspent, ArtifactOfKnowledgePlugin.ClientConfig.KeybindShowMenu.ToString() };
                capstoneImage.sprite = KnowledgeArtifact.instance.iconResource;
            } else {
                fillPanelPulser.enabled = false;
                var img = fillPanel.GetComponent<Image>();
                img.color = new Color(img.color.r, img.color.g, img.color.b, 0.75f);
                pulser1.SetActive(false);
                pulser2.SetActive(false);
                unspentText.gameObject.SetActive(false);
                capstoneImage.sprite = KnowledgeArtifact.instance.iconResourceDisabled;
            }

            levelText.text = (unspent + spent).ToString();
        }
    }
}
