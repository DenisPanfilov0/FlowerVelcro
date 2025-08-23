using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements; 
using System.Collections.Generic;


namespace UAI
{ 
    public class NameGeneratorWindow : EditorWindow
    {
        #region Constants
        private string SYSTEM_INIT_PROMPT = "You are a creative writer, who can generate random names for characters, cities, and other things. You answer only with the names. You always answer in " + GPTClient.language;
        private const float PROGRESS_BAR_ANIMATION_SPEED = 0.5f;
        #endregion

        #region UIElements
        
        private VisualElement _rootElement;
        private VisualElement _headerbar;
        private VisualElement _mainLayout;
        private VisualElement _sidebar;
        private VisualElement _mainContent;
        private VisualElement _activePanel;
        private VisualElement _responsePanel;
        
        
        private Button _characterTabButton;
        private Button _cityTabButton;
        private Button _customTabButton; 
        
        
        private VisualElement _characterPanel;
        private VisualElement _cityPanel;
        private VisualElement _customPanel;
        
        
        private TextField _gameWorldCharacterField;
        private TextField _gameWorldCityField;
        private TextField _gameWorldCustomField;
        private SliderInt _numberOfNamesSliderCharacter;
        private SliderInt _numberOfNamesSliderCity;
        private SliderInt _numberOfNamesSliderCustom;
        private TextField _characterInfoField;
        private TextField _cityInfoField;
        private TextField _customInfoField;
        
        
        private Button _characterButton;
        private Button _cityButton;
        private Button _customButton;
        private Button _cancelButton;
        
        
        private ScrollView _responseScrollView;
        private VisualElement _progressBar;
        private Label _progressLabel;
        #endregion

        #region Private Fields
        private string _apiResponse = "";
        private string _gameWorldDescription = "My game world is a fantasy world with magic, dragons, elves, dwarves, and orcs.";
        private string _extraCharacterInfo = "The character is an elf.";
        private string _extraCityInfo = "It is a city where elves live.";
        private string _extraCustomInfo = "I am searching for a name for my pet.";
        private int _numberOfNames = 1;
        private bool _isGenerating = false;
        private float _progressValue = 0f;
        private NameGeneratorMode _currentMode = NameGeneratorMode.Character;
        #endregion

        #region Enums
        private enum NameGeneratorMode
        {
            Character,
            City,
            Custom
        }
        #endregion

        #region Menu Item
        [MenuItem("Tools/uAI Creator/Name Generator", false, 100)]
        public static void Init()
        {
            NameGeneratorWindow window = (NameGeneratorWindow)GetWindow(typeof(NameGeneratorWindow), false, "Name Generator");
            window.minSize = new Vector2(750, 600);
            window.Show();
        }
        #endregion

        #region Unity Lifecycle Methods
        private void CreateGUI()
        {
            
            _rootElement = rootVisualElement;
            
            
            LoadSavedData();
            
            
            BuildUIStructure();
            
            
            SetupEventHandlers();
            
            
            UpdateUIState();
            
            
            EditorApplication.update += UpdateProgress;
        }
        
        private void OnDisable()
        {
            
            EditorApplication.update -= UpdateProgress;
            
            
            SaveUserData();
        }
        #endregion

        #region UI Building
        private void BuildUIStructure()
        {
            
            _rootElement.Clear();
            
            
            _mainLayout = new VisualElement();
            _mainLayout.style.flexDirection = FlexDirection.Row;
            _mainLayout.style.flexGrow = 1;
            _rootElement.Add(_mainLayout);

            
            _headerbar = new VisualElement();
            _headerbar.style.flexGrow = 1;
            _headerbar.style.flexDirection = FlexDirection.Row;
            _headerbar.style.height = 50;
            _headerbar.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            
            _headerbar.style.display = DisplayStyle.None;
            _mainLayout.Add(_headerbar);
            
            
            CreateSidebar();
            
            
            _mainContent = new VisualElement();
            _mainContent.style.flexGrow = 1;
            _mainContent.style.flexDirection = FlexDirection.Column;
            _mainContent.style.paddingLeft = 15;
            _mainContent.style.paddingRight = 15;
            _mainContent.style.paddingTop = 15;
            _mainContent.style.paddingBottom = 15;
            _mainLayout.Add(_mainContent);
            
            
            CreateCharacterPanel();
            CreateCityPanel();
            CreateCustomPanel();
            
            
            CreateResponseSection();
        }
        
