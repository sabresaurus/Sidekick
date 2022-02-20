﻿using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Sabresaurus.Sidekick
{
    public static class SidekickSettings
    {
        public static bool HideAutoGenerated
        {
            get => EditorPrefs.GetBool("SidekickSettings.HideAutoGenerated", true);
            set => EditorPrefs.SetBool("SidekickSettings.HideAutoGenerated", value);
        }
        
        public static bool PreferUnityAttributes
        {
            get => EditorPrefs.GetBool("SidekickSettings.PreferUnityAttributes", false);
            set => EditorPrefs.SetBool("SidekickSettings.PreferUnityAttributes", value);
        }
    }

    // Register a SettingsProvider using IMGUI for the drawing framework:
    static class SidekickSettingsRegister
    {
        public const string SETTINGS_PATH = "Preferences/Sidekick";
        private static readonly GUIContent HideAutoGenerated = new GUIContent("Hide Auto Generated", "Whether automatically generated get/set methods and backing fields are hidden");
        private static readonly GUIContent PreferUnityAttributes = new GUIContent("Prefer Unity Attributes", "");
        
        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider()
        {
            var provider = new SettingsProvider(SETTINGS_PATH, SettingsScope.User)
            {
                // Create the SettingsProvider and initialize its drawing (IMGUI) function in place:
                guiHandler = _ =>
                {
                    SidekickSettings.HideAutoGenerated = EditorGUILayout.Toggle(HideAutoGenerated, SidekickSettings.HideAutoGenerated);
                    SidekickSettings.PreferUnityAttributes = EditorGUILayout.Toggle(PreferUnityAttributes, SidekickSettings.PreferUnityAttributes);
                },

                // Populate the search keywords to enable smart search filtering and label highlighting:
                keywords = new HashSet<string>(new[] {HideAutoGenerated.text, PreferUnityAttributes.text})
            };

            return provider;
        }
    }
}