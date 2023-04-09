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

        List<string> _collisionTriggerOptions;
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

        JSONStorableFloat _triggerChanceJsf;

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

#region Buttons Preparation

            _playJsb = new JSONStorableBool("Play", true)
            {
                storeType = JSONStorableParam.StoreType.Full,
            };
            RegisterBool(_playJsb);
            CreateToggle(_playJsb);

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

            var manualTriggerAction = new JSONStorableAction("Trigger transition", SetNewRandomMorphValues);
            RegisterAction(manualTriggerAction);

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

#endregion

#region Helper Buttons

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
            clearSearchBtn.button.onClick.AddListener(() => _filterInputField.text = "");

            // ******* AND SEARCH ***********
            _filterAndSearchJsb = new JSONStorableBool("AND search", false);
            _filterAndSearchJsb.setCallbackFunction += val => OnFilterChanged();
            CreateToggle(_filterAndSearchJsb, true);

            _onlyShowActiveJsb = new JSONStorableBool("Only show active morphs", false);
            _onlyShowActiveJsb.setCallbackFunction += val => OnFilterChanged();
            CreateToggle(_onlyShowActiveJsb, true);

            // ******* MORPHS HEADER ***********
            {
                var filterHeaderJss = new JSONStorableString("MorphsHeader", "");
                var textField = CreateTextField(filterHeaderJss, true);
                textField.UItext.fontSize = 28;
                // textField.textColor = Color.white;
                textField.backgroundColor = Color.clear;
                textField.text = "<size=8>\n</size><b>Morphs</b>";
                var layout = textField.GetComponent<LayoutElement>();
                layout.preferredHeight = 42;
                layout.minHeight = 42;
            }

