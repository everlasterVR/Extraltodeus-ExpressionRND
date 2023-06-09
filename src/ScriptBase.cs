﻿using System;
using System.Collections;
using UnityEngine.UI;

class ScriptBase : MVRScript
{
    UnityEventsListener pluginUIEventsListener { get; set; }
    protected bool? initialized { get; set; }

    // Prevent ScriptBase from showing up as a plugin in Plugins tab
    public override bool ShouldIgnore()
    {
        return true;
    }

    public override void InitUI()
    {
        base.InitUI();
        if(!UITransform || pluginUIEventsListener)
        {
            return;
        }

        StartCoroutine(InitUICo());
    }

    // ReSharper disable once VirtualMemberNeverOverridden.Global
    protected virtual Action OnUIEnabled()
    {
        return null;
    }

    // ReSharper disable once VirtualMemberNeverOverridden.Global
    protected virtual Action OnUIDisabled()
    {
        return null;
    }

    IEnumerator InitUICo()
    {
        pluginUIEventsListener = UITransform.gameObject.AddComponent<UnityEventsListener>();
        pluginUIEventsListener.onEnable.AddListener(() => StartCoroutine(OnUIEnabledCo(OnUIEnabled())));

        while(initialized == null)
        {
            yield return null;
        }

        if(initialized == false)
        {
            enabledJSON.val = false;
            yield break;
        }

        var onUIDisabled = OnUIDisabled();
        if(onUIDisabled != null)
        {
            pluginUIEventsListener.onDisable.AddListener(() => StartCoroutine(OnUIDisabledCo(onUIDisabled)));
        }
    }

    bool _inEnabledCo;

    IEnumerator OnUIEnabledCo(Action callback = null)
    {
        if(_inEnabledCo)
        {
            /* When VAM UI is toggled back on with the plugin UI already active, onEnable gets called twice and onDisable once.
             * This ensures onEnable logic executes just once.
             */
            yield break;
        }

        _inEnabledCo = true;
        SetGrayBackground();

        if(callback != null)
        {
            yield return null;
            yield return null;
            yield return null;

            while(initialized == null)
            {
                yield return null;
            }

            if(initialized == false)
            {
                yield break;
            }

            callback();
        }

        _inEnabledCo = false;
    }

    void SetGrayBackground()
    {
        var background = rightUIContent.parent.parent.parent.transform.GetComponent<Image>();
        background.color = Colors.backgroundGray;
    }

    IEnumerator OnUIDisabledCo(Action callback)
    {
        if(_inEnabledCo)
        {
            /* When VAM UI is toggled back on with the plugin UI already active, onEnable gets called twice and onDisable once.
             * This ensures only onEnable logic executes.
             */
            yield break;
        }

        callback();
    }

    public void DeferSetSaveFileBrowser(string fileExt)
    {
        StartCoroutine(SetSaveFileBrowser(fileExt));
    }

    static IEnumerator SetSaveFileBrowser(string fileExt)
    {
        yield return null;
        var browser = SuperController.singleton.mediaFileBrowserUI;
        browser.browseVarFilesAsDirectories = false;
        browser.SetTextEntry(true);
        browser.fileEntryField.text = $"{((int) (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds).ToString()}.{fileExt}";
        browser.ActivateFileNameField();
    }

    protected void OnDestroy()
    {
        DestroyImmediate(pluginUIEventsListener);
    }
}
