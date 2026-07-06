using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Farbod.DeveloperConsole
{

#if UNITY_2023_2_OR_NEWER
    [UxmlElement]
#endif
    public partial class DefaultDeveloperConsole : VisualElement
    {
#if !UNITY_2023_2_OR_NEWER
        public new class UxmlFactory : UxmlFactory<DefaultDeveloperConsole, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlBoolAttributeDescription m_ShowStartupMessage = new UxmlBoolAttributeDescription { name = "show-startup-message" };
            UxmlIntAttributeDescription m_MaximumSuggestions = new UxmlIntAttributeDescription { name = "max-suggest" };

            public override System.Collections.Generic.IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                ((DefaultDeveloperConsole)ve).ShowStartupMessage = m_ShowStartupMessage.GetValueFromBag(bag, cc);
                ((DefaultDeveloperConsole)ve).MaximumSuggestions = m_MaximumSuggestions.GetValueFromBag(bag, cc);
            }
        }
#endif
        string m_StyleSheetResourcePath = "Style/RuntimeConsoleStyle";
        public static readonly string ussClassName = "developer-console";
        public static readonly string logViewUssClassName = "developer-console__log";
        public static readonly string logEntryWarningUssClassName = "developer-console__entry--warning";
        public static readonly string logEntryErrorUssClassName = "developer-console__entry--error";
        public static readonly string logEntryInputUssClassName = "developer-console__entry--input";

        public static readonly string inputRowUssClassName = "developer-console__input-row";
        public static readonly string inputContainerUssClassName = "developer-console__input-field-container";
        public static readonly string inputUssClassName = "developer-console__input-field";
        public static readonly string submitButtonUssClassName = "developer-console__submit-button";
        public static readonly string suggestionPopupUssClassName = "developer-console__autocomplete";


        private bool m_ShowStartupMessage = true;
#if UNITY_2023_2_OR_NEWER
        [UxmlAttribute]
#endif
        public bool ShowStartupMessage
        {
            get
            {
                return m_ShowStartupMessage;
            }
            set
            {
                if (m_ShowStartupMessage == value)
                    return;

                m_ShowStartupMessage = value;
                Clear();
            }
        }


#if UNITY_2023_2_OR_NEWER
        [UxmlAttribute]