#endregion

            foreach(var morphModel in _morphModels)
            {
                morphModel.EnabledJsb = new JSONStorableBool(
                    morphModel.UpperRegion + "/" + morphModel.DisplayName,
                    morphModel.DefaultOn
                )
                {
                    storeType = JSONStorableParam.StoreType.Full,
                    setCallbackFunction = on =>
                    {
                        if(!on)
                        {
                            var morph = _morphsControlUI.GetMorphByDisplayName(morphModel.DisplayName);
                            morph.morphValue = 0;
                        }

                        _enabledMorphs = _morphModels.Where(item => item.EnabledJsb.val).ToList();
                    },
                };
                RegisterBool(morphModel.EnabledJsb);
                morphModel.Toggle = CreateToggle(morphModel.EnabledJsb, true);
            }

            _enabledMorphs = _morphModels.Where(item => item.EnabledJsb.val).ToList();

            var transitionButton = CreateButton("Trigger Transition");
            transitionButton.button.onClick.AddListener(SetNewRandomMorphValues);
            transitionButton.buttonColor = new Color(0.5f, 1f, 0.5f);

            _collisionTriggerOptions = new List<string>
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

            _colliderStringChooser = new JSONStorableStringChooser("Collision trigger", _collisionTriggerOptions, "None", "Collision trigger");
            RegisterStringChooser(_colliderStringChooser);
            var dp = CreatePopup(_colliderStringChooser);
            dp.popup.onOpenPopupHandlers += () =>
            {
                _manualJsb.val = true;
                _randomJsb.val = true;
            };

            var animatableButton = CreateButton("Clear Animatable (from selected)"); // TODO what does this do?
            animatableButton.button.onClick.AddListener(() =>
            {
                foreach(var morphModel in _morphModels)
                {
                    if(morphModel.EnabledJsb.val)
                    {
                        morphModel.Morph.animatable = true;
                    }
                }
            });

            var setAsDefaultButton = CreateButton("Set Current State As Default");
            setAsDefaultButton.button.onClick.AddListener(() =>
            {
                foreach(var morphModel in _morphModels)
                {
                    morphModel.UpdateDefaultValue();
                }
            });

            var resetButton = CreateButton("Reset To Default/Load State");
            resetButton.button.onClick.AddListener(() =>
            {
                _playJsb.val = false;
                foreach(var morphModel in _morphModels)
                {
                    morphModel.ResetToDefault();
                }
            });

            var zeroMorphButton = CreateButton("Zero Selected");
            zeroMorphButton.button.onClick.AddListener(() =>
            {
                _playJsb.val = false;
                foreach(var morphModel in _morphModels)
                {
                    morphModel.ZeroValue();
                }
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

            SuperController.singleton.onBeforeSceneSaveHandlers += OnBeforeSceneSave;
            SuperController.singleton.onSceneSavedHandlers += OnSceneSaved;

            _initialized = true;
        }

        UIDynamicButton SmallButton(string label, int x, int y)
        {
            var parent = UITransform.Find("Scroll View/Viewport/Content");
            var buttonTransform = Instantiate(manager.configurableButtonPrefab, parent, false);
            Destroy(buttonTransform.GetComponent<LayoutElement>());
            var rectTransform = buttonTransform.GetComponent<RectTransform>();
            rectTransform.pivot = new Vector2(0, 0);
            rectTransform.anchoredPosition = new Vector2(x, y);
            rectTransform.sizeDelta = new Vector2(-805, 52);
            var button = buttonTransform.GetComponent<UIDynamicButton>();
            button.label = label;
            return button;
        }

        UIDynamicButton ClearButton()
        {
            var parent = UITransform.Find("Scroll View/Viewport/Content");
            var buttonTransform = Instantiate(manager.configurableButtonPrefab, parent, false);
            Destroy(buttonTransform.GetComponent<LayoutElement>());
            var rectTransform = buttonTransform.GetComponent<RectTransform>();
            rectTransform.pivot = new Vector2(0, 0);
            rectTransform.anchoredPosition = new Vector2(936, -208);
            rectTransform.sizeDelta = new Vector2(-940, 63);
            var button = buttonTransform.GetComponent<UIDynamicButton>();
            button.label = "Clear";
            button.buttonColor = new Color(0.6f, 0.3f, 0.3f, 1f);
            button.textColor = new Color(1f, 1f, 1f, 1f);
            return button;
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

            // Field reset
            if(_filterInputField.text.Trim() == "")
            {
                _filterInputField.text = FILTER_DEFAULT_VAL;
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
                if(!_onlyShowActiveJsb.val && _filterInputField.text == FILTER_DEFAULT_VAL)
                {
                    SetUIDynamicToggleVisibility(morphModel.Toggle, true);
                    continue;
                }

                int searchHit = 0;

                if(!_onlyShowActiveJsb.val || _onlyShowActiveJsb.val && morphModel.EnabledJsb.val)
                {
                    // Doing word search only if we don't have the default value
                    if(_filterInputField.text != FILTER_DEFAULT_VAL)
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
                    SetUIDynamicToggleVisibility(morphModel.Toggle, true);
                }
                else if(_filterAndSearchJsb.val && searchHit == searchList.Count)
                {
                    SetUIDynamicToggleVisibility(morphModel.Toggle, true);
                }
                else
                {
                    SetUIDynamicToggleVisibility(morphModel.Toggle, false);
                }
            }
        }

        static void SetUIDynamicToggleVisibility(UIDynamicToggle target, bool enabled)
        {
            if(!target)
            {
                return;
            }

            var layoutElement = target.GetComponent<LayoutElement>();
            if(layoutElement)
            {
                layoutElement.transform.localScale = enabled ? new Vector3(1, 1, 1) : new Vector3(0, 0, 0);
                layoutElement.ignoreLayout = !enabled;
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
                // morph progressively every morphs to their new values
                const string textBoxMessage = "\n Animatable morphs (not animated) :\n";
                string animatableSelected = textBoxMessage;

                foreach(var morphModel in _enabledMorphs)
                {
                    if(!morphModel.EnabledJsb.val)
                    {
                        continue;
                    }

                    if(morphModel.Morph.animatable)
                    {
                        morphModel.CalculateMorphValue(_smoothJsb.val, _animLengthJsf.val, _masterSpeedJsf.val, _timer, _animWaitJsf.val);
                    }
                    else
                    {
                        animatableSelected += $" {morphModel.DisplayName}\n";
                    }
                }

                if(animatableSelected != textBoxMessage || _morphListJss.val != textBoxMessage)
                {
                    _morphListJss.val = animatableSelected;
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
            foreach(string triggerName in _collisionTriggerOptions)
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

            if(_colliderStringChooser.val != "None")
            {
                CreateTrigger(_colliderStringChooser.val);
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
    }

    sealed class MorphModel
    {
        public DAZMorph Morph { get; }
        public string DisplayName { get; }
        public string UpperRegion { get; }
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
            UpperRegion = Regex.Split(region, "/").LastOrDefault() ?? "";
            _initialMorphValue = Morph.morphValue;
            _defaultMorphValue = _initialMorphValue;
            Morph.morphValue = 0; // TODO correct?
            _currentMorphValue = Morph.morphValue;
        }

        public void CalculateMorphValue(bool smooth, float animLength, float masterSpeed, float timer, float animWait)
        {
            _currentMorphValue = smooth
                ? SmoothValeur(animLength, masterSpeed, timer, animWait)
                : LinearValeur(animLength, masterSpeed);
            Morph.morphValue = _currentMorphValue;
        }

        public void SetNewMorphValue(float min, float max, float multi, bool aba)
        {
            if(Morph.animatable)
            {
                _newMorphValue = aba && _currentMorphValue > 0.1f
                    ? 0
                    : UnityEngine.Random.Range(min, max) * multi;
            }
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

        float SmoothValeur(float animLength, float masterSpeed, float timer, float animWait)
        {
            return Mathf.Lerp(_currentMorphValue, _newMorphValue,
                Time.deltaTime *
                animLength *
                masterSpeed *
                Mathf.Sin(timer / (animWait / masterSpeed) * Mathf.PI));
        }

        float LinearValeur(float animLength, float masterSpeed)
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
}
