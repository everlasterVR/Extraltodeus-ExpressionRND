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
                    [uid] = $"{GetPackagePath()}Custom/Scripts/__Frequently Used/ExpressionRandomizer.cslist",
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

        /* This gets called when loading an old scene saved with Extraltodeus-ExpressionRND */
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

            MigrateFromLegacyJSON(jc);

            /*
             * 1. Create fake preset JSON for loading the plugin with stored data from the .cslist
             * 2. Save fake preset JSON to temporary file
             * 3. Merge load the temporary preset
             * 4. Delete the temporary preset
             * 5. Remove this legacy plugin from the atom
             */

            string tmpPresetName = $"tmp_{Guid.NewGuid().ToString().Substring(0, 7)}";
            string uid = FindPluginUid(jc); // e.g. plugin#0
            if(uid == null)
            {
                SuperController.LogError(
                    $"{nameof(ExpressionRandomizer)}: Failed to auto-migrate from VamTimbo.Extraltodeus-ExpressionRND.1.var:" +
                    $" could not find plugin UID from plugin JSON or pluginManager JSON"
                );
                yield break;
            }

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

        string FindPluginUid(JSONClass jc)
        {
            /* Plugin data storable exists */
            if(jc.HasKey("id"))
            {
                return jc["id"].Value.Split('_').First();
            }

            /* Plugin data storable does not exist -> find uid from manager JSON */
            var managerPluginsJSON = manager.GetJSON()["plugins"].AsObject;
            foreach(string key in managerPluginsJSON.Keys)
            {
                string fullPath = managerPluginsJSON[key].Value;
                if(fullPath.StartsWith("VamTimbo.Extraltodeus-ExpressionRND"))
                {
                    return key;
                }
            }

            return null;
        }

        static void MigrateFromLegacyJSON(JSONClass jc)
        {
            foreach(string key in new[]
            {
                "Brow/Brow Inner Up Left",
                "Brow/Brow Inner Up Right",
                "Brow/Brow Outer Up Left",
                "Brow/Brow Outer Up Right",
                "Expressions/Concentrate",
                "Expressions/Desire",
                "Expressions/Flirting",
                "Expressions/Snarl Left",
                "Expressions/Snarl Right",
                "Eyes/Eyes Squint Left",
                "Eyes/Eyes Squint Right",
                "Eyes/Pupils Dialate",
                "Lips/Lips Pucker",
            })
            {
                if(!jc.HasKey(key))
                {
                    jc[key].AsBool = true;
                }
            }

            if(!jc.HasKey("Minimum value"))
            {
                jc["Minimum value"].AsFloat = -0.15f;
            }

            if(!jc.HasKey("Maximum value"))
            {
                jc["Maximum value"].AsFloat = 0.35f;
            }

            if(!jc.HasKey("Multiplier"))
            {
                jc["Multiplier"].AsFloat = 1f;
            }

            if(!jc.HasKey("Master speed"))
            {
                jc["Master speed"].AsFloat = 1f;
            }

            if(!jc.HasKey("Loop length"))
            {
                jc["Loop length"].AsFloat = 2f;
            }

            if(!jc.HasKey("Morphing speed"))
            {
                jc["Morphing speed"].AsFloat = 1f;
            }

            if(!jc.HasKey("Play"))
            {
                jc["Play"].AsBool = true;
            }

            if(!jc.HasKey("Smooth"))
            {
                jc["Smooth"].AsBool = true;
            }

            if(!jc.HasKey("Reset used expressions at loop"))
            {
                jc["Reset used expressions at loop"].AsBool = true;
            }
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