        private void CreateSidebar()
        {
            _sidebar = new VisualElement();
            _sidebar.style.width = 150;
            _sidebar.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            _mainLayout.Add(_sidebar);
            
            
            var logoArea = new VisualElement();
            logoArea.style.height = 50;
            logoArea.style.minHeight = 30;
            logoArea.style.alignItems = Align.Center;
            logoArea.style.justifyContent = Justify.Center;
            logoArea.style.marginTop = 10;
            logoArea.style.marginBottom = 10;
            _sidebar.Add(logoArea);
            
            var logoLabel = new Label("UAI");
            logoLabel.style.fontSize = 24;
            logoLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            logoLabel.style.color = new Color(0.35f, 0.65f, 0.9f);
            logoArea.Add(logoLabel); 
            
            
            _characterTabButton = new Button(() => SwitchMode(NameGeneratorMode.Character));
            _characterTabButton.text = "Character Names";
            _characterTabButton.AddToClassList("sidebar-button");
            _sidebar.Add(_characterTabButton);
            
            Button headerCharacterButton = new Button(() => SwitchMode(NameGeneratorMode.Character));
            headerCharacterButton.text = "Character Names";
            headerCharacterButton.AddToClassList("sidebar-button"); 
            _headerbar.Add(headerCharacterButton);
            
            _cityTabButton = new Button(() => SwitchMode(NameGeneratorMode.City));
            _cityTabButton.text = "City Names";
            _cityTabButton.AddToClassList("sidebar-button");
            _sidebar.Add(_cityTabButton);
            
            Button headerCityButton = new Button(() => SwitchMode(NameGeneratorMode.City));
            headerCityButton.text = "City Names";
            headerCityButton.AddToClassList("sidebar-button"); 
            _headerbar.Add(headerCityButton);
            
            _customTabButton = new Button(() => SwitchMode(NameGeneratorMode.Custom));
            _customTabButton.text = "Custom Names";
            _customTabButton.AddToClassList("sidebar-button");
            _sidebar.Add(_customTabButton);
            
            Button headerCustomButton = new Button(() => SwitchMode(NameGeneratorMode.Custom));
            headerCustomButton.text = "Custom Names";
            headerCustomButton.AddToClassList("sidebar-button");

            _headerbar.Add(headerCustomButton); 
 
            
            EditorHelper.drawSidebarButtons(_sidebar);
            
            
            
            _rootElement.Query<Button>().Class("sidebar-button").ForEach((button) => {
                button.style.height = 40;
                button.style.marginTop = 5;
                button.style.marginBottom = 5;
                button.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
                button.style.color = Color.white;
                button.style.borderTopWidth = 0;
                button.style.borderBottomWidth = 0;
                button.style.borderLeftWidth = 0;
                button.style.borderRightWidth = 0;
                
                
                button.RegisterCallback<MouseEnterEvent>(evt => {
                    button.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
                });
                
                button.RegisterCallback<MouseLeaveEvent>(evt => {
                    if (IsActiveButton(button)) {
                        button.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
                    } else {
                        button.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
                    }
                });
            });
        }
         
