using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using System.Threading;
using System.Text.RegularExpressions;

namespace extraltodeuslExpRandPlugin {
	public class ExpressionRandomizer : MVRScript {
        string[] bodyRegion =
        {
            "Arms",
            "Body",
            "Chest",
            "Finger",
            "Hip",
            "Legs",
            "Neck",
            "Feet",
            "Waist",
						"Eyes Closed Left",
            "Eyes Closed Right",
						"Mouth Smile Simple Left",
						"Mouth Smile Simple Right"
        };

        string[] defaultOn =
        {
            "Brow Outer Up Left",
            "Brow Outer Up Right",
						"Brow Inner Up Left",
            "Brow Inner Up Right",
            "Concentrate",
						"Desire",
            "Flirting",
						"Pupils Dialate",
						"Snarl Left",
            "Snarl Right",
						"Eyes Squint Left",
            "Eyes Squint Right",
						"Lips Pucker"
        };
				float[] defaultSliders = {1f,1f};

				string[] preset1 =
				{
						"Brow Inner Up Left",
						"Brow Inner Up Right",
						"01-Extreme Pleasure",
						"Concentrate",
						"Confused",
						"Pain",
						"Eyes Squint Left",
						"Eyes Squint Right",
						"Surprise",
						"Mouth Smile Simple",
						"Pupils Dialate"
				};
				float[] preset1Sliders = {1.6f,3f};

				string[] preset2 =
				{
						"Brow Inner Up Left",
						"Brow Inner Up Right",
						"Brow Outer Down Left",
						"Brow Outer Down Right",
						"Brow Squeeze",
						"Eyes Closed",
						"Eyes Squint Left",
						"Eyes Squint Right",
				};
				float[] preset2Sliders = {1.8f,4.2f};

				// particular morph names to add
				string[] tailorList =
				{
					"Pupils Dialate",
					"Eye Roll Back_DD"
				};

        string[] poseRegion =
        {
            "Pose",
            "Expressions"
        };

        private JSONStorableBool filterAndSearch;
        private JSONStorableBool onlyShowActive;
        private InputField filterInputField;
        Dictionary<string, float>  initialMorphValues   = new Dictionary<string, float>();
        Dictionary<string, float>  newMorphValues       = new Dictionary<string, float>();
        Dictionary<string, float>  CurrentMorphsValues  = new Dictionary<string, float>();
        Dictionary<string, UIDynamicToggle> toggles = new Dictionary<string, UIDynamicToggle>();
        Dictionary<string, UIDynamicButton> buttons = new Dictionary<string, UIDynamicButton>();
        Dictionary<string, string> toggleRelations = new Dictionary<string, string>();
		Dictionary<string, UIDynamicToggle> togglesOn;

		protected JSONStorableFloat masterSpeedSlider;
        protected JSONStorableFloat minSlider;
        protected JSONStorableFloat maxSlider;
        protected JSONStorableFloat multiSlider;
        protected JSONStorableFloat animLengthSlider;
        protected JSONStorableFloat animWaitSlider;
		protected JSONStorableBool  smoothToggle;
		protected JSONStorableBool  manualToggle;
		protected JSONStorableBool  randomToggle;
		protected JSONStorableBool  abaToggle;
        protected JSONStorableBool  play;

				protected JSONStorableStringChooser colliderStringChooser;

				protected JSONStorableAction manualTrigger;
				protected JSONStorableFloat triggerChanceSlider;
				List<string> colliderChoices;

        protected float timer;
        protected float forceTimer;

				JSONStorableString morphList;

				GenerateDAZMorphsControlUI returnMorphs() {
					JSONStorable geometry = containingAtom.GetStorableByID("geometry");
					DAZCharacterSelector character = geometry as DAZCharacterSelector;
					return character.morphsControlUI;
				}

