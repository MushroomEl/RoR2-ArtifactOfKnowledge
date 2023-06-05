using R2API;
using RoR2;
using RoR2.UI;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using TILER2;
using UnityEngine;
using UnityEngine.UI;

namespace ThinkInvisible.ArtifactOfKnowledge {
	public class KnowledgePickerPanelModule : T2Module<KnowledgePickerPanelModule> {

		public override bool managedEnable => false;



		////// Config //////



		////// Other Fields/Properties //////

		public GameObject panelPrefab { get; private set; }



		////// TILER2 Module Setup //////

		public KnowledgePickerPanelModule() {
		}

		public override void SetupAttributes() {
			base.SetupAttributes();

			var tmpPanelSetup = LegacyResourcesAPI.Load<GameObject>("Prefabs/UI/ScrapperPickerPanel").InstantiateClone("AKnowTempSetupPrefab", false);

			//set label text & size
			var label = tmpPanelSetup.transform.Find("MainPanel/Juice/Label").GetComponent<HGTextMeshProUGUI>();
			label.text = "<b>Knowledge is Power</b>\r\n<i>Pick a new item or a gear swap (NYI).</i>\r\n\r\n- {UPTS} upgrade points // {RPTS} rerolls -";
			var labelTMC = tmpPanelSetup.transform.Find("MainPanel/Juice/Label").GetComponent<LanguageTextMeshController>();
			labelTMC.formatArgs = new object[] { 0, 0 };
			labelTMC.token = "AKNOW_PANEL_TEXT";

			//move container
			var container = tmpPanelSetup.transform.Find("MainPanel/Juice/IconContainer").gameObject;
			container.GetComponent<RectTransform>().offsetMin = new Vector2(64, 96);

			//create actions area
			var actionButtonContainer = new GameObject("ActionsContainer", typeof(RectTransform));
			actionButtonContainer.transform.parent = tmpPanelSetup.transform.Find("MainPanel/Juice");
			var actionContainerRtf = actionButtonContainer.GetComponent<RectTransform>();
			actionContainerRtf.anchorMax = new Vector2(0.8f, 0f);
			actionContainerRtf.anchorMin = new Vector2(0.2f, 0f);
			actionContainerRtf.pivot = new Vector2(0.5f, 0f);
			actionContainerRtf.offsetMin = new Vector2(0f, 8f);
			actionContainerRtf.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 48f);
			var actionContainerHlg = actionButtonContainer.AddComponent<HorizontalLayoutGroup>();
			actionContainerHlg.spacing = 2f;
			actionContainerHlg.padding = new RectOffset(2, 2, 2, 2);

			//move cancel button
			var cancelButton = tmpPanelSetup.transform.Find("MainPanel/Juice/CancelButton").gameObject;
			cancelButton.transform.SetParent(actionButtonContainer.transform, false);

			//create reroll button (also acts as template for further buttons)
			var rerollButton = GameObject.Instantiate(cancelButton, actionButtonContainer.transform);
			rerollButton.name = "RerollButton";
			var rerollButtonButton = rerollButton.GetComponent<HGButton>();
			for(var i = 0; i < rerollButtonButton.onClick.GetPersistentEventCount(); i++) {
				rerollButtonButton.onClick.SetPersistentListenerState(i, UnityEngine.Events.UnityEventCallState.Off);
			}
			var rerollLabel = rerollButton.GetComponent<LanguageTextMeshController>();
			rerollLabel.token = "AKNOW_PANEL_REROLL";

			//swap panel controller type
			var ppp = tmpPanelSetup.GetComponent<PickupPickerPanel>();
			var kpp = tmpPanelSetup.AddComponent<KnowledgePickerPanel>();
			kpp.coloredImages = ppp.coloredImages;
			kpp.darkColoredImages = ppp.darkColoredImages;
			kpp.buttonPrefab = ppp.buttonPrefab;
			kpp.buttonContainer = ppp.buttonContainer;
			kpp.layoutGroup = ppp.gridlayoutGroup;
			kpp.maxColumnCount = ppp.maxColumnCount;
			GameObject.Destroy(ppp);