        private void CreateCharacterPanel()
        {
            _characterPanel = new VisualElement();
            _characterPanel.style.display = DisplayStyle.None;
            _mainContent.Add(_characterPanel);
            
            
            var scrollView = new ScrollView();
            scrollView.style.flexGrow = 1;
            _characterPanel.Add(scrollView);
            
            
            var header = new Label("Character Name Generator");
            header.style.fontSize = 18;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.marginBottom = 15;
            scrollView.Add(header);
            elementsToHideOnDock.Add(header);
            
            
            var contentContainer = new VisualElement();
            contentContainer.style.flexDirection = FlexDirection.Row;
            contentContainer.style.flexGrow = 1;
            scrollView.Add(contentContainer);
            
            
            worldCharacterContainer = new Box();
            worldCharacterContainer.style.flexGrow = 1;
            worldCharacterContainer.style.marginRight = 10;
            worldCharacterContainer.style.paddingTop = 10;
            worldCharacterContainer.style.paddingBottom = 10;
            worldCharacterContainer.style.paddingLeft = 10;
            worldCharacterContainer.style.paddingRight = 10;
            contentContainer.Add(worldCharacterContainer);
            
            var worldLabel = new Label("World Description");
            worldLabel.tooltip = "This description helps the AI understand the context of your game world.";
            worldLabel.style.fontSize = 14;
            worldLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            worldLabel.style.marginBottom = 5;
            worldCharacterContainer.Add(worldLabel);
            
            var worldDescription = new Label("This description helps the AI understand the context of your game world.");
            worldDescription.style.fontSize = 12;
            worldDescription.style.marginBottom = 8;
            worldDescription.style.whiteSpace = WhiteSpace.Normal;
            worldCharacterContainer.Add(worldDescription);
            elementsToHideOnDock.Add(worldDescription);
            
            _gameWorldCharacterField = new TextField();
            _gameWorldCharacterField.multiline = true;
            _gameWorldCharacterField.value = _gameWorldDescription;
            _gameWorldCharacterField.style.height = 150;
            _gameWorldCharacterField.style.whiteSpace = WhiteSpace.Normal;
            _gameWorldCharacterField.style.unityTextAlign = TextAnchor.UpperLeft;
            _gameWorldCharacterField.style.marginBottom = 10;
            _gameWorldCharacterField.Q("unity-text-input").style.alignSelf = Align.Auto;
            worldCharacterContainer.Add(_gameWorldCharacterField);
            
            
            var sliderContainer = new VisualElement();
            sliderContainer.style.flexDirection = FlexDirection.Column;
            sliderContainer.style.alignItems = Align.FlexStart;
            worldCharacterContainer.Add(sliderContainer);
            
            var sliderLabel = new Label("Number of names:");
            sliderLabel.style.minWidth = 120;
            sliderContainer.Add(sliderLabel);
            
            var sliderWrapper = new VisualElement();
            sliderWrapper.style.flexGrow = 1;
            sliderWrapper.style.flexDirection = FlexDirection.Row;
            sliderWrapper.style.minWidth = 150;
            sliderContainer.Add(sliderWrapper);
            
            _numberOfNamesSliderCharacter = new SliderInt(1, 10);
            _numberOfNamesSliderCharacter.showInputField = true;
            _numberOfNamesSliderCharacter.value = _numberOfNames;
            _numberOfNamesSliderCharacter.style.flexGrow = 1;
            sliderWrapper.Add(_numberOfNamesSliderCharacter);
            
            
            infoCharacterContainer = new Box();
            infoCharacterContainer.style.flexGrow = 1;
            infoCharacterContainer.style.marginLeft = 10;
            infoCharacterContainer.style.paddingTop = 10;
            infoCharacterContainer.style.paddingBottom = 10;
            infoCharacterContainer.style.paddingLeft = 10;
            infoCharacterContainer.style.paddingRight = 10;
            contentContainer.Add(infoCharacterContainer);
            
            var infoLabel = new Label("Extra info for the character:");
            infoLabel.tooltip = "Add details about the character you want names for (race, profession, personality, etc.)";
            
            infoLabel.style.fontSize = 14;
            infoLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            infoLabel.style.marginBottom = 5;
            infoLabel.style.whiteSpace = WhiteSpace.Normal;
            infoCharacterContainer.Add(infoLabel);
            
            var helpText = new Label("Add details about the character you want names for (race, profession, personality, etc.)");
            helpText.style.fontSize = 12;
            helpText.style.marginBottom = 8;
            helpText.style.whiteSpace = WhiteSpace.Normal;
            infoCharacterContainer.Add(helpText);
            elementsToHideOnDock.Add(helpText);
            
            _characterInfoField = new TextField();
            _characterInfoField.multiline = true;
            _characterInfoField.value = _extraCharacterInfo;
            _characterInfoField.style.height = 150;
            _characterInfoField.style.whiteSpace = WhiteSpace.Normal;
            _characterInfoField.style.unityTextAlign = TextAnchor.UpperLeft;
            _characterInfoField.style.marginBottom = 10;
            _characterInfoField.Q("unity-text-input").style.alignSelf = Align.Auto;
            infoCharacterContainer.Add(_characterInfoField);
            
            
            var examplesTitle = new Label("Example character descriptions:");
            examplesTitle.style.fontSize = 12;
            examplesTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            examplesTitle.style.marginTop = 5;
            examplesTitle.style.whiteSpace = WhiteSpace.Normal;
            infoCharacterContainer.Add(examplesTitle);
            elementsToHideOnDock.Add(examplesTitle);
            
            var examples = new[] {
                "A noble elf warrior who leads the forest kingdom's armies",
                "A grumpy dwarf blacksmith who crafts magical weapons"
            };

            foreach (var example in examples)
            {
                var exampleLabel = new Label("• " + example);
                exampleLabel.style.fontSize = 11;
                exampleLabel.style.marginTop = 2;
                exampleLabel.style.whiteSpace = WhiteSpace.Normal;
                infoCharacterContainer.Add(exampleLabel);
                elementsToHideOnDock.Add(exampleLabel);
            }
            
            
            var buttonContainer = new VisualElement();
            buttonContainer.style.alignItems = Align.Center;
            buttonContainer.style.justifyContent = Justify.Center;
            buttonContainer.style.marginTop = 20;
            buttonContainer.style.marginBottom = 10;
            scrollView.Add(buttonContainer);
            
            _characterButton = new Button();
            _characterButton.text = "Generate Character Names";
            _characterButton.AddToClassList("primary-button");
            buttonContainer.Add(_characterButton);
            
            
            _characterButton.style.height = 40;
            _characterButton.style.width = 250;
            _characterButton.style.backgroundColor = new Color(0.35f, 0.65f, 0.9f);
            _characterButton.style.color = Color.white;
            _characterButton.style.fontSize = 14;
            _characterButton.style.unityFontStyleAndWeight = FontStyle.Bold;
            _characterButton.style.borderTopWidth = 0;
            _characterButton.style.borderBottomWidth = 0;
            _characterButton.style.borderLeftWidth = 0;
            _characterButton.style.borderRightWidth = 0;
            
            
            _characterButton.RegisterCallback<MouseEnterEvent>(evt => {
                _characterButton.style.backgroundColor = new Color(0.45f, 0.75f, 1f);
            });
            
            _characterButton.RegisterCallback<MouseLeaveEvent>(evt => {
                _characterButton.style.backgroundColor = new Color(0.35f, 0.65f, 0.9f);
            });
        }
        