        protected void UpdateRandomParams(){
					if (UnityEngine.Random.Range(0f,100f) <= triggerChanceSlider.val || !randomToggle.val){
						GenerateDAZMorphsControlUI morphControl = returnMorphs();
						// define the random values to switch to
						foreach(KeyValuePair<string, UIDynamicToggle> entry in togglesOn){
							string name = entry.Key;
							if (name != "Play"){
								DAZMorph morph = morphControl.GetMorphByDisplayName(name);
								if (morph.animatable == true){
									if (CurrentMorphsValues[name] > 0.1f && abaToggle.val){
										newMorphValues[name] = 0;
									}else{
										float valeur = UnityEngine.Random.Range(minSlider.val, maxSlider.val) * multiSlider.val;
										newMorphValues[name] = valeur;
									}
								}
							}
						}
					}
        }

				void Update() {
					if (toggles["Play"].toggle.isOn){
						timer += Time.deltaTime;
						if (timer >= animWaitSlider.val/masterSpeedSlider.val)
						{
							timer = 0;
							if (!manualToggle.val)
								UpdateRandomParams();
						}
					}
				}

        protected void FixedUpdate()
        {
            if (toggles["Play"].toggle.isOn)
            {
              GenerateDAZMorphsControlUI morphControl = returnMorphs();
              // morph progressively every morphs to their new values
							string textBoxMessage = "\n Animatable morphs (not animated) :\n";
							string animatableSelected = textBoxMessage;
							foreach(KeyValuePair<string, UIDynamicToggle> entry in togglesOn)
							{
								string name = entry.Key;
								if (name != "Play")
								{
									DAZMorph morph = morphControl.GetMorphByDisplayName(name);
									if (morph.animatable == true && morph != null)
									{
										float valeur = 0;
										if (smoothToggle.val){
											valeur = Mathf.Lerp(CurrentMorphsValues[name], newMorphValues[name], Time.deltaTime * animLengthSlider.val * masterSpeedSlider.val * Mathf.Sin(timer/(animWaitSlider.val/masterSpeedSlider.val)*Mathf.PI));
										}else{
											valeur = Mathf.Lerp(CurrentMorphsValues[name], newMorphValues[name], Time.deltaTime * animLengthSlider.val * masterSpeedSlider.val);
										}
										CurrentMorphsValues[name] = morph.morphValue = valeur;
									}else{
										animatableSelected = animatableSelected+" "+name+"\n";
									}
								}
							}
							if (animatableSelected != textBoxMessage || morphList.val != textBoxMessage)
								morphList.val = animatableSelected;
            }
        }

        private void UpdateInitialMorphs(){
            GenerateDAZMorphsControlUI morphControl = returnMorphs();
            morphControl.GetMorphDisplayNames().ForEach((name) =>{
                if (toggles.ContainsKey(name))
                    initialMorphValues[name] = morphControl.GetMorphByDisplayName(name).morphValue;
            });
        }

        private void UpdateNewMorphs(){
            GenerateDAZMorphsControlUI morphControl = returnMorphs();
            morphControl.GetMorphDisplayNames().ForEach((name) =>{
                if (toggles.ContainsKey(name))
                    newMorphValues[name] = morphControl.GetMorphByDisplayName(name).morphValue;
            });
        }

        private void UpdateCurrentMorphs(){
            GenerateDAZMorphsControlUI morphControl = returnMorphs();
            morphControl.GetMorphDisplayNames().ForEach((name) =>{
                    CurrentMorphsValues[name] = morphControl.GetMorphByDisplayName(name).morphValue;
            });
        }

        private void ResetMorphs(){
            GenerateDAZMorphsControlUI morphControl = returnMorphs();
            morphControl.GetMorphDisplayNames().ForEach((name) =>{
                if (toggleRelations.ContainsKey(name)){
                    if (toggles.ContainsKey(name)){
                        DAZMorph morph = morphControl.GetMorphByDisplayName(name);
                        morph.morphValue = initialMorphValues[name];
                    }
                }
            });
        }

        private void ZeroMorphs(){
            GenerateDAZMorphsControlUI morphControl = returnMorphs();
            morphControl.GetMorphDisplayNames().ForEach((name) => {
                if (toggleRelations.ContainsKey(name)) {
                    if (toggles.ContainsKey(name) && toggles[name].toggle.isOn){
                        DAZMorph morph = morphControl.GetMorphByDisplayName(name);
                        morph.morphValue = 0;
                    }
                }
            });
        }

