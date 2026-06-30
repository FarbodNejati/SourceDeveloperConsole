using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Farbod.DeveloperConsole
{
    public enum ConsoleLogType
    {
        standard,
        error,
        warning,
        user_input
    }
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
        public static List<string> command_index { get; private set; } = new();
        public static event Action<string, ConsoleLogType> OnLog;

        [ConsoleVariable("show_full_stacktrace", "should the console show a full error traceback, or simply the name and message.")]
        public static bool ShowFullErrorStackTrace { get; set; } = false;


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

            // Order command name index by name
            command_index.OrderBy(x => x).ToList();

            Print($"Indexed {command_index.Count} commands.");
        }

        public static void IndexCommandsIfNotIndexed()
        {
            if(console_methods == null || console_variables == null)
            {
                IndexCommands();
            }
        }
        
        public static void ExecuteCommand(string command)
        {
            OnLog.Invoke("> "+command, ConsoleLogType.user_input);
            CommandParser.ExecuteString(command);
        }
        public static object ExecuteConMethod(ConsoleMethod command, params object[] user_args)
        {
            //This code is for automatically putting in default parameter values if they exist and weren't specified explicitly by the user
            object[] method_arguments;
            var method_params = command.MethodInfo.GetParameters();

            //If proper arguments are given
            if (user_args != null && user_args.Length == method_params.Length)
            {
                method_arguments = user_args;
            }
            //Otherwise, try to fill the needed slots with user args, and fil the others with their default value
            else
            {
                method_arguments = new object[method_params.Length];
                for (int i = 0; i < method_params.Length; i++)
                {
                    //Copy user args to new args
                    if (i < user_args.Length)
                        method_arguments[i] = user_args[i];
                    //Get default value if available
                    else if (method_params[i].HasDefaultValue)
                        method_arguments[i] = method_params[i].DefaultValue;
                }
            }


            object return_object = null;
            // Invoke the method
            try
            {
                return_object = command.MethodInfo.Invoke(null, method_arguments);
            }
            catch (TargetInvocationException e) // Catch inner error for invocation errors
            {
                // Get the actual exception that was thrown
                Exception innerException = e.InnerException;
                // Show the real error
                Error(innerException);
            }
            catch (Exception e)
            {
                Error(e);

                // Also display in the default console
                UnityEngine.Debug.LogError($"{e.GetType().Name}: {e.Message}");
            }

            //Return the commands return object
            return command.MethodInfo.ReturnType == null ? null : return_object;
        }
        public static object ExecuteConVar(ConsoleVariable conVariable, object value)
        {
            //Get and print the value
            if (value == null)
            {
                if (conVariable.PropertyInfo != null)
                    return conVariable.PropertyInfo.GetMethod.Invoke(null, null);

                else
                    return conVariable.FieldInfo.GetValue(null);
            }
            else
            {
                //if user has inputted multiple arguments into the console input string, we just use the first one
                object singleArg = null;
                if (value is object[])
                {
                    if (((object[])value).Length > 0)
                    {
                        singleArg = ((object[])value)[0];
                    }
                }
                else
                {
                    singleArg = value;
                }

                try
                {
                    if (conVariable.PropertyInfo != null)
                        conVariable.PropertyInfo.SetValue(null, singleArg);
                    else
                        conVariable.FieldInfo.SetValue(null, singleArg);
                }
                catch (TargetInvocationException e) // Catch inner error for invocation errors
                {
                    // Get the actual exception that was thrown
                    Exception innerException = e.InnerException;
                    // Show the real error
                    Error(innerException);
                }
                catch (Exception e)
                {
                    Error(e);

                    // Also display in the default console
                    UnityEngine.Debug.LogError($"{e.GetType().Name}: {e.Message}");
                }
            }

            return null;
        }

        public static List<string> GetAutoCompleteMatches(string input, int maximumResults = 6)
        {
            string query = input?.Trim().ToLower() ?? "";
            if (string.IsNullOrEmpty(query))
                return new(0);

            //Search for commands that start with our query text, and order them by their name.
            return command_index
                .Where(cmd => cmd.StartsWith(query))
                .OrderByDescending(cmd => cmd.StartsWith(query)) // Exact prefix match first
                .ThenBy(cmd => cmd.Length) // Shortest first (likely most relevant)
                .ThenBy(cmd => cmd) // Then alphabetical
                .Take(6)
                .ToList();
        }

        [ConsoleMethod("print", "log a message to the console.")]
        public static void Print(string message)
        {
            if (message == null)
                throw new ArgumentNullException("message");
            OnLog.Invoke(message, ConsoleLogType.standard);
        }
        [ConsoleMethod("error", "log an error to the console.")]
        public static void Error(string message)
        {
            OnLog.Invoke(message, ConsoleLogType.error);
        }

        public static void Error(System.Exception exception)
        {
            if (ShowFullErrorStackTrace)
                Error(exception.ToString());
            else
                Error($"{exception.GetType().Name}: {exception.Message}");
        }

        [ConsoleMethod("warn", "log an error to the console.")]
        public static void Warn(string message)
        {
            OnLog.Invoke(message, ConsoleLogType.warning);
        }

        [ConsoleMethod("help", "List all commands, or log the information of a command.")]
        public static void Help(string commandName = null)
        {
            //If no name is given, list all commands
            if(commandName == null || commandName.Trim() == "")
            {
                foreach (var cmd in command_index)
                    Print(cmd);

                return;
            }



            var command = CommandParser.ParseCommand(commandName);
            if (command == null)
            {
                Error("Unknown command, Use 'help' to get a list of all available commands.");
                return;
            }
                
            //Description
            string desc = command.GetDescription();
            if (!string.IsNullOrEmpty(desc))
            {
                Print($"Description: \"{command.GetDescription()}\"");
            }
            //Command usage (with parameters)
            int parametersLength = command.GetParametersLength();
            if(parametersLength > 0)
            {
                // If we are dealing with a console variable (which only takes one value, if it can be set in the first place)
                // Then simply display the type of the console variable.
                if (command is ConsoleVariable)
                {
                    Print($"To Set: <color=\"yellow\">{command.GetName()} <color=\"grey\"><{(command as ConsoleVariable).VariableType.Name}>");
                    Print($"To Get: <color=\"yellow\">{command.GetName()}");
                }
                //Console method (has multiple parameters)
                else
                {
                    string[] paramsUsage = new string[parametersLength];
                    var methodParams = (command as ConsoleMethod).MethodInfo.GetParameters();
                    for (int i = 0; i < parametersLength; i++)
                    {
                        string optional = methodParams[i].IsOptional ? " (optional)" : "";
                        paramsUsage[i] = $"<{methodParams[i].Name} : {methodParams[i].ParameterType.Name}{optional}>";
                    }

                    Print($"Usage: <color=\"yellow\">{command.GetName()} <color=\"grey\">{string.Join(" ", paramsUsage)}");
                }

                
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

