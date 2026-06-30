using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Farbod.DeveloperConsole
{
    public class DeveloperConsoleEditorWindow : EditorWindow
    {
        private VisualElement m_Root;
        private DefaultDeveloperConsole m_ConsoleElement;

        /// <summary>
        /// The menu item available in the editor toolbar for opening this window.
        /// </summary>
        [MenuItem("Tools/Developer Console")]
        public static void ShowWindow()
        {
            var wnd = GetWindow<DeveloperConsoleEditorWindow>();
            var icon = EditorGUIUtility.IconContent("d_InputField Icon").image;
            wnd.titleContent = new GUIContent("Developer Console", icon);
            wnd.minSize = new(400, 200);
        }


        protected virtual void CreateGUI()
        {
            m_Root = base.rootVisualElement;

            m_ConsoleElement = new();
            m_Root.hierarchy.Add(m_ConsoleElement);
        }
    }
}