				// Function taken from VAMDeluxe's code :)
				static JSONStorable GetPluginStorableById(Atom atom, string id){
						string storableIdName = atom.GetStorableIDs().FirstOrDefault((string storeId) =>{
								if (string.IsNullOrEmpty(storeId)){
										return false;
								}
								return storeId.Contains(id);
						});
						if (storableIdName == null){
								return null;
						}
						return atom.GetStorableByID(storableIdName);
				}


				// Thanks to VRStudy for helping for the trigger-related functions !!
				void cleanTriggers() {
					foreach(string triggerName in colliderChoices) {
						if (triggerName != colliderStringChooser.val && triggerName != "None"){
							clearTriggers(triggerName);
						}
					}
				}

				void clearTriggers(string triggerName) {
					CollisionTrigger trig = containingAtom.GetStorableByID(triggerName) as CollisionTrigger;
					JSONClass trigClass = trig.trigger.GetJSON();
					JSONArray trigArray = trigClass["startActions"].AsArray;
					for (int i=0;i< trigArray.Count;i++) {
				    if (trigArray[i]["name"].Value == "ExpRandTrigger") {
				        trigArray.Remove(i);
				    }
					}
					trig.trigger.RestoreFromJSON(trigClass);
				}

				bool checkIfTriggerExists(CollisionTrigger trig) {
					JSONNode presentTriggers = trig.trigger.GetJSON();
					JSONArray asArray = presentTriggers["startActions"].AsArray;
					for (int i = 0; i < asArray.Count; i++) {
							JSONClass asObject = asArray[i].AsObject;
							string name = asObject["name"];
							if (name == "ExpRandTrigger" && asObject["receiver"] != null){
								return true;
							}
					}
					return false;
				}

				void createTrigger(string triggerName){
					CollisionTrigger trig = containingAtom.GetStorableByID(triggerName) as CollisionTrigger;
					if (!checkIfTriggerExists(trig)){
						if(trig!=null) {
							trig.enabled = true;
							TriggerActionDiscrete startTrigger;
							startTrigger=trig.trigger.CreateDiscreteActionStartInternal();
							startTrigger.name = "ExpRandTrigger";
							startTrigger.receiverAtom = containingAtom;
							startTrigger.receiver = GetPluginStorableById(GetContainingAtom(),"ExpressionRandomizer");
							startTrigger.receiverTargetName = "Trigger transition";
						}
					}
				}

				void triggerMaintainer() {
					if (colliderStringChooser.val != "None"){
						createTrigger(colliderStringChooser.val);
						cleanTriggers();
					}
				}

				void enableManualMode(){
					manualToggle.val = true;
					randomToggle.val = true;
				}

				void presetSliders(float[] values){
					multiSlider.val = values[0];
					masterSpeedSlider.val = values[1];
				}

