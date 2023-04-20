using extraltodeus;
using MVR.FileManagementSecure;
using SimpleJSON;
using System.Collections.Generic;

sealed class PresetHandler
{
    readonly ExpressionRandomizer _script;
    string _lastDir;
    public JSONStorableUrl loadPresetJsu { get; }
    public JSONStorableUrl savePresetJsu { get; }

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
            Name.LOAD_PRESET_WITH_PATH,
            OnLoadPathSelected,
            loadPresetWithPathJsu
        );

        script.RegisterPresetFilePathAction(loadPresetWithPathJSON);
    }

    // see e.g. DAZCharacterTextureControl.BeginBrowse
    void BeginLoadBrowse(JSONStorableUrl jsu)
    {
        FileUtils.EnsureDirExists(FileUtils.PRESET_DIR);
        LastDirFallback();
        jsu.shortCuts = FileUtils.GetShortCutsForDirectory(FileUtils.PRESET_DIR);
    }

    void BeginSaveBrowse(JSONStorableUrl jsu)
    {
        FileUtils.EnsureDirExists(FileUtils.PRESET_DIR);
        LastDirFallback();
        _script.DeferSetSaveFileBrowser(PRESET_EXT); // JSONStorableUrl calls BeginSaveBrowse before creating the file browser
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

    static bool ValidatePreset(JSONClass jc, string path)
    {
        string id = jc["id"].Value;
        if(!id.EndsWith($"{nameof(extraltodeus)}.{nameof(ExpressionRandomizer)}"))
        {
            Loggr.Error($"Loaded preset is not a valid {nameof(ExpressionRandomizer)} preset. Path: {path}");
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
            _lastDir = FileUtils.PRESET_DIR;
        }
    }
}
