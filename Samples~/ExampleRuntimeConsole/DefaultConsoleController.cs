using UnityEngine;
using UnityEngine.UIElements;

public class DefaultConsoleController : MonoBehaviour
{
    [SerializeField] UIDocument _document;
    [SerializeField] StyleSheet _stylesheet;
    [SerializeField] KeyCode _consoleButton = KeyCode.BackQuote;
    public bool ConsoleEnabled { get; private set; } = true;
    VisualElement _consoleWindow;
    private void Start()
    {
        Debug.Assert(_document != null, "Console ui document is null!");

        _document.rootVisualElement.styleSheets.Add(_stylesheet);
        _consoleWindow = _document.rootVisualElement.Q<VisualElement>("console");

        var close_btn = _consoleWindow.Q<Button>("close_button");
        close_btn.clicked += () => SetConsoleEnabled(false);


        SetConsoleEnabled(ConsoleEnabled);
    }
    void Update()
    {
        // Check if the Tilda key (backtick) is pressed
        if (Input.GetKeyDown(_consoleButton))
        {
            Debug.Assert(_document != null, "Console ui document is null!");
            SetConsoleEnabled(!ConsoleEnabled);
        }
    }
    void SetConsoleEnabled(bool enabled)
    {
        _consoleWindow.style.display = enabled ? DisplayStyle.Flex : DisplayStyle.None;
        ConsoleEnabled = enabled;
    }
}
