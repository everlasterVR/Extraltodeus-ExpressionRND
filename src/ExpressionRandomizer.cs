#define ENV_DEVELOPMENT
using ExpressionRND.Models;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace extraltodeus
{
    sealed class ExpressionRandomizer : ScriptBase
    {
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        const string VERSION = "0.0.0";
        const string COLLISION_TRIGGER_DEFAULT_VAL = "None";
        const string FILTER_DEFAULT_VAL = "Filter morphs...";

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
            "Hands",
            "Eyes Closed Left",
            "Eyes Closed Right",
            "Mouth Smile Simple Left",
            "Mouth Smile Simple Right",
            "Pose Controls/JCM", // JointCorrect morphs by FallenDancer
        };

        readonly string[] _poseRegions =
        {
            "Pose",
            "Expressions",
        };

        readonly string[] _includeMorphNames =
        {
            "Pupils Dialate",
            "Eye Roll Back_DD",
        };

        readonly string[] _excludeMorphNames =
        {
            "Jaw Side-Side",
        };

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        readonly Dictionary<string, JSONClass> _presetJSONs = new Dictionary<string, JSONClass>
        {
            {
                Name.IDLE,
                new JSONClass
                {
                    /* Settings */
                    [Name.PLAY] = { AsBool = true },
                    [Name.SMOOTH] = { AsBool = true },
                    [Name.RESET_USED_EXPRESSIONS_AT_LOOP] = { AsBool = true },
                    [Name.TRIGGER_TRANSITIONS_MANUALLY] = { AsBool = false },
                    [Name.RANDOM_CHANCES_FOR_TRANSITIONS] = { AsBool = true },
                    [Name.CHANCE_TO_TRIGGER] = { AsFloat = 90f },
                    [Name.MINIMUM_VALUE] = { AsFloat = -0.15f },
                    [Name.MAXIMUM_VALUE] = { AsFloat = 0.30f },
                    [Name.MULTIPLIER] = { AsFloat = 1.00f },
                    [Name.MASTER_SPEED] = { AsFloat = 1.00f },
                    [Name.LOOP_LENGTH] = { AsFloat = 4.00f },
                    [Name.MORPHING_SPEED] = { AsFloat = 3.00f },
                    /* Enabled morphs */
                    ["Brow/Brow Down"] = { AsBool = true },
                    ["Brow/Brow Inner Down Left"] = { AsBool = true },
                    ["Cheeks and Jaw/Cheek Eye Flex"] = { AsBool = true },
                    ["Cheeks and Jaw/Jaw In-Out"] = { AsBool = true },
                    ["Expressions/Smile Full Face"] = { AsBool = true },
                    ["Mouth/Mouth Smile Simple Left"] = { AsBool = true },
                    ["Nose/Nose Wrinkle"] = { AsBool = true },
                }
            },
            {
                Name.FLIRT,
                new JSONClass
                {
                    /* Settings */
                    [Name.PLAY] = { AsBool = true },
                    [Name.SMOOTH] = { AsBool = true },
                    [Name.RESET_USED_EXPRESSIONS_AT_LOOP] = { AsBool = true },
                    [Name.TRIGGER_TRANSITIONS_MANUALLY] = { AsBool = false },
                    [Name.RANDOM_CHANCES_FOR_TRANSITIONS] = { AsBool = true },
                    [Name.CHANCE_TO_TRIGGER] = { AsFloat = 66f },
                    [Name.MINIMUM_VALUE] = { AsFloat = 0.06f },
                    [Name.MAXIMUM_VALUE] = { AsFloat = 0.48f },
                    [Name.MULTIPLIER] = { AsFloat = 1.00f },
                    [Name.MASTER_SPEED] = { AsFloat = 3.00f },
                    [Name.LOOP_LENGTH] = { AsFloat = 8.00f },
                    [Name.MORPHING_SPEED] = { AsFloat = 4.00f },
                    /* Enabled morphs */
                    ["Brow/Brow Inner Down Right"] = { AsBool = true },
                    ["Brow/Brow Inner Up Right"] = { AsBool = true },
                    ["Cheeks and Jaw/Cheek Eye Flex"] = { AsBool = true },
                    ["Expressions/Flirting"] = { AsBool = true },
                    ["Expressions/Smile Full Face"] = { AsBool = true },
                    ["Eyes/Eyes Squint"] = { AsBool = true },
                    ["Eyes/Pupils Dialate"] = { AsBool = true },
                    ["Lips/Lip Bottom In Left"] = { AsBool = true },
                    ["Lips/Lips Pucker"] = { AsBool = true },
                    ["Mouth/Mouth Smile Simple Left"] = { AsBool = true },
                    ["Mouth/Mouth Smile Simple Right"] = { AsBool = true },
                }
            },
            {
                Name.ENJOY,
                new JSONClass
                {
                    /* Settings */
                    [Name.PLAY] = { AsBool = true },
                    [Name.SMOOTH] = { AsBool = true },
                    [Name.RESET_USED_EXPRESSIONS_AT_LOOP] = { AsBool = true },
                    [Name.TRIGGER_TRANSITIONS_MANUALLY] = { AsBool = false },
                    [Name.RANDOM_CHANCES_FOR_TRANSITIONS] = { AsBool = true },
                    [Name.CHANCE_TO_TRIGGER] = { AsFloat = 75f },
                    [Name.MINIMUM_VALUE] = { AsFloat = 0.07f },
                    [Name.MAXIMUM_VALUE] = { AsFloat = 0.36f },
                    [Name.MULTIPLIER] = { AsFloat = 1.00f },
                    [Name.MASTER_SPEED] = { AsFloat = 2.50f },
                    [Name.LOOP_LENGTH] = { AsFloat = 6.00f },
                    [Name.MORPHING_SPEED] = { AsFloat = 2.50f },
                    /* Enabled morphs */
                    ["Brow/Brow Inner Up Left"] = { AsBool = true },
                    ["Brow/Brow Inner Up Right"] = { AsBool = true },
                    ["Cheeks and Jaw/Jaw In-Out"] = { AsBool = true },
                    ["Expressions/Happy"] = { AsBool = true },
                    ["Expressions/Shock"] = { AsBool = true },
                    ["Expressions/Surprise"] = { AsBool = true },
                    ["Eyes/Eyes Closed"] = { AsBool = true },
                    ["Eyes/Pupils Dialate"] = { AsBool = true },
                    ["Lips/Lip Bottom Out"] = { AsBool = true },
                    ["Lips/Lips Pucker"] = { AsBool = true },
                    ["Mouth/Mouth Open"] = { AsBool = true },
                    ["Nose/Nose Wrinkle"] = { AsBool = true },
                }
            },
            { Name.SLOT_1, null },
            { Name.SLOT_2, null },
        };

        string _presetSetOnInit;
        bool _restoringFromJson;
        Atom _person;
        GenerateDAZMorphsControlUI _morphsControlUI;
        List<MorphModel> _enabledMorphs;
        List<MorphModel> _awaitingResetMorphs;
        readonly List<MorphModel> _morphModels = new List<MorphModel>();

        readonly List<JSONStorableFloat> _alwaysStoreFloatParams = new List<JSONStorableFloat>();
        readonly List<JSONStorableBool> _alwaysStoreBoolParams = new List<JSONStorableBool>();

        JSONStorableFloat _minJsf;
        JSONStorableFloat _maxJsf;
        JSONStorableFloat _multiJsf;
        JSONStorableFloat _masterSpeedJsf;
        JSONStorableBool _playJsb;
        JSONStorableBool _smoothJsb;
        JSONStorableFloat _loopLengthJsf;
        JSONStorableFloat _morphingSpeedJsf;
        JSONStorableBool _resetUsedExpressionsAtLoopJsb;
        JSONStorableBool _triggerTransitionsManuallyJsb;
        JSONStorableBool _randomChancesForTransitionsJsb;
        JSONStorableFloat _chanceToTriggerJsf;
        JSONStorableAction _triggerTransitionAction;
        JSONStorableAction _loadIdlePresetAction;
        JSONStorableAction _loadFlirtPresetAction;
        JSONStorableAction _loadEnjoyPresetAction;
        JSONStorableAction _loadPreset1Action;
        JSONStorableAction _loadPreset2Action;
        JSONStorableUrl _loadPresetWithPathUrlJSON;
        JSONStorableActionPresetFilePath _loadPresetWithPathJSON;
        JSONStorableStringChooser _collisionTriggerJssc;
        JSONStorableBool _useAndFilterJsb;
        JSONStorableBool _onlyShowActiveJsb;
        JSONStorableString _pagesJss;
        JSONStorableStringChooser _regionJssc;

        InputField _filterInputField;
        UnityEventsListener _colliderTriggerPopupListener;
        UnityEventsListener _regionPopupListener;

        public override void Init()
        {
            if(containingAtom.type != "Person")
            {
                Loggr.Error($"Add to a Person atom, not {containingAtom.type}.");
                enabled = false;
                return;
            }

            StartCoroutine(InitCo());
        }

        JSONStorableFloat NewStorableFloat(string name, float val, float min, float max, bool alwaysStore = true)
        {
            var jsf = new JSONStorableFloat(name, val, min, max)
            {
                storeType = JSONStorableParam.StoreType.Full,
            };
            RegisterFloat(jsf);
            if(alwaysStore)
            {
                _alwaysStoreFloatParams.Add(jsf);
            }

            return jsf;
        }

        JSONStorableBool NewStorableBool(string name, bool val, bool alwaysStore = true)
        {
            var jsb = new JSONStorableBool(name, val)
            {
                storeType = JSONStorableParam.StoreType.Full,
            };
            RegisterBool(jsb);
            if(alwaysStore)
            {
                _alwaysStoreBoolParams.Add(jsb);
            }

            return jsb;
        }

        JSONStorableAction NewStorableAction(string name, JSONStorableAction.ActionCallback callback)
        {
            var action = new JSONStorableAction(name, callback);
            RegisterAction(action);
            return action;
        }

        IEnumerator InitCo()
        {
            yield return new WaitForEndOfFrame();
            while(SuperController.singleton.isLoading)
            {
                yield return null;
            }

            _person = containingAtom;
            _person.GetStorableByID("AutoExpressions").SetBoolParamValue("enabled", false);
            var geometry = (DAZCharacterSelector) _person.GetStorableByID("geometry");
            _morphsControlUI = geometry.morphsControlUI;

            foreach(var morph in _morphsControlUI.GetMorphs())
            {
                if(
                    !morph.hasBoneModificationFormulas &&
                    !morph.hasBoneRotationFormulas &&
                    _poseRegions.Any(morph.region.Contains) &&
                    !_excludeMorphNames.Any(morph.displayName.Contains) &&
                    !_excludeRegions.Any(morph.region.Contains) ||
                    _includeMorphNames.Any(morph.displayName.Contains)
                )
                {
                    var morphModel = new MorphModel(morph, morph.displayName, morph.region);
                    _morphModels.Add(morphModel);
                }
            }

            _morphModels.Sort((a, b) => string.Compare(a.Label, b.Label, StringComparison.Ordinal));

            _minJsf = NewStorableFloat(Name.MINIMUM_VALUE, -0.15f, -1f, 1.0f);
            _maxJsf = NewStorableFloat(Name.MAXIMUM_VALUE, 0.35f, -1f, 1.0f);
            _multiJsf = NewStorableFloat(Name.MULTIPLIER, 1f, 0f, 2f);
            _masterSpeedJsf = NewStorableFloat(Name.MASTER_SPEED, 1f, 0.1f, 10f);
            _playJsb = NewStorableBool(Name.PLAY, true);
            _playJsb.setCallbackFunction = value => _play = value;
            _play = _playJsb.val;
            _smoothJsb = NewStorableBool(Name.SMOOTH, true);
            _loopLengthJsf = NewStorableFloat(Name.LOOP_LENGTH, 2f, 0.1f, 20f);
            _morphingSpeedJsf = NewStorableFloat(Name.MORPHING_SPEED, 1.0f, 0.1f, 20f);
            _resetUsedExpressionsAtLoopJsb = NewStorableBool(Name.RESET_USED_EXPRESSIONS_AT_LOOP, true);
            _triggerTransitionsManuallyJsb = NewStorableBool(Name.TRIGGER_TRANSITIONS_MANUALLY, false, false);
            _randomChancesForTransitionsJsb = NewStorableBool(Name.RANDOM_CHANCES_FOR_TRANSITIONS, true, false);
            _chanceToTriggerJsf = NewStorableFloat(Name.CHANCE_TO_TRIGGER, 75f, 0f, 100f, false);

            _masterSpeedJsf.setCallbackFunction = value => _timeout = _loopLengthJsf.val / value;
            _loopLengthJsf.setCallbackFunction = value => _timeout = value / _masterSpeedJsf.val;
            _timeout = _loopLengthJsf.val / _masterSpeedJsf.val;

            _triggerTransitionAction = NewStorableAction(Name.TRIGGER_TRANSITION, NewRandomTargetMorphValues);
            _loadIdlePresetAction = NewStorableAction(Name.LOAD_IDLE_PRESET, () =>
            {
                base.RestoreFromJSON(_presetJSONs[Name.IDLE]);
                UpdatePresetButtons(Name.IDLE);
            });
            _loadFlirtPresetAction = NewStorableAction(Name.LOAD_FLIRT_PRESET, () =>
            {
                base.RestoreFromJSON(_presetJSONs[Name.FLIRT]);
                UpdatePresetButtons(Name.FLIRT);
            });
            _loadEnjoyPresetAction = NewStorableAction(Name.LOAD_ENJOY_PRESET, () =>
            {
                base.RestoreFromJSON(_presetJSONs[Name.ENJOY]);
                UpdatePresetButtons(Name.ENJOY);
            });
            _loadPreset1Action = NewStorableAction(Name.LOAD_PRESET_1, () => OnCustomPresetButtonClicked(Name.SLOT_1));
            _loadPreset2Action = NewStorableAction(Name.LOAD_PRESET_2, () => OnCustomPresetButtonClicked(Name.SLOT_2));
            _loadPresetWithPathUrlJSON = new JSONStorableUrl("Load preset with path url", string.Empty, "json", FileUtils.GetLastBrowseDir())
            {
                allowFullComputerBrowse = true,
                allowBrowseAboveSuggestedPath = true,
                hideExtension = true,
                showDirs = true,
            };
            _loadPresetWithPathJSON = new JSONStorableActionPresetFilePath(
                Name.LOAD_PRESET_WITH_PATH,
                OnLoadPathSelected,
                _loadPresetWithPathUrlJSON
            );
            RegisterPresetFilePathAction(_loadPresetWithPathJSON);

            _collisionTriggerJssc = new JSONStorableStringChooser(
                Name.COLLISION_TRIGGER,
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
                Name.COLLISION_TRIGGER
            );
            RegisterStringChooser(_collisionTriggerJssc);
            _collisionTriggerJssc.setCallbackFunction = val =>
            {
                if(val != COLLISION_TRIGGER_DEFAULT_VAL)
                {
                    _triggerTransitionsManuallyJsb.val = true;
                    _randomChancesForTransitionsJsb.val = true;
                }
                else
                {
                    ClearOtherTriggers();
                }
            };
            if(_collisionTriggerJssc.val != "None")
            {
                _triggerTransitionsManuallyJsb.val = true;
                _randomChancesForTransitionsJsb.val = true;
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
            _awaitingResetMorphs = new List<MorphModel>();
            foreach(var morphModel in _morphModels)
            {
                morphModel.EnabledJsb = NewStorableBool(morphModel.Label, false, false);
                morphModel.EnabledJsb.setCallbackFunction = on =>
                {
                    if(on)
                    {
                        morphModel.UpdateInitialValue();
                        _awaitingResetMorphs.Remove(morphModel);
                        _enabledMorphs.Add(morphModel);
                    }
                    else
                    {
                        _enabledMorphs.Remove(morphModel);
                        _awaitingResetMorphs.Add(morphModel);
                    }

                    if(_onlyShowActiveJsb.val)
                    {
                        OnFilterChanged();
                    }
                };

                if(morphModel.EnabledJsb.val)
                {
                    _enabledMorphs.Add(morphModel);
                }
            }

            if(!_restoringFromJson)
            {
                _presetSetOnInit = Name.IDLE;
                base.RestoreFromJSON(_presetJSONs[Name.IDLE]);
            }

            InvokeRepeating(nameof(TriggerMaintainer), 3f, 3f); // To check if the selected collision trigger is still there every 3 seconds
            SuperController.singleton.onBeforeSceneSaveHandlers += OnBeforeSceneSave;
            SuperController.singleton.onSceneSavedHandlers += OnSceneSaved;
            initialized = true;
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
            var collisionTrigger = _person.GetStorableByID(triggerName) as CollisionTrigger;
            if(!CheckIfTriggerExists(collisionTrigger))
            {
                if(collisionTrigger)
                {
                    collisionTrigger.enabled = true;
                    var startTrigger = collisionTrigger.trigger.CreateDiscreteActionStartInternal();
                    startTrigger.name = Name.EXP_RAND_TRIGGER;
                    startTrigger.receiverAtom = _person;
                    startTrigger.receiver = this;
                    startTrigger.receiverTargetName = _triggerTransitionAction.name;
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
            if(triggerName == COLLISION_TRIGGER_DEFAULT_VAL || _person == null || !_person.gameObject)
            {
                return;
            }

            var collisionTrigger = _person.GetStorableByID(triggerName) as CollisionTrigger;
            if(collisionTrigger)
            {
                var triggerJSON = collisionTrigger.trigger.GetJSON();
                var startActions = triggerJSON["startActions"].AsArray;
                for(int i = 0; i < startActions.Count; i++)
                {
                    if(startActions[i]["name"].Value == Name.EXP_RAND_TRIGGER)
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

        readonly Dictionary<string, UIDynamicButton> _presetButtons = new Dictionary<string, UIDynamicButton>();

        UIDynamicButton _saveButton;

        UIDynamicButton _moreButton;
        UIDynamicSlider _loopLengthSlider;
        UIDynamicSlider _morphingSpeedSlider;
        UIDynamicToggle _abaToggle;

        UIDynamicButton _backButton;
        UIDynamicToggle _triggerTransitionsManuallyToggle;
        UIDynamicToggle _randomToggle;
        UIDynamicSlider _triggerChanceSlider;
        UIDynamicButton _triggerTransitionButton;
        UIDynamicPopup _collisionTriggerPopup;

        bool _isSavingPreset;

        void CreateLeftUI()
        {
            CreateSpacer().height = 106f;

            var idlePresetButton = CreateCustomButton(Name.IDLE, new Vector2(10, -62), new Vector2(-911, 52));
            idlePresetButton.buttonColor = new Color(0.66f, 0.77f, 0.88f);
            _presetButtons[Name.IDLE] = idlePresetButton;
            _loadIdlePresetAction.RegisterButton(idlePresetButton);

            var flirtPresetButton = CreateCustomButton(Name.FLIRT, new Vector2(10 + 176, -62), new Vector2(-911, 52));
            flirtPresetButton.buttonColor = new Color(0.88f, 0.77f, 0.66f);
            _presetButtons[Name.FLIRT] = flirtPresetButton;
            _loadFlirtPresetAction.RegisterButton(flirtPresetButton);

            var enjoyPresetButton = CreateCustomButton(Name.ENJOY, new Vector2(10 + 176 * 2, -62), new Vector2(-911, 52));
            enjoyPresetButton.buttonColor = new Color(0.88f, 0.66f, 0.77f);
            _presetButtons[Name.ENJOY] = enjoyPresetButton;
            _loadEnjoyPresetAction.RegisterButton(enjoyPresetButton);

            _saveButton = CreateCustomButton("Save...", new Vector2(10 + 28, -118), new Vector2(-939, 52));
            _saveButton.button.onClick.AddListener(() => StartCoroutine(OnSaveButtonClicked()));

            var preset1Button = CreateCustomButton(Name.SLOT_1, new Vector2(10 + 176, -118), new Vector2(-969, 52));
            _presetButtons[Name.SLOT_1] = preset1Button;
            _loadPreset1Action.RegisterButton(preset1Button);

            var preset2Button = CreateCustomButton(Name.SLOT_2, new Vector2(10 + 293, -118), new Vector2(-969, 52));
            _presetButtons[Name.SLOT_2] = preset2Button;
            _loadPreset2Action.RegisterButton(preset2Button);

            UpdatePresetButtons(_presetSetOnInit);

            var fileButton = CreateCustomButton(Name.FILE, new Vector2(10 + 410, -118), new Vector2(-969, 52));
            _presetButtons[Name.FILE] = fileButton;
            fileButton.button.onClick.AddListener(() =>
            {
                if(_isSavingPreset)
                {
                    FileUtils.OpenSavePresetDialog(OnSavePathSelected);
                }
                else
                {
                    FileUtils.OpenLoadPresetDialog(OnLoadPathSelected);
                }
            });

            CreateSlider(_minJsf);
            CreateSlider(_maxJsf);
            CreateSlider(_multiJsf);
            CreateSlider(_masterSpeedJsf);
            CreateSpacer().height = 106f;
            var toggle = CreateCustomToggle(_playJsb, new Vector2(10f, -721f), -285);
            toggle.toggle.onValueChanged.AddListener(val => toggle.textColor = val ? Color.black : Colors.darkRed);
            CreateCustomToggle(_smoothJsb, new Vector2(280, -721), -285);

            _triggerTransitionButton = CreateCustomButton("Trigger transition", new Vector2(279, -786), new Vector2(-824, 52));
            _triggerTransitionAction.RegisterButton(_triggerTransitionButton);
            _triggerTransitionsManuallyToggle = CreateCustomToggle(_triggerTransitionsManuallyJsb, new Vector2(10, -786), -285);
            _triggerTransitionsManuallyToggle.label = "Manual mode";

            CreateSpacer().height = 63f;
            CreateAdditionalOptionsUI();
            CreateMoreAdditionalOptionsUI();
            SelectOptionsUI(false);
        }

        void OnLoadPathSelected(string path)
        {
            OnPathSelected(path, () =>
            {
                var presetJSON = LoadJSON(path).AsObject;
                if(presetJSON != null)
                {
                    presetJSON["enabled"].AsBool = enabled;
                    base.RestoreFromJSON(presetJSON);
                    UpdatePresetButtons(Name.FILE);
                }
            });
        }

        void OnSavePathSelected(string path)
        {
            OnPathSelected(path, () =>
            {
                if(!path.ToLower().EndsWith(FileUtils.PRESET_EXT.ToLower()))
                {
                    path += "." + FileUtils.PRESET_EXT;
                }

                var jc = GetJSONInternal();
                jc.Remove("enabled");
                SaveJSON(jc, path);
                SuperController.singleton.DoSaveScreenshot(path);
                UpdatePresetButtons(Name.FILE);
            });
        }

        void OnPathSelected(string path, Action saveOrLoadAction)
        {
            _isSavingPreset = false;
            if(string.IsNullOrEmpty(path))
            {
                return;
            }

            FileUtils.UpdateLastBrowseDir(path);
            saveOrLoadAction();
        }

        void OnCustomPresetButtonClicked(string slot)
        {
            if(_isSavingPreset)
            {
                var jc = GetJSONInternal();
                jc.Remove("enabled");
                _presetJSONs[slot] = jc;
                UpdatePresetButtons(slot);
            }
            else
            {
                LoadCustomPreset(slot);
            }

            _isSavingPreset = false;
        }

        void LoadCustomPreset(string slot)
        {
            var presetJSON = _presetJSONs[slot];
            if(presetJSON == null)
            {
                Loggr.Message($"Preset {slot} is not saved yet.");
            }
            else
            {
                presetJSON["enabled"].AsBool = enabled;
                base.RestoreFromJSON(presetJSON);
                UpdatePresetButtons(slot);
            }
        }

        void UpdatePresetButtons(string selectedPresetName)
        {
            foreach(var kvp in _presetButtons)
            {
                kvp.Value.label = kvp.Key;
                kvp.Value.SetNormalFocusedColor();
            }

            if(!string.IsNullOrEmpty(selectedPresetName) && _presetButtons.ContainsKey(selectedPresetName))
            {
                _presetButtons[selectedPresetName].label = _presetButtons[selectedPresetName].label.Bold();
                _presetButtons[selectedPresetName].SetInvisibleFocusedColor();
            }
        }

        readonly static Color _oscillationEndColor = new Color(0.66f, 0.88f, 0.77f);
        readonly static Color _oscillationStartColor = 0.67f * _oscillationEndColor;
        const float OSCILLATION_FREQUENCY = 1f;

        IEnumerator OnSaveButtonClicked()
        {
            _isSavingPreset = true;
            _saveButton.SetActiveStyle(false, true);

            float timeout = Time.unscaledTime + 5;
            while(Time.unscaledTime < timeout && _isSavingPreset)
            {
                float t = (Mathf.Sin(Time.unscaledTime * OSCILLATION_FREQUENCY * 2 * Mathf.PI) + 1) / 2;
                var currentColor = Color.Lerp(_oscillationStartColor, _oscillationEndColor, t);
                _presetButtons[Name.SLOT_1].buttonColor = currentColor;
                _presetButtons[Name.SLOT_2].buttonColor = currentColor;
                _presetButtons[Name.FILE].buttonColor = currentColor;
                yield return null;
            }

            _presetButtons[Name.SLOT_1].buttonColor = Colors.defaultBtnColor;
            _presetButtons[Name.SLOT_2].buttonColor = Colors.defaultBtnColor;
            _presetButtons[Name.FILE].buttonColor = Colors.defaultBtnColor;

            _isSavingPreset = false;
            _saveButton.SetActiveStyle(true);
        }

        void CreateAdditionalOptionsUI()
        {
            _moreButton = CreateCustomButton("Randomness, Collision trigger >", new Vector2(129, -863), new Vector2(-675, 52));
            _moreButton.buttonColor = Color.gray;
            _moreButton.textColor = Color.white;
            _moreButton.button.onClick.AddListener(() => SelectOptionsUI(true));
            _moreButton.buttonText.fontSize = 26;

            _loopLengthSlider = CreateSlider(_loopLengthJsf);
            _loopLengthSlider.label = $"{Name.LOOP_LENGTH} (s)";
            _morphingSpeedSlider = CreateSlider(_morphingSpeedJsf);
            _abaToggle = CreateToggle(_resetUsedExpressionsAtLoopJsb);
        }

        void CreateMoreAdditionalOptionsUI()
        {
            _backButton = CreateCustomButton("< Back", new Vector2(404, -863), new Vector2(-950, 52));
            _backButton.buttonColor = Color.gray;
            _backButton.textColor = Color.white;
            _backButton.button.onClick.AddListener(() => SelectOptionsUI(false));
            _moreButton.buttonText.fontSize = 26;

            _randomToggle = CreateToggle(_randomChancesForTransitionsJsb);
            _triggerChanceSlider = CreateSlider(_chanceToTriggerJsf);
            _triggerChanceSlider.label = $"{Name.CHANCE_TO_TRIGGER} (%)";
            _triggerChanceSlider.valueFormat = "F0";

            _collisionTriggerPopup = CreateCollisionTriggerPopup();

            /* Back button is higher in hierarchy due to being parented to Content instead of LeftContent.
             * Custom listener is added because _colliderTriggerPopup.popup doesn't have an "onClosePopupHandlers" delegate.
             */
            _colliderTriggerPopupListener = _collisionTriggerPopup.popup.popupPanel.gameObject.AddComponent<UnityEventsListener>();
            _colliderTriggerPopupListener.onEnable.AddListener(() =>
            {
                _backButton.SetVisible(false);
                _triggerTransitionButton.SetVisible(false);
                if(_regionPopup)
                {
                    _regionPopup.popup.visible = false;
                }
            });
            _colliderTriggerPopupListener.onDisable.AddListener(() =>
            {
                _backButton.SetVisible(true);
                _triggerTransitionButton.SetVisible(true);
            });
        }

        void SelectOptionsUI(bool alt)
        {
            _loopLengthSlider.SetVisible(!alt);
            _morphingSpeedSlider.SetVisible(!alt);
            _abaToggle.SetVisible(!alt);
            _moreButton.SetVisible(!alt);

            _randomToggle.SetVisible(alt);
            _triggerChanceSlider.SetVisible(alt);
            _collisionTriggerPopup.SetVisible(alt);
            _backButton.SetVisible(alt);
        }

        string _filterText = "";
        UIDynamicButton _prevPageButton;
        UIDynamicButton _nextPageButton;
        UIDynamicPopup _regionPopup;

        readonly UIDynamicToggle[] _morphToggles = new UIDynamicToggle[ITEMS_PER_PAGE];

        void CreateRightUI()
        {
            {
                var jss = new JSONStorableString("Morphs Header", "  Morphs");
                var textField = CreateTextField(jss, true);
                textField.UItext.fontSize = 32;
                textField.backgroundColor = Color.clear;
                textField.text = "\n".Size(8) + jss.val;
                var layout = textField.GetComponent<LayoutElement>();
                layout.preferredHeight = 50f;
                layout.minHeight = 50f;
            }

            var selectNoneButton = CreateCustomButton("Select none", new Vector2(717, -65), new Vector2(-920, 52));
            selectNoneButton.buttonText.fontSize = 26;
            selectNoneButton.button.onClick.AddListener(() =>
            {
                foreach(var morphModel in _morphModels)
                {
                    morphModel.EnabledJsb.val = false;
                }
            });

            var zeroSelectedButton = CreateCustomButton("Zero selected", new Vector2(890, -65), new Vector2(-900, 52));
            zeroSelectedButton.buttonText.fontSize = 26;
            zeroSelectedButton.buttonColor = Colors.rustRed;
            zeroSelectedButton.textColor = Color.white;
            zeroSelectedButton.button.onClick.AddListener(() =>
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

            _regionPopup = CreateRegionPopup();

            CreateSpacer(true).height = 42f;
            CreateCustomToggle(_useAndFilterJsb, new Vector2(10f, -252f), -285f, true);
            CreateCustomToggle(_onlyShowActiveJsb, new Vector2(280f, -252f), -285f, true);

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

            // CreateDevSliders();

            for(int i = 0; i < ITEMS_PER_PAGE; i++)
            {
                if(_morphModels.Count < ITEMS_PER_PAGE)
                {
                    Loggr.Message($"Fatal: less than {ITEMS_PER_PAGE} morphs found.");
                    break;
                }

                /* Morph toggles initialized with the storables of the first 10 morph models. Should be always correct on init. */
                _morphToggles[i] = CreateToggle(_morphModels[i].EnabledJsb, true);
            }

            _prevPageButton = CreateCustomButton("< Prev", new Vector2(549, -1229), new Vector2(-885, 52));
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

            _nextPageButton = CreateCustomButton("Next >", new Vector2(880, -1229), new Vector2(-885, 52));
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

        UIDynamicToggle CreateCustomToggle(JSONStorableBool jsb, Vector2 pos, float sizeX, bool rightSide = false, bool callbacks = false)
        {
            var t = InstantiateToContent(manager.configurableTogglePrefab, rightSide);
            t.GetComponent<LayoutElement>().ignoreLayout = true;
            var rectTransform = GetRekt(t);
            rectTransform.anchoredPosition = pos;
            rectTransform.sizeDelta = new Vector2(sizeX, 52);
            #if ENV_DEVELOPMENT
            if(callbacks)
            {
                SetDevUISliderCallbacks(rectTransform);
            }
            #endif

            var toggle = t.GetComponent<UIDynamicToggle>();
            toggle.label = jsb.name;
            AddToggleToJsb(toggle, jsb);
            return toggle;
        }

        UIDynamicButton CreateCustomButton(string label, Vector2 pos, Vector2 size, bool callbacks = false)
        {
            var t = InstantiateToContent(manager.configurableButtonPrefab);
            var rectTransform = GetRekt(t);
            rectTransform.anchoredPosition = pos;
            rectTransform.sizeDelta = size;
            #if ENV_DEVELOPMENT
            if(callbacks)
            {
                SetDevUISliderCallbacks(rectTransform);
            }
            #endif

            var button = t.GetComponent<UIDynamicButton>();
            button.label = label;
            return button;
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        UIDynamicSlider CreateCustomSlider(string label, Vector2 pos, Vector2 size, bool callbacks = false)
        {
            var t = InstantiateToContent(manager.configurableSliderPrefab);
            var rectTransform = GetRekt(t);
            rectTransform.anchoredPosition = pos;
            rectTransform.sizeDelta = size;
            #if ENV_DEVELOPMENT
            if(callbacks)
            {
                SetDevUISliderCallbacks(rectTransform);
            }
            #endif

            var slider = t.GetComponent<UIDynamicSlider>();
            slider.rangeAdjustEnabled = false;
            slider.label = label;
            return slider;
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
            rectTransform.anchoredPosition = new Vector2(740, -1223);
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
            rectTransform.anchoredPosition = new Vector2(973, -318);
            rectTransform.sizeDelta = new Vector2(-980, 50);
            var button = t.GetComponent<UIDynamicButton>();
            button.label = "Clear";
            button.buttonText.fontSize = 26;
            button.buttonColor = Colors.rustRed;
            button.textColor = new Color(1f, 1f, 1f, 1f);
            return button;
        }

        InputField CreateFilterInputField()
        {
            var filterTextJss = new JSONStorableString("FilterText", FILTER_DEFAULT_VAL);
            var filterTextField = CreateTextField(filterTextJss, true);
            var tfLayout = filterTextField.GetComponent<LayoutElement>();
            tfLayout.preferredHeight = tfLayout.minHeight = 53f;
            filterTextField.height = 53f;
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
        const int ITEMS_PER_PAGE = 13;
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

                if(_onlyShowActiveJsb.val && !morphModel.EnabledJsb.val)
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
                ? "\n".Size(8) + "1 / 1"
                : "\n".Size(8) + $"{_currentPage + 1} / {_totalPages}";

            foreach(var morphToggle in _morphToggles)
            {
                morphToggle.toggle.onValueChanged.RemoveAllListeners();
                toggleToJSONStorableBool.Remove(morphToggle);
                morphToggle.SetVisible(true);
            }

            int onPageCount = 0;

            for(int i = 0; i < _filteredIndices.Count; i++)
            {
                bool isOnPage = i >= _currentPage * ITEMS_PER_PAGE && i < (_currentPage + 1) * ITEMS_PER_PAGE;
                var morphModel = _morphModels[_filteredIndices[i]];
                if(isOnPage)
                {
                    onPageCount++;
                    var morphToggle = _morphToggles[i % ITEMS_PER_PAGE];
                    toggleToJSONStorableBool[morphToggle] = morphModel.EnabledJsb;
                    morphToggle.label = _regionJssc.val == "All" ? morphModel.Label : morphModel.DisplayName;
                    morphModel.EnabledJsb.toggle = morphToggle.toggle;
                    morphToggle.toggle.isOn = morphModel.EnabledJsb.val;
                    morphToggle.toggle.onValueChanged.AddListener(val => morphModel.EnabledJsb.val = val);
                }
                else
                {
                    morphModel.EnabledJsb.toggle = null;
                }
            }

            /* Hide toggles not associated with any storable on the final page */
            for(int i = onPageCount; i < ITEMS_PER_PAGE; i++)
            {
                _morphToggles[i].SetVisible(false);
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
        float _timeout;

        void Update()
        {
            _globalAnimationFrozen = Utils.GlobalAnimationFrozen();
            if(SkipUpdate)
            {
                return;
            }

            try
            {
                float deltaTime = Time.deltaTime;
                if(_timer >= _timeout)
                {
                    _timer = 0f;
                    if(!_triggerTransitionsManuallyJsb.val)
                    {
                        NewRandomTargetMorphValues();
                    }
                }

                _timer += deltaTime;
                UpdateMorphs(_timer, deltaTime);
                ResetAwaitingMorphs(deltaTime);
            }
            catch(Exception e)
            {
                Loggr.Message($"{nameof(Update)} error: {e}");
                enabled = false;
            }
        }

        void ResetAwaitingMorphs(float deltaTime)
        {
            if(_awaitingResetMorphs.Count <= 0)
            {
                return;
            }

            for(int i = _awaitingResetMorphs.Count - 1; i >= 0; i--)
            {
                var morphModel = _awaitingResetMorphs[i];
                if(morphModel.SmoothResetTimer >= _timeout)
                {
                    morphModel.SmoothResetTimer = 0f;
                }

                morphModel.SmoothResetTimer += deltaTime;

                float resetInterpolant = _smoothJsb.val
                    ? SmoothInterpolant(deltaTime, _morphingSpeedJsf.val, _masterSpeedJsf.val, morphModel.SmoothResetTimer, _loopLengthJsf.val)
                    : LinearInterpolant(deltaTime, _morphingSpeedJsf.val, _masterSpeedJsf.val, _loopLengthJsf.val);
                bool finished = morphModel.SmoothResetValue(resetInterpolant);
                if(finished)
                {
                    _awaitingResetMorphs.RemoveAt(i);
                }
            }
        }

        void NewRandomTargetMorphValues()
        {
            foreach(var morphModel in _enabledMorphs)
            {
                if(!_randomChancesForTransitionsJsb.val || UnityEngine.Random.Range(0f, 100f) <= _chanceToTriggerJsf.val)
                {
                    morphModel.SetTargetValue(_minJsf.val, _maxJsf.val, _multiJsf.val, _resetUsedExpressionsAtLoopJsb.val);
                }
            }
        }

        void UpdateMorphs(float deltaTime, float timer)
        {
            float interpolant = _smoothJsb.val
                ? SmoothInterpolant(deltaTime, _morphingSpeedJsf.val, _masterSpeedJsf.val, timer, _loopLengthJsf.val)
                : LinearInterpolant(deltaTime, _morphingSpeedJsf.val, _masterSpeedJsf.val, _loopLengthJsf.val);

            foreach(var morphModel in _enabledMorphs)
            {
                morphModel.CalculateValue(interpolant);
            }
        }

        static float SmoothInterpolant(float deltaTime, float morphingSpeed, float masterSpeed, float timer, float loopLength)
        {
            return deltaTime *
                morphingSpeed *
                masterSpeed *
                Mathf.Sin(timer / (loopLength / masterSpeed) * Mathf.PI);
        }

        static float LinearInterpolant(float deltaTime, float morphingSpeed, float masterSpeed, float loopLength)
        {
            return deltaTime * morphingSpeed * masterSpeed / (loopLength * 30);
        }

        static bool CheckIfTriggerExists(CollisionTrigger trig)
        {
            JSONNode presentTriggers = trig.trigger.GetJSON();
            var asArray = presentTriggers["startActions"].AsArray;
            for(int i = 0; i < asArray.Count; i++)
            {
                var asObject = asArray[i].AsObject;
                string name = asObject["name"];
                if(name == Name.EXP_RAND_TRIGGER && asObject["receiver"] != null)
                {
                    return true;
                }
            }

            return false;
        }

        void ResetActiveMorphs()
        {
            foreach(var morphModel in _morphModels)
            {
                if(morphModel.EnabledJsb.val)
                {
                    morphModel.ResetToInitial();
                }
            }

            foreach(var morphModel in _awaitingResetMorphs)
            {
                morphModel.ResetToInitial();
            }

            _awaitingResetMorphs.Clear();
        }

        [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalse")]
        public override JSONClass GetJSON(bool includePhysical = true, bool includeAppearance = true, bool forceStore = false)
        {
            // prevent potentially hundreds of morphs from being stored as false
            forceStore = false;
            var jc = GetJSONInternal(includePhysical, includeAppearance, forceStore);
            if(_presetJSONs[Name.SLOT_1] != null)
            {
                jc[Name.PRESET_1] = _presetJSONs[Name.SLOT_1];
            }

            if(_presetJSONs[Name.SLOT_2] != null)
            {
                jc[Name.PRESET_1] = _presetJSONs[Name.SLOT_2];
            }

            return jc;
        }

        JSONClass GetJSONInternal(bool includePhysical = true, bool includeAppearance = true, bool forceStore = false)
        {
            var jc = base.GetJSON(includePhysical, includeAppearance, forceStore);
            foreach(var param in _alwaysStoreFloatParams)
            {
                jc[param.name].AsFloat = param.val;
            }

            foreach(var param in _alwaysStoreBoolParams)
            {
                jc[param.name].AsBool = param.val;
            }

            return jc;
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

            if(!jc.Keys.Any())
            {
                /* When migrating from legacy JSON, for some reason jc == {} if the storable was not present in the original save file,
                 * even though file loaded with MergeLoadJSON does contain all necessary storables. No clue why.
                 *
                 * In this case, the restore is skipped and default Idle preset is enabled instead.
                 */
                base.RestoreFromJSON(_presetJSONs[Name.IDLE]);
                _presetSetOnInit = Name.IDLE;
            }
            else
            {
                base.RestoreFromJSON(jc, restorePhysical, restoreAppearance, presetAtoms, setMissingToDefault);
                if(jc.HasKey(Name.PRESET_1))
                {
                    _presetJSONs[Name.SLOT_1] = jc[Name.PRESET_1].AsObject;
                }

                if(jc.HasKey(Name.PRESET_2))
                {
                    _presetJSONs[Name.SLOT_2] = jc[Name.PRESET_2].AsObject;
                }

                CheckPresetEnabledInJSON(jc);
            }

            _restoringFromJson = false;
        }

        void CheckPresetEnabledInJSON(JSONClass jc)
        {
            /* Storable params that don't matter for the equality comparison of stored JSON to built-in preset JSON */
            string[] nonComparisonKeys =
            {
                "id",
                "pluginLabel",
                "enabled",
                Name.PLAY,
                Name.TRIGGER_TRANSITIONS_MANUALLY,
                Name.RANDOM_CHANCES_FOR_TRANSITIONS,
                Name.CHANCE_TO_TRIGGER,
                Name.COLLISION_TRIGGER,
                Name.PRESET_1,
                Name.PRESET_2,
            };

            // string[] comparisonKeys =
            // {
            //
            // }

            var jcKeys = jc.Keys.ToList();
            jcKeys.RemoveAll(nonComparisonKeys.Contains);
            jcKeys.Sort();

            foreach(var kvp in _presetJSONs)
            {
                if(kvp.Value == null)
                {
                    continue;
                }

                var presetJSON = kvp.Value;
                var presetJSONKeys = presetJSON.Keys.ToList();
                presetJSONKeys.RemoveAll(nonComparisonKeys.Contains);
                presetJSONKeys.Sort();

                /* If keys are the same, the same morphs are enabled, but other relevant float/bool params might still differ. */
                if(jcKeys.SequenceEqual(presetJSONKeys) && CheckEqualityComparisonKeys(jc, presetJSON))
                {
                    _presetSetOnInit = kvp.Key;
                    break;
                }
            }
        }

        static bool CheckEqualityComparisonKeys(JSONClass jc, JSONClass presetJSON)
        {
            string[] boolKeys =
            {
                Name.SMOOTH,
                Name.RESET_USED_EXPRESSIONS_AT_LOOP,
            };

            string[] floatKeys =
            {
                Name.MINIMUM_VALUE,
                Name.MAXIMUM_VALUE,
                Name.MASTER_SPEED,
                Name.LOOP_LENGTH,
                Name.MORPHING_SPEED,
            };

            foreach(string key in boolKeys)
            {
                if(jc.HasKey(key) && presetJSON.HasKey(key))
                {
                    if(jc[key].AsBool != presetJSON[key].AsBool)
                    {
                        return false;
                    }
                }
            }

            foreach(string key in floatKeys)
            {
                if(jc.HasKey(key) && presetJSON.HasKey(key))
                {
                    if(Math.Abs(jc[key].AsFloat - presetJSON[key].AsFloat) > 0.009f)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        static bool _savingScene;

        void OnBeforeSceneSave()
        {
            _savingScene = true;
            ResetActiveMorphs();
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
                ResetActiveMorphs();
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

        readonly JSONStorableFloat _posX = new JSONStorableFloat("posX", 0, 0, 1000);
        readonly JSONStorableFloat _posY = new JSONStorableFloat("posY", 0, -2000, 1000);
        readonly JSONStorableFloat _sizeX = new JSONStorableFloat("sizeX", 0, -1200, 1000);
        readonly JSONStorableFloat _sizeY = new JSONStorableFloat("sizeY", 0, -1000, 1000);

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        void CreateDevSliders()
        {
            _morphModels.Clear();
            /* Dev sliders for aligning custom elements */
            var posXSlider = CreateSlider(_posX, true);
            var posYSlider = CreateSlider(_posY, true);
            var sizeXSlider = CreateSlider(_sizeX, true);
            var sizeYSlider = CreateSlider(_sizeY, true);

            posXSlider.valueFormat = "F0";
            posYSlider.valueFormat = "F0";
            sizeXSlider.valueFormat = "F0";
            sizeYSlider.valueFormat = "F0";
        }

        void SetDevUISliderCallbacks(RectTransform rectTransform)
        {
            if(_posX == null || _posY == null || _sizeX == null || _sizeY == null)
            {
                return;
            }

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
