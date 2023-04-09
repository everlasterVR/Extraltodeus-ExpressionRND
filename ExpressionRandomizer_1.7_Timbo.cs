using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace extraltodeuslExpRandPlugin
{
    sealed class ExpressionRandomizer : MVRScript
    {
        readonly string[] _bodyRegion =
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
            "Mouth Smile Simple Right",
        };
        readonly Dictionary<string, float> _currentMorphsValues = new Dictionary<string, float>();

        readonly string[] _defaultOn =
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
            "Lips Pucker",
        };
        readonly float[] _defaultSliders = { 1f, 1f };
        readonly Dictionary<string, float> _initialMorphValues = new Dictionary<string, float>();
        readonly Dictionary<string, float> _newMorphValues = new Dictionary<string, float>();

        readonly string[] _poseRegion =
        {
            "Pose",
            "Expressions",
        };

        readonly string[] _preset1 =
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
            "Pupils Dialate",
        };
        readonly float[] _preset1Sliders = { 1.6f, 3f };

        readonly string[] _preset2 =
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
        readonly float[] _preset2Sliders = { 1.8f, 4.2f };

        // particular morph names to add
        readonly string[] _tailorList =
        {
            "Pupils Dialate",
            "Eye Roll Back_DD",
        };
        readonly Dictionary<string, string> _toggleRelations = new Dictionary<string, string>();
        readonly Dictionary<string, UIDynamicToggle> _toggles = new Dictionary<string, UIDynamicToggle>();
        JSONStorableBool _abaJsb;
        JSONStorableFloat _animLengthJsf;
        JSONStorableFloat _animWaitJsf;

        List<string> _colliderChoices;
        JSONStorableStringChooser _colliderStringChooser;

        JSONStorableBool _filterAndSearchJsb;
        InputField _filterInputField;
        JSONStorableBool _manualJsb;

        JSONStorableFloat _masterSpeedJsf;
        JSONStorableFloat _maxJsf;
        JSONStorableFloat _minJsf;
        JSONStorableString _morphListJss;
        GenerateDAZMorphsControlUI _morphsControlUI;
        JSONStorableFloat _multiJsf;
        JSONStorableBool _onlyShowActiveJsb;
        JSONStorableBool _randomJsb;
        JSONStorableBool _smoothJsb;

        float _timer;
        Dictionary<string, UIDynamicToggle> _togglesOn;
        JSONStorableFloat _triggerChanceJsf;

        void Start()
        {
            _timer = 0f;
            UpdateInitialMorphs();
            UpdateNewMorphs();
            InvokeRepeating(nameof(TriggerMaintainer), 3f, 3f); // To check if the selected collision trigger is still there every 3 seconds
        }

        void Update()
        {
            if(!_toggles["Play"].toggle.isOn)
            {
                return;
            }

            _timer += Time.deltaTime;
            if(_timer >= _animWaitJsf.val / _masterSpeedJsf.val)
            {
                _timer = 0;
                if(!_manualJsb.val)
                {
                    UpdateRandomParams();
                }
            }
        }

        void FixedUpdate()
        {
            if(_toggles["Play"].toggle.isOn)
            {
                // morph progressively every morphs to their new values
                const string textBoxMessage = "\n Animatable morphs (not animated) :\n";
                string animatableSelected = textBoxMessage;
                foreach(var entry in _togglesOn)
                {
                    string morphName = entry.Key;
                    if(morphName != "Play")
                    {
                        var morph = _morphsControlUI.GetMorphByDisplayName(morphName);
                        if(morph != null && morph.animatable)
                        {
                            float valeur;
                            if(_smoothJsb.val)
                            {
                                valeur = Mathf.Lerp(_currentMorphsValues[morphName], _newMorphValues[morphName],
                                    Time.deltaTime *
                                    _animLengthJsf.val *
                                    _masterSpeedJsf.val *
                                    Mathf.Sin(_timer / (_animWaitJsf.val / _masterSpeedJsf.val) * Mathf.PI));
                            }
                            else
                            {
                                valeur = Mathf.Lerp(_currentMorphsValues[morphName], _newMorphValues[morphName],
                                    Time.deltaTime * _animLengthJsf.val * _masterSpeedJsf.val);
                            }

                            _currentMorphsValues[morphName] = morph.morphValue = valeur;
                        }
                        else
                        {
                            animatableSelected = animatableSelected + " " + morphName + "\n";
                        }
                    }
                }

                if(animatableSelected != textBoxMessage || _morphListJss.val != textBoxMessage)
                {
                    _morphListJss.val = animatableSelected;
                }
            }
        }

        void UpdateRandomParams()
        {
            if(UnityEngine.Random.Range(0f, 100f) <= _triggerChanceJsf.val || !_randomJsb.val)
            {
                // define the random values to switch to
                foreach(var entry in _togglesOn)
                {
                    string morphName = entry.Key;
                    if(morphName != "Play")
                    {
                        var morph = _morphsControlUI.GetMorphByDisplayName(morphName);
                        if(morph.animatable)
                        {
                            if(_currentMorphsValues[morphName] > 0.1f && _abaJsb.val)
                            {
                                _newMorphValues[morphName] = 0;
                            }
                            else
                            {
                                float valeur = UnityEngine.Random.Range(_minJsf.val, _maxJsf.val) * _multiJsf.val;
                                _newMorphValues[morphName] = valeur;
                            }
                        }
                    }
                }
            }
        }

        void UpdateInitialMorphs()
        {
            _morphsControlUI.GetMorphDisplayNames()
                .ForEach(name =>
                {
                    if(_toggles.ContainsKey(name))
                    {
                        _initialMorphValues[name] = _morphsControlUI.GetMorphByDisplayName(name).morphValue;
                    }
                });
        }

        void UpdateNewMorphs()
        {
            _morphsControlUI.GetMorphDisplayNames()
                .ForEach(name =>
                {
                    if(_toggles.ContainsKey(name))
                    {
                        _newMorphValues[name] = _morphsControlUI.GetMorphByDisplayName(name).morphValue;
                    }
                });
        }

        void UpdateCurrentMorphs()
        {
            _morphsControlUI.GetMorphDisplayNames()
                .ForEach(name => { _currentMorphsValues[name] = _morphsControlUI.GetMorphByDisplayName(name).morphValue; });
        }

        void ResetMorphs()
        {
            _morphsControlUI.GetMorphDisplayNames()
                .ForEach(name =>
                {
                    if(_toggleRelations.ContainsKey(name))
                    {
                        if(_toggles.ContainsKey(name))
                        {
                            var morph = _morphsControlUI.GetMorphByDisplayName(name);
                            morph.morphValue = _initialMorphValues[name];
                        }
                    }
                });
        }

        void ZeroMorphs()
        {
            _morphsControlUI.GetMorphDisplayNames()
                .ForEach(name =>
                {
                    if(_toggleRelations.ContainsKey(name))
                    {
                        if(_toggles.ContainsKey(name) && _toggles[name].toggle.isOn)
                        {
                            var morph = _morphsControlUI.GetMorphByDisplayName(name);
                            morph.morphValue = 0;
                        }
                    }
                });
        }

        // Function taken from VAMDeluxe's code :)
        static JSONStorable GetPluginStorableById(Atom atom, string id)
        {
            string storableIdName = atom.GetStorableIDs()
                .FirstOrDefault(storeId => !string.IsNullOrEmpty(storeId) && storeId.Contains(id));
            return storableIdName == null ? null : atom.GetStorableByID(storableIdName);
        }

        // Thanks to VRStudy for helping for the trigger-related functions !!
        void CleanTriggers()
        {
            foreach(string triggerName in _colliderChoices)
            {
                if(triggerName != _colliderStringChooser.val && triggerName != "None")
                {
                    ClearTriggers(triggerName);
                }
            }
        }

        void ClearTriggers(string triggerName)
        {
            var trig = containingAtom.GetStorableByID(triggerName) as CollisionTrigger;
            if(trig)
            {
                var trigClass = trig.trigger.GetJSON();
                var trigArray = trigClass["startActions"].AsArray;
                for(int i = 0; i < trigArray.Count; i++)
                {
                    if(trigArray[i]["name"].Value == "ExpRandTrigger")
                    {
                        trigArray.Remove(i);
                    }
                }

                trig.trigger.RestoreFromJSON(trigClass);
            }
            else
            {
                SuperController.LogMessage("Couldn't find trigger " + triggerName);
            }
        }

        static bool CheckIfTriggerExists(CollisionTrigger trig)
        {
            JSONNode presentTriggers = trig.trigger.GetJSON();
            var asArray = presentTriggers["startActions"].AsArray;
            for(int i = 0; i < asArray.Count; i++)
            {
                var asObject = asArray[i].AsObject;
                string name = asObject["name"];
                if(name == "ExpRandTrigger" && asObject["receiver"] != null)
                {
                    return true;
                }
            }

            return false;
        }

        void CreateTrigger(string triggerName)
        {
            var trig = containingAtom.GetStorableByID(triggerName) as CollisionTrigger;
            if(!CheckIfTriggerExists(trig))
            {
                if(trig)
                {
                    trig.enabled = true;
                    var startTrigger = trig.trigger.CreateDiscreteActionStartInternal();
                    startTrigger.name = "ExpRandTrigger";
                    startTrigger.receiverAtom = containingAtom;
                    startTrigger.receiver = GetPluginStorableById(GetContainingAtom(), "ExpressionRandomizer");
                    startTrigger.receiverTargetName = "Trigger transition";
                }
            }
        }

        void TriggerMaintainer()
        {
            if(_colliderStringChooser.val != "None")
            {
                CreateTrigger(_colliderStringChooser.val);
                CleanTriggers();
            }
        }

        void EnableManualMode()
        {
            _manualJsb.val = true;
            _randomJsb.val = true;
        }

        void PresetSliders(float[] values)
        {
            _multiJsf.val = values[0];
            _masterSpeedJsf.val = values[1];
        }

        public override void Init()
        {
            try
            {
                GetContainingAtom().GetStorableByID("AutoExpressions").SetBoolParamValue("enabled", false);
                var geometry = (DAZCharacterSelector) containingAtom.GetStorableByID("geometry");
                _morphsControlUI = geometry.morphsControlUI;
                UpdateInitialMorphs();
                UpdateNewMorphs();
                UpdateCurrentMorphs();

#region Sliders

                _minJsf = new JSONStorableFloat("Minimum value", -0.15f, -1f, 1.0f)
                {
                    storeType = JSONStorableParam.StoreType.Full,
                };
                RegisterFloat(_minJsf);
                CreateSlider(_minJsf);

                _maxJsf = new JSONStorableFloat("Maximum value", 0.35f, -1f, 1.0f)
                {
                    storeType = JSONStorableParam.StoreType.Full,
                };
                RegisterFloat(_maxJsf);
                CreateSlider(_maxJsf);

                _multiJsf = new JSONStorableFloat("Multiplier", 1f, 0f, 2f)
                {
                    storeType = JSONStorableParam.StoreType.Full,
                };
                RegisterFloat(_multiJsf);
                CreateSlider(_multiJsf);

                _masterSpeedJsf = new JSONStorableFloat("Master speed", 1f, 0f, 10f)
                {
                    storeType = JSONStorableParam.StoreType.Full,
                };
                RegisterFloat(_masterSpeedJsf);
                CreateSlider(_masterSpeedJsf);

#endregion

#region Region Buttons Preparation

                var temporaryToggles = new Dictionary<string, string>();

                var playingBool = new JSONStorableBool("Play", true)
                {
                    storeType = JSONStorableParam.StoreType.Full,
                };
                RegisterBool(playingBool);
                _toggles["Play"] = CreateToggle(playingBool);

                _smoothJsb = new JSONStorableBool("Smooth transitions", true);
                RegisterBool(_smoothJsb);
                CreateToggle(_smoothJsb);

                _abaJsb = new JSONStorableBool("Reset used expressions at loop", true);
                RegisterBool(_abaJsb);
                CreateToggle(_abaJsb);

                _manualJsb = new JSONStorableBool("Trigger transitions manually", false);
                RegisterBool(_manualJsb);
                CreateToggle(_manualJsb);

                _randomJsb = new JSONStorableBool("Random chances for transitions", false);
                RegisterBool(_randomJsb);
                CreateToggle(_randomJsb);

                _triggerChanceJsf = new JSONStorableFloat("Chance to trigger", 75f, 0f, 100f)
                {
                    storeType = JSONStorableParam.StoreType.Full,
                };
                RegisterFloat(_triggerChanceJsf);
                CreateSlider(_triggerChanceJsf);

                var manualTriggerAction = new JSONStorableAction("Trigger transition", UpdateRandomParams);
                RegisterAction(manualTriggerAction);

                _morphsControlUI.GetMorphDisplayNames()
                    .ForEach(name =>
                    {
                        var morph = _morphsControlUI.GetMorphByDisplayName(name);

                        if(
                            _poseRegion.Any(str => morph.region.Contains(str)) &&
                            !_bodyRegion.Any(str => morph.region.Contains(str)) ||
                            _tailorList.Any(name.Contains)
                        )
                        {
                            string[] posePaths = Regex.Split(morph.region, "/");
                            string morphUpperRegion = "";

                            foreach(string posePath in posePaths)
                            {
                                morphUpperRegion = posePath;
                            }

                            _toggleRelations[name] = morphUpperRegion;
                            temporaryToggles[name] = morphUpperRegion + "/" + name;
                        }
                    });

#region Region Helper Buttons

                var selectNone = CreateButton("Select None", true);
                selectNone.button.onClick.AddListener(() => _toggles.Values.ToList().ForEach(toggle => { toggle.toggle.isOn = false; }));

                var selectDefault = CreateButton("Select Default", true);
                selectDefault.button.onClick.AddListener(() =>
                {
                    foreach(var entry in _toggles)
                    {
                        if(_manualJsb.val)
                        {
                            PresetSliders(_defaultSliders);
                        }

                        if(entry.Key != "Play")
                        {
                            _toggles[entry.Key].toggle.isOn = _defaultOn.Any(str => entry.Key.Equals(str));
                        }
                    }
                });

                var selectPres1 = CreateButton("Select preset 1", true);
                selectPres1.button.onClick.AddListener(() =>
                {
                    foreach(var entry in _toggles)
                    {
                        if(_manualJsb.val)
                        {
                            PresetSliders(_preset1Sliders);
                        }

                        if(entry.Key != "Play")
                        {
                            _toggles[entry.Key].toggle.isOn = _preset1.Any(str => entry.Key.Equals(str));
                        }
                    }
                });

                var selectPres2 = CreateButton("Select preset 2", true);
                selectPres2.button.onClick.AddListener(() =>
                {
                    foreach(var entry in _toggles)
                    {
                        if(_manualJsb.val)
                        {
                            PresetSliders(_preset2Sliders);
                        }

                        if(entry.Key != "Play")
                        {
                            _toggles[entry.Key].toggle.isOn = _preset2.Any(str => entry.Key.Equals(str));
                        }
                    }
                });
                CreateSpacer(true).height = 10f;

                // ******* FILTER BOX ***********
                var filterTextJss = new JSONStorableString("FilterText", "Search a morph...");
                var tmpTextfield = CreateTextField(filterTextJss, true);
                SetupTextField(tmpTextfield, 110f, false, false);
                _filterInputField = tmpTextfield.UItext.gameObject.AddComponent<InputField>();
                _filterInputField.textComponent = tmpTextfield.UItext;
                _filterInputField.lineType = InputField.LineType.SingleLine;
                _filterInputField.onValueChanged.AddListener(_ => OnFilterChanged());
                _filterInputField.text = "Search a morph...";

                var clearSearchBtn = CreateButton("Clear", true);
                clearSearchBtn.button.onClick.AddListener(() => { _filterInputField.text = ""; });

                SetupButtonWithoutLayout(clearSearchBtn, 130f, new Vector2(385, -370));
                SetButtonColor(clearSearchBtn, new Color(0.6f, 0.3f, 0.3f, 1f));
                SetButtonTextColor(clearSearchBtn, new Color(1f, 1f, 1f, 1f));

                // ******* AND SEARCH ***********
                _filterAndSearchJsb = new JSONStorableBool("AND Search", false);
                _filterAndSearchJsb.setCallbackFunction += val => OnFilterChanged();
                CreateToggle(_filterAndSearchJsb, true);

                _onlyShowActiveJsb = new JSONStorableBool("Only show active morphs", false);
                _onlyShowActiveJsb.setCallbackFunction += val => OnFilterChanged();
                CreateToggle(_onlyShowActiveJsb, true);

                var tmpSpacer = CreateSpacer(true);
                SetupSpacer(tmpSpacer, 25);

#endregion

#region Region checkbox generation

                foreach(var entry in temporaryToggles)
                {
                    var checkBoxTickJsb = new JSONStorableBool(entry.Value, _defaultOn.Any(str => entry.Key.Equals(str)))
                    {
                        storeType = JSONStorableParam.StoreType.Full,
                        setCallbackFunction = on =>
                        {

                            _togglesOn = _toggles.Where(t => t.Value.toggle.isOn).ToDictionary(p => p.Key, p => p.Value);

                            if(!on && entry.Key != "Play")
                            {
                                var morph = _morphsControlUI.GetMorphByDisplayName(entry.Key);
                                morph.morphValue = 0;
                            }
                        },
                    };
                    RegisterBool(checkBoxTickJsb);

                    _toggles[entry.Key] = CreateToggle(checkBoxTickJsb, true);
                }

                _togglesOn = _toggles.Where(t => t.Value.toggle.isOn).ToDictionary(p => p.Key, p => p.Value);

#endregion

#endregion

                //CreateSpacer();
                var transitionButton = CreateButton("Trigger transition");
                transitionButton.button.onClick.AddListener(UpdateRandomParams);
                transitionButton.buttonColor = new Color(0.5f, 1f, 0.5f);

                _colliderChoices = new List<string>
                {
                    "None",
                    "LipTrigger",
                    "MouthTrigger",
                    "ThroatTrigger",
                    "lNippleTrigger",
                    "rNippleTrigger",
                    "LabiaTrigger",
                    "VaginaTrigger",
                    "DeepVaginaTrigger",
                    "DeeperVaginaTrigger",
                };

                _colliderStringChooser = new JSONStorableStringChooser("Collision trigger", _colliderChoices, "None", "Collision trigger");
                RegisterStringChooser(_colliderStringChooser);
                var dp = CreatePopup(_colliderStringChooser);
                dp.popup.onOpenPopupHandlers += EnableManualMode;

                var animatableButton = CreateButton("Clear Animatable (from selected)");
                animatableButton.button.onClick.AddListener(() => _morphsControlUI.GetMorphDisplayNames()
                    .ForEach(name =>
                    {
                        var morph = _morphsControlUI.GetMorphByDisplayName(name);
                        if(_toggles.ContainsKey(name) && _toggles[name].toggle.isOn)
                        {
                            morph.animatable = true;
                        }
                    }));

                var setAsDefaultButton = CreateButton("Set current state as default");
                setAsDefaultButton.button.onClick.AddListener(UpdateInitialMorphs);

                var resetButton = CreateButton("Reset to default/load state");
                resetButton.button.onClick.AddListener(() =>
                {
                    _toggles["Play"].toggle.isOn = false;
                    ResetMorphs();
                });

                var zeroMorphButton = CreateButton("Zero Selected");
                zeroMorphButton.button.onClick.AddListener(() =>
                {
                    _toggles["Play"].toggle.isOn = false;
                    ZeroMorphs();
                });

                _animWaitJsf = new JSONStorableFloat("Loop length", 2f, 0.1f, 20f)
                {
                    storeType = JSONStorableParam.StoreType.Full,
                };
                RegisterFloat(_animWaitJsf);
                CreateSlider(_animWaitJsf);

                _animLengthJsf = new JSONStorableFloat("Morphing speed", 1.0f, 0.1f, 20f)
                {
                    storeType = JSONStorableParam.StoreType.Full,
                };
                RegisterFloat(_animLengthJsf);
                CreateSlider(_animLengthJsf);

                _morphListJss = new JSONStorableString("Morph list", "");
                UIDynamic morphListText = CreateTextField(_morphListJss);
                morphListText.height = 320;
            }
            catch(Exception e)
            {
                SuperController.LogMessage($"{nameof(ExpressionRandomizer)}: Init error: " + e);
            }
        }

        static void SetupSpacer(UIDynamic target, float newHeight)
        {
            var tmpLe = target.GetComponent<LayoutElement>();
            tmpLe.minHeight = newHeight;
            tmpLe.preferredHeight = newHeight;
        }

        static void SetupTextField(UIDynamicTextField target, float fieldHeight, bool disableBackground = true, bool disableScroll = true)
        {
            if(disableBackground)
            {
                target.backgroundColor = new Color(1f, 1f, 1f, 0f);
            }

            var tfLayout = target.GetComponent<LayoutElement>();
            tfLayout.preferredHeight = tfLayout.minHeight = fieldHeight;
            target.height = fieldHeight;
            if(disableScroll)
            {
                DisableScrollOnText(target);
            }
        }

        static void DisableScrollOnText(UIDynamicTextField target)
        {
            var targetSr = target.UItext.transform.parent.transform.parent.transform.parent.GetComponent<ScrollRect>();
            if(targetSr != null)
            {
                targetSr.horizontal = false;
                targetSr.vertical = false;
            }
        }

        static void SetupButtonWithoutLayout(UIDynamicButton target, float newSize, Vector2 newPosition)
        {
            var tmpCsf = target.button.transform.gameObject.AddComponent<ContentSizeFitter>();
            tmpCsf.horizontalFit = ContentSizeFitter.FitMode.MinSize;
            var tmpLe = target.button.transform.gameObject.GetComponent<LayoutElement>();
            var tmpRectT = target.button.transform.GetComponent<RectTransform>();
            tmpRectT.pivot = new Vector2(0f, 0.5f);
            tmpLe.minWidth = newSize;
            tmpLe.preferredWidth = newSize;
            tmpLe.ignoreLayout = true;
            tmpRectT.anchoredPosition = newPosition;
        }

        static void SetButtonColor(UIDynamicButton targetBtn, Color newColor)
        {
            targetBtn.buttonColor = newColor;
        }

        static void SetButtonTextColor(UIDynamicButton targetBtn, Color newColor)
        {
            targetBtn.textColor = newColor;
        }

        void OnFilterChanged()
        {
            if(_filterInputField == null || _onlyShowActiveJsb == null || _filterAndSearchJsb == null)
            {
                return;
            }



            // Field reset
            if(_filterInputField.text.Trim() == "")
            {
                _filterInputField.Select();
                return;
            }

            // not searching under 3 letters
            if(_filterInputField.text.Length <= 3)
            {
                return;
            }

            // Grabbing the search
            string[] searchWords = _filterInputField.text.Split(' ');
            var searchList = new List<string>();

            // Cleaning the search
            foreach(string t in searchWords)
            {
                if(t.Length > 1)
                {
                    searchList.Add(t);
                }
            }

            // Searching the toggl
            foreach(var tog in _toggles)
            {
                // Displaying everything and returning if we have the default value in the field and do not try
                // to filter active only
                if(!_onlyShowActiveJsb.val && _filterInputField.text == "Search a morph...")
                {
                    ToggleUIDynamicToggle(tog.Value, true);
                    continue;
                }

                if(tog.Key == "Play")
                {
                    continue; // I don't know why play is in there...
                }

                int searchHit = 0;

                // If only looking for active morphs, then simply not doing the word search if the thing is inactive
                if(!_onlyShowActiveJsb.val || _onlyShowActiveJsb.val && tog.Value.toggle.isOn)
                {
                    // Doing word search only if we don't have the default value
                    if(_filterInputField.text != "Search a morph...")
                    {
                        foreach(string search in searchList)
                        {
                            string labelText = tog.Value.labelText.text.ToLower();
                            string searchVal = search.ToLower();
                            if(labelText.Contains(searchVal))
                            {
                                searchHit++;
                            }
                        }
                    }
                    else if(_onlyShowActiveJsb.val && tog.Value.toggle.isOn)
                    {
                        // We have the default value in the search text and we want to only show active
                        // So we simply make this result valid for any situation below
                        searchHit = searchList.Count;
                    }
                }

                if(!_filterAndSearchJsb.val && searchHit > 0)
                {
                    ToggleUIDynamicToggle(tog.Value, true);
                }
                else if(_filterAndSearchJsb.val && searchHit == searchList.Count)
                {
                    ToggleUIDynamicToggle(tog.Value, true);
                }
                else
                {
                    ToggleUIDynamicToggle(tog.Value, false);
                }
            }
        }

        static void ToggleUIDynamicToggle(UIDynamicToggle target, bool enabled)
        {
            if(target)
            {
                return;
            }

            var tmpLe = target.GetComponent<LayoutElement>();
            if(tmpLe)
            {
                tmpLe.transform.localScale = enabled ? new Vector3(1, 1, 1) : new Vector3(0, 0, 0);
                tmpLe.ignoreLayout = !enabled;
            }
        }
    }
}
