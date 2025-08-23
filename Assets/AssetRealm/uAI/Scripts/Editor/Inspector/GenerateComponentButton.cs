using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Linq;

namespace UAI
{

    [InitializeOnLoad]
    public static class GenerateComponentButton
    {
        static GenerateComponentButton()
        {
            Selection.selectionChanged += OnSelectionChanged;
            OnSelectionChanged();
        }

        static void OnSelectionChanged()
        {
            EditorApplication.delayCall += AddButton;
        }

        static void AddButton()
        {
            if (Selection.activeGameObject == null) return;

            var windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
            foreach (var window in windows)
            {
                if (window.GetType().Name != "InspectorWindow") continue;
                var root = window.rootVisualElement;
                var container = root.Q<VisualElement>(name: "unity-content-container");
                if (container == null)
                {
                    Debug.LogWarning("Could not find unity-content-container in InspectorWindow");
                    continue;
                }

                if (root.Q<Button>("generate-component-btn") == null)
                {
                    var generateBtn = new Button(clicked)
                    {
                        text = "Generate Component",
                        name = "generate-component-btn"
                    };
                    generateBtn.style.marginTop = 2;
                    generateBtn.style.marginLeft = 3;
                    generateBtn.style.marginRight = 3;
                    generateBtn.style.height = 23;
                    generateBtn.style.width = 230;
                    generateBtn.style.alignSelf = Align.Center;

                    container.Add(generateBtn);
                } 
                break;
            }
        }

        static void clicked()
        {
            ScriptCreatorWindow.Init();
            ScriptCreatorWindow.SelectedGameObjectsWhenOpened = Selection.gameObjects.ToList();
        }
    }
}