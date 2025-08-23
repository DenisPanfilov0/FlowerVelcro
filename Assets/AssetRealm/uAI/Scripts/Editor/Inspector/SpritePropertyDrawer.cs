using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace UAI
{

    [CustomPropertyDrawer(typeof(Sprite), useForChildren: true)]
    public class SpritePropertyDrawer : PropertyDrawer
    {
        const float k_ButtonWidth = 40f;
        const float k_Spacing = 2f;

        // Only one line high now
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            => EditorGUIUtility.singleLineHeight;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // 1) Draw the label and grab the remaining rect
            var contentRect = EditorGUI.PrefixLabel(position, label); 

            // 2) Split that rect into field + button
            var fieldRect = new Rect(
                contentRect.x,
                contentRect.y,
                100,// contentRect.width/2 - k_ButtonWidth - k_Spacing,
                contentRect.height
            );
            var buttonRect = new Rect(
                fieldRect.xMax + k_Spacing,
                contentRect.y,
                k_ButtonWidth,
                contentRect.height
            ); 
            // 3) Draw the Sprite slot (no label) and the AI button
            EditorGUI.ObjectField(fieldRect, property, GUIContent.none);
            if (GUI.Button(buttonRect, "AI"))
            {
                buttonClickedSprite(property, property.objectReferenceValue as Sprite);
            }

            EditorGUI.EndProperty();
        }

        // UIElements version, also forced onto one row
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var container = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems    = Align.Center,
                    flexGrow      = 1,
                    flexWrap      = Wrap.NoWrap,
                    //max 100%
                    maxWidth      = new StyleLength(new Length(100, LengthUnit.Percent)),
                }
            };

            // label
            var label = new Label(property.displayName)
            {
                style = { marginRight = k_Spacing }
            };
            container.Add(label);

            // ObjectField that takes up the rest
            var objField = new ObjectField()
            {
                objectType = typeof(Sprite),
                allowSceneObjects = false,
                bindingPath = property.propertyPath,
                style =
                {
                    flexGrow  = 1,
                    flexShrink    = 1,
                    marginRight = k_Spacing
                }
            };
            container.Add(objField);

            // fixed-width button
            var button = new Button()
            {
                text = "AI",
                style =
                {
                    width = k_ButtonWidth,
                    unityTextAlign = TextAnchor.MiddleCenter
                }
            };
            button.clicked += () => buttonClicked(property, objField);
            container.Add(button);

            return container;
        }

        private void buttonClickedSprite(SerializedProperty property, Sprite objField)
        {
            AssetCreatorWindow.ShowWindow();
            //check if sprite is assigned
            if (objField != null)
            {
                //get path of the sprite
                string path = AssetDatabase.GetAssetPath(objField);
                AssetCreatorWindow window = AssetCreatorWindow.GetWindow<AssetCreatorWindow>();
                window.AddReferenceImage(path);
                window.ApplyPrompt("Create a variation of this image I send you.");
                window.SelectBestFittingSizeByAspectRatio(objField.rect.width, objField.rect.height);

                window.Focus();
            }
        }

        
        private void buttonClicked(SerializedProperty property, ObjectField objField)
        {

            AssetCreatorWindow.ShowWindow();
            //check if sprite is assigned
            if (objField.value != null)
            {
                Sprite sprite = objField.value as Sprite;

                //get path of the sprite
                string path = AssetDatabase.GetAssetPath(objField.value);
                AssetCreatorWindow window = AssetCreatorWindow.GetWindow<AssetCreatorWindow>();
                window.AddReferenceImage(path);
                window.ApplyPrompt("Create a variation of this image I send you.");
                window.SelectBestFittingSizeByAspectRatio(sprite.rect.width, sprite.rect.height);

                window.Focus();
            }
        }
    }
    

}