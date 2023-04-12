using System;
using MVR.FileManagementSecure;
using uFileBrowser;

static class FileUtils
{
    const string SAVES_DIR = "Saves/PluginData/ExpressionRandomizer/Presets";
    public const string PRESET_EXT = "json";
    static string _lastBrowseDir;

    public static void OpenSavePresetDialog(FileBrowserCallback callback)
    {
        EnsureSavesDirExists();
        if(string.IsNullOrEmpty(_lastBrowseDir))
        {
            _lastBrowseDir = SAVES_DIR;
        }

        SuperController.singleton.NormalizeMediaPath(_lastBrowseDir + "/"); // Sets lastMediaDir if path it exists
        SuperController.singleton.GetMediaPathDialog(callback, PRESET_EXT);

        // Update the browser to be a Save browser
        var browser = SuperController.singleton.mediaFileBrowserUI;
        browser.SetTextEntry(true);
        browser.fileEntryField.text = $"{((int) (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds).ToString()}.{PRESET_EXT}";
        browser.ActivateFileNameField();
    }

    public static void OpenLoadPresetDialog(FileBrowserCallback callback)
    {
        EnsureSavesDirExists();
        if(string.IsNullOrEmpty(_lastBrowseDir))
        {
            _lastBrowseDir = SAVES_DIR;
        }

        SuperController.singleton.NormalizeMediaPath(_lastBrowseDir + "/"); // Sets lastMediaDir if path it exists
        SuperController.singleton.GetMediaPathDialog(callback, PRESET_EXT);
    }

    public static void UpdateLastBrowseDir(string path)
    {
        _lastBrowseDir = path.Substring(0, path.LastIndexOfAny(new[] { '/', '\\' })) + @"\";
    }

    static void EnsureSavesDirExists()
    {
        if(!FileManagerSecure.DirectoryExists(SAVES_DIR))
        {
            FileManagerSecure.CreateDirectory(SAVES_DIR);
        }
    }
}
