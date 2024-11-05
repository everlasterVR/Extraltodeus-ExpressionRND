using extraltodeus;
using MVR.FileManagementSecure;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;

namespace everlaster
{
    sealed class PresetHandler
    {
        readonly ExpressionRandomizer _script;
        string _lastDir;
        public JSONStorableUrl loadPresetJsu { get; }
        public JSONStorableUrl savePresetJsu { get; }

        const string PRESET_DIR = "Saves/PluginData/ExpressionRandomizer/Presets";
        const string PRESET_EXT = "json";

        public PresetHandler(ExpressionRandomizer script)
        {
            _script = script;
            LastDirFallback();

            loadPresetJsu = new JSONStorableUrl("Load Preset", string.Empty, "json", _lastDir)
            {
                allowFullComputerBrowse = true,
                allowBrowseAboveSuggestedPath = true,
                hideExtension = true,
                showDirs = true,
                beginBrowseWithObjectCallback = BeginLoadBrowse,
                endBrowseWithObjectCallback = jsu => OnLoadPathSelected(jsu.val),
            };

            savePresetJsu = new JSONStorableUrl("Save Preset", string.Empty, "json", _lastDir)
            {
                allowFullComputerBrowse = true,
                allowBrowseAboveSuggestedPath = true,
                hideExtension = true,
                showDirs = true,
                beginBrowseWithObjectCallback = BeginSaveBrowse,
                endBrowseWithObjectCallback = jsu => OnSavePathSelected(jsu.val),
                shortCuts = new List<ShortCut>(), // clear shortcuts
            };

            var loadPresetWithPathJsu = new JSONStorableUrl("_loadPresetWithPath", string.Empty, "json", _lastDir)
            {
                allowFullComputerBrowse = true,
                allowBrowseAboveSuggestedPath = true,
                hideExtension = true,
                showDirs = true,
                beginBrowseWithObjectCallback = BeginLoadBrowse,
            };

            var loadPresetWithPathJSON = new JSONStorableActionPresetFilePath(
                "LoadPresetWithPath",
                OnLoadPathSelected,
                loadPresetWithPathJsu
            );

            script.RegisterPresetFilePathAction(loadPresetWithPathJSON);
        }

        // see e.g. DAZCharacterTextureControl.BeginBrowse
        void BeginLoadBrowse(JSONStorableUrl jsu)
        {
            FileUtils.EnsureDirExists(PRESET_DIR);
            LastDirFallback();
            jsu.shortCuts = FileUtils.GetShortCutsForDirectory(PRESET_DIR);
        }

        void BeginSaveBrowse(JSONStorableUrl jsu)
        {
            FileUtils.EnsureDirExists(PRESET_DIR);
            LastDirFallback();
            _script.StartCoroutine(SetSaveFileBrowserCo());
        }

        static IEnumerator SetSaveFileBrowserCo()
        {
            yield return null;
            var browser = SuperController.singleton.mediaFileBrowserUI;
            browser.SetTitle("Select Save File");
            browser.browseVarFilesAsDirectories = false;
            browser.SetTextEntry(true);
            browser.fileEntryField.text = $"{((int) (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds).ToString()}.{PRESET_EXT}";
            browser.ActivateFileNameField();
        }

        void OnLoadPathSelected(string path)
        {
            if(string.IsNullOrEmpty(path))
            {
                return;
            }

            OnPathSelected(ref path);
            var jc = _script.LoadJSON(path).AsObject;
            if(jc != null && ValidatePreset(jc, path))
            {
                jc["enabled"].AsBool = _script.enabled;
                _script.RestoreFromPresetJSON(jc);
            }

            loadPresetJsu.val = ""; // ensure empty value when next browse cancelled
            _script.UpdatePresetButtons(Name.FILE);
        }

        bool ValidatePreset(JSONClass jc, string path)
        {
            string id = jc["id"].Value;
            if(!id.EndsWith($"{nameof(extraltodeus)}.{nameof(ExpressionRandomizer)}"))
            {
                _script.logBuilder.Error($"Loaded preset is not a valid {nameof(ExpressionRandomizer)} preset. Path: {path}");
                return false;
            }

            return true;
        }

        void OnSavePathSelected(string path)
        {
            if(string.IsNullOrEmpty(path))
            {
                return;
            }

            OnPathSelected(ref path);
            if(!path.ToLower().EndsWith(PRESET_EXT.ToLower()))
            {
                path += "." + PRESET_EXT;
            }

            var jc = _script.GetJSONInternal();
            jc.Remove("enabled");
            _script.SaveJSON(jc, path);
            SuperController.singleton.DoSaveScreenshot(path);
            savePresetJsu.val = ""; // ensure empty value when next browse cancelled
            _script.UpdatePresetButtons(Name.FILE);
        }

        void OnPathSelected(ref string path)
        {
            path = SuperController.singleton.NormalizePath(path);
            string containingDir = path.Substring(0, path.LastIndexOfAny(new[] { '/', '\\' }));
            FileUtils.EnsureDirExists(containingDir);
            _lastDir = containingDir + @"\";
            loadPresetJsu.suggestedPath = _lastDir;
        }

        void LastDirFallback()
        {
            if(!FileUtils.DirectoryExists(_lastDir))
            {
                _lastDir = PRESET_DIR;
            }
        }
    }
}
