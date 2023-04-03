using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Graphs;
using UnityEngine;
using UnityEngine.UIElements;

namespace VRRefAssist
{
    internal class VRRefAssistSettings : ScriptableObject
    {
        private static string SettingsPath => "Assets/VR RefAssist/Settings/VR RefAssist Settings.asset";
        
        public bool executeRunOnBuildMethodsWhenEnteringPlayMode = true;
        public bool executeFieldAutomationWhenEnteringPlayMode = true;
        
        public bool showPopupWarnsForFailedFieldAutomation = false;

        private static VRRefAssistSettings _settings;
        
        public static VRRefAssistSettings GetOrCreateSettings()
        {
            if(_settings != null)
                return _settings;
            
            string settingsPath = SettingsPath;
            VRRefAssistSettings settings = AssetDatabase.LoadAssetAtPath<VRRefAssistSettings>(settingsPath);
            if (settings == null)
            {
                if (!AssetDatabase.IsValidFolder(Path.GetDirectoryName(settingsPath)))
                    Directory.CreateDirectory(Path.GetDirectoryName(settingsPath));
                
                _settings = settings = CreateInstance<VRRefAssistSettings>();
                AssetDatabase.CreateAsset(settings, settingsPath);
                AssetDatabase.SaveAssets();
            }

            return settings;
        }
    }

    internal class VRRefAssistSettingsProvider : SettingsProvider
    {
        SerializedObject m_SerializedObject;
        SerializedProperty executeRunOnBuildMethodsWhenEnteringPlayModeProp;
        SerializedProperty executeFieldAutomationWhenEnteringPlayModeProp;
        SerializedProperty showPopupWarnsForFailedFieldAutomationProp;
        
        private class Styles
        {
            public static readonly GUIContent _executeRunOnBuildWhenEnteringPlayModeLabel = new GUIContent("Execute RunOnBuild Methods when entering Play Mode", "If enabled, RunOnBuild methods will be executed when entering Play Mode.");
            public static readonly GUIContent _runFieldAutomationWhenEnteringPlayModeLabel = new GUIContent("Run Field Automation when entering Play Mode", "If enabled, Field Automation will be executed when entering Play Mode.");
            
            public static readonly GUIContent _showPopupWarnsForFailedFieldAutomationLabel = new GUIContent("Show Popup Warns for Failed Field Automation", "If enabled, a popup will be shown when Field Automation fails asking if you want to abort a build.\nThis can be annoying if you have many fields and don't necessarily care if some reference is missing.");
        }

        [MenuItem("VR RefAssist/Settings", priority = 200)]
        public static void OpenSettings()
        {
            SettingsService.OpenProjectSettings("Project/VR RefAssist");
        }

        public VRRefAssistSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null) : base(path, scopes, keywords)
        {
        }
        
        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            m_SerializedObject = new SerializedObject(VRRefAssistSettings.GetOrCreateSettings());
            executeRunOnBuildMethodsWhenEnteringPlayModeProp = m_SerializedObject.FindProperty(nameof(VRRefAssistSettings.executeRunOnBuildMethodsWhenEnteringPlayMode));
            executeFieldAutomationWhenEnteringPlayModeProp = m_SerializedObject.FindProperty(nameof(VRRefAssistSettings.executeFieldAutomationWhenEnteringPlayMode));
            
            showPopupWarnsForFailedFieldAutomationProp = m_SerializedObject.FindProperty(nameof(VRRefAssistSettings.showPopupWarnsForFailedFieldAutomation));
        }
        
        [SettingsProvider]
        public static SettingsProvider CreateMySingletonProvider()
        {
            var provider = new VRRefAssistSettingsProvider("Project/VR RefAssist", SettingsScope.Project, GetSearchKeywordsFromGUIContentProperties<Styles>());
            return provider;
        }
        
        public override void OnGUI(string searchContext)
        {
            var settings = VRRefAssistSettings.GetOrCreateSettings();
            
            using (CreateSettingsWindowGUIScope())
            {
                m_SerializedObject.Update();
                EditorGUI.BeginChangeCheck();

                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("Entering Play Mode", EditorStyles.boldLabel);
                    executeRunOnBuildMethodsWhenEnteringPlayModeProp.boolValue = EditorGUILayout.ToggleLeft(Styles._executeRunOnBuildWhenEnteringPlayModeLabel, executeRunOnBuildMethodsWhenEnteringPlayModeProp.boolValue);
                    executeFieldAutomationWhenEnteringPlayModeProp.boolValue = EditorGUILayout.ToggleLeft(Styles._runFieldAutomationWhenEnteringPlayModeLabel, executeFieldAutomationWhenEnteringPlayModeProp.boolValue);
                }
                
                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("Aborting Builds", EditorStyles.boldLabel);
                    showPopupWarnsForFailedFieldAutomationProp.boolValue = EditorGUILayout.ToggleLeft(Styles._showPopupWarnsForFailedFieldAutomationLabel, showPopupWarnsForFailedFieldAutomationProp.boolValue);
                }
                

                if (EditorGUI.EndChangeCheck())
                {
                    m_SerializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(settings);
                }
            }
        }
        
        private IDisposable CreateSettingsWindowGUIScope()
        {
            var unityEditorAssembly = Assembly.GetAssembly(typeof(EditorWindow));
            var type = unityEditorAssembly.GetType("UnityEditor.SettingsWindow+GUIScope");
            return Activator.CreateInstance(type) as IDisposable;
        }
    }
}