#endif
        public int MaximumSuggestions { get; set; } = 6;
        

        private ScrollView m_Log;
        private TextField m_Input;
        private Button m_SubmitButton;
        private VisualElement m_SuggestionPopup, m_InputRow;

        private Label[] m_SuggestionLabels;
        int m_ActiveSuggestionIndex = 0;

        private bool SuggestionPopupActive => m_SuggestionPopup.visible;
        private ConsoleSuggestionHandler suggestionHandler;
        public override VisualElement contentContainer => m_Log.contentContainer;
        public DefaultDeveloperConsole()
        {
            // Set up this visual element and its sub-elements, and register its callbacks.
            PopulateElement();
            RegisterCallbacks();

            //Make sure the command index is ready
            DeveloperConsole.IndexCommandsIfNotIndexed();

            //Show startup message
            if (ShowStartupMessage)
                CreateLogEntry("<color=\"grey\">Welcome to the developer console. For info on all available commands, use 'help'");

            //Create a new suggestion handler instance
            suggestionHandler = new(true, true);
        }
        protected void PopulateElement()
        {
            this.AddToClassList(ussClassName);

            //Create the log scroll view
            m_Log = new ScrollView();
            m_Log.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            m_Log.verticalScrollerVisibility = ScrollerVisibility.Auto;
            m_Log.AddToClassList(logViewUssClassName);
            hierarchy.Add(m_Log);

            //Create a column to hold the input field and submit button
            m_InputRow = new VisualElement();
            m_InputRow.AddToClassList(inputRowUssClassName);
            m_InputRow.style.flexDirection = FlexDirection.Row;
            hierarchy.Add(m_InputRow);

            //Add the container for the input field and the suggestion popup
            VisualElement inputContainer = new VisualElement();
            inputContainer.AddToClassList(inputContainerUssClassName);
            m_InputRow.Add(inputContainer);

            //Input field
            m_Input = new TextField();
            m_Input.AddToClassList(inputUssClassName);
            m_Input.tabIndex = -1;
            inputContainer.Add(m_Input);

            //Autocomplete popup
            m_SuggestionPopup = new VisualElement();
            m_SuggestionPopup.AddToClassList(suggestionPopupUssClassName);
            m_SuggestionPopup.style.position = new(Position.Absolute); //make sure the position mode is absolute.
            inputContainer.Add(m_SuggestionPopup);
            HideSuggestionPopup();

            //Add the submit button
            m_SubmitButton = new Button();
            m_SubmitButton.text = "Submit";
            m_SubmitButton.AddToClassList(submitButtonUssClassName);
            m_InputRow.Add(m_SubmitButton);

            //Load default stylesheet
            StyleSheet style = Resources.Load<StyleSheet>(m_StyleSheetResourcePath);
            Debug.Assert(style != null, "RuntimeConsole Style not found at "+ m_StyleSheetResourcePath);
            this.styleSheets.Add(style);
        }
        protected void RegisterCallbacks()
        {
            //Display any logs from the developer console
            DeveloperConsole.OnLog += (log, type) => CreateLogEntry(log, type);
            DeveloperConsole.OnClearLog += ClearLogs;

            // Submit button
            m_SubmitButton.clicked += () => SubmitCommand();

            // Value changed callback for autocomplete popup
            m_Input.RegisterValueChangedCallback(OnInputValueChanged);
            
            // Input field submit
            m_Input.RegisterCallback<KeyDownEvent>(OnInputFieldKeyDown, TrickleDown.TrickleDown);
        }

        private void OnInputFieldKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter || evt.character == '\n')
            {
                SubmitCommand();

#if UNITY_2023_2_OR_NEWER
                focusController.IgnoreEvent(evt);
#else
                evt.PreventDefault();
#endif
                evt.StopImmediatePropagation();
            }
            if (evt.keyCode == KeyCode.Tab && SuggestionPopupActive && m_ActiveSuggestionIndex != -1)
            {
                ApplySuggestion(m_SuggestionLabels[m_ActiveSuggestionIndex].text);
#if UNITY_2023_2_OR_NEWER
                focusController.IgnoreEvent(evt);
#else
                evt.PreventDefault();
#endif
                evt.StopImmediatePropagation();
            }

            if (evt.keyCode == KeyCode.UpArrow && SuggestionPopupActive && m_SuggestionLabels != null)
            {
                SwitchSuggestion(true);
#if UNITY_2023_2_OR_NEWER
                focusController.IgnoreEvent(evt);
#else
                evt.PreventDefault();
#endif
                evt.StopImmediatePropagation();
            }
            if (evt.keyCode == KeyCode.DownArrow && SuggestionPopupActive && m_SuggestionLabels != null)
            {
                SwitchSuggestion(false);
#if UNITY_2023_2_OR_NEWER
                focusController.IgnoreEvent(evt);
#else
                evt.PreventDefault();
#endif
                evt.StopImmediatePropagation();
            }
        }
        private void SwitchSuggestion(bool forward)
        {
            // Check direction (based on the css direction of the menu)
            int dir = (forward ? 1 : -1) *
                      (m_SuggestionPopup.resolvedStyle.flexDirection == FlexDirection.ColumnReverse ||
                       m_SuggestionPopup.resolvedStyle.flexDirection == FlexDirection.RowReverse ? 1 : -1);
            int newIndex = m_ActiveSuggestionIndex + dir;

            // Update active suggestion
            if (newIndex >= 0 && newIndex < m_SuggestionLabels.Length)
            {
                m_SuggestionLabels[m_ActiveSuggestionIndex].RemoveFromClassList("active");
                m_SuggestionLabels[m_ActiveSuggestionIndex = newIndex].AddToClassList("active");
            }
        }

        /// <summary>
        /// Monitor the input field value for autocomplete recommendations
        /// </summary>
        /// <param name="evt"></param>
        private void OnInputValueChanged(ChangeEvent<string> evt)
        {
            var hasSuggestions = suggestionHandler.UpdateSuggestions(evt.newValue);
            var suggestions = suggestionHandler.suggestions;
            if (hasSuggestions)
            {
                ShowSuggestionPopup(suggestions?.Take(MaximumSuggestions).ToArray(), suggestions?.Count>MaximumSuggestions);
            }
            else if(SuggestionPopupActive)
            {
                HideSuggestionPopup();
            }
        }

        void ShowSuggestionPopup(string[] items, bool ellipses)
        {
            m_SuggestionPopup.style.display = DisplayStyle.Flex;
            m_SuggestionPopup.Clear();

            //Show the hint label
            if (!string.IsNullOrEmpty(suggestionHandler.current_hint))
            {
                Label info = new Label(suggestionHandler.current_hint);
		info.enableRichText = true;
                m_SuggestionPopup.Add(info);
            }

            
            if (items == null || items.Length==0)
            {
                m_ActiveSuggestionIndex = -1;
                m_SuggestionLabels = null;
                return;
            }

            //Show suggestions
            m_SuggestionLabels = new Label[items.Count()];
            for (int i = 0; i < m_SuggestionLabels.Length; i++)
            {
                string txt = items[i];
                Label itemLabel = new Label(txt);
                itemLabel.AddToClassList("replacement");
                itemLabel.RegisterCallback<ClickEvent>(evt => ApplySuggestion(txt));
                m_SuggestionPopup.Add(itemLabel);

                m_SuggestionLabels[i] = itemLabel;
            }
            
            //Set active suggestion
            m_SuggestionLabels[m_ActiveSuggestionIndex = 0].AddToClassList("active");

            //Show ellipses if too many items
            if (ellipses)
            {
                Label itemLabel = new Label("...");
                m_SuggestionPopup.Add(itemLabel);
            }
        }
        void HideSuggestionPopup()
        {
            m_SuggestionPopup.style.display = DisplayStyle.None;
        }
        void ApplySuggestion(string suggestion = null)
        {
            var newString = suggestionHandler.ApplySuggestion(m_Input.value, suggestion); //Apply suggesgion
            m_Input.value = newString; //Replace text field value

            //Move the cursor to the end
#if UNITY_2023_2_OR_NEWER
            m_Input.cursorIndex = newString.Length;
            m_Input.SelectNone();
#else
            // In older versions, use Select to move cursor
            m_Input.SelectRange(newString.Length, 0);
#endif

            HideSuggestionPopup();
        }
        /// <summary>
        /// Submit the current inputted command
        /// </summary>
        public void SubmitCommand()
        {
            string input = m_Input.value.Trim();
            if (string.IsNullOrEmpty(input))
                return;

            //Log command execution attempt on THIS CONSOLE ONLY
            CreateLogEntry("> " + input, ConsoleLogType.user_input);

            DeveloperConsole.ExecuteCommand(input);
            m_Input.value = "";
        }

        /// <summary>
        /// Create a new entry in the log view.
        /// </summary>
        /// <param name="text">The text of this entry</param>
        /// <param name="logType">How this entry is displayed</param>
        public void CreateLogEntry(string text, ConsoleLogType logType = ConsoleLogType.standard)
        {
            Label entry = new Label(text);

#if UNITY_2023_2_OR_NEWER
            entry.selection.isSelectable = true; //make text selectable for copying console logs
#else
            entry.enableRichText = true;
#endif

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

            bool wasAtBottom = m_Log.IsScrolledToBottom();
            m_Log.Add(entry);

            //Scroll to bottom after adding the object, if the view was not scrolled up before
            if(wasAtBottom)
                m_Log.ScrollToBottom();
        }
        /// <summary>
        /// Clears this console's logs.
        /// </summary>
        public void ClearLogs()
        {
            m_Log.Clear();
        }
    }
}