        private void CreateCityPanel()
        {
            _cityPanel = new VisualElement();
            _cityPanel.style.display = DisplayStyle.None;
            _mainContent.Add(_cityPanel);
            
            
            var scrollView = new ScrollView();
            scrollView.style.flexGrow = 1;
            _cityPanel.Add(scrollView);
            
            
            var header = new Label("City Name Generator");
            header.style.fontSize = 18;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.marginBottom = 15;
            scrollView.Add(header);
            elementsToHideOnDock.Add(header);
            
            
            var contentContainer = new VisualElement();
            contentContainer.style.flexDirection = FlexDirection.Row;
            contentContainer.style.flexGrow = 1;
            scrollView.Add(contentContainer);
            
            
            worldCityContainer = new Box();
            worldCityContainer.style.flexGrow = 1;
            worldCityContainer.style.marginRight = 10;
            worldCityContainer.style.paddingTop = 10;
            worldCityContainer.style.paddingBottom = 10;
            worldCityContainer.style.paddingLeft = 10;
            worldCityContainer.style.paddingRight = 10;
            contentContainer.Add(worldCityContainer);
            
            var worldLabel = new Label("World Description");
            worldLabel.tooltip = "This description helps the AI understand the context of your game world.";
            worldLabel.style.fontSize = 14;
            worldLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            worldLabel.style.marginBottom = 5;
            worldCityContainer.Add(worldLabel);
            
            var worldDescription = new Label("This description helps the AI understand the context of your game world.");
            worldDescription.style.fontSize = 12;
            worldDescription.style.marginBottom = 8;
            worldDescription.style.whiteSpace = WhiteSpace.Normal;
            worldCityContainer.Add(worldDescription);
            elementsToHideOnDock.Add(worldDescription);
            
            _gameWorldCityField = new TextField();
            _gameWorldCityField.multiline = true;
            _gameWorldCityField.value = _gameWorldDescription;
            _gameWorldCityField.style.height = 150;
            _gameWorldCityField.style.whiteSpace = WhiteSpace.Normal;
            _gameWorldCityField.style.unityTextAlign = TextAnchor.UpperLeft;
            _gameWorldCityField.style.marginBottom = 10;
            _gameWorldCityField.Q("unity-text-input").style.alignSelf = Align.Auto;
            worldCityContainer.Add(_gameWorldCityField);
            
            
            var sliderContainer = new VisualElement();
            sliderContainer.style.flexDirection = FlexDirection.Column;
            sliderContainer.style.alignItems = Align.FlexStart;
            worldCityContainer.Add(sliderContainer);
            
            var sliderLabel = new Label("Number of names:");
            sliderLabel.style.minWidth = 120;
            sliderContainer.Add(sliderLabel);
            
            var sliderWrapper = new VisualElement();
            sliderWrapper.style.flexGrow = 1;
            sliderWrapper.style.flexDirection = FlexDirection.Column;
            sliderWrapper.style.minWidth = 150;
            sliderContainer.Add(sliderWrapper);
            
            _numberOfNamesSliderCity = new SliderInt(1, 10);
            _numberOfNamesSliderCity.showInputField = true;
            _numberOfNamesSliderCity.value = _numberOfNames;
            _numberOfNamesSliderCity.style.flexGrow = 1;
            sliderWrapper.Add(_numberOfNamesSliderCity);
            
            
            infoCityContainer = new Box();
            infoCityContainer.style.flexGrow = 1;
            infoCityContainer.style.marginLeft = 10;
            infoCityContainer.style.paddingTop = 10;
            infoCityContainer.style.paddingBottom = 10;
            infoCityContainer.style.paddingLeft = 10;
            infoCityContainer.style.paddingRight = 10;
            contentContainer.Add(infoCityContainer);
            
            var infoLabel = new Label("Extra info for the city:");
            infoLabel.tooltip = "Add details about the city you want names for (location, inhabitants, purpose, size, climate, etc.)";
            infoLabel.style.fontSize = 14;
            infoLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            infoLabel.style.marginBottom = 5;
            infoCityContainer.Add(infoLabel);
            
            var helpText = new Label("Add details about the city you want names for (location, inhabitants, purpose, size, climate, etc.)");
            helpText.style.fontSize = 12;
            helpText.style.marginBottom = 8;
            helpText.style.whiteSpace = WhiteSpace.Normal;
            infoCityContainer.Add(helpText);
            elementsToHideOnDock.Add(helpText);
            
            _cityInfoField = new TextField();
            _cityInfoField.multiline = true;
            _cityInfoField.value = _extraCityInfo;
            _cityInfoField.style.height = 150;
            _cityInfoField.style.whiteSpace = WhiteSpace.Normal;
            _cityInfoField.style.unityTextAlign = TextAnchor.UpperLeft;
            _cityInfoField.style.marginBottom = 10;
            _cityInfoField.Q("unity-text-input").style.alignSelf = Align.Auto;
            infoCityContainer.Add(_cityInfoField);
            
            
            var examplesTitle = new Label("Example city descriptions:");
            examplesTitle.style.fontSize = 12;
            examplesTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            examplesTitle.style.marginTop = 5;
            infoCityContainer.Add(examplesTitle);
            elementsToHideOnDock.Add(examplesTitle);
            
            var examples = new[] {
                "A coastal trading port with diverse merchants",
                "A dwarven mining city carved into a mountain"
            };

            foreach (var example in examples)
            {
                var exampleLabel = new Label("• " + example);
                exampleLabel.style.fontSize = 11;
                exampleLabel.style.marginTop = 2;
                exampleLabel.style.whiteSpace = WhiteSpace.Normal;
                infoCityContainer.Add(exampleLabel);
                elementsToHideOnDock.Add(exampleLabel);
            }
            
            
            var buttonContainer = new VisualElement();
            buttonContainer.style.alignItems = Align.Center;
            buttonContainer.style.justifyContent = Justify.Center;
            buttonContainer.style.marginTop = 20;
            buttonContainer.style.marginBottom = 10;
            scrollView.Add(buttonContainer);
            
            _cityButton = new Button();
            _cityButton.text = "Generate City Names";
            _cityButton.AddToClassList("primary-button");
            buttonContainer.Add(_cityButton);
            
            
            _cityButton.style.height = 40;
            _cityButton.style.width = 250;
            _cityButton.style.backgroundColor = new Color(0.35f, 0.65f, 0.9f);
            _cityButton.style.color = Color.white;
            _cityButton.style.fontSize = 14;
            _cityButton.style.unityFontStyleAndWeight = FontStyle.Bold;
            _cityButton.style.borderTopWidth = 0;
            _cityButton.style.borderBottomWidth = 0;
            _cityButton.style.borderLeftWidth = 0;
            _cityButton.style.borderRightWidth = 0;
            
            
            _cityButton.RegisterCallback<MouseEnterEvent>(evt => {
                _cityButton.style.backgroundColor = new Color(0.45f, 0.75f, 1f);
            });
            
            _cityButton.RegisterCallback<MouseLeaveEvent>(evt => {
                _cityButton.style.backgroundColor = new Color(0.35f, 0.65f, 0.9f);
            });
        }
        
