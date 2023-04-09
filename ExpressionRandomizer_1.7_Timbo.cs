using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace extraltodeuslExpRandPlugin
{
    sealed class ExpressionRandomizer : MVRScript
    {
        readonly string[] _excludeRegions =
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
            "Torso",
            "Eyes Closed Left",
            "Eyes Closed Right",
            "Mouth Smile Simple Left",
            "Mouth Smile Simple Right",
            "Pose Controls/JCM", // JointCorrect morphs by FallenDancer
        };

        readonly string[] _defaultOn =
        {
            "Brow Outer Up Left",
            "Brow Outer Up Right",
            "Brow Inner Up Left",
            "Brow Inner Up Right",
            "Concentrate",
            "Desire",
            "Flirting",
            "Pupils Dialate", //sic
            "Snarl Left",
            "Snarl Right",
            "Eyes Squint Left",
            "Eyes Squint Right",
            "Lips Pucker",
        };

        readonly string[] _poseRegions =
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

        // particular morph names to add
        readonly string[] _tailorList =
        {
            "Pupils Dialate",
            "Eye Roll Back_DD",
        };

        bool _initialized;
        bool _restoringFromJson;
        List<MorphModel> _enabledMorphs;
        readonly List<MorphModel> _morphModels = new List<MorphModel>();

        JSONStorableBool _playJsb;
        JSONStorableBool _abaJsb;
        JSONStorableFloat _animLengthJsf;
        JSONStorableFloat _animWaitJsf;
        JSONStorableStringChooser _colliderTriggerJssc;
        JSONStorableBool _filterAndSearchJsb;
        JSONStorableBool _manualJsb;
        JSONStorableFloat _masterSpeedJsf;
        JSONStorableFloat _maxJsf;
        JSONStorableFloat _minJsf;
        JSONStorableFloat _multiJsf;
        JSONStorableBool _onlyShowActiveJsb;
        JSONStorableBool _randomJsb;
        JSONStorableBool _smoothJsb;
        JSONStorableFloat _triggerChanceJsf;
        JSONStorableAction _manualTriggerAction;

        InputField _filterInputField;

        GenerateDAZMorphsControlUI _morphsControlUI;

        readonly Color _rustRed = new Color(0.6f, 0.3f, 0.3f, 1f);
        readonly Color _darkRed = new Color(0.75f, 0f, 0f, 1f);

        const string FILTER_DEFAULT_VAL = "Filter morphs...";

        void Start()
        {
            _timer = 0f;
            InvokeRepeating(nameof(TriggerMaintainer), 3f, 3f); // To check if the selected collision trigger is still there every 3 seconds
        }

        public override void Init()
        {
            try
            {
                StartCoroutine(InitCo());
            }
            catch(Exception e)
            {
                SuperController.LogMessage($"{nameof(ExpressionRandomizer)}: {nameof(Init)} error: " + e);
            }
        }

        JSONStorableFloat NewStorableFloat(string name, float val, float min, float max)
        {
            var jsf = new JSONStorableFloat(name, val, min, max)
            {
                storeType = JSONStorableParam.StoreType.Full,
            };
            RegisterFloat(jsf);
            return jsf;
        }

        JSONStorableBool NewStorableBool(string name, bool val)
        {
            var jsb = new JSONStorableBool(name, val)
            {
                storeType = JSONStorableParam.StoreType.Full,
            };
            RegisterBool(jsb);
            return jsb;
        }

        IEnumerator InitCo()
        {
            yield return new WaitForEndOfFrame();
            while(SuperController.singleton.isLoading)
            {
                yield return null;
            }

            GetContainingAtom().GetStorableByID("AutoExpressions").SetBoolParamValue("enabled", false);
            var geometry = (DAZCharacterSelector) containingAtom.GetStorableByID("geometry");
            _morphsControlUI = geometry.morphsControlUI;

            foreach(var morph in _morphsControlUI.GetMorphs())
            {
                if(
                    _poseRegions.Any(morph.region.Contains) &&
                    !_excludeRegions.Any(morph.region.Contains) ||
                    _tailorList.Any(morph.displayName.Contains)
                )
                {
                    var morphModel = new MorphModel(morph, morph.displayName, morph.region)
                    {
                        DefaultOn = _defaultOn.Any(morph.displayName.Equals),
                        Preset1On = _preset1.Any(morph.displayName.Equals),
                        Preset2On = _preset2.Any(morph.displayName.Equals),
                    };
                    _morphModels.Add(morphModel);
                }
            }

            _morphModels.Sort((a, b) => string.Compare(a.Label, b.Label, StringComparison.Ordinal));

            _minJsf = NewStorableFloat("Minimum value", -0.15f, -1f, 1.0f);
            _maxJsf = NewStorableFloat("Maximum value", 0.35f, -1f, 1.0f);
            _multiJsf = NewStorableFloat("Multiplier", 1f, 0f, 2f);
            _masterSpeedJsf = NewStorableFloat("Master speed", 1f, 0f, 10f);
            _playJsb = NewStorableBool("Play", true);
            _smoothJsb = NewStorableBool("Smooth", true);
            _animWaitJsf = NewStorableFloat("Loop length", 2f, 0.1f, 20f);
            _animLengthJsf = NewStorableFloat("Morphing speed", 1.0f, 0.1f, 20f);
            _abaJsb = NewStorableBool("Reset used expressions at loop", true);
            _manualJsb = NewStorableBool("Trigger transitions manually", false);
            _randomJsb = NewStorableBool("Random chances for transitions", false);
            _triggerChanceJsf = NewStorableFloat("Chance to trigger", 75f, 0f, 100f);
            _manualTriggerAction = new JSONStorableAction("Trigger transition", SetNewRandomMorphValues);
            RegisterAction(_manualTriggerAction);
            _colliderTriggerJssc = new JSONStorableStringChooser(
                "Collision trigger",
                new List<string>
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
                },
                "None",
                "Collision trigger"
            );
            RegisterStringChooser(_colliderTriggerJssc);
            _filterAndSearchJsb = new JSONStorableBool("AND search", false);
            _filterAndSearchJsb.setCallbackFunction += val => OnFilterChanged();
            _onlyShowActiveJsb = new JSONStorableBool("Only show active morphs", false);
            _onlyShowActiveJsb.setCallbackFunction += val => OnFilterChanged();

            CreateLeftUI();
            CreateRightUI();

            _enabledMorphs = _morphModels.Where(item => item.EnabledJsb.val).ToList();
            SuperController.singleton.onBeforeSceneSaveHandlers += OnBeforeSceneSave;
            SuperController.singleton.onSceneSavedHandlers += OnSceneSaved;
            _initialized = true;
        }

        UIDynamicButton _moreButton;
        UIDynamicSlider _loopLengthSlider;
        UIDynamicSlider _morphingSpeedSlider;
        UIDynamicButton _setAsDefaultButton;
        UIDynamicButton _resetButton;
        UIDynamicToggle _abaToggle;

        UIDynamicButton _backButton;
        UIDynamicToggle _manualToggle;
        UIDynamicToggle _randomToggle;
        UIDynamicSlider _triggerChanceSlider;
        UIDynamicButton _transitionButton;
        UIDynamicPopup _colliderTriggerPopup;

        void CreateLeftUI()
        {
            CreateSlider(_minJsf);
            CreateSlider(_maxJsf);
            CreateSlider(_multiJsf);
            CreateSlider(_masterSpeedJsf);
            CreateSpacer().height = 50;
            var toggle = SmallToggle(_playJsb, 10, -600);
            toggle.toggle.onValueChanged.AddListener(val => toggle.textColor = val ? Color.black : _darkRed);
            SmallToggle(_smoothJsb, 272, -600);
            CreateAdditionalOptionsUI();
            CreateMoreAdditionalOptionsUI();
            SelectOptionsUI(false);
        }

        void CreateAdditionalOptionsUI()
        {
            _moreButton = OptionsNavButton("More >", 355, -665);
            _moreButton.buttonColor = Color.gray;
            _moreButton.textColor = Color.white;
            _moreButton.button.onClick.AddListener(() => SelectOptionsUI(true));

            CreateHeaderTextField(new JSONStorableString("OptionsHeader", "Additional options"));

            _loopLengthSlider = CreateSlider(_animWaitJsf);
            _morphingSpeedSlider = CreateSlider(_animLengthJsf);
            _setAsDefaultButton = CreateButton("Set Current State As Default");
            _setAsDefaultButton.button.onClick.AddListener(() =>
            {
                foreach(var morphModel in _morphModels)
                {
                    morphModel.UpdateDefaultValue();
                }
            });

            _resetButton = CreateButton("Reset To Default/Load State");
            _resetButton.button.onClick.AddListener(() =>
            {
                _playJsb.val = false;
                foreach(var morphModel in _morphModels)
                {
                    morphModel.ResetToDefault();
                }
            });

            _abaToggle = CreateToggle(_abaJsb);
        }

        void CreateMoreAdditionalOptionsUI()
        {
            _backButton = OptionsNavButton("< Back", 355, -665);
            _backButton.buttonColor = Color.gray;
            _backButton.textColor = Color.white;
            _backButton.button.onClick.AddListener(() => SelectOptionsUI(false));

            _manualToggle = CreateToggle(_manualJsb);
            _randomToggle = CreateToggle(_randomJsb);
            _triggerChanceSlider = CreateSlider(_triggerChanceJsf);

            _transitionButton = CreateButton("Trigger Transition");
            _transitionButton.buttonColor = new Color(0.5f, 1f, 0.5f);
            _manualTriggerAction.RegisterButton(_transitionButton);

            _colliderTriggerPopup = CreatePopup(_colliderTriggerJssc);
            _colliderTriggerPopup.popup.onOpenPopupHandlers += () =>
            {
                _manualJsb.val = true;
                _randomJsb.val = true;
            };
        }

        void SelectOptionsUI(bool alt)
        {
            _loopLengthSlider.SetVisible(!alt);
            _morphingSpeedSlider.SetVisible(!alt);
            _setAsDefaultButton.SetVisible(!alt);
            _resetButton.SetVisible(!alt);
            _abaToggle.SetVisible(!alt);
            _moreButton.SetVisible(!alt);

            _manualToggle.SetVisible(alt);
            _randomToggle.SetVisible(alt);
            _triggerChanceSlider.SetVisible(alt);
            _transitionButton.SetVisible(alt);
            _colliderTriggerPopup.SetVisible(alt);
            _backButton.SetVisible(alt);
        }

        void CreateRightUI()
        {
            CreateSpacer(true).height = 120f;

            var selectNone = SmallButton("Select None", 536, -62);
            selectNone.button.onClick.AddListener(() =>
            {
                foreach(var morphModel in _morphModels)
                {
                    morphModel.EnabledJsb.val = false;
                }
            });

            var selectDefault = SmallButton("Select Default", 800, -62);
            selectDefault.button.onClick.AddListener(() =>
            {
                foreach(var morphModel in _morphModels)
                {
                    if(_manualJsb.val)
                    {
                        _multiJsf.val = 1;
                        _masterSpeedJsf.val = 1;
                    }

                    morphModel.EnabledJsb.val = morphModel.DefaultOn;
                }
            });

            var selectPreset1 = SmallButton("Select Preset 1", 536, -132);
            selectPreset1.button.onClick.AddListener(() =>
            {
                foreach(var morphModel in _morphModels)
                {
                    if(_manualJsb.val)
                    {
                        _multiJsf.val = 1.6f;
                        _masterSpeedJsf.val = 3.0f;
                    }

                    morphModel.EnabledJsb.val = morphModel.Preset1On;
                }
            });

            var selectPreset2 = SmallButton("Select Preset 2", 800, -132);
            selectPreset2.button.onClick.AddListener(() =>
            {
                foreach(var morphModel in _morphModels)
                {
                    if(_manualJsb.val)
                    {
                        _multiJsf.val = 1.8f;
                        _masterSpeedJsf.val = 4.2f;
                    }

                    morphModel.EnabledJsb.val = morphModel.Preset2On;
                }
            });

            // ******* FILTER BOX ***********
            {
                var filterTextJss = new JSONStorableString("FilterText", FILTER_DEFAULT_VAL);
                var filterTextField = CreateTextField(filterTextJss, true);
                SetupTextField(filterTextField, 63, false, false);
                _filterInputField = filterTextField.gameObject.AddComponent<InputField>();
                _filterInputField.textComponent = filterTextField.UItext;
                _filterInputField.lineType = InputField.LineType.SingleLine;
                _filterInputField.onValueChanged.AddListener(_ => OnFilterChanged());
                _filterInputField.text = filterTextJss.val;

                var pointerListener = _filterInputField.gameObject.AddComponent<PointerUpDownListener>();
                pointerListener.PointerDownAction = () =>
                {
                    _preventFilterChangeCallback = true;
                    if(_filterInputField.text == FILTER_DEFAULT_VAL)
                    {
                        _filterInputField.text = "";
                    }

                    _preventFilterChangeCallback = false;
                };
            }

            var clearSearchBtn = ClearButton();
            clearSearchBtn.button.onClick.AddListener(() =>
            {
                _filterInputField.text = "";
                OnFilterChanged(); // force trigger if value unchanged
            });

            CreateToggle(_filterAndSearchJsb, true);
            CreateToggle(_onlyShowActiveJsb, true);

            var zeroMorphButton = SmallButton("Zero selected", 800, -405);
            zeroMorphButton.buttonColor = _rustRed;
            zeroMorphButton.textColor = Color.white;
            zeroMorphButton.button.onClick.AddListener(() =>
            {
                _playJsb.val = false;
                foreach(var morphModel in _morphModels)
                {
                    morphModel.ZeroValue();
                }
            });

            CreateHeaderTextField(new JSONStorableString("MorphsHeader", "Morphs"), true);

            /* Dev sliders for aligning custom elements */
            // CreateSlider(_posX, true);
            // CreateSlider(_posY, true);
            // CreateSlider(_sizeX, true);
            // CreateSlider(_sizeY, true);

            foreach(var morphModel in _morphModels)
            {
                morphModel.EnabledJsb = NewStorableBool(morphModel.Label, morphModel.DefaultOn);
                morphModel.EnabledJsb.setCallbackFunction = on =>
                {
                    if(!on)
                    {
                        var morph = _morphsControlUI.GetMorphByDisplayName(morphModel.DisplayName);
                        morph.morphValue = 0;
                    }

                    _enabledMorphs = _morphModels.Where(item => item.EnabledJsb.val).ToList();
                };
                morphModel.Toggle = CreateToggle(morphModel.EnabledJsb, true);
            }
        }

        void CreateHeaderTextField(JSONStorableString filterHeaderJss, bool rightSide = false)
        {
            var textField = CreateTextField(filterHeaderJss, rightSide);
            textField.UItext.fontSize = 30;
            textField.backgroundColor = Color.clear;
            textField.text = $"<size=8>\n</size><b>{filterHeaderJss.val}</b>";
            var layout = textField.GetComponent<LayoutElement>();
            layout.preferredHeight = 50;
            layout.minHeight = 50;
        }

        UIDynamicToggle SmallToggle(JSONStorableBool jsb, int x, int y, bool callbacks = false)
        {
            var t = InstantiateToContent(manager.configurableTogglePrefab);
            var rectTransform = GetRekt(t);
            rectTransform.anchoredPosition = new Vector2(x, y);
            rectTransform.sizeDelta = new Vector2(-805, 52);
            if(callbacks)
            {
                SetDevUISliderCallbacks(rectTransform);
            }

            var toggle = t.GetComponent<UIDynamicToggle>();
            toggle.label = jsb.name;
            AddToggleToJsb(toggle, jsb);
            return toggle;
        }

        UIDynamicButton SmallButton(string label, int x, int y, bool callbacks = false)
        {
            var t = InstantiateToContent(manager.configurableButtonPrefab);
            var rectTransform = GetRekt(t);
            rectTransform.anchoredPosition = new Vector2(x, y);
            rectTransform.sizeDelta = new Vector2(-805, 52);
            if(callbacks)
            {
                SetDevUISliderCallbacks(rectTransform);
            }

            var button = t.GetComponent<UIDynamicButton>();
            button.label = label;
            return button;
        }

        UIDynamicButton OptionsNavButton(string label, int x, int y)
        {
            var t = InstantiateToContent(manager.configurableButtonPrefab);
            var rectTransform = GetRekt(t);
            rectTransform.anchoredPosition = new Vector2(x, y);
            rectTransform.sizeDelta = new Vector2(-890, 52);
            var button = t.GetComponent<UIDynamicButton>();
            button.label = label;
            return button;
        }

        UIDynamicButton ClearButton()
        {
            var t = InstantiateToContent(manager.configurableButtonPrefab);
            var rectTransform = GetRekt(t);
            rectTransform.anchoredPosition = new Vector2(936, -208);
            rectTransform.sizeDelta = new Vector2(-940, 63);
            var button = t.GetComponent<UIDynamicButton>();
            button.label = "Clear";
            button.buttonColor = _rustRed;
            button.textColor = new Color(1f, 1f, 1f, 1f);
            return button;
        }

        Transform InstantiateToContent<T>(T prefab) where T : Transform
        {
            var parent = UITransform.Find("Scroll View/Viewport/Content");
            var childTransform = Instantiate(prefab, parent, false);
            return childTransform;
        }

        static RectTransform GetRekt(Transform transform)
        {
            var rectTransform = transform.GetComponent<RectTransform>();
            rectTransform.pivot = new Vector2(0, 0);
            return rectTransform;
        }

        void AddToggleToJsb(UIDynamicToggle toggle, JSONStorableBool jsb)
        {
            jsb.toggle = toggle.toggle;
            toggleToJSONStorableBool.Add(toggle, jsb);
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
            var scrollRect = target.UItext.transform.parent.transform.parent.transform.parent.GetComponent<ScrollRect>();
            if(scrollRect)
            {
                scrollRect.horizontal = false;
                scrollRect.vertical = false;
            }
        }

        bool _preventFilterChangeCallback;

        void OnFilterChanged()
        {
            if(!_filterInputField || _onlyShowActiveJsb == null || _filterAndSearchJsb == null || _preventFilterChangeCallback)
            {
                return;
            }

            string trimmed = _filterInputField.text.Trim();

            // Field reset
            if(string.IsNullOrEmpty(trimmed))
            {
                _filterInputField.text = FILTER_DEFAULT_VAL;
                _filterInputField.Select();
                return;
            }

            // not searching under 3 letters
            if(trimmed.Length <= 3)
            {
                return;
            }

            // Grabbing the search
            string[] searchWords = trimmed.Split(' ');
            var searchList = new List<string>();

            // Cleaning the search
            foreach(string word in searchWords)
            {
                if(word.Length > 1)
                {
                    searchList.Add(word);
                }
            }

            // Searching the toggle
            foreach(var morphModel in _morphModels)
            {
                // Displaying everything and returning if we have the default value in the field and do not try to filter active only
                if(!_onlyShowActiveJsb.val && trimmed == FILTER_DEFAULT_VAL)
                {
                    morphModel.Toggle.SetVisible(true);
                    continue;
                }

                int searchHit = 0;

                if(!_onlyShowActiveJsb.val || _onlyShowActiveJsb.val && morphModel.EnabledJsb.val)
                {
                    // Doing word search only if we don't have the default value
                    if(trimmed != FILTER_DEFAULT_VAL)
                    {
                        foreach(string search in searchList)
                        {
                            string labelText = morphModel.Toggle.labelText.text.ToLower();
                            string searchVal = search.ToLower();
                            if(labelText.Contains(searchVal))
                            {
                                searchHit++;
                            }
                        }
                    }
                    else if(_onlyShowActiveJsb.val && morphModel.EnabledJsb.val)
                    {
                        // We have the default value in the search text and we want to only show active
                        // So we simply make this result valid for any situation below
                        searchHit = searchList.Count;
                    }
                }

                if(!_filterAndSearchJsb.val && searchHit > 0)
                {
                    morphModel.Toggle.SetVisible(true);
                }
                else if(_filterAndSearchJsb.val && searchHit == searchList.Count)
                {
                    morphModel.Toggle.SetVisible(true);
                }
                else
                {
                    morphModel.Toggle.SetVisible(false);
                }
            }
        }

        bool _globalAnimationFrozen;
        float _timer;

        void Update()
        {
            _globalAnimationFrozen = GlobalAnimationFrozen();
            if(!_playJsb.val || _globalAnimationFrozen || _savingScene || !_initialized || _restoringFromJson)
            {
                return;
            }

            try
            {
                _timer += Time.deltaTime;
                if(_timer >= _animWaitJsf.val / _masterSpeedJsf.val)
                {
                    _timer = 0;
                    if(!_manualJsb.val)
                    {
                        SetNewRandomMorphValues();
                    }
                }
            }
            catch(Exception e)
            {
                SuperController.LogMessage($"{nameof(ExpressionRandomizer)}: {nameof(Update)} error: " + e);
                enabled = false;
            }
        }

        void FixedUpdate()
        {
            if(!_playJsb.val || !enabled || _globalAnimationFrozen || _savingScene || !_initialized || _restoringFromJson)
            {
                return;
            }

            try
            {
                foreach(var morphModel in _enabledMorphs)
                {
                    if(morphModel.EnabledJsb.val)
                    {
                        morphModel.CalculateMorphValue(_smoothJsb.val, _animLengthJsf.val, _masterSpeedJsf.val, _timer, _animWaitJsf.val);
                    }
                }
            }
            catch(Exception e)
            {
                SuperController.LogMessage($"{nameof(ExpressionRandomizer)}: {nameof(FixedUpdate)} error: " + e);
                enabled = false;
            }
        }

        void SetNewRandomMorphValues()
        {
            if(UnityEngine.Random.Range(0f, 100f) <= _triggerChanceJsf.val || !_randomJsb.val)
            {
                foreach(var morphModel in _enabledMorphs)
                {
                    if(!morphModel.EnabledJsb.val)
                    {
                        continue;
                    }

                    morphModel.SetNewMorphValue(_minJsf.val, _maxJsf.val, _multiJsf.val, _abaJsb.val);
                }
            }
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
            foreach(string triggerName in _colliderTriggerJssc.choices)
            {
                if(triggerName != _colliderTriggerJssc.val && triggerName != "None")
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
                SuperController.LogMessage($"{nameof(ExpressionRandomizer)}: Couldn't find trigger " + triggerName);
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
            if(!enabled)
            {
                return;
            }

            if(_colliderTriggerJssc.val != "None")
            {
                CreateTrigger(_colliderTriggerJssc.val);
                CleanTriggers();
            }
        }

        void ResetMorphs()
        {
            foreach(var morphModel in _morphModels)
            {
                morphModel.ResetToInitial();
            }
        }

        public override void RestoreFromJSON(
            JSONClass jc,
            bool restorePhysical = true,
            bool restoreAppearance = true,
            JSONArray presetAtoms = null,
            bool setMissingToDefault = true
        )
        {
            _restoringFromJson = true;

            StartCoroutine(
                RestoreFromJSONCo(
                    jc,
                    restorePhysical,
                    restoreAppearance,
                    presetAtoms,
                    setMissingToDefault
                )
            );
        }

        IEnumerator RestoreFromJSONCo(
            JSONClass jc,
            bool restorePhysical,
            bool restoreAppearance,
            JSONArray presetAtoms,
            bool setMissingToDefault
        )
        {
            while(!_initialized)
            {
                yield return null;
            }

            base.RestoreFromJSON(jc, restorePhysical, restoreAppearance, presetAtoms, setMissingToDefault);
            _restoringFromJson = false;
        }

        static bool _savingScene;

        void OnBeforeSceneSave()
        {
            _savingScene = true;
            ResetMorphs();
        }

        static void OnSceneSaved()
        {
            _savingScene = false;
        }

        void OnDisable()
        {
            try
            {
                ResetMorphs();
            }
            catch(Exception e)
            {
                SuperController.LogMessage($"{nameof(ExpressionRandomizer)}: {nameof(OnDisable)} error: " + e);
                throw;
            }
        }

        void OnDestroy()
        {
            try
            {
                SuperController.singleton.onSceneSavedHandlers -= OnSceneSaved;
                SuperController.singleton.onBeforeSceneSaveHandlers -= OnBeforeSceneSave;
            }
            catch(Exception e)
            {
                SuperController.LogMessage($"{nameof(ExpressionRandomizer)}: {nameof(OnDestroy)} error: " + e);
                throw;
            }
        }

#region Utils

        static bool GlobalAnimationFrozen()
        {
            bool mainToggleFrozen =
                SuperController.singleton.freezeAnimationToggle &&
                SuperController.singleton.freezeAnimationToggle.isOn;
            bool altToggleFrozen =
                SuperController.singleton.freezeAnimationToggleAlt &&
                SuperController.singleton.freezeAnimationToggleAlt.isOn;
            return mainToggleFrozen || altToggleFrozen;
        }

#endregion

#region dev

        readonly JSONStorableFloat _posX = new JSONStorableFloat("posX", 0, -3000, 3000);
        readonly JSONStorableFloat _posY = new JSONStorableFloat("posY", 0, -3000, 3000);
        readonly JSONStorableFloat _sizeX = new JSONStorableFloat("sizeX", 0, -2000, 2000);
        readonly JSONStorableFloat _sizeY = new JSONStorableFloat("sizeY", 0, -2000, 2000);

        void SetDevUISliderCallbacks(RectTransform rectTransform)
        {
            var anchoredPosition = rectTransform.anchoredPosition;
            _posX.defaultVal = anchoredPosition.x;
            _posY.defaultVal = anchoredPosition.y;
            _posX.val = _posX.defaultVal;
            _posY.val = _posY.defaultVal;
            var sizeDelta = rectTransform.sizeDelta;
            _sizeX.defaultVal = sizeDelta.x;
            _sizeY.defaultVal = sizeDelta.y;
            _sizeX.val = _sizeX.defaultVal;
            _sizeY.val = _sizeY.defaultVal;

            _posX.setCallbackFunction = value =>
            {
                if(rectTransform)
                {
                    rectTransform.anchoredPosition = new Vector2(value, _posY.val);
                }
            };
            _posY.setCallbackFunction = value =>
            {
                if(rectTransform)
                {
                    rectTransform.anchoredPosition = new Vector2(_posX.val, value);
                }
            };
            _sizeX.setCallbackFunction = value =>
            {
                if(rectTransform)
                {
                    rectTransform.sizeDelta = new Vector2(value, _sizeY.val);
                }
            };
            _sizeY.setCallbackFunction = value =>
            {
                if(rectTransform)
                {
                    rectTransform.sizeDelta = new Vector2(_sizeX.val, value);
                }
            };
        }

#endregion
    }

    sealed class MorphModel
    {
        public DAZMorph Morph { get; }
        public string DisplayName { get; }
        public string Label { get; }
        public bool DefaultOn { get; set; }
        public bool Preset1On { get; set; }
        public bool Preset2On { get; set; }
        public JSONStorableBool EnabledJsb { get; set; }
        public UIDynamicToggle Toggle { get; set; }

        readonly float _initialMorphValue;
        float _defaultMorphValue;
        float _currentMorphValue;
        float _newMorphValue;

        public MorphModel(DAZMorph morph, string displayName, string region)
        {
            Morph = morph;
            DisplayName = displayName;
            string upperRegion = Regex.Split(region, "/").LastOrDefault() ?? "";
            Label = upperRegion + "/" + DisplayName;
            _initialMorphValue = Morph.morphValue;
            _defaultMorphValue = _initialMorphValue;
            Morph.morphValue = 0; // TODO correct?
            _currentMorphValue = Morph.morphValue;
        }

        public void CalculateMorphValue(bool smooth, float animLength, float masterSpeed, float timer, float animWait)
        {
            _currentMorphValue = smooth
                ? SmoothValue(animLength, masterSpeed, timer, animWait)
                : LinearValue(animLength, masterSpeed);
            Morph.morphValue = _currentMorphValue;
        }

        public void SetNewMorphValue(float min, float max, float multi, bool aba)
        {
            _newMorphValue = aba && _currentMorphValue > 0.1f
                ? 0
                : UnityEngine.Random.Range(min, max) * multi;
        }

        public void UpdateDefaultValue()
        {
            _defaultMorphValue = Morph.morphValue;
        }

        public void ResetToDefault()
        {
            Morph.morphValue = _defaultMorphValue;
        }

        public void ResetToInitial()
        {
            Morph.morphValue = _initialMorphValue;
        }

        public void ZeroValue()
        {
            if(EnabledJsb.val)
            {
                Morph.morphValue = 0;
            }
        }

        float SmoothValue(float animLength, float masterSpeed, float timer, float animWait)
        {
            return Mathf.Lerp(_currentMorphValue, _newMorphValue,
                Time.deltaTime *
                animLength *
                masterSpeed *
                Mathf.Sin(timer / (animWait / masterSpeed) * Mathf.PI));
        }

        float LinearValue(float animLength, float masterSpeed)
        {
            return Mathf.Lerp(_currentMorphValue, _newMorphValue, Time.deltaTime * animLength * masterSpeed);
        }
    }

    sealed class PointerUpDownListener : MonoBehaviour, IPointerUpHandler, IPointerDownHandler
    {
        public bool isDown;

        public Action PointerUpAction { private get; set; }
        public Action PointerDownAction { private get; set; }

        public void OnPointerUp(PointerEventData data)
        {
            isDown = false;
            PointerUpAction?.Invoke();
        }

        public void OnPointerDown(PointerEventData data)
        {
            isDown = true;
            PointerDownAction?.Invoke();
        }
    }

    static class UIDynamicExtensions
    {
        public static void SetVisible(this UIDynamic uiDynamic, bool visible)
        {
            if(!uiDynamic)
            {
                return;
            }

            var layoutElement = uiDynamic.GetComponent<LayoutElement>();
            if(layoutElement)
            {
                layoutElement.transform.localScale = visible ? new Vector3(1, 1, 1) : new Vector3(0, 0, 0);
                layoutElement.ignoreLayout = !visible;
            }
        }
    }
}
