using MVR.FileManagementSecure;
using System.Collections.Generic;

static class FileUtils
{
    public const string PRESET_DIR = "Saves/PluginData/ExpressionRandomizer/Presets";

    public static List<ShortCut> GetShortCutsForDirectory(string path)
    {
        var cutsForDirectory = FileManagerSecure.GetShortCutsForDirectory(path, true, true, true);
        var rootShortCut = new ShortCut
        {
            displayName = "Root",
            path = FileManagerSecure.GetFullPath("."),
        };
        cutsForDirectory.Insert(0, rootShortCut);
        return cutsForDirectory;
    }

    public static void EnsureDirExists(string dirName)
    {
        if(!DirectoryExists(dirName))
        {
            FileManagerSecure.CreateDirectory(dirName);
        }
    }

    public static bool DirectoryExists(string dirName)
    {
        return FileManagerSecure.DirectoryExists(dirName);
    }
}