        private void CreateCustomPanel()
        {
            _customPanel = new VisualElement();
            _customPanel.style.display = DisplayStyle.None;
            _mainContent.Add(_customPanel);
            
            
            var scrollView = new ScrollView();
            scrollView.style.flexGrow = 1;
            _customPanel.Add(scrollView);
            
            
            var header = new Label("Custom Name Generator");
            header.style.fontSize = 18;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.marginBottom = 15;
            scrollView.Add(header);
            elementsToHideOnDock.Add(header);
            
            
            var contentContainer = new VisualElement();
            contentContainer.style.flexDirection = FlexDirection.Row;
            contentContainer.style.flexGrow = 1;
            scrollView.Add(contentContainer);
            
            
            worldCustomContainer = new Box();
            worldCustomContainer.style.flexGrow = 1;
            worldCustomContainer.style.marginRight = 10;
            worldCustomContainer.style.paddingTop = 10;
            worldCustomContainer.style.paddingBottom = 10;
            worldCustomContainer.style.paddingLeft = 10;
            worldCustomContainer.style.paddingRight = 10;
            contentContainer.Add(worldCustomContainer);
            
            var worldLabel = new Label("World Description");
            worldLabel.tooltip = "This description helps the AI understand the context of your game world.";
            worldLabel.style.fontSize = 14;
            worldLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            worldLabel.style.marginBottom = 5;
            worldCustomContainer.Add(worldLabel);
            
            var worldDescription = new Label("This description helps the AI understand the context of your game world.");
            worldDescription.style.fontSize = 12;
            worldDescription.style.marginBottom = 8;
            worldDescription.style.whiteSpace = WhiteSpace.Normal;
            worldCustomContainer.Add(worldDescription);
            elementsToHideOnDock.Add(worldDescription);
            
            _gameWorldCustomField = new TextField();
            _gameWorldCustomField.multiline = true;
            _gameWorldCustomField.value = _gameWorldDescription;
            _gameWorldCustomField.style.height = 150;
            _gameWorldCustomField.style.whiteSpace = WhiteSpace.Normal;
            _gameWorldCustomField.style.unityTextAlign = TextAnchor.UpperLeft;
            _gameWorldCustomField.style.marginBottom = 10;
            _gameWorldCustomField.Q("unity-text-input").style.alignSelf = Align.Auto;
            worldCustomContainer.Add(_gameWorldCustomField);
            
            
            var sliderContainer = new VisualElement();
            sliderContainer.style.flexDirection = FlexDirection.Column;
            sliderContainer.style.alignItems = Align.FlexStart;
            worldCustomContainer.Add(sliderContainer);
            
            var sliderLabel = new Label("Number of names:");
            sliderLabel.style.minWidth = 120;
            sliderContainer.Add(sliderLabel);
            
            var sliderWrapper = new VisualElement();
            sliderWrapper.style.flexGrow = 1;
            sliderWrapper.style.flexDirection = FlexDirection.Column;
            sliderWrapper.style.minWidth = 150;
            sliderContainer.Add(sliderWrapper);
            
            _numberOfNamesSliderCustom = new SliderInt(1, 10);
            _numberOfNamesSliderCustom.showInputField = true;
            _numberOfNamesSliderCustom.value = _numberOfNames;
            _numberOfNamesSliderCustom.style.flexGrow = 1;
            sliderWrapper.Add(_numberOfNamesSliderCustom);
            
            
            infoCustomContainer = new Box();
            infoCustomContainer.style.flexGrow = 1;
            infoCustomContainer.style.marginLeft = 10;
            infoCustomContainer.style.paddingTop = 10;
            infoCustomContainer.style.paddingBottom = 10;
            infoCustomContainer.style.paddingLeft = 10;
            infoCustomContainer.style.paddingRight = 10;
            contentContainer.Add(infoCustomContainer);
            
            var infoLabel = new Label("Extra info for your custom name:");
            infoLabel.tooltip = "Describe what you need names for (items, spells, weapons, creatures, etc.)";
            infoLabel.style.fontSize = 14;
            infoLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            infoLabel.style.marginBottom = 5;
            infoCustomContainer.Add(infoLabel);
            
            var helpText = new Label("Describe what you need names for (items, spells, weapons, creatures, etc.)");
            helpText.style.fontSize = 12;
            helpText.style.marginBottom = 8;
            helpText.style.whiteSpace = WhiteSpace.Normal;
            infoCustomContainer.Add(helpText);
            elementsToHideOnDock.Add(helpText);
            
            _customInfoField = new TextField();
            _customInfoField.multiline = true;
            _customInfoField.value = _extraCustomInfo;
            _customInfoField.style.height = 150;
            _customInfoField.style.whiteSpace = WhiteSpace.Normal;
            _customInfoField.style.unityTextAlign = TextAnchor.UpperLeft;
            _customInfoField.style.marginBottom = 10;
            _customInfoField.Q("unity-text-input").style.alignSelf = Align.Auto;
            infoCustomContainer.Add(_customInfoField);
            
            
            var examplesTitle = new Label("Example custom descriptions:");
            examplesTitle.style.fontSize = 12;
            examplesTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            examplesTitle.style.marginTop = 5;
            infoCustomContainer.Add(examplesTitle);
            elementsToHideOnDock.Add(examplesTitle);
            
            var examples = new[] {
                "Powerful magical weapons with elemental energy",
                "Ancient tomes containing forbidden knowledge"
            };

            foreach (var example in examples)
            {
                var exampleLabel = new Label("• " + example);
                exampleLabel.style.fontSize = 11;
                exampleLabel.style.marginTop = 2;
                exampleLabel.style.whiteSpace = WhiteSpace.Normal;
                infoCustomContainer.Add(exampleLabel);
                elementsToHideOnDock.Add(exampleLabel);
            }
            
            
            var buttonContainer = new VisualElement();
            buttonContainer.style.alignItems = Align.Center;
            buttonContainer.style.justifyContent = Justify.Center;
            buttonContainer.style.marginTop = 20;
            buttonContainer.style.marginBottom = 10;
            scrollView.Add(buttonContainer);
            
            _customButton = new Button();
            _customButton.text = "Generate Custom Names";
            _customButton.AddToClassList("primary-button");
            buttonContainer.Add(_customButton);
            
            
            _customButton.style.height = 40;
            _customButton.style.width = 250;
            _customButton.style.backgroundColor = new Color(0.35f, 0.65f, 0.9f);
            _customButton.style.color = Color.white;
            _customButton.style.fontSize = 14;
            _customButton.style.unityFontStyleAndWeight = FontStyle.Bold;
            _customButton.style.borderTopWidth = 0;
            _customButton.style.borderBottomWidth = 0;
            _customButton.style.borderLeftWidth = 0;
            _customButton.style.borderRightWidth = 0;
            
            
            _customButton.RegisterCallback<MouseEnterEvent>(evt => {
                _customButton.style.backgroundColor = new Color(0.45f, 0.75f, 1f);
            });
            
            _customButton.RegisterCallback<MouseLeaveEvent>(evt => {
                _customButton.style.backgroundColor = new Color(0.35f, 0.65f, 0.9f);
            });
        }
        