        public override void Init() {
					try {
								GetContainingAtom().GetStorableByID("AutoExpressions").SetBoolParamValue("enabled", false);

                UpdateInitialMorphs();
                UpdateNewMorphs();
                UpdateCurrentMorphs();

                #region Sliders
                minSlider = new JSONStorableFloat("Minimum value", -0.15f, -1f, 1.0f, true);
                minSlider.storeType = JSONStorableParam.StoreType.Full;
                RegisterFloat(minSlider);
                CreateSlider(minSlider, false);

                maxSlider = new JSONStorableFloat("Maximum value", 0.35f, -1f, 1.0f, true);
                maxSlider.storeType = JSONStorableParam.StoreType.Full;
                RegisterFloat(maxSlider);
                CreateSlider(maxSlider, false);

                multiSlider = new JSONStorableFloat("Multiplier", 1f, 0f, 2f, true);
                multiSlider.storeType = JSONStorableParam.StoreType.Full;
                RegisterFloat(multiSlider);
                CreateSlider(multiSlider, false);

								masterSpeedSlider = new JSONStorableFloat("Master speed", 1f, 0f, 10f, true);
								masterSpeedSlider.storeType = JSONStorableParam.StoreType.Full;
								RegisterFloat(masterSpeedSlider);
								CreateSlider(masterSpeedSlider, false);
                #endregion

                #region Region Buttons Preparation
                GenerateDAZMorphsControlUI morphControl = returnMorphs();

                HashSet<string> regions = new HashSet<string>();
                HashSet<string> LastRegions = new HashSet<string>();
                Dictionary<string, string> temporaryToggles = new Dictionary<string, string>();

                JSONStorableBool playingBool = new JSONStorableBool("Play", true);
                playingBool.storeType = JSONStorableParam.StoreType.Full;
                RegisterBool(playingBool);
                toggles["Play"] = CreateToggle((playingBool), false);

								smoothToggle = new JSONStorableBool("Smooth transitions", true);
								RegisterBool(smoothToggle);
								CreateToggle((smoothToggle), false);

								abaToggle = new JSONStorableBool("Reset used expressions at loop", true);
								RegisterBool(abaToggle);
								CreateToggle((abaToggle), false);

								manualToggle = new JSONStorableBool("Trigger transitions manually", false);
								RegisterBool(manualToggle);
								CreateToggle((manualToggle), false);

								randomToggle = new JSONStorableBool("Random chances for transitions", false);
								RegisterBool(randomToggle);
								CreateToggle((randomToggle), false);

								triggerChanceSlider = new JSONStorableFloat("Chance to trigger", 75f, 0f, 100f, true);
								triggerChanceSlider.storeType = JSONStorableParam.StoreType.Full;
								RegisterFloat(triggerChanceSlider);
								CreateSlider(triggerChanceSlider, false);

								JSONStorableAction manualTrigger = new JSONStorableAction("Trigger transition", () =>{
										UpdateRandomParams();
								});
								RegisterAction(manualTrigger);

                morphControl.GetMorphDisplayNames().ForEach((name) =>{
                    DAZMorph morph = morphControl.GetMorphByDisplayName(name);
                    regions.Add(morph.region);

                    if (
                         (poseRegion.Any((str) => morph.region.Contains(str)) &&
                        !bodyRegion.Any((str) => morph.region.Contains(str))) ||
												tailorList.Any((str) => name.Contains(str))
                    ){
                        string[] posePaths = Regex.Split(morph.region, "/");
                        string morphUpperRegion = "";

                        foreach (string posePath in posePaths){
                            morphUpperRegion = posePath;
                        }

                        LastRegions.Add(morphUpperRegion);
                        toggleRelations[name] = morphUpperRegion;
                        temporaryToggles[name] = morphUpperRegion + "/" + name;
                    }
                });

                #region Region Helper Buttons
                UIDynamicButton selectNone = CreateButton("Select None", true);
                selectNone.button.onClick.AddListener(delegate () {
                    toggles.Values.ToList().ForEach((toggle) =>
                    {
                        toggle.toggle.isOn = false;
                    });
                });

								UIDynamicButton selectDefault = CreateButton("Select Default", true);
								selectDefault.button.onClick.AddListener(delegate () {
										foreach (KeyValuePair<string, UIDynamicToggle> entry in toggles)
										{
											if (manualToggle.val)
												presetSliders(defaultSliders);
											if (entry.Key != "Play")
												toggles[entry.Key].toggle.isOn = defaultOn.Any((str) => entry.Key.Equals(str));
										};
								});

								UIDynamicButton selectPres1 = CreateButton("Select preset 1", true);
								selectPres1.button.onClick.AddListener(delegate () {
										foreach (KeyValuePair<string, UIDynamicToggle> entry in toggles)
										{
											if (manualToggle.val)
												presetSliders(preset1Sliders);
											if (entry.Key != "Play")
												toggles[entry.Key].toggle.isOn = preset1.Any((str) => entry.Key.Equals(str));
										};
								});

								UIDynamicButton selectPres2 = CreateButton("Select preset 2", true);
								selectPres2.button.onClick.AddListener(delegate () {
										foreach (KeyValuePair<string, UIDynamicToggle> entry in toggles)
										{
											if (manualToggle.val)
												presetSliders(preset2Sliders);
											if (entry.Key != "Play")
												toggles[entry.Key].toggle.isOn = preset2.Any((str) => entry.Key.Equals(str));
										};
								});
								CreateSpacer(true).height = 10f;

								// ******* FILTER BOX ***********
								JSONStorableString filterText = new JSONStorableString("FilterText", "Search a morph..."){isStorable=false,isRestorable=false};
								UIDynamicTextField tmpTextfield = CreateTextField(filterText,true);
								setupTextField(tmpTextfield, 110f, false, false);
								filterInputField = tmpTextfield.UItext.gameObject.AddComponent<InputField>();
								filterInputField.textComponent = tmpTextfield.UItext;
								filterInputField.lineType = InputField.LineType.SingleLine;
								filterInputField.onValueChanged.AddListener( delegate { OnFilterChanged(); } );
								filterInputField.text = "Search a morph...";

								UIDynamicButton clearSearchBtn = CreateButton("Clear", true);
								clearSearchBtn.button.onClick.AddListener(() => { filterInputField.text = ""; });

								setupButtonWithoutLayout(clearSearchBtn, 130f, new Vector2(385, -370));
								setButtonColor(clearSearchBtn, new Color(0.6f, 0.3f, 0.3f, 1f));
								setButtonTextColor(clearSearchBtn, new Color(1f, 1f, 1f, 1f));

								// ******* AND SEARCH ***********
								filterAndSearch = new JSONStorableBool("AND Search", false){isStorable=false,isRestorable=false};
								filterAndSearch.setCallbackFunction += (val) => { OnFilterChanged(); };
								UIDynamicToggle tmpToggle = CreateToggle(filterAndSearch,true);

								onlyShowActive = new JSONStorableBool("Only show active morphs", false){isStorable=false,isRestorable=false};
								onlyShowActive.setCallbackFunction += (val) => { OnFilterChanged(); };
								tmpToggle = CreateToggle(onlyShowActive,true);

								UIDynamic tmpSpacer = CreateSpacer(true);
								setupSpacer(tmpSpacer, 25);

                #endregion

                #region Region checkbox generation
                foreach (KeyValuePair<string, string> entry in temporaryToggles) {
                    JSONStorableBool checkBoxTick = new JSONStorableBool(entry.Value, defaultOn.Any((str) => entry.Key.Equals(str)), (bool on)=>{

											togglesOn = toggles.Where(t => t.Value.toggle.isOn).ToDictionary(p => p.Key, p => p.Value);

											if (!on && entry.Key != "Play") {
												DAZMorph morph = morphControl.GetMorphByDisplayName(entry.Key);
												morph.morphValue = 0;
											}
										});
                    checkBoxTick.storeType = JSONStorableParam.StoreType.Full;
                    RegisterBool(checkBoxTick);

                    toggles[entry.Key] = CreateToggle(checkBoxTick, true);
                }
								togglesOn = toggles.Where(t => t.Value.toggle.isOn).ToDictionary(p => p.Key, p => p.Value);
                #endregion

                #endregion

                //CreateSpacer();
								UIDynamicButton transitionButton = CreateButton("Trigger transition", false);
								transitionButton.button.onClick.AddListener(delegate () {
									UpdateRandomParams();
								});
								transitionButton.buttonColor = new Color(0.5f, 1f, 0.5f);

								colliderChoices = new List<string>();
								colliderChoices.Add("None");
								colliderChoices.Add("LipTrigger");
								colliderChoices.Add("MouthTrigger");
								colliderChoices.Add("ThroatTrigger");
								colliderChoices.Add("lNippleTrigger");
								colliderChoices.Add("rNippleTrigger");
								colliderChoices.Add("LabiaTrigger");
								colliderChoices.Add("VaginaTrigger");
								colliderChoices.Add("DeepVaginaTrigger");
								colliderChoices.Add("DeeperVaginaTrigger");

								colliderStringChooser = new JSONStorableStringChooser("Collision trigger", colliderChoices, "None", "Collision trigger");
								RegisterStringChooser(colliderStringChooser);
								UIDynamicPopup dp = CreatePopup(colliderStringChooser, false);
								dp.popup.onOpenPopupHandlers += enableManualMode;

                UIDynamicButton animatableButton = CreateButton("Clear Animatable (from selected)", false);
                animatableButton.button.onClick.AddListener(delegate () {
                    morphControl.GetMorphDisplayNames().ForEach((name) => {
                        DAZMorph morph = morphControl.GetMorphByDisplayName(name);
                        if (toggles.ContainsKey(name) && toggles[name].toggle.isOn)
                            morph.animatable = true;
                    });
                });

                #region SetAsDef button
                UIDynamicButton setasdefButton = CreateButton("Set current state as default", false);
                setasdefButton.button.onClick.AddListener(delegate () {
                  UpdateInitialMorphs();
                });
                #endregion

                #region Reset button
                UIDynamicButton resetButton = CreateButton("Reset to default/load state", false);
                resetButton.button.onClick.AddListener(delegate () {
									toggles["Play"].toggle.isOn = false;
                  ResetMorphs();
                });
                #endregion

                #region ZeroMorph button
                UIDynamicButton ZeroMorphButton = CreateButton("Zero Selected", false);
                ZeroMorphButton.button.onClick.AddListener(delegate () {
									toggles["Play"].toggle.isOn = false;
                  ZeroMorphs();
                });
                #endregion

                animWaitSlider = new JSONStorableFloat("Loop length", 2f, 0.1f, 20f, true);
                animWaitSlider.storeType = JSONStorableParam.StoreType.Full;
                RegisterFloat(animWaitSlider);
                CreateSlider(animWaitSlider, false);

                animLengthSlider = new JSONStorableFloat("Morphing speed", 1.0f, 0.1f, 20f, true);
                animLengthSlider.storeType = JSONStorableParam.StoreType.Full;
                RegisterFloat(animLengthSlider);
                CreateSlider(animLengthSlider, false);

								morphList = new JSONStorableString("FooString","");
								UIDynamic morphListText = CreateTextField(morphList,false);
								morphListText.height = 320;
            }
            catch (Exception e) {
				SuperController.LogError("Exception caught: " + e);
			}
		}

