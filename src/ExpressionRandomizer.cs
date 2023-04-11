#define ENV_DEVELOPMENT
using ExpressionRND.Models;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace extraltodeus
{
    sealed class ExpressionRandomizer : ScriptBase
    {
        const string VERSION = "0.0.0";

        public override bool ShouldIgnore()
        {
            return false;
        }

        bool _uiInitialized;

        protected override Action OnUIEnabled()
        {
            return UIEnabled;
        }

        void UIEnabled()
        {
            if(_uiInitialized)
            {
                return;
            }

            CreateLeftUI();
            CreateRightUI();

            _uiInitialized = true;
            OnFilterChanged();
        }

        protected override Action OnUIDisabled()
        {
            return UIDisabled;
        }

        void UIDisabled()
        {
            if(_collisionTriggerPopup)
            {
                _collisionTriggerPopup.popup.visible = false;
            }

            if(_regionPopup)
            {
                _regionPopup.popup.visible = false;
            }
        }

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

        bool _restoringFromJson;
        GenerateDAZMorphsControlUI _morphsControlUI;
        List<MorphModel> _enabledMorphs;
        readonly List<MorphModel> _morphModels = new List<MorphModel>();

        JSONStorableFloat _minJsf;
        JSONStorableFloat _maxJsf;
        JSONStorableFloat _multiJsf;
        JSONStorableFloat _masterSpeedJsf;
        JSONStorableBool _playJsb;
        JSONStorableBool _smoothJsb;
        JSONStorableFloat _animWaitJsf;
        JSONStorableFloat _animLengthJsf;
        JSONStorableBool _abaJsb;
        JSONStorableBool _manualJsb;
        JSONStorableBool _randomJsb;
        JSONStorableFloat _triggerChanceJsf;
        JSONStorableAction _manualTriggerAction;
        JSONStorableStringChooser _collisionTriggerJssc;
        JSONStorableBool _useAndFilterJsb;
        JSONStorableBool _onlyShowActiveJsb;
        JSONStorableString _pagesJss;
        JSONStorableStringChooser _regionJssc;

        InputField _filterInputField;
        UnityEventsListener _colliderTriggerPopupListener;
        UnityEventsListener _regionPopupListener;

        const string EXP_RAND_TRIGGER = "ExpRandTrigger";
        const string COLLISION_TRIGGER_DEFAULT_VAL = "None";
        const string FILTER_DEFAULT_VAL = "Filter morphs...";

        void Start()
        {
            _timer = 0f;
            InvokeRepeating(nameof(TriggerMaintainer), 3f, 3f); // To check if the selected collision trigger is still there every 3 seconds
        }

        void TriggerMaintainer()
        {
            if(!enabled)
            {
                return;
            }

            if(_collisionTriggerJssc.val != COLLISION_TRIGGER_DEFAULT_VAL)
            {
                CreateTrigger(_collisionTriggerJssc.val);
                ClearOtherTriggers();
            }
        }

        void CreateTrigger(string triggerName)
        {
            var collisionTrigger = containingAtom.GetStorableByID(triggerName) as CollisionTrigger;
            if(!CheckIfTriggerExists(collisionTrigger))
            {
                if(collisionTrigger)
                {
                    collisionTrigger.enabled = true;
                    var startTrigger = collisionTrigger.trigger.CreateDiscreteActionStartInternal();
                    startTrigger.name = EXP_RAND_TRIGGER;
                    startTrigger.receiverAtom = containingAtom;
                    startTrigger.receiver = this;
                    startTrigger.receiverTargetName = _manualTriggerAction.name;
                }
            }
        }

        void ClearOtherTriggers()
        {
            foreach(string triggerName in _collisionTriggerJssc.choices)
            {
                if(triggerName != _collisionTriggerJssc.val)
                {
                    ClearTriggers(triggerName);
                }
            }
        }

        void ClearTriggers(string triggerName)
        {
            if(triggerName == COLLISION_TRIGGER_DEFAULT_VAL)
            {
                return;
            }

            var collisionTrigger = containingAtom.GetStorableByID(triggerName) as CollisionTrigger;
            if(collisionTrigger)
            {
                var triggerJSON = collisionTrigger.trigger.GetJSON();
                var startActions = triggerJSON["startActions"].AsArray;
                for(int i = 0; i < startActions.Count; i++)
                {
                    if(startActions[i]["name"].Value == EXP_RAND_TRIGGER)
                    {
                        startActions.Remove(i);
                    }
                }

                collisionTrigger.trigger.RestoreFromJSON(triggerJSON);
            }
            else
            {
                Loggr.Message($"{nameof(ClearTriggers)} error: Couldn't find trigger " + triggerName);
            }
        }

        public override void Init()
        {
            StartCoroutine(InitCo());
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

            containingAtom.GetStorableByID("AutoExpressions").SetBoolParamValue("enabled", false);
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
            _playJsb.setCallbackFunction = value => _play = value;
            _play = _playJsb.val;
            _smoothJsb = NewStorableBool("Smooth", true);
            _animWaitJsf = NewStorableFloat("Loop length", 2f, 0.1f, 20f);
            _animLengthJsf = NewStorableFloat("Morphing speed", 1.0f, 0.1f, 20f);
            _abaJsb = NewStorableBool("Reset used expressions at loop", true);
            _manualJsb = NewStorableBool("Trigger transitions manually", false);
            _randomJsb = NewStorableBool("Random chances for transitions", false);
            _triggerChanceJsf = NewStorableFloat("Chance to trigger", 75f, 0f, 100f);
            _manualTriggerAction = new JSONStorableAction("Trigger transition", SetNewRandomMorphValues);
            RegisterAction(_manualTriggerAction);

            _collisionTriggerJssc = new JSONStorableStringChooser(
                "Collision trigger",
                new List<string>
                {
                    COLLISION_TRIGGER_DEFAULT_VAL,
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
                COLLISION_TRIGGER_DEFAULT_VAL,
                "Collision trigger"
            );
            RegisterStringChooser(_collisionTriggerJssc);
            _collisionTriggerJssc.setCallbackFunction = val =>
            {
                if(val != COLLISION_TRIGGER_DEFAULT_VAL)
                {
                    _manualJsb.val = true;
                    _randomJsb.val = true;
                }
                else
                {
                    ClearOtherTriggers();
                }
            };
            if(_collisionTriggerJssc.val != "None")
            {
                _manualJsb.val = true;
                _randomJsb.val = true;
            }

            _useAndFilterJsb = new JSONStorableBool("AND filter", false)
            {
                setCallbackFunction = _ => OnFilterChanged(),
            };
            _onlyShowActiveJsb = new JSONStorableBool("Active only", false)
            {
                setCallbackFunction = _ => OnFilterChanged(),
            };
            _pagesJss = new JSONStorableString("Pages", "");

            var regionOptions = new List<string> { "All" };
            regionOptions.AddRange(_morphModels.Select(morphModel => morphModel.FinalTwoRegions).Distinct());
            _regionJssc = new JSONStorableStringChooser(
                "Region",
                regionOptions,
                "All",
                "Region",
                (string _) => OnFilterChanged()
            );

            _enabledMorphs = new List<MorphModel>();
            foreach(var morphModel in _morphModels)
            {
                morphModel.EnabledJsb = NewStorableBool(morphModel.Label, morphModel.DefaultOn);
                morphModel.EnabledJsb.setCallbackFunction = on =>
                {
                    if(!on)
                    {
                        morphModel.ZeroValue();
                    }

                    _enabledMorphs.Clear();
                    _enabledMorphs.AddRange(_morphModels.Where(item => item.EnabledJsb.val));
                };
                if(morphModel.EnabledJsb.val)
                {
                    _enabledMorphs.Add(morphModel);
                }
            }

            SuperController.singleton.onBeforeSceneSaveHandlers += OnBeforeSceneSave;
            SuperController.singleton.onSceneSavedHandlers += OnSceneSaved;
            initialized = true;
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
        UIDynamicPopup _collisionTriggerPopup;

        void CreateLeftUI()
        {
            CreateHeaderTextField($"ExpressionRandomizer v{VERSION}", 33);
            CreateSlider(_minJsf);
            CreateSlider(_maxJsf);
            CreateSlider(_multiJsf);
            CreateSlider(_masterSpeedJsf);
            CreateSpacer().height = 50;
            var toggle = CreateSmallToggle(_playJsb, 10, -668);
            toggle.toggle.onValueChanged.AddListener(val => toggle.textColor = val ? Color.black : Colors.darkRed);
            CreateSmallToggle(_smoothJsb, 280, -668);
            CreateAdditionalOptionsUI();
            CreateMoreAdditionalOptionsUI();
            SelectOptionsUI(false);
        }

        void CreateAdditionalOptionsUI()
        {
            _moreButton = CreateNavButton("More >", 339, -733);
            _moreButton.buttonColor = Color.gray;
            _moreButton.textColor = Color.white;
            _moreButton.button.onClick.AddListener(() => SelectOptionsUI(true));

            CreateHeaderTextField("Additional Options", 30);

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
            _backButton = CreateNavButton("< Back", 339, -733);
            _backButton.buttonColor = Color.gray;
            _backButton.textColor = Color.white;
            _backButton.button.onClick.AddListener(() => SelectOptionsUI(false));

            _manualToggle = CreateToggle(_manualJsb);
            _randomToggle = CreateToggle(_randomJsb);
            _triggerChanceSlider = CreateSlider(_triggerChanceJsf);

            _transitionButton = CreateButton("Trigger Transition");
            _transitionButton.buttonColor = new Color(0.5f, 1f, 0.5f);
            _manualTriggerAction.RegisterButton(_transitionButton);

            _collisionTriggerPopup = CreateCollisionTriggerPopup();

            /* Back button is higher in hierarchy due to being parented to Content instead of LeftContent.
             * Custom listener is added because _colliderTriggerPopup.popup doesn't have an "onClosePopupHandlers" delegate.
             */
            _colliderTriggerPopupListener = _collisionTriggerPopup.popup.popupPanel.gameObject.AddComponent<UnityEventsListener>();
            _colliderTriggerPopupListener.onEnable.AddListener(() =>
            {
                _backButton.SetVisible(false);
                if(_regionPopup)
                {
                    _regionPopup.popup.visible = false;
                }
            });
            _colliderTriggerPopupListener.onDisable.AddListener(() => _backButton.SetVisible(true));
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
            _collisionTriggerPopup.SetVisible(alt);
            _backButton.SetVisible(alt);
        }

        string _filterText = "";
        UIDynamicButton _prevPageButton;
        UIDynamicButton _nextPageButton;
        UIDynamicPopup _regionPopup;

        readonly UIDynamicToggle[] _morphToggles = new UIDynamicToggle[10];

        void CreateRightUI()
        {
            CreateSpacer(true).height = 120f;

            var selectNone = CreateSmallButton("Select None", 550, -62);
            selectNone.button.onClick.AddListener(() =>
            {
                foreach(var morphModel in _morphModels)
                {
                    morphModel.EnabledJsb.val = false;
                }
            });

            var selectDefault = CreateSmallButton("Select Default", 820, -62);
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

            var selectPreset1 = CreateSmallButton("Select Preset 1", 550, -132);
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

            var selectPreset2 = CreateSmallButton("Select Preset 2", 820, -132);
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

            var zeroMorphButton = CreateSmallButton("Zero selected", 820, -198);
            zeroMorphButton.buttonColor = Colors.rustRed;
            zeroMorphButton.textColor = Color.white;
            zeroMorphButton.button.onClick.AddListener(() =>
            {
                _playJsb.val = false;
                foreach(var morphModel in _morphModels)
                {
                    if(morphModel.EnabledJsb.val)
                    {
                        morphModel.ZeroValue();
                    }
                }
            });

            CreateHeaderTextField("Morphs", 30, true);

            _regionPopup = CreateRegionPopup();
            CreateSmallToggle(_useAndFilterJsb, 10, -392, true);
            CreateSmallToggle(_onlyShowActiveJsb, 280, -392, true);
            CreateSpacer(true).height = 48;

            _filterInputField = CreateFilterInputField();
            _filterInputField.onValueChanged.AddListener(value =>
            {
                _filterText = value == FILTER_DEFAULT_VAL ? "" : value;
                _filterInputField.textComponent.color = value.Length < 3 ? Colors.rustRed : Color.black;
                OnFilterChanged();
            });

            var filterInputPointerListener = _filterInputField.gameObject.AddComponent<PointerUpDownListener>();
            filterInputPointerListener.PointerDownAction = () =>
            {
                _preventFilterChangeCallback = true;
                if(_filterInputField.text == FILTER_DEFAULT_VAL)
                {
                    _filterInputField.text = "";
                }

                _preventFilterChangeCallback = false;
            };

            var clearButton = ClearButton();
            clearButton.button.onClick.AddListener(() =>
            {
                _filterInputField.text = FILTER_DEFAULT_VAL;
                OnFilterChanged(); // force trigger if value unchanged
            });

            /* Clear button is higher in hierarchy due to being parented to Content instead of LeftContent.
             * Custom listener is added because _colliderTriggerPopup.popup doesn't have an "onClosePopupHandlers" delegate.
             */
            _regionPopupListener = _regionPopup.popup.popupPanel.gameObject.AddComponent<UnityEventsListener>();
            _regionPopupListener.onEnable.AddListener(() =>
            {
                if(_collisionTriggerPopup)
                {
                    _collisionTriggerPopup.popup.visible = false;
                }

                clearButton.SetVisible(false);
            });
            _regionPopupListener.onDisable.AddListener(() => clearButton.SetVisible(true));

            /* Dev sliders for aligning custom elements */
            // CreateSlider(_posX, true);
            // CreateSlider(_posY, true);
            // CreateSlider(_sizeX, true);
            // CreateSlider(_sizeY, true);

            for(int i = 0; i < 10; i++)
            {
                /* Morph toggles initialized with the storables of the first 10 morph models.
                 * Should be always correct on init.
                 * There are always way more than 10 suitable morphs (built in)
                 */
                _morphToggles[i] = CreateToggle(_morphModels[i].EnabledJsb, true);
            }

            _prevPageButton = CreateNavButton("< Prev", 549, -1205);
            _prevPageButton.buttonColor = Color.gray;
            _prevPageButton.textColor = Color.white;
            _prevPageButton.button.onClick.AddListener(() =>
            {
                if(_currentPage > 0)
                {
                    _currentPage--;
                    ShowTogglesOnPage();
                }
            });
            _prevPageButton.button.interactable = false;

            CreatePageTextField();

            _nextPageButton = CreateNavButton("Next >", 880, -1205);
            _nextPageButton.buttonColor = Color.gray;
            _nextPageButton.textColor = Color.white;
            _nextPageButton.button.onClick.AddListener(() =>
            {
                if(_currentPage < _totalPages - 1)
                {
                    _currentPage++;
                    ShowTogglesOnPage();
                }
            });
            _nextPageButton.button.interactable = false;
        }

        void CreateHeaderTextField(string text, int fontSize, bool rightSide = false)
        {
            var jss = new JSONStorableString(text, text);
            var textField = CreateTextField(jss, rightSide);
            textField.UItext.fontSize = fontSize;
            textField.backgroundColor = Color.clear;
            textField.text = $"<size=8>\n</size>{jss.val}";
            var layout = textField.GetComponent<LayoutElement>();
            layout.preferredHeight = 50;
            layout.minHeight = 50;
        }

        UIDynamicToggle CreateSmallToggle(JSONStorableBool jsb, int x, int y, bool rightSide = false)
        {
            var t = InstantiateToContent(manager.configurableTogglePrefab, rightSide);
            t.GetComponent<LayoutElement>().ignoreLayout = true;
            var rectTransform = GetRekt(t);
            rectTransform.anchoredPosition = new Vector2(x, y);
            rectTransform.sizeDelta = new Vector2(-285, 52);
            var toggle = t.GetComponent<UIDynamicToggle>();
            toggle.label = jsb.name;
            AddToggleToJsb(toggle, jsb);
            return toggle;
        }

        UIDynamicButton CreateSmallButton(string label, int x, int y)
        {
            var t = InstantiateToContent(manager.configurableButtonPrefab);
            var rectTransform = GetRekt(t);
            rectTransform.anchoredPosition = new Vector2(x, y);
            rectTransform.sizeDelta = new Vector2(-825, 52);
            var button = t.GetComponent<UIDynamicButton>();
            button.label = label;
            return button;
        }

        UIDynamicButton CreateNavButton(string label, int x, int y)
        {
            var t = InstantiateToContent(manager.configurableButtonPrefab);
            var rectTransform = GetRekt(t);
            rectTransform.anchoredPosition = new Vector2(x, y);
            rectTransform.sizeDelta = new Vector2(-885, 52);
            var button = t.GetComponent<UIDynamicButton>();
            button.label = label;
            return button;
        }

        UIDynamicPopup CreateCollisionTriggerPopup()
        {
            var popup = CreateScrollablePopup(_collisionTriggerJssc);
            popup.labelTextColor = Color.black;
            var uiPopup = popup.popup;
            popup.popupPanelHeight = 640f;
            uiPopup.popupPanel.offsetMin += new Vector2(0, popup.popupPanelHeight + 60);
            uiPopup.popupPanel.offsetMax += new Vector2(0, popup.popupPanelHeight + 60);
            return popup;
        }

        /* ty acidbubbles -everlaster */
        UIDynamicPopup CreateRegionPopup()
        {
            var popup = CreateFilterablePopup(_regionJssc, true);
            popup.labelTextColor = Color.black;
            var uiPopup = popup.popup;

            uiPopup.labelText.alignment = TextAnchor.UpperCenter;
            uiPopup.labelText.GetComponent<RectTransform>().anchorMax = new Vector2(0, 0.89f);

            {
                var btn = Instantiate(manager.configurableButtonPrefab, popup.transform, false);
                Destroy(btn.GetComponent<LayoutElement>());
                btn.GetComponent<UIDynamicButton>().label = "<";
                btn.GetComponent<UIDynamicButton>()
                    .button.onClick.AddListener(
                        () =>
                        {
                            uiPopup.visible = false;
                            uiPopup.SelectPrevious();
                        }
                    );
                var prevBtnRect = btn.GetComponent<RectTransform>();
                prevBtnRect.pivot = new Vector2(0, 0);
                prevBtnRect.anchoredPosition = new Vector2(10f, 0);
                prevBtnRect.sizeDelta = new Vector2(0f, 0f);
                prevBtnRect.offsetMin = new Vector2(5f, 5f);
                prevBtnRect.offsetMax = new Vector2(80f, 70f);
                prevBtnRect.anchorMin = new Vector2(0f, 0f);
                prevBtnRect.anchorMax = new Vector2(0f, 0f);
            }

            {
                var btn = Instantiate(manager.configurableButtonPrefab, popup.transform, false);
                Destroy(btn.GetComponent<LayoutElement>());
                btn.GetComponent<UIDynamicButton>().label = ">";
                btn.GetComponent<UIDynamicButton>()
                    .button.onClick.AddListener(
                        () =>
                        {
                            uiPopup.visible = false;
                            uiPopup.SelectNext();
                        }
                    );
                var prevBtnRect = btn.GetComponent<RectTransform>();
                prevBtnRect.pivot = new Vector2(0, 0);
                prevBtnRect.anchoredPosition = new Vector2(10f, 0);
                prevBtnRect.sizeDelta = new Vector2(0f, 0f);
                prevBtnRect.offsetMin = new Vector2(82f, 5f);
                prevBtnRect.offsetMax = new Vector2(157f, 70f);
                prevBtnRect.anchorMin = new Vector2(0f, 0f);
                prevBtnRect.anchorMax = new Vector2(0f, 0f);
            }

            const float maxHeight = 820f;
            float height = 50f + _regionJssc.choices.Count * 60f;
            popup.popupPanelHeight = height > maxHeight ? maxHeight : height;
            return popup;
        }

        void CreatePageTextField()
        {
            var t = InstantiateToContent(manager.configurableTextFieldPrefab);
            var rectTransform = GetRekt(t);
            rectTransform.anchoredPosition = new Vector2(740, -1205);
            rectTransform.sizeDelta = new Vector2(-949, 52);
            var textField = t.GetComponent<UIDynamicTextField>();
            textField.textColor = Color.black;
            textField.backgroundColor = Color.clear;
            textField.text = _pagesJss.val;
            textField.UItext.fontSize = 30;
            textField.UItext.alignment = TextAnchor.LowerCenter;
            _pagesJss.dynamicText = textField;
            textFieldToJSONStorableString.Add(textField, _pagesJss);
        }

        UIDynamicButton ClearButton()
        {
            var t = InstantiateToContent(manager.configurableButtonPrefab);
            var rectTransform = GetRekt(t);
            rectTransform.anchoredPosition = new Vector2(965, -470);
            rectTransform.sizeDelta = new Vector2(-970, 63);
            var button = t.GetComponent<UIDynamicButton>();
            button.label = "Clear";
            button.buttonColor = Colors.rustRed;
            button.textColor = new Color(1f, 1f, 1f, 1f);
            return button;
        }

        InputField CreateFilterInputField()
        {
            var filterTextJss = new JSONStorableString("FilterText", FILTER_DEFAULT_VAL);
            var filterTextField = CreateTextField(filterTextJss, true);
            var tfLayout = filterTextField.GetComponent<LayoutElement>();
            tfLayout.preferredHeight = tfLayout.minHeight = 63;
            filterTextField.height = 63;
            filterTextField.DisableScrollOnText();
            _filterInputField = filterTextField.gameObject.AddComponent<InputField>();
            _filterInputField.textComponent = filterTextField.UItext;
            _filterInputField.lineType = InputField.LineType.SingleLine;
            _filterInputField.text = filterTextJss.val;
            return _filterInputField;
        }

        Transform InstantiateToContent<T>(T prefab, bool rightSide) where T : Transform
        {
            var parent = UITransform.Find($"Scroll View/Viewport/Content/{(rightSide ? "Right" : "Left")}Content");
            return Instantiate(prefab, parent, false);
        }

        Transform InstantiateToContent<T>(T prefab) where T : Transform
        {
            var parent = UITransform.Find("Scroll View/Viewport/Content");
            return Instantiate(prefab, parent, false);
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

        bool _preventFilterChangeCallback;
        const int ITEMS_PER_PAGE = 10;
        readonly List<int> _filteredIndices = new List<int>();
        int _currentPage;
        int _totalPages;

        void OnFilterChanged()
        {
            if(!_filterInputField || _preventFilterChangeCallback || !_uiInitialized)
            {
                return;
            }

            string trimmed = _filterText.Trim();
            bool useTextFilter = !string.IsNullOrEmpty(trimmed) && trimmed.Length >= 3 && trimmed != FILTER_DEFAULT_VAL;

            var patterns = new List<string>();
            if(useTextFilter)
            {
                foreach(string pattern in trimmed.Split(' '))
                {
                    if(pattern.Length > 1)
                    {
                        patterns.Add(pattern.ToLower());
                    }
                }
            }

            _filteredIndices.Clear();
            bool includeAll = _regionJssc.val == "All";

            for(int i = 0; i < _morphModels.Count; i++)
            {
                var morphModel = _morphModels[i];

                if(_onlyShowActiveJsb.val && !_morphModels[i].EnabledJsb.val)
                {
                    continue;
                }

                bool match = !useTextFilter;

                if(useTextFilter)
                {
                    int hits = patterns.Count(morphModel.Label.ToLower().Contains);
                    if(!_useAndFilterJsb.val && hits > 0 || _useAndFilterJsb.val && hits == patterns.Count)
                    {
                        match = true;
                    }
                }

                if(match && (includeAll || morphModel.FinalTwoRegions == _regionJssc.val))
                {
                    _filteredIndices.Add(i);
                }
            }

            _totalPages = (int) Math.Ceiling((double) _filteredIndices.Count / ITEMS_PER_PAGE);
            _currentPage = 0;
            ShowTogglesOnPage();
        }

        void ShowTogglesOnPage()
        {
            _pagesJss.val = _totalPages == 0
                ? "<size=8>\n</size>1 / 1"
                : $"<size=8>\n</size>{_currentPage + 1} / {_totalPages}";

            foreach(var morphToggle in _morphToggles)
            {
                morphToggle.toggle.onValueChanged.RemoveAllListeners();
                toggleToJSONStorableBool.Remove(morphToggle);
                morphToggle.SetVisible(true);
            }

            for(int i = 0; i < _filteredIndices.Count; i++)
            {
                bool isOnPage = i >= _currentPage * ITEMS_PER_PAGE && i < (_currentPage + 1) * ITEMS_PER_PAGE;
                var morphModel = _morphModels[_filteredIndices[i]];
                if(isOnPage)
                {
                    var morphToggle = _morphToggles[i % 10];
                    toggleToJSONStorableBool[morphToggle] = morphModel.EnabledJsb;
                    morphToggle.label = _regionJssc.val == "All" ? morphModel.Label : morphModel.DisplayName;
                    morphModel.EnabledJsb.toggle = morphToggle.toggle;
                    morphToggle.toggle.isOn = morphModel.EnabledJsb.val;
                    morphToggle.toggle.onValueChanged.AddListener((val) => morphModel.EnabledJsb.val = val);
                }
                else
                {
                    morphModel.EnabledJsb.toggle = null;
                }
            }

            /* Hide toggles not associated with any storable on the final page */
            if(_currentPage == _totalPages - 1 || _totalPages == 0)
            {
                for(int i = _filteredIndices.Count % 10; i < ITEMS_PER_PAGE; i++)
                {
                    _morphToggles[i].SetVisible(false);
                }
            }

            if(_prevPageButton)
            {
                _prevPageButton.button.interactable = _currentPage > 0;
            }

            if(_nextPageButton)
            {
                _nextPageButton.button.interactable = _currentPage < _totalPages - 1;
            }
        }

        bool _play;
        bool _globalAnimationFrozen;
        bool SkipUpdate => !_play || _globalAnimationFrozen || !enabled || _savingScene || initialized != true || _restoringFromJson;
        float _timer;

        void Update()
        {
            _globalAnimationFrozen = Utils.GlobalAnimationFrozen();
            if(SkipUpdate)
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
                Loggr.Message($"{nameof(Update)} error: {e}");
                enabled = false;
            }
        }

        void FixedUpdate()
        {
            if(SkipUpdate)
            {
                return;
            }

            try
            {
                float interpolant = _smoothJsb.val
                    ? SmoothInterpolant(_animLengthJsf.val, _masterSpeedJsf.val, _timer, _animWaitJsf.val)
                    : LinearInterpolant(_animLengthJsf.val, _masterSpeedJsf.val);
                foreach(var morphModel in _enabledMorphs)
                {
                    if(morphModel.EnabledJsb.val)
                    {
                        morphModel.CalculateMorphValue(interpolant);
                    }
                }
            }
            catch(Exception e)
            {
                Loggr.Message($"{nameof(FixedUpdate)} error: {e}");
                enabled = false;
            }
        }

        static float SmoothInterpolant(float animLength, float masterSpeed, float timer, float animWait)
        {
            return Time.deltaTime *
                animLength *
                masterSpeed *
                Mathf.Sin(timer / (animWait / masterSpeed) * Mathf.PI);
        }

        static float LinearInterpolant(float animLength, float masterSpeed)
        {
            return Time.deltaTime * animLength * masterSpeed;
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

        static bool CheckIfTriggerExists(CollisionTrigger trig)
        {
            JSONNode presentTriggers = trig.trigger.GetJSON();
            var asArray = presentTriggers["startActions"].AsArray;
            for(int i = 0; i < asArray.Count; i++)
            {
                var asObject = asArray[i].AsObject;
                string name = asObject["name"];
                if(name == EXP_RAND_TRIGGER && asObject["receiver"] != null)
                {
                    return true;
                }
            }

            return false;
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
            while(initialized == null)
            {
                yield return null;
            }

            if(initialized == false)
            {
                yield break;
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
            if(initialized != true)
            {
                return;
            }

            try
            {
                ResetMorphs();
            }
            catch(Exception e)
            {
                Loggr.Message($"{nameof(OnDisable)} error: " + e);
            }
        }

        new void OnDestroy()
        {
            try
            {
                base.OnDestroy();
                Destroy(_colliderTriggerPopupListener);
                ClearTriggers(_collisionTriggerJssc.val);
                ClearOtherTriggers();
                SuperController.singleton.onSceneSavedHandlers -= OnSceneSaved;
                SuperController.singleton.onBeforeSceneSaveHandlers -= OnBeforeSceneSave;
            }
            catch(Exception e)
            {
                Loggr.Message($"{nameof(OnDestroy)} error: " + e);
            }
        }

        #if ENV_DEVELOPMENT

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

        #endif
    }
}
