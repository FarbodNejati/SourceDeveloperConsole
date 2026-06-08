using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Farbod.DeveloperConsole
{
    public static class DeveloperConsole
    {
        /// <summary>
        /// Static methods that may be executed through the console.
        /// </summary>
        public static List<ConsoleMethod> console_methods {  get; private set; }

        /// <summary>
        /// Static variables that may be set or viewed through the console.
        /// </summary>
        public static List<ConsoleVariable> console_variables { get; private set; }

        /// <summary>
        /// All indexed console commands as their string name
        /// </summary>
        public static List<string> command_index { get; private set; }

        /// <summary>
        /// Find every command and index their names.
        /// </summary>
        /// <exception cref="RepeatedCommandException"></exception>
        [ConsoleMethod("reindex", "regenerate the index of console commands")]
        public static void IndexCommands()
        {
            var assemblies = new Assembly[] { Assembly.GetAssembly(typeof(DeveloperConsole))};

            console_methods = new();
            console_variables = new();
            command_index.Clear();

            foreach (var conMethod in FindAllMethodAttributes(assemblies))
            {
                string name = conMethod.GetName();

                //Check for dupes
                if (command_index.Contains(name))
                    throw new RepeatedCommandException($"A command(method/convar) with the name {name} already exists.");

                //Index and add to commands list
                command_index.Add(name);
                console_methods.Add(conMethod);
            }
            foreach (var conVar in FindAllConVarAttributes(assemblies))
            {
                string name = conVar.GetName();

                //Check for dupes
                if (command_index.Contains(name))
                    throw new RepeatedCommandException($"A command(method/convar) with the name {conVar} already exists.");

                //Index and add to commands list
                command_index.Add(name);
                console_variables.Add(conVar);
            }
        }

        private static ConsoleMethod[] FindAllMethodAttributes(Assembly[] assemblies)
        {
            List<ConsoleMethod> result = new();
            foreach (var assembly in assemblies)
            {
                var methods = assembly.GetTypes().SelectMany(t => t.GetMethods()).Where(m => m.GetCustomAttributes(typeof(ConsoleMethod), true).Length > 0);
                foreach (var method in methods)
                {
                    //Throw an error for non static methods
                    if (!method.IsStatic)
                        throw new NonStaticCommandException($"Command methods must be static.");

                    ConsoleMethod attribute = GetAttribute<ConsoleMethod>(method);
                    attribute.MethodInfo = method;
                    result.Add(attribute);
                }
            }

            return result.ToArray();


            T GetAttribute<T>(MethodInfo method) where T : Attribute
            {
                object[] attributes = method.GetCustomAttributes(typeof(T), true);
                foreach (var att in attributes)
                {
                    if (att.GetType() == typeof(T))
                        return (T)att;
                }
                return default(T);
            }
        }
        private static ConsoleVariable[] FindAllConVarAttributes(Assembly[] assemblies)
        {
            List<ConsoleVariable> result = new();
            foreach (var assembly in assemblies)
            {
                //Properties
                var props = assembly.GetTypes().SelectMany(t => t.GetProperties()).Where(m => m.GetCustomAttributes(typeof(ConsoleVariable), true).Length > 0);
                foreach (var prop in props)
                {
                    ConsoleVariable attribute = GetConVarAttribute<ConsoleVariable>(prop);
                    attribute.PropertyInfo = prop;
                    result.Add(attribute);
                }

                //Fields
                var fields = assembly.GetTypes().SelectMany(t => t.GetFields()).Where(m => m.GetCustomAttributes(typeof(ConsoleVariable), true).Length > 0);
                foreach (var field in fields)
                {
                    ConsoleVariable attribute = GetConVarAttribute<ConsoleVariable>(field);
                    attribute.FieldInfo = field;
                    result.Add(attribute);
                }
            }

            return result.ToArray();


            
        }

        public static T GetConVarAttribute<T>(FieldInfo field) where T : ConsoleVariable
        {
            object[] attributes = field.GetCustomAttributes(typeof(T), true);
            foreach (var att in attributes)
            {
                if (att.GetType() == typeof(T))
                    return (T)att;
            }
            return default(T);
        }
        public static T GetConVarAttribute<T>(PropertyInfo prop) where T : ConsoleVariable
        {
            object[] attributes = prop.GetCustomAttributes(typeof(T), true);
            foreach (var att in attributes)
            {
                if (att.GetType() == typeof(T))
                    return (T)att;
            }
            return default(T);
        }

        private class RepeatedCommandException : Exception
        {
            public RepeatedCommandException(string message) : base(message) { }
        }
        private class NonStaticCommandException : Exception
        {
            public NonStaticCommandException(string message) : base(message) { }
        }
    }

    public interface IConsoleCommand
    {
        int GetParametersLength();
        string GetName();

        /// <summary>
        /// The amount of variables this console command accepts
        /// </summary>
        string GetDescription();
    }

    /// <summary>
    /// Console methods are used to invoke static methods by entering their name into the console.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ConsoleMethod : Attribute, IConsoleCommand
    {
        public string NameOverride { get; private set; }
        public string Description { get; private set; }

        public MethodInfo MethodInfo { get; set; }

        public ConsoleMethod(string nameOverride = null, string description = null)
        {
            NameOverride = nameOverride.Trim().ToLower();
            Description = description;
        }

        /// <summary>
        /// The name of this console command, that is used to invoke it (fully lowercase)
        /// </summary>
        /// <returns>Returns custom name if available, otherwise returns method name</returns>
        public string GetName()
        {
            return !string.IsNullOrEmpty(NameOverride) ? MethodInfo.Name.ToLower() : NameOverride;
        }

        /// <summary>
        /// A short description explaining the use of this command
        /// </summary>
        public string GetDescription() => Description;

        /// <summary>
        /// The amount of variables this console command accepts
        /// </summary>
        public int GetParametersLength() => MethodInfo.GetParameters().Length;
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
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public class ConsoleVariable : Attribute, IConsoleCommand
    {
        public string NameOverride { get; private set; }
        public string Description { get; private set; }

        public bool CanBeSet => (FieldInfo != null || (PropertyInfo != null && PropertyInfo.GetSetMethod(false) != null));


        public FieldInfo FieldInfo { get; set; }
        public PropertyInfo PropertyInfo { get; set; }

        public ConsoleVariable(string nameOverride = null, string description = null)
        {
            NameOverride = nameOverride.Trim().ToLower();
            Description = description;
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

        /// <summary>
        /// The amount of variables this console command accepts
        /// </summary>
        public int GetParametersLength() => CanBeSet ? 1 : 0;
    }
}

