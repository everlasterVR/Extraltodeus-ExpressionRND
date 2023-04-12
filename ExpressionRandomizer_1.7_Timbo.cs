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
                    [uid] = $"{GetPackagePath(uid)}Custom/Scripts/__Frequently Used/ExpressionRandomizer.cslist",
                },
            });
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

            /*
             * 1. Create fake preset JSON for loading the plugin with stored data from the .cslist
             * 2. Save fake preset JSON to temporary file
             * 3. Merge load the temporary preset
             * 4. Delete the temporary preset
             * 5. Remove this legacy plugin from the atom
             */

            string tmpPresetName = $"tmp_{Guid.NewGuid().ToString().Substring(0, 7)}";
            string uid = GetPluginId();
            MigrateFromLegacyJSON(jc, uid);

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

        static void MigrateFromLegacyJSON(JSONClass jc, string uid)
        {
            jc["id"] = $"{uid}_extraltodeus.ExpressionRandomizer";

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

        string GetPluginId()
        {
            return name.Substring(0, name.IndexOf('_'));
        }

        string GetPackageId(string pluginUid)
        {
            string filename = manager.GetJSON()["plugins"][pluginUid].Value;
            int idx = filename.IndexOf(":/", StringComparison.Ordinal);
            return idx >= 0 ? filename.Substring(0, idx) : null;
        }

        string GetPackagePath(string pluginUid)
        {
            string packageId = GetPackageId(pluginUid);
            return packageId == null ? "" : $"{packageId}:/";
        }
    }
}
