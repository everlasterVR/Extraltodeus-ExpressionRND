using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace extraltodeuslExpRandPlugin
{
    sealed class ExpressionRandomizer : MVRScript
    {
        readonly Color _backgroundGray = new Color(0.85f, 0.85f, 0.85f);
        readonly Color _rustRed = new Color(0.6f, 0.3f, 0.3f, 1f);
        readonly Color _darkRed = new Color(0.75f, 0f, 0f, 1f);

#region InitUI

        UnityEventsListener _pluginUIEventsListener;

        public override void InitUI()
        {
            base.InitUI();
            if(!UITransform || _pluginUIEventsListener)
            {
                return;
            }

            StartCoroutine(InitUICo());
        }

        bool _inEnabledCo;

        IEnumerator InitUICo()
        {
            _pluginUIEventsListener = UITransform.gameObject.AddComponent<UnityEventsListener>();
            _pluginUIEventsListener.onEnable.AddListener(() => StartCoroutine(OnUIEnabledCo()));

            while(_initialized == null)
            {
                yield return null;
            }

            if(_initialized == false)
            {
                enabledJSON.val = false;
                yield break;
            }

            _pluginUIEventsListener.onDisable.AddListener(() => StartCoroutine(OnUIDisabledCo()));
        }

        IEnumerator OnUIEnabledCo()
        {
            if(_inEnabledCo)
            {
                /* When VAM UI is toggled back on with the plugin UI already active, onEnable gets called twice and onDisable once.
                 * This ensures onEnable logic executes just once.
                 */
                yield break;
            }

            _inEnabledCo = true;
            var background = rightUIContent.parent.parent.parent.transform.GetComponent<Image>();
            background.color = _backgroundGray;
            _inEnabledCo = false;
        }

        IEnumerator OnUIDisabledCo()
        {
            if(_inEnabledCo)
            {
                /* When VAM UI is toggled back on with the plugin UI already active, onEnable gets called twice and onDisable once.
                 * This ensures only onEnable logic executes.
                 */
                yield break;
            }

            if(_collisionTriggerPopup)
            {
                _collisionTriggerPopup.popup.visible = false;
            }

            if(_regionPopup)
            {
                _regionPopup.popup.visible = false;
            }
        }

#endregion

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

        bool? _initialized;
        bool _restoringFromJson;
        List<MorphModel> _enabledMorphs;
        readonly List<MorphModel> _morphModels = new List<MorphModel>();

        JSONStorableBool _playJsb;
        JSONStorableBool _abaJsb;
        JSONStorableFloat _animLengthJsf;
        JSONStorableFloat _animWaitJsf;
        JSONStorableStringChooser _collisionTriggerJssc;
        JSONStorableBool _useAndFilterJsb;
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
        JSONStorableString _pagesJss;
        JSONStorableStringChooser _regionJssc;

        InputField _filterInputField;
        UnityEventsListener _colliderTriggerPopupListener;
        UnityEventsListener _regionPopupListener;

        GenerateDAZMorphsControlUI _morphsControlUI;

        const string FILTER_DEFAULT_VAL = "Filter morphs...";

        void Start()
        {
            _timer = 0f;
            InvokeRepeating(nameof(TriggerMaintainer), 3f, 3f); // To check if the selected collision trigger is still there every 3 seconds
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

            _collisionTriggerJssc = new JSONStorableStringChooser(
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
            RegisterStringChooser(_collisionTriggerJssc);
            _collisionTriggerJssc.setCallbackFunction = val =>
            {
                if(val != "None")
                {
                    _manualJsb.val = true;
                    _randomJsb.val = true;
                }
            };
            if(_collisionTriggerJssc.val != "None")
            {
                _manualJsb.val = true;
                _randomJsb.val = true;
            }

            _useAndFilterJsb = new JSONStorableBool("AND filter", false)
            {
                setCallbackFunction = val => OnFilterChanged(),
            };
            _onlyShowActiveJsb = new JSONStorableBool("Active only", false)
            {
                setCallbackFunction = val => OnFilterChanged(),
            };
            _pagesJss = new JSONStorableString("Pages", "");

            var regionOptions = new List<string> { "All" };
            regionOptions.AddRange(_morphModels.Select(morphModel => morphModel.UpperRegion).Distinct());
            _regionJssc = new JSONStorableStringChooser(
                "Region",
                regionOptions,
                "All",
                "Region",
                (string _) => OnFilterChanged()
            );

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
        UIDynamicPopup _collisionTriggerPopup;

        void CreateLeftUI()
        {
            CreateHeaderTextField("Expression Randomizer v1.7.2", 34);
            CreateSlider(_minJsf);
            CreateSlider(_maxJsf);
            CreateSlider(_multiJsf);
            CreateSlider(_masterSpeedJsf);
            CreateSpacer().height = 50;
            var toggle = CreateSmallToggle(_playJsb, 10, -668);
            toggle.toggle.onValueChanged.AddListener(val => toggle.textColor = val ? Color.black : _darkRed);
            CreateSmallToggle(_smoothJsb, 280, -668);
            CreateAdditionalOptionsUI();
            CreateMoreAdditionalOptionsUI();
            SelectOptionsUI(false);
        }

        void CreateAdditionalOptionsUI()
        {
            _moreButton = NavButton("More >", 339, -733);
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
            _backButton = NavButton("< Back", 339, -733);
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

        void CreateRightUI()
        {
            CreateSpacer(true).height = 120f;

            var selectNone = SmallButton("Select None", 550, -62);
            selectNone.button.onClick.AddListener(() =>
            {
                foreach(var morphModel in _morphModels)
                {
                    morphModel.EnabledJsb.val = false;
                }
            });

            var selectDefault = SmallButton("Select Default", 820, -62);
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

            var selectPreset1 = SmallButton("Select Preset 1", 550, -132);
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

            var selectPreset2 = SmallButton("Select Preset 2", 820, -132);
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

            var zeroMorphButton = SmallButton("Zero selected", 820, -198);
            zeroMorphButton.buttonColor = _rustRed;
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

            // ******* FILTER BOX ***********
            {
                var filterTextJss = new JSONStorableString("FilterText", FILTER_DEFAULT_VAL);
                var filterTextField = CreateTextField(filterTextJss, true);
                SetupTextField(filterTextField, 63, false, false);
                _filterInputField = filterTextField.gameObject.AddComponent<InputField>();
                _filterInputField.textComponent = filterTextField.UItext;
                _filterInputField.lineType = InputField.LineType.SingleLine;
                _filterInputField.text = filterTextJss.val;
                _filterInputField.onValueChanged.AddListener(value =>
                {
                    _filterText = value == FILTER_DEFAULT_VAL ? "" : value;
                    _filterInputField.textComponent.color = value.Length < 3 ? _rustRed : Color.black;
                    OnFilterChanged();
                });

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

                clearSearchBtn.SetVisible(false);
            });
            _regionPopupListener.onDisable.AddListener(() => clearSearchBtn.SetVisible(true));

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
                        morphModel.ZeroValue();
                    }

                    _enabledMorphs = _morphModels.Where(item => item.EnabledJsb.val).ToList();
                };
                morphModel.Toggle = CreateToggle(morphModel.EnabledJsb, true);
                morphModel.Toggle.SetVisible(false);
            }

            _prevPageButton = NavButton("< Prev", 549, -1205);
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

            _nextPageButton = NavButton("Next >", 880, -1205);
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

            OnFilterChanged();
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

        UIDynamicToggle CreateSmallToggle(JSONStorableBool jsb, int x, int y, bool rightSide = false, bool callbacks = false)
        {
            var t = InstantiateToContent(manager.configurableTogglePrefab, rightSide);
            t.GetComponent<LayoutElement>().ignoreLayout = true;
            var rectTransform = GetRekt(t);
            rectTransform.anchoredPosition = new Vector2(x, y);
            rectTransform.sizeDelta = new Vector2(-285, 52);
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
            rectTransform.sizeDelta = new Vector2(-825, 52);
            if(callbacks)
            {
                SetDevUISliderCallbacks(rectTransform);
            }

            var button = t.GetComponent<UIDynamicButton>();
            button.label = label;
            return button;
        }

        UIDynamicButton NavButton(string label, int x, int y, bool callbacks = false)
        {
            var t = InstantiateToContent(manager.configurableButtonPrefab);
            var rectTransform = GetRekt(t);
            rectTransform.anchoredPosition = new Vector2(x, y);
            rectTransform.sizeDelta = new Vector2(-885, 52);
            if(callbacks)
            {
                SetDevUISliderCallbacks(rectTransform);
            }

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
            button.buttonColor = _rustRed;
            button.textColor = new Color(1f, 1f, 1f, 1f);
            return button;
        }

        Transform InstantiateToContent<T>(T prefab, bool rightSide) where T : Transform
        {
            var parent = UITransform.Find($"Scroll View/Viewport/Content/{(rightSide ? "Right" : "Left")}Content");
            var childTransform = Instantiate(prefab, parent, false);
            return childTransform;
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
        const int ITEMS_PER_PAGE = 10;
        readonly List<int> _filteredIndices = new List<int>();
        int _currentPage;
        int _totalPages;

        void OnFilterChanged()
        {
            if(!_filterInputField || _regionJssc == null || _onlyShowActiveJsb == null || _useAndFilterJsb == null || _preventFilterChangeCallback)
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
                morphModel.Toggle.SetVisible(false);

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

                if(match && (includeAll || morphModel.UpperRegion == _regionJssc.val))
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

            for(int i = 0; i < _filteredIndices.Count; i++)
            {
                int index = _filteredIndices[i];
                bool isVisible = i >= _currentPage * ITEMS_PER_PAGE && i < (_currentPage + 1) * ITEMS_PER_PAGE;
                _morphModels[index].Toggle.SetVisible(isVisible);
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

        bool _globalAnimationFrozen;
        float _timer;

        void Update()
        {
            _globalAnimationFrozen = GlobalAnimationFrozen();
            if(!_playJsb.val || _globalAnimationFrozen || _savingScene || _initialized != true || _restoringFromJson)
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
            if(!_playJsb.val || !enabled || _globalAnimationFrozen || _savingScene || _initialized != true || _restoringFromJson)
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
                SuperController.LogMessage($"{nameof(ExpressionRandomizer)}: {nameof(FixedUpdate)} error: " + e);
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
            foreach(string triggerName in _collisionTriggerJssc.choices)
            {
                if(triggerName != _collisionTriggerJssc.val && triggerName != "None")
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

            if(_collisionTriggerJssc.val != "None")
            {
                CreateTrigger(_collisionTriggerJssc.val);
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
            while(_initialized == null)
            {
                yield return null;
            }

            if(_initialized == false)
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
                DestroyImmediate(_pluginUIEventsListener);
                Destroy(_colliderTriggerPopupListener);
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
        readonly DAZMorph _morph;
        public string UpperRegion { get; }
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
            _morph = morph;
            UpperRegion = Regex.Split(region, "/").LastOrDefault() ?? "";
            Label = UpperRegion + "/" + displayName;
            _initialMorphValue = _morph.morphValue;
            _defaultMorphValue = _initialMorphValue;
            _morph.morphValue = 0;
            _currentMorphValue = _morph.morphValue;
        }

        public void CalculateMorphValue(float interpolant)
        {
            _currentMorphValue = Mathf.Lerp(_currentMorphValue, _newMorphValue, interpolant);
            _morph.morphValue = _currentMorphValue;
        }

        public void SetNewMorphValue(float min, float max, float multi, bool aba)
        {
            _newMorphValue = aba && _currentMorphValue > 0.1f
                ? 0
                : UnityEngine.Random.Range(min, max) * multi;
        }

        public void UpdateDefaultValue()
        {
            _defaultMorphValue = _morph.morphValue;
        }

        public void ResetToDefault()
        {
            _morph.morphValue = _defaultMorphValue;
        }

        public void ResetToInitial()
        {
            _morph.morphValue = _initialMorphValue;
        }

        public void ZeroValue()
        {
            _morph.morphValue = 0;
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

    static class UIPopupExtensions
    {
        const int MAX_VISIBLE_COUNT = 400;

        public static void SelectPrevious(this UIPopup uiPopup)
        {
            if(uiPopup.currentValue == uiPopup.popupValues.First())
            {
                uiPopup.currentValue = uiPopup.LastVisibleValue();
            }
            else
            {
                uiPopup.SetPreviousValue();
            }
        }

        public static void SelectNext(this UIPopup uiPopup)
        {
            if(uiPopup.currentValue == uiPopup.LastVisibleValue())
            {
                uiPopup.currentValue = uiPopup.popupValues.First();
            }
            else
            {
                uiPopup.SetNextValue();
            }
        }

        static string LastVisibleValue(this UIPopup uiPopup)
        {
            return uiPopup.popupValues.Length > MAX_VISIBLE_COUNT
                ? uiPopup.popupValues[MAX_VISIBLE_COUNT - 1]
                : uiPopup.popupValues.Last();
        }
    }

    sealed class UnityEventsListener : MonoBehaviour
    {
        public readonly UnityEvent onEnable = new UnityEvent();
        public readonly UnityEvent onDisable = new UnityEvent();

        void OnEnable()
        {
            onEnable.Invoke();
        }

        void OnDisable()
        {
            onDisable.Invoke();
        }

        void OnDestroy()
        {
            onEnable.RemoveAllListeners();
            onDisable.RemoveAllListeners();
        }
    }
}
