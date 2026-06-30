using NUnit.Framework.Constraints;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Windows;
using static UnityEditor.Progress;

namespace Farbod.DeveloperConsole
{
    [UxmlElement]
    public partial class DefaultDeveloperConsole : VisualElement
    {
        string m_StyleSheetResourcePath = "Style/RuntimeConsoleStyle";
        public static readonly string ussClassName = "developer-console";
        public static readonly string logViewUssClassName = "developer-console__log";
        public static readonly string logEntryWarningUssClassName = "developer-console__entry--warning";
        public static readonly string logEntryErrorUssClassName = "developer-console__entry--error";
        public static readonly string logEntryInputUssClassName = "developer-console__entry--input";

        public static readonly string inputColumnUssClassName = "developer-console__input-col";
        public static readonly string inputContainerUssClassName = "developer-console__input-field-container";
        public static readonly string inputUssClassName = "developer-console__input-field";
        public static readonly string submitButtonUssClassName = "developer-console__submit-button";
        public static readonly string inputAutocompletePopupUssClassName = "developer-console__autocomplete";

        [UxmlAttribute]
        public bool ShowStartupMessage { get; set; } = true;

        private ScrollView m_Log;
        private TextField m_Input;
        private Button m_SubmitButton;
        private VisualElement m_AutoCompletePopup;

        private bool m_AutocompletePopupActive = false;
        private string[] m_AutocompleteResult;
        public override VisualElement contentContainer => m_Log.contentContainer;
        public DefaultDeveloperConsole()
        {
            // Set up this visual element and its sub-elements, and register its callbacks.
            PopulateElement();
            RegisterCallbacks();
            DeveloperConsole.IndexCommandsIfNotIndexed();

            if (ShowStartupMessage)
            {
                Log("Welcome to the developer console. For info on all available commands, use 'help'");
            }
        }
        protected void PopulateElement()
        {
            this.AddToClassList(ussClassName);
            m_Log = new ScrollView();
            m_Log.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            m_Log.verticalScrollerVisibility = ScrollerVisibility.Auto;

            m_Log.AddToClassList(logViewUssClassName);
            hierarchy.Add(m_Log);

            VisualElement col = new VisualElement();
            col.AddToClassList(inputColumnUssClassName);
            hierarchy.Add(col);

            m_SubmitButton = new Button();
            m_SubmitButton.text = "Submit";
            m_SubmitButton.AddToClassList(submitButtonUssClassName);
            col.Add(m_SubmitButton);

            VisualElement inputContainer = new VisualElement();
            inputContainer.AddToClassList(inputContainerUssClassName);
            col.Add(inputContainer);

            m_Input = new TextField();
            m_Input.AddToClassList(inputUssClassName);
            inputContainer.Add(m_Input);

            m_AutoCompletePopup = new VisualElement();
            m_AutoCompletePopup.AddToClassList(inputAutocompletePopupUssClassName);
            inputContainer.Add(m_AutoCompletePopup);
            HideAutoCompletePopup();


            StyleSheet style = Resources.Load<StyleSheet>(m_StyleSheetResourcePath);
            Debug.Assert(style != null, "RuntimeConsole Style not found at "+ m_StyleSheetResourcePath);
            this.styleSheets.Add(style);
        }
        protected void RegisterCallbacks()
        {
            //Display any logs from the developer console
            DeveloperConsole.OnLog += (log, type) => Log(log, type);


            // Submit button
            m_SubmitButton.clicked += () => SubmitCommand();

            // Value changed callback for autocomplete popup
            m_Input.RegisterValueChangedCallback(OnInputValueChanged);
            
            // Input field submit
            m_Input.RegisterCallback<KeyDownEvent>(OnInputFieldKeyDown);
        }

        private void OnInputFieldKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter || evt.character == '\n')
            {
                SubmitCommand();
                focusController.IgnoreEvent(evt);
                evt.StopImmediatePropagation();
                m_Input.Blur();
            }
            if (evt.keyCode == KeyCode.Tab && m_AutocompletePopupActive)
            {
                m_Input.value = m_AutocompleteResult[0];
                evt.StopImmediatePropagation();
                evt.StopPropagation();
                focusController.IgnoreEvent(evt);
            }
        }

        private void OnInputValueChanged(ChangeEvent<string> evt)
        {
            string input = evt.newValue.Trim();

            if (string.IsNullOrEmpty(input) && m_AutocompletePopupActive)
            {
                HideAutoCompletePopup();
                return;
            }

            m_AutocompleteResult = DeveloperConsole.GetAutoCompleteMatches(input, 4).ToArray();
            if(m_AutocompleteResult.Length > 0)
                ShowAutoCompletePopup(m_AutocompleteResult);
        }

        void ShowAutoCompletePopup(string[] items)
        {
            if(items == null || items.Length == 0) 
                return;

            m_AutoCompletePopup.Clear();
            foreach (string item in items)
            {
                Label cmdLabel = new Label(item);
                m_AutoCompletePopup.Add(cmdLabel);
                m_AutoCompletePopup.RegisterCallback<ClickEvent>(evt => ClickAutoCompleteItem(item));
            }

            m_AutoCompletePopup.style.display = DisplayStyle.Flex;
            m_AutocompletePopupActive = true;
        }
        void HideAutoCompletePopup()
        {
            m_AutoCompletePopup.style.display = DisplayStyle.None;
            m_AutocompletePopupActive = false;
        }
        void ClickAutoCompleteItem(string command)
        {
            m_Input.value = command;
            HideAutoCompletePopup();
        }
        /// <summary>
        /// Submit the current inputted command
        /// </summary>
        public void SubmitCommand()
        {
            string input = m_Input.value.Trim();
            if (string.IsNullOrEmpty(input))
                return;


            DeveloperConsole.ExecuteCommand(input);
            m_Input.value = "";
        }
        /// <summary>
        /// Create a new entry in the log view.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="logType"></param>
        public void Log(string text, ConsoleLogType logType = ConsoleLogType.standard)
        {
            Label entry = new Label(text);
            //Assign css classes
            switch (logType)
            {
                case ConsoleLogType.warning:
                    entry.AddToClassList(logEntryWarningUssClassName);
                    break;
                case ConsoleLogType.error:
                    entry.AddToClassList(logEntryErrorUssClassName);
                    break;
                case ConsoleLogType.user_input:
                    entry.AddToClassList(logEntryInputUssClassName);
                    break;
            }

            m_Log.Add(entry);
        }
        /// <summary>
        /// Clear the command view.
        /// </summary>
        public new void Clear()
        {
            m_Log.Clear();
        }

        
        
    }
}