			//add tooltip to template
			var ttp = kpp.buttonPrefab.AddComponent<TooltipProvider>();

			//finalize prefab setup
			panelPrefab = tmpPanelSetup.InstantiateClone("KnowledgeUpgradePickerPanel", false);
			GameObject.Destroy(tmpPanelSetup);
		}

		public override void Install() {
			base.Install();
		}

		public override void Uninstall() {
			base.Uninstall();
		}



		////// Hooks //////

	}

	[RequireComponent(typeof(RectTransform))]
	public class KnowledgePickerPanel : MonoBehaviour {
		public Action<int> onItemButtonPressed;
		public GridLayoutGroup layoutGroup;
		public RectTransform buttonContainer;
		public GameObject buttonPrefab;
		public Image[] coloredImages;
		public Image[] darkColoredImages;
		public int maxColumnCount;
		private UIElementAllocator<MPButton> buttonAllocator;

		private static readonly string[] voidDescTokens =
			Enumerable.Range(0, 6)
			.Select(i => $"AKNOW_VOID_DESC_{TextSerialization.ToStringInvariant(i)}")
			.ToArray();

		public KnowledgePickerPanel() {
		}

		private void Awake() {
			buttonAllocator = new UIElementAllocator<MPButton>(buttonContainer, buttonPrefab, true, false);
			buttonAllocator.onCreateElement = new UIElementAllocator<MPButton>.ElementOperationDelegate(OnCreateButton);
			layoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
			layoutGroup.constraintCount = maxColumnCount;
		}

		private void OnCreateButton(int index, MPButton button) {
			button.onClick.AddListener(() => onItemButtonPressed?.Invoke(index));
		}

		public void SetPickupOptions((PickupIndex index, Color borderColor)[] options) {
			buttonAllocator.AllocateElements(options.Length);
			ReadOnlyCollection<MPButton> elements = this.buttonAllocator.elements;
			if(options.Length > 0) {
				var averageBaseColor = options.Aggregate(new Color(0, 0, 0), (sum, pind) => sum + PickupCatalog.GetPickupDef(pind.index)?.baseColor ?? new Color(0, 0, 0)) / options.Length;
				var averageDarkColor = options.Aggregate(new Color(0, 0, 0), (sum, pind) => sum + PickupCatalog.GetPickupDef(pind.index)?.darkColor ?? new Color(0, 0, 0)) / options.Length;
				foreach(var img in coloredImages)
					img.color *= averageBaseColor;
				foreach(var img in darkColoredImages)
					img.color *= averageDarkColor;
			}
			var defaultColors = buttonPrefab.GetComponent<HGButton>().colors;
			for(int i = 0; i < options.Length; i++) {
				//setup navigation
				int row = i - i % maxColumnCount;
				int column = i % maxColumnCount;
				int neighborUpIndex = column - maxColumnCount;
				int neighborLeftIndex = column - 1;
				int neighborRightIndex = column + 1;
				int neighborDownIndex = column + maxColumnCount;

				var nav = elements[i].navigation;
				nav.mode = Navigation.Mode.Explicit;
				if(neighborLeftIndex >= 0) {
					var neighborLeft = elements[row + neighborLeftIndex];
					nav.selectOnLeft = neighborLeft;
				}
				if(neighborRightIndex < maxColumnCount && (row + neighborRightIndex) < options.Length) {
					var neighborRight = elements[row + neighborRightIndex];
					nav.selectOnRight = neighborRight;
				}
				if(row + neighborUpIndex >= 0) {
					var neighborUp = elements[row + neighborUpIndex];
					nav.selectOnUp = neighborUp;
				}
				if(row + neighborDownIndex < options.Length) {
					var neighborDown = elements[row + neighborDownIndex];
					nav.selectOnDown = neighborDown;
				}
				elements[i].navigation = nav;

				//retrieve item info
				var pdef = PickupCatalog.GetPickupDef(options[i].index);
				ItemDef idef = null;
				EquipmentDef edef = null;
				if(pdef != null) {
					if(pdef.itemIndex != ItemIndex.None)
						idef = ItemCatalog.GetItemDef(pdef.itemIndex);
					if(pdef.equipmentIndex != EquipmentIndex.None)
						edef = EquipmentCatalog.GetEquipmentDef(pdef.equipmentIndex);
				}

				//apply color & image
				var iconImage = elements[i].GetComponent<ChildLocator>().FindChild("Icon").GetComponent<Image>();
				if(pdef == null) {
					iconImage.sprite = null;
					iconImage.color = options[i].borderColor;
					elements[i].interactable = false;
				} else {
					iconImage.sprite = pdef.iconSprite;
					iconImage.color = Color.white;
					elements[i].interactable = pdef.itemIndex != ItemIndex.None || pdef.equipmentIndex != EquipmentIndex.None;
				}
				var hgButton = elements[i].GetComponent<HGButton>();
				var colorBlock = hgButton.colors;
				colorBlock.normalColor = Color.Lerp(defaultColors.normalColor, options[i].borderColor, 0.35f);
				colorBlock.highlightedColor = Color.Lerp(defaultColors.highlightedColor, options[i].borderColor, 0.35f);
				colorBlock.selectedColor = Color.Lerp(defaultColors.selectedColor, options[i].borderColor, 0.35f);
				colorBlock.pressedColor = Color.Lerp(defaultColors.pressedColor, options[i].borderColor, 0.35f);
				hgButton.colors = colorBlock;
				var bgImage = elements[i].GetComponent<Image>();
				//bgImage.color = Color.Lerp(bgImage.color, options[i].borderColor, 0.15f);

				//apply tooltip
				var tooltipProvider = elements[i].GetComponent<TooltipProvider>();
				var tooltipContent = default(TooltipContent);
				if(pdef == null) {
					tooltipContent.titleToken = "AKNOW_VOID_NAME";
					tooltipContent.titleColor = new Color(0f, 0f, 0f);
					tooltipContent.bodyColor = new Color(0f, 0f, 0f);
					tooltipContent.bodyToken = voidDescTokens[UnityEngine.Random.Range(0, voidDescTokens.Length)];
				} else {
					tooltipContent.titleToken = pdef.nameToken;
					tooltipContent.titleColor = pdef.darkColor;
					tooltipContent.bodyColor = pdef.darkColor;
					if(idef)
						tooltipContent.bodyToken = idef.pickupToken;
					else if(edef)
						tooltipContent.bodyToken = edef.pickupToken;
					else
						tooltipContent.bodyToken = pdef.interactContextToken;
				}
				tooltipProvider.SetContent(tooltipContent);
			}
		}

		public void AddActionButton(string name, string token, Action onClick) {
			var container = this.gameObject.transform.Find("MainPanel/Juice/ActionsContainer");
			var buttonTemplate = this.gameObject.transform.Find("MainPanel/Juice/ActionsContainer/RerollButton").gameObject;
			var newButton = GameObject.Instantiate(buttonTemplate, container.transform);
			newButton.name = name;
			var buttonLTMC = newButton.GetComponent<LanguageTextMeshController>();
			buttonLTMC.token = token;
			newButton.GetComponent<HGButton>().onClick.AddListener(() => { onClick?.Invoke(); });
		}

		public void SetActionButtonFormatArgs(string name, params object[] args) {
			var buttonTsf = this.gameObject.transform.Find($"MainPanel/Juice/ActionsContainer/{name}");
			if(!buttonTsf) return;
			var buttonLTMC = buttonTsf.GetComponent<LanguageTextMeshController>();
			buttonLTMC.formatArgs = args;
		}
	}
}