        void Start() {
            timer = 0f;
            UpdateInitialMorphs();
            UpdateNewMorphs();
						InvokeRepeating("triggerMaintainer", 3f, 3f); // To check if the selected collision trigger is still there every 3 seconds
        }

        // **************************
        // Haz stuffs
        // **************************
        private void logDebug( string debugText ) {
	        SuperController.LogMessage( debugText );
        }

        private void setupSpacer(UIDynamic target, float newHeight)
        {
	        LayoutElement tmpLE = target.GetComponent<LayoutElement>();
	        tmpLE.minHeight = newHeight;
	        tmpLE.preferredHeight = newHeight;
        }

        protected void setupTextField( UIDynamicTextField target, float fieldHeight, bool disableBackground = true, bool disableScroll = true ) {
	        if( disableBackground ) target.backgroundColor = new Color(1f, 1f, 1f, 0f);
	        LayoutElement tfLayout = target.GetComponent<LayoutElement>();
	        tfLayout.preferredHeight = tfLayout.minHeight = fieldHeight;
	        target.height = fieldHeight;
	        if( disableScroll ) disableScrollOnText(target);
        }

        private static void disableScrollOnText(UIDynamicTextField target) {
	        ScrollRect targetSR = target.UItext.transform.parent.transform.parent.transform.parent.GetComponent<ScrollRect>();
	        if( targetSR != null ) {
		        targetSR.horizontal = false;
		        targetSR.vertical = false;
	        }
        }

