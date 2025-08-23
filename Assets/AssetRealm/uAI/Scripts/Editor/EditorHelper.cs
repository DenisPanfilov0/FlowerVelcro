using System.Collections; 
using UnityEngine;
using UnityEngine.UIElements; 
using UnityEditor; 


namespace UAI{ 
    public class EditorHelper 
    {   
        public static void drawSidebarButtons(VisualElement _sidebar){
            
            // Add a separator line
            var separator = new VisualElement();
            separator.style.height = 1;
            separator.style.backgroundColor = new Color(0.4f, 0.4f, 0.4f);
            separator.style.marginTop = 10;
            separator.style.marginBottom = 10;
            _sidebar.Add(separator);

            //lets make a scrollview for the sidebar
            var scrollView = new ScrollView();
            scrollView.AddToClassList("sidebar-scrollview");
            _sidebar.Add(scrollView);

            //scriptCreatorButton
            var scriptCreatorButton = new Button(() => {
                ScriptCreatorWindow.Init();
            });
            scriptCreatorButton.text = "Script Creator";
            scriptCreatorButton.AddToClassList("sidebar-button");
            scriptCreatorButton.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            scrollView.Add(scriptCreatorButton);

            //scriptdocButton
            var scriptdocButton = new Button(() => {
                ScriptDocWindow.Init();
            });
            scriptdocButton.text = "Script Doc";
            scriptdocButton.AddToClassList("sidebar-button");
            scriptdocButton.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            scrollView.Add(scriptdocButton);


            //shaderCreatorButton
            var shaderCreatorButton = new Button(() => {
                ShaderCreatorWindow.Init();
            });
            shaderCreatorButton.text = "Shader Creator";
            shaderCreatorButton.AddToClassList("sidebar-button");
            shaderCreatorButton.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            scrollView.Add(shaderCreatorButton);

            //imageCreatorButton
            var imageCreatorButton = new Button(() => {
                ImageCreatorWindow.ShowWindow();
            });
            imageCreatorButton.text = "Image Creator";
            imageCreatorButton.AddToClassList("sidebar-button");
            imageCreatorButton.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            scrollView.Add(imageCreatorButton);


            var translatorButton = new Button(() => {
                TranslatorWindow.Init();
            });  
            translatorButton.text = "Translator";
            translatorButton.AddToClassList("sidebar-button");
            translatorButton.style.backgroundColor = new Color(0.35f, 0.65f, 0.9f);
            scrollView.Add(translatorButton);

            var textRefinerButton = new Button(() => {
                TextRefinerWindow.Init();
            });
            textRefinerButton.text = "Text Refiner";
            textRefinerButton.AddToClassList("sidebar-button");
            textRefinerButton.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f); 
            scrollView.Add(textRefinerButton);

            //spellcheckerButton
            var spellcheckerButton = new Button(() => {
                SpellCheckerWindow.Init();
            });
            spellcheckerButton.text = "Spell Checker";
            spellcheckerButton.AddToClassList("sidebar-button");
            spellcheckerButton.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            scrollView.Add(spellcheckerButton);

            //namegeneratorButton
            var nameGeneratorButton = new Button(() => {
                NameGeneratorWindow.Init();
            });
            nameGeneratorButton.text = "Name Generator";
            nameGeneratorButton.AddToClassList("sidebar-button");
            nameGeneratorButton.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            scrollView.Add(nameGeneratorButton);
            
            //questGeneratorButton
            var questGeneratorButton = new Button(() => {
                QuestGeneratorWindow.Init();
            });
            questGeneratorButton.text = "Quest Generator";
            questGeneratorButton.AddToClassList("sidebar-button");
            questGeneratorButton.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            scrollView.Add(questGeneratorButton);

            //senseiAIButton
            var senseiAIButton = new Button(() => {
                SenseiAIWindow.Init();
            });
            senseiAIButton.text = "Sensei AI";
            senseiAIButton.AddToClassList("sidebar-button");
            senseiAIButton.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            scrollView.Add(senseiAIButton);
            
            
            var separator2 = new VisualElement();
            separator2.style.height = 1;
            separator2.style.backgroundColor = new Color(0.4f, 0.4f, 0.4f);
            separator2.style.marginTop = 10;
            separator2.style.marginBottom = 10;
            _sidebar.Add(separator2);

            
            // Spacer
            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            _sidebar.Add(spacer); 
            
            // show the current model the user is using
            // var currentModel = new Label("Current Model:"); 
            // currentModel.style.color = new Color(0.8f, 0.8f, 0.8f);
            // currentModel.style.fontSize = 8;  
            // currentModel.style.marginLeft = 10;
            // _sidebar.Add(currentModel);

            // var currentModelName = new Label( GPTClient.Instance.model);
            // currentModelName.style.color = new Color(0.8f, 0.8f, 0.8f);
            // currentModelName.style.fontSize = 8; 
            // currentModelName.style.marginLeft = 10;
            // _sidebar.Add(currentModelName);

            // Settings button
            Button  _settingsButton = new Button();
            _settingsButton.text = "Settings";
            _settingsButton.AddToClassList("sidebar-button");
            _settingsButton.RegisterCallback<MouseUpEvent>(evt => { 
                SettingsWindow.ShowWindow();
            }); 
            _sidebar.Add(_settingsButton);
        }
    }
}