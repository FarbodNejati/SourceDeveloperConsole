using System.Reflection;
using System.Runtime.CompilerServices;
using System;
using UnityEngine;

namespace Farbod.DeveloperConsole
{
    public interface IConsoleCommand
    {
        int GetParametersLength();
        bool CanExecuteInEditMode();
        string GetName();
        string GetDescription();
        string GetUsage();
    }


    /// <summary>
    /// Console methods are used to invoke static methods by entering their name into the console.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ConsoleMethod : Attribute, IConsoleCommand
    {
        public string NameOverride { get; private set; }
        public string Description { get; private set; }

        /// <summary>
        /// Is this command allowed to be executed in edit-mode
        /// </summary>
        private bool _executeInEditMode;
        public bool CanExecuteInEditMode() => _executeInEditMode;


        /// <summary>
        /// Source file used to reference the method when throwing errors
        /// </summary>
        public string sourceFilePath { get; private set; }

        /// <summary>
        /// Source line number used to reference the method when throwing errors
        /// </summary>
        public int sourceLine { get; private set; }

        public MethodInfo MethodInfo { get; set; }

        public ConsoleMethod(
            string nameOverride = null,
            string description = null,
            bool executeInEditMode = false,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            NameOverride = nameOverride?.Trim().ToLower();
            Description = description;
            _executeInEditMode = executeInEditMode;

#if UNITY_EDITOR
            // Normalize to forward slashes
            string normalizedPath = filePath.Replace('\\', '/');
            string dataPath = Application.dataPath.Replace('\\', '/');
            if (normalizedPath.StartsWith(dataPath))
            {
                // Strip the dataPath and prepend "Assets"
                string relative = "Assets" + normalizedPath.Substring(dataPath.Length);
                sourceFilePath = relative;   // e.g., "Assets/SourceDeveloperConsole/..."
            }
            // Fallback: keep the original
            else
                sourceFilePath = normalizedPath;

            sourceLine = lineNumber;
#endif
        }

        /// <summary>
        /// The name of this console command, that is used to invoke it (fully lowercase)
        /// </summary>
        /// <returns>Returns custom name if available, otherwise returns method name</returns>
        public string GetName()
        {
            return string.IsNullOrEmpty(NameOverride) ? MethodInfo.Name.ToLower() : NameOverride;
        }

        /// <summary>
        /// A short description explaining the use of this command
        /// </summary>
        public string GetDescription() => Description;

        /// <summary>
        /// The amount of variables this console command accepts
        /// </summary>
        public int GetParametersLength() => MethodInfo.GetParameters().Length;

        public string GetUsage()
        {
            var methodParams = MethodInfo.GetParameters();
            string[] paramsUsage = new string[methodParams.Length];

            for (int i = 0; i < paramsUsage.Length; i++)
            {
                string optional = methodParams[i].IsOptional ? "(optional)" : "";
                paramsUsage[i] = $"<{methodParams[i].Name}:{methodParams[i].ParameterType.Name}{optional}>";
            }

            return ($"{GetName()} {string.Join(" ", paramsUsage)}");
        }
    }

    /// <summary>
    /// A console variable.
    /// <para>
    /// Valid Settable ConVars (fields, or properties with setter methods) can be set by simply entering the name of the convar followed by the desired value : [convar] [value]
    /// </para>
    /// 
    /// <para>
    /// To get the value of a ConVar you must simply enter its name in the console : [convar]
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class ConsoleVariable : Attribute, IConsoleCommand
    {
        public string NameOverride { get; private set; }
        public string Description { get; private set; }

        private bool _executeInEditMode;
        public bool CanExecuteInEditMode() => _executeInEditMode;



        /// <summary>
        /// Source file used to reference the method when throwing errors
        /// </summary>
        public string sourceFilePath { get; private set; }

        /// <summary>
        /// Source line number used to reference the method when throwing errors
        /// </summary>
        public int sourceLine { get; private set; }

        /// <summary>
        /// whether this conVar has a valid setter
        /// </summary>
        public bool CanBeSet => (FieldInfo != null || (PropertyInfo != null && PropertyInfo.GetSetMethod(false) != null));

        /// <summary>
        /// The object type of this variable
        /// </summary>
        public Type VariableType => PropertyInfo?.PropertyType ?? FieldInfo?.FieldType;

        public FieldInfo FieldInfo { get; set; }
        public PropertyInfo PropertyInfo { get; set; }

        public ConsoleVariable(
            string nameOverride = null,
            string description = null,
            bool executeInEditMode = false,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            NameOverride = nameOverride?.Trim().ToLower();
            Description = description;
            _executeInEditMode = executeInEditMode;

#if UNITY_EDITOR
            // Normalize to forward slashes
            string normalizedPath = filePath.Replace('\\', '/');
            string dataPath = Application.dataPath.Replace('\\', '/');
            if (normalizedPath.StartsWith(dataPath))
            {
                // Strip the dataPath and prepend "Assets"
                string relative = "Assets" + normalizedPath.Substring(dataPath.Length);
                sourceFilePath = relative;   // e.g., "Assets/SourceDeveloperConsole/..."
            }
            // Fallback: keep the original
            else
                sourceFilePath = normalizedPath;

            sourceLine = lineNumber;
#endif
        }

        /// <summary>
        /// The name of this console command, that is used to get/set it (fully lowercase)
        /// </summary>
        /// <returns>Returns custom name if available, otherwise returns method name</returns>
        public string GetName()
        {
            if (!string.IsNullOrEmpty(NameOverride))
                return NameOverride;
            if (PropertyInfo != null)
                return PropertyInfo.Name.ToLower();
            else
                return FieldInfo.Name.ToLower();
        }

        /// <summary>
        /// A short description explaining the use of this command
        /// </summary>
        public string GetDescription() => Description;

        public string GetUsage()
        {
            if (CanBeSet)
                return $"{GetName()} <value:{VariableType.Name}(optional)>";
            else
                return GetName();
        }
        /// <summary>
        /// The amount of variables this console command accepts
        /// </summary>
        public int GetParametersLength() => CanBeSet ? 1 : 0;

        public object GetValue() => PropertyInfo?.GetMethod.Invoke(null, null) ?? FieldInfo?.GetValue(null);
        public void SetValue(object value)
        {
            if (PropertyInfo != null)
                PropertyInfo.SetValue(null, value);
            else
                FieldInfo.SetValue(null, value);
        }


    }

}

