using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UAI
{

    [Serializable]
    public class GameArtPresetSelection
    {
        public string category;
        public string preset;

        public GameArtPresetSelection(string category, string preset)
        {
            this.category = category;
            this.preset = preset;
        }

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(category) && !string.IsNullOrEmpty(preset);
        }

        public override string ToString()
        {
            return $"{category} - {preset}";
        }
    }

    public class GameArtPresetDropdown
    {
        // Static reference to the preset data
        private static Dictionary<string, Dictionary<string, string>> presets;

        // Current selection state
        private string selectedCategory;
        private string selectedPreset;

        // Callback for when selection changes
        private Action<GameArtPresetSelection> onSelectionChanged;

        // Style cache
        private GUIStyle popupStyle;

        public GameArtPresetDropdown(
            Dictionary<string, Dictionary<string, string>> presetData,
            Action<GameArtPresetSelection> onChange = null,
            GameArtPresetSelection initialSelection = null)
        {
            presets = presetData;
            onSelectionChanged = onChange;

            // Set initial selection if provided
            if (initialSelection != null && initialSelection.IsValid())
            {
                selectedCategory = initialSelection.category;
                selectedPreset = initialSelection.preset;
            }
            else if (presets.Count > 0)
            {
                // Default to first category
                selectedCategory = presets.Keys.First();

                // Default to first preset in the category
                var categoryPresets = presets[selectedCategory];
                if (categoryPresets.Count > 0)
                {
                    selectedPreset = categoryPresets.Keys.First();
                }
            }

            // Initialize styles
            popupStyle = new GUIStyle(EditorStyles.popup)
            {
                fixedHeight = 20,
                margin = new RectOffset(2, 2, 2, 2)
            };
        }

        public void Draw()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Game Art Preset", EditorStyles.boldLabel);

            // Category dropdown
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Category:", GUILayout.Width(70));

            string[] categories = presets.Keys.ToArray();
            int categoryIndex = Array.IndexOf(categories, selectedCategory);
            if (categoryIndex < 0) categoryIndex = 0;

            int newCategoryIndex = EditorGUILayout.Popup(categoryIndex, categories, popupStyle);
            if (newCategoryIndex != categoryIndex)
            {
                selectedCategory = categories[newCategoryIndex];

                // When category changes, default to first preset in the new category
                var categoryPresets = presets[selectedCategory];
                if (categoryPresets.Count > 0)
                {
                    selectedPreset = categoryPresets.Keys.First();

                    // Trigger onChange callback
                    if (onSelectionChanged != null)
                    {
                        onSelectionChanged.Invoke(getCurrentSelection());
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            // Preset dropdown
            if (!string.IsNullOrEmpty(selectedCategory))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Preset:", GUILayout.Width(70));

                var categoryPresets = presets[selectedCategory];
                string[] presetOptions = categoryPresets.Keys.ToArray();
                int presetIndex = Array.IndexOf(presetOptions, selectedPreset);
                if (presetIndex < 0) presetIndex = 0;

                int newPresetIndex = EditorGUILayout.Popup(presetIndex, presetOptions, popupStyle);
                if (newPresetIndex != presetIndex)
                {
                    selectedPreset = presetOptions[newPresetIndex];

                    // Trigger onChange callback
                    if (onSelectionChanged != null)
                    {
                        onSelectionChanged.Invoke(getCurrentSelection());
                    }
                }
                EditorGUILayout.EndHorizontal();

                // Display current preset description (optional)
                if (!string.IsNullOrEmpty(selectedPreset))
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Description:", EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;
                    EditorGUILayout.LabelField(getPresetDescription(), EditorStyles.wordWrappedLabel);
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.EndVertical();
        }

        private string getPresetDescription()
        {
            if (string.IsNullOrEmpty(selectedCategory) || string.IsNullOrEmpty(selectedPreset))
            {
                return string.Empty;
            }

            try
            {
                return presets[selectedCategory][selectedPreset];
            }
            catch
            {
                return "Description not available.";
            }
        }

        public GameArtPresetSelection getCurrentSelection()
        {
            return new GameArtPresetSelection(selectedCategory, selectedPreset);
        }

        public void setSelection(GameArtPresetSelection selection)
        {
            if (selection != null && selection.IsValid())
            {
                // Verify the selection exists in our presets
                if (presets.ContainsKey(selection.category) &&
                    presets[selection.category].ContainsKey(selection.preset))
                {
                    selectedCategory = selection.category;
                    selectedPreset = selection.preset;
                }
            }
        }

        public string getCurrentPresetText()
        {
            if (string.IsNullOrEmpty(selectedCategory) || string.IsNullOrEmpty(selectedPreset))
            {
                return string.Empty;
            }

            try
            {
                return presets[selectedCategory][selectedPreset];
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}