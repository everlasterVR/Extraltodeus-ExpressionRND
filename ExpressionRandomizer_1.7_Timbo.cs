using MeshVR;
using MVR.FileManagementSecure;
using SimpleJSON;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace extraltodeuslExpRandPlugin
{
    sealed class ExpressionRandomizer : MVRScript
    {
        bool? _initialized;

        public override void Init()
        {
            StartCoroutine(InitCo());
        }

        IEnumerator InitCo()
        {
            yield return new WaitForEndOfFrame();
            while(SuperController.singleton.isLoading)
            {
                yield return null;
            }

            _initialized = true;
        }

        public override void RestoreFromJSON(
            JSONClass jc,
            bool restorePhysical = true,
            bool restoreAppearance = true,
            JSONArray presetAtoms = null,
            bool setMissingToDefault = true
        )
        {
            StartCoroutine(RestoreFromJSONCo(jc));
        }

        JSONClass FakeCslistPluginPresetJSON(JSONClass storedParams, string uid)
        {
            var storables = new JSONArray();
            storables.Add(new JSONClass
            {
                ["id"] = "PluginManager",
                ["plugins"] = new JSONClass
                {
                    [uid] = $"{GetPackagePath()}Custom/Scripts/everlaster/Extraltodeus-ExpressionRND/ExpressionRandomizer.cslist",
                },
            });
            storedParams["id"] = storedParams["id"].Value.Replace("extraltodeuslExpRandPlugin", "extraltodeus");
            storables.Add(storedParams);

            return new JSONClass
            {
                ["setUnlistedParamsToDefault"] = { AsBool = true },
                ["storables"] = storables,
            };
        }

        IEnumerator RestoreFromJSONCo(JSONClass jc)
        {
            while(_initialized == null)
            {
                yield return null;
            }

            if(_initialized == false)
            {
                yield break;
            }

            /* This gets called when loading an old scene saved with Extraltodeus-ExpressionRND
             * 1. Create fake preset JSON for loading the plugin with stored data from the .cslist
             * 2. Save fake preset JSON to temporary file
             * 3. Merge load the temporary preset
             * 4. Delete the temporary preset
             * 5. Remove this legacy plugin from the atom
             */

            string tmpPresetName = $"tmp_{Guid.NewGuid().ToString().Substring(0, 7)}";
            string uid = jc["id"].Value.Split('_').First(); // e.g. plugin#0
            string savePath = $"Custom/Atom/Person/Plugins/Preset_{tmpPresetName}.vap";
            FileManagerSecure.WriteAllText(savePath, FakeCslistPluginPresetJSON(jc, uid).ToString(string.Empty));

            var pluginPresetManager = containingAtom.GetStorableByID("PluginPresets").GetComponentInChildren<PresetManager>();
            var pluginPresetManagerControl = containingAtom.presetManagerControls.Find(pmc => pmc.name == "PluginPresets");

            string currentPresetName = pluginPresetManagerControl.GetStringParamValue("presetName");

            pluginPresetManagerControl.SetStringParamValue("presetName", tmpPresetName);
            pluginPresetManager.MergeLoadPreset();
            pluginPresetManager.DeletePreset();
            pluginPresetManagerControl.SetStringParamValue("presetName", currentPresetName);

            manager.RemovePluginWithUID(uid);
        }

        string GetPackageId()
        {
            string id = name.Substring(0, name.IndexOf('_'));
            string filename = manager.GetJSON()["plugins"][id].Value;
            int idx = filename.IndexOf(":/", StringComparison.Ordinal);
            return idx >= 0 ? filename.Substring(0, idx) : null;
        }

        string GetPackagePath()
        {
            string packageId = GetPackageId();
            return packageId == null ? "" : $"{packageId}:/";
        }
    }
}