        private void CreateResponseSection()
        {
            _responsePanel = new VisualElement();
            _responsePanel.style.display = string.IsNullOrEmpty(_apiResponse) ? DisplayStyle.None : DisplayStyle.Flex;
            _responsePanel.style.borderTopWidth = 1;
            _responsePanel.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f);
            _responsePanel.style.paddingTop = 15;
            _responsePanel.style.marginTop = 15;
            _mainContent.Add(_responsePanel);
            
            
            var progressContainer = new VisualElement();
            progressContainer.name = "progress-container";
            progressContainer.style.flexDirection = FlexDirection.Row;
            progressContainer.style.alignItems = Align.Center;
            progressContainer.style.display = DisplayStyle.None;
            progressContainer.style.marginBottom = 10;
            _responsePanel.Add(progressContainer);
            
            _progressLabel = new Label("Generating names...");
            _progressLabel.style.marginRight = 10;
            progressContainer.Add(_progressLabel);
            
            _progressBar = new VisualElement();
            _progressBar.style.flexGrow = 1;
            _progressBar.style.height = 10;
            _progressBar.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
            progressContainer.Add(_progressBar);
            
            var progressFill = new VisualElement();
            progressFill.name = "progress-fill";
            progressFill.style.width = 0;
            progressFill.style.height = 10;
            progressFill.style.backgroundColor = new Color(0.35f, 0.65f, 0.9f);
            _progressBar.Add(progressFill);
            
            _cancelButton = new Button(CancelGeneration);
            _cancelButton.text = "Cancel";
            _cancelButton.style.marginLeft = 10;
            _cancelButton.style.width = 80;
            progressContainer.Add(_cancelButton);
            
            
            var responseHeader = new VisualElement();
            responseHeader.style.flexDirection = FlexDirection.Row;
            responseHeader.style.justifyContent = Justify.SpaceBetween;
            responseHeader.style.marginBottom = 10;
            _responsePanel.Add(responseHeader);
            
            
            var responseTitle = new Label("Generated Names");
            responseTitle.style.fontSize = 16;
            responseTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            responseHeader.Add(responseTitle);
            
            
            var copyButton = new Button(() => CopyToClipboard(_apiResponse));
            copyButton.text = "Copy All";
            copyButton.style.width = 80;
            copyButton.style.height = 25;
            responseHeader.Add(copyButton);