        private void setupButtonWithoutLayout(UIDynamicButton target, float newSize, Vector2 newPosition)
        {
	        ContentSizeFitter tmpCSF = target.button.transform.gameObject.AddComponent<ContentSizeFitter>();
	        tmpCSF.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.MinSize;
	        LayoutElement tmpLE = target.button.transform.gameObject.GetComponent<LayoutElement>();
	        RectTransform tmpRectT = target.button.transform.GetComponent<RectTransform>();
	        Rect tmpRect = tmpRectT.rect;
	        tmpRectT.pivot = new Vector2(0f, 0.5f);
	        tmpLE.minWidth = newSize;
	        tmpLE.preferredWidth = newSize;
	        tmpLE.ignoreLayout = true;
	        tmpRectT.anchoredPosition = newPosition;
        }

        private void setButtonColor(UIDynamicButton targetBtn, Color newColor)
        {
	        targetBtn.buttonColor = newColor;
        }

        private void setButtonTextColor(UIDynamicButton targetBtn, Color newColor)
        {
	        targetBtn.textColor = newColor;
        }

        protected void OnFilterChanged()
        {
	        // Ya know...
	        if (filterInputField == null || onlyShowActive == null || filterAndSearch == null) return;

	        // Field reset
	        if (filterInputField.text.Trim() == "")
	        {
		        filterInputField.text = "Search a morph...";
		        filterInputField.Select();
		        return;
	        }

	        // not searching under 3 letters
	        if (filterInputField.text.Length <= 3) return;

	        // Grabbing the search
	        string[] searchWords = filterInputField.text.Split(' ');
	        List<string> searchList = new List<string>();

	        // Cleaning the search
	        for (var i = 0; i < searchWords.Length; i++)
	        {
		        if (searchWords[i].Length > 1)
		        {
			        searchList.Add(searchWords[i]);
		        }
	        }

	        // Searching the toggl
	        foreach (var tog in toggles)
	        {
		        // Displaying everything and returning if we have the default value in the field and do not try
		        // to filter active only
		        if (onlyShowActive.val == false && filterInputField.text == "Search a morph...")
		        {
			        ToggleUIDynamicToggle(tog.Value, true);
			        continue;
		        }

		        if (tog.Key == "Play") continue; // I don't know why play is in there...

		        int searchHit = 0;

		        // If only looking for active morphs, then simply not doing the word search if the thing is inactive
		        if ( onlyShowActive.val == false || ( onlyShowActive.val == true && tog.Value.toggle.isOn == true ) )
		        {
			        // Doing word search only if we don't have the default value
			        if (filterInputField.text != "Search a morph...")
			        {
				        foreach (string search in searchList)
				        {
					        string labelText = tog.Value.labelText.text.ToLower();
					        string searchVal = search.ToLower();
					        if (labelText.Contains(searchVal))
					        {
						        searchHit++;
						        if (filterAndSearch.val == false) continue;
					        }
				        }
			        }
			        else if ( onlyShowActive.val == true && tog.Value.toggle.isOn == true )
			        {
				        // We have the default value in the search text and we want to only show active
				        // So we simply make this result valid for any situation below
				        searchHit = searchList.Count;
			        }
		        }

		        if ( filterAndSearch.val == false && searchHit > 0 )
		        {
			        ToggleUIDynamicToggle(tog.Value, true);
		        } else if ( filterAndSearch.val == true && searchHit == searchList.Count )
		        {
			        ToggleUIDynamicToggle(tog.Value, true);
		        }
		        else
		        {
			        ToggleUIDynamicToggle(tog.Value, false);
		        }
	        }
        }

        protected void ToggleUIDynamicToggle(UIDynamicToggle target, bool enabled)
        {
	        if (target == null) return;

	        LayoutElement tmpLE = target.GetComponent<LayoutElement>();

	        if (tmpLE != null)
	        {
		        if (enabled == true)
		        {
			        tmpLE.transform.localScale = new Vector3(1, 1, 1);
			        tmpLE.ignoreLayout = false;
		        }
		        else
		        {
			        tmpLE.transform.localScale = new Vector3(0, 0, 0);
			        tmpLE.ignoreLayout = true;
		        }
	        }
        }
	}
}