            var closeButton = new Button(() => {
                _responsePanel.style.display = DisplayStyle.None;
                _apiResponse = "";
            });
            closeButton.text = "Close";
            closeButton.style.width = 80;
            closeButton.style.height = 25;
            responseHeader.Add(closeButton);
            
            
            _responseScrollView = new ScrollView();
            _responseScrollView.style.height = 180;
            _responseScrollView.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f);
            _responseScrollView.style.borderTopWidth = 1;
            _responseScrollView.style.borderBottomWidth = 1;
            _responseScrollView.style.borderLeftWidth = 1;
            _responseScrollView.style.borderRightWidth = 1;
            _responseScrollView.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f);
            _responseScrollView.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
            _responseScrollView.style.borderLeftColor = new Color(0.3f, 0.3f, 0.3f);
            _responseScrollView.style.borderRightColor = new Color(0.3f, 0.3f, 0.3f);
            _responsePanel.Add(_responseScrollView);
            
            if (!string.IsNullOrEmpty(_apiResponse))
            {
                AddResponseText(_apiResponse);
            }
        }
        #endregion

        #region Event Handlers
        private void SetupEventHandlers()
        {
            
            if (_gameWorldCharacterField != null)
                _gameWorldCharacterField.RegisterValueChangedCallback(evt => _gameWorldDescription = evt.newValue);
                
            
            if (_numberOfNamesSliderCharacter != null)
                _numberOfNamesSliderCharacter.RegisterValueChangedCallback(evt => _numberOfNames = evt.newValue);

            if (_numberOfNamesSliderCity != null)
                _numberOfNamesSliderCity.RegisterValueChangedCallback(evt => _numberOfNames = evt.newValue);
            if (_numberOfNamesSliderCustom != null)
                _numberOfNamesSliderCustom.RegisterValueChangedCallback(evt => _numberOfNames = evt.newValue);
            
                
            
            if (_characterInfoField != null)
                    _characterInfoField.RegisterValueChangedCallback(evt => _extraCharacterInfo = evt.newValue);
                
            
            if (_cityInfoField != null)
                _cityInfoField.RegisterValueChangedCallback(evt => _extraCityInfo = evt.newValue);
                
            
            if (_customInfoField != null)
                _customInfoField.RegisterValueChangedCallback(evt => _extraCustomInfo = evt.newValue);
                
            
            if (_characterButton != null)
                _characterButton.clicked += () => SendRequestToGPT(BuildPrompt("character", _extraCharacterInfo));
                
            if (_cityButton != null)
                _cityButton.clicked += () => SendRequestToGPT(BuildPrompt("city", _extraCityInfo));
                
            if (_customButton != null)
                _customButton.clicked += () => SendRequestToGPT(BuildPrompt("custom", _extraCustomInfo));
                 
        }
         
        #endregion

        #region Helper Methods
        private void LoadSavedData()
        {
            
            _gameWorldDescription = PlayerPrefs.GetString("gameWorldDescription_uAI", _gameWorldDescription);
            _extraCharacterInfo = PlayerPrefs.GetString("extraCharacterInfo_uAI", _extraCharacterInfo);
            _extraCityInfo = PlayerPrefs.GetString("extraCityInfo_uAI", _extraCityInfo);
            _extraCustomInfo = PlayerPrefs.GetString("extraCustomInfo_uAI", _extraCustomInfo);
            _numberOfNames = PlayerPrefs.GetInt("numberOfNames_uAI", _numberOfNames);
        }
        
        private void SaveUserData()
        {
            
            PlayerPrefs.SetString("gameWorldDescription_uAI", _gameWorldDescription);
            PlayerPrefs.SetString("extraCharacterInfo_uAI", _extraCharacterInfo);
            PlayerPrefs.SetString("extraCityInfo_uAI", _extraCityInfo);
            PlayerPrefs.SetString("extraCustomInfo_uAI", _extraCustomInfo);
            PlayerPrefs.SetInt("numberOfNames_uAI", _numberOfNames);
        }
        
        private string BuildPrompt(string type, string extraInfo)
        {
            string prompt = _gameWorldDescription;
            
            switch (type)
            {
                case "character":
                    prompt += $" - Generate {_numberOfNames} random character name{(_numberOfNames > 1 ? "s" : "")}. {extraInfo}";
                    break;
                    
                case "city":
                    prompt += $" - Generate {_numberOfNames} random city name{(_numberOfNames > 1 ? "s" : "")}. {extraInfo}";
                    break;
                    
                case "custom":
                    prompt += $" - {extraInfo} - Generate {_numberOfNames} random name{(_numberOfNames > 1 ? "s" : "")}.";
                    break;
            }
            
            // Debug.Log($"[NameGenerator] Sending prompt: {prompt}");
            return prompt;
        }
        
        private void SendRequestToGPT(string prompt)
        {
            if (_isGenerating) return;
            
            _apiResponse = "";
            _isGenerating = true;
            _progressValue = 0f;
            
            ShowProgressBar();
            ShowResponsePanel();
            
            GPTClient.Instance.SystemInitPrompt = SYSTEM_INIT_PROMPT;
            
            GPTClient.Instance.OnResponseReceived = null;
            GPTClient.Instance.OnResponseReceived += (response, index) =>
            {
                _apiResponse = response;
                _isGenerating = false;
                
                HideProgressBar();
                UpdateResponsePanel(); 
            };
            
            GPTClient.Instance.OnPartResponseReceived = null;
            GPTClient.Instance.OnPartResponseReceived += (response) =>
            {
                _apiResponse += response;
                
                EditorApplication.delayCall += () => {
                    UpdateResponsePanel();
                };
            };
            
            GPTClient.Instance.SendRequest(prompt);
        }
         
        
        private void CancelGeneration()
        {
            if (!_isGenerating) return;
            
            GPTClient.StopGeneration();
            _isGenerating = false;
            HideProgressBar();
        }
        
        private bool? lastDockedState = null;
        private List<VisualElement> elementsToHideOnDock = new List<VisualElement>();
        private Box worldCharacterContainer;
        private Box infoCharacterContainer;
        

        private Box worldCityContainer;
        private Box infoCityContainer;
        private Box worldCustomContainer;
        private Box infoCustomContainer;

        private void dockingStateChanged()
        {
            
            if (lastDockedState == true)
            {
                _sidebar.style.display = DisplayStyle.None;
                _headerbar.style.display = DisplayStyle.Flex;
                _mainLayout.style.flexDirection = FlexDirection.Column;
                _mainContent.style.flexGrow = 1;
                _gameWorldCharacterField.style.height = 60;
                _gameWorldCityField.style.height = 60;
                _gameWorldCustomField.style.height = 60;
                _cityInfoField.style.height = 60;
                _characterInfoField.style.height = 60;
                _customInfoField.style.height = 60;


                
                worldCharacterContainer.style.width = new Length(50, LengthUnit.Percent);
                infoCharacterContainer.style.width = new Length(50, LengthUnit.Percent);
                worldCityContainer.style.width = new Length(50, LengthUnit.Percent);
                infoCityContainer.style.width = new Length(50, LengthUnit.Percent);
                worldCustomContainer.style.width = new Length(50, LengthUnit.Percent);
                infoCustomContainer.style.width = new Length(50, LengthUnit.Percent);
            }
            else
            {
                _sidebar.style.display = DisplayStyle.Flex;
                _headerbar.style.display = DisplayStyle.None;
                _mainLayout.style.flexDirection = FlexDirection.Row;
                _mainContent.style.flexGrow = 1;
                _gameWorldCharacterField.style.height = 200;
                _gameWorldCityField.style.height = 200;
                _gameWorldCustomField.style.height = 200;
                _cityInfoField.style.height = 200;
                _characterInfoField.style.height = 200;
                _customInfoField.style.height = 200;
            }

            foreach (var element in elementsToHideOnDock)
            {
                element.style.display = lastDockedState == true ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }
        private void UpdateProgress()
        {
            if (lastDockedState == null || docked != lastDockedState)
            {
                lastDockedState = docked;
                dockingStateChanged();
            }
            if (!_isGenerating) return;
            
            _progressValue = Mathf.Repeat(_progressValue + (PROGRESS_BAR_ANIMATION_SPEED * 0.01f), 1f);
            
            
            if (_progressBar != null)
            {
                var progressFill = _progressBar.Q<VisualElement>("progress-fill");
                if (progressFill != null)
                {
                    progressFill.style.width = new Length(_progressValue * 100, LengthUnit.Percent);
                }
            }
        }
        
        private void ShowProgressBar()
        {
            var progressContainer = _responsePanel?.Q<VisualElement>("progress-container");
            if (progressContainer != null)
                progressContainer.style.display = DisplayStyle.Flex;
        }
        
        private void HideProgressBar()
        {
            var progressContainer = _responsePanel?.Q<VisualElement>("progress-container");
            if (progressContainer != null)
                progressContainer.style.display = DisplayStyle.None;
        }
        
        private void ShowResponsePanel()
        {
            if (_responsePanel != null)
                _responsePanel.style.display = DisplayStyle.Flex;
        }
        
        private void UpdateResponsePanel()
        {
            if (_responseScrollView == null) return;
            
            _responseScrollView.Clear();
            AddResponseText(_apiResponse);
        }
        
        private void AddResponseText(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            
            var responseText = new TextField();
            responseText.multiline = true;
            responseText.SetValueWithoutNotify(text);
            responseText.isReadOnly = true;
            responseText.style.whiteSpace = WhiteSpace.Normal;
            responseText.style.unityTextAlign = TextAnchor.UpperLeft;
            responseText.style.backgroundColor = Color.clear;
            responseText.style.borderTopWidth = 0;
            responseText.style.borderBottomWidth = 0;
            responseText.style.borderLeftWidth = 0;
            responseText.style.borderRightWidth = 0;
            
            
            var textInput = responseText.Q("unity-text-input");
            if (textInput != null)
            {
                textInput.style.alignSelf = Align.Auto;
                textInput.style.backgroundColor = Color.clear;
                textInput.style.borderTopWidth = 0;
                textInput.style.borderBottomWidth = 0;
                textInput.style.borderLeftWidth = 0;
                textInput.style.borderRightWidth = 0;
                textInput.style.color = new Color(0.9f, 0.9f, 0.9f);
            }
            
            _responseScrollView.Add(responseText);
        }

        private void CopyToClipboard(string text)
        {
            EditorGUIUtility.systemCopyBuffer = text;
            // Debug.Log("Copied to clipboard: " + text);
            ShowNotification(new GUIContent("Copied to clipboard"));
        }
         
        
        private void SwitchMode(NameGeneratorMode mode)
        {
            _currentMode = mode;
            UpdateUIState();
        }
        
        private void UpdateUIState()
        {
            
            if (_characterPanel != null)
                _characterPanel.style.display = DisplayStyle.None;
                
            if (_cityPanel != null)
                _cityPanel.style.display = DisplayStyle.None;
                
            if (_customPanel != null)
                _customPanel.style.display = DisplayStyle.None;
                
            
            switch (_currentMode)
            {
                case NameGeneratorMode.Character:
                    if (_characterPanel != null)
                    {
                        _characterPanel.style.display = DisplayStyle.Flex;
                        _activePanel = _characterPanel;
                    }
                    break;
                    
                case NameGeneratorMode.City:
                    if (_cityPanel != null)
                    {
                        _cityPanel.style.display = DisplayStyle.Flex;
                        _activePanel = _cityPanel;
                    }
                    break;
                    
                case NameGeneratorMode.Custom:
                    if (_customPanel != null)
                    {
                        _customPanel.style.display = DisplayStyle.Flex;
                        _activePanel = _customPanel;
                    }
                    break;
            }
            
            
            UpdateSidebarSelection();
        }
        
        private void UpdateSidebarSelection()
        {
            
            if (_characterTabButton != null)
                _characterTabButton.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
                
            if (_cityTabButton != null)
                _cityTabButton.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
                
            if (_customTabButton != null)
                _customTabButton.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
                
            
            switch (_currentMode)
            {
                case NameGeneratorMode.Character:
                    if (_characterTabButton != null)
                        _characterTabButton.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
                    break;
                    
                case NameGeneratorMode.City:
                    if (_cityTabButton != null)
                        _cityTabButton.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
                    break;
                    
                case NameGeneratorMode.Custom:
                    if (_customTabButton != null)
                        _customTabButton.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
                    break;
            }
        }
        
        private bool IsActiveButton(Button button)
        {
            if (button == _characterTabButton && _currentMode == NameGeneratorMode.Character)
                return true;
                
            if (button == _cityTabButton && _currentMode == NameGeneratorMode.City)
                return true;
                
            if (button == _customTabButton && _currentMode == NameGeneratorMode.Custom)
                return true;
                
            return false;
        }
        #endregion
    }
}