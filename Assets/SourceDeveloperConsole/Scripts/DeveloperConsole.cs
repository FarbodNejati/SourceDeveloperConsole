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
        public static List<ConsoleMethod> console_methods { get; private set; }

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
        [ConsoleMethod("reindex", "regenerate the index of console commands", true)]
        public static void IndexCommands()
        {
            var stopwatch = Stopwatch.StartNew();//Measure operation time

            var assemblies = new Assembly[] { Assembly.GetAssembly(typeof(DeveloperConsole)) };

            List<string> new_index = new();
            int changes = 0;

            console_methods = new();
            console_variables = new();
            new_index.Clear();

            foreach (var conMethod in FindAllMethodAttributes(assemblies))
            {
                string name = conMethod.GetName();

                //Check for dupes
                if (new_index.Contains(name))
                    throw new RepeatedCommandException($"A command(method/convar) with the name {name} already exists.");

                //Keep track of new commands
                if (!command_index.Contains(name))
                    changes++;

                //Index and add to commands list
                new_index.Add(name);
                console_methods.Add(conMethod);
            }
            foreach (var conVar in FindAllConVarAttributes(assemblies))
            {
                string name = conVar.GetName();

                //Check for dupes
                if (new_index.Contains(name))
                    throw new RepeatedCommandException($"A command(method/convar) with the name {conVar} already exists.");

                //Keep track of new commands
                if (!command_index.Contains(name))
                    changes++;

                //Index and add to commands list
                new_index.Add(name);
                console_variables.Add(conVar);
            }

            stopwatch.Stop();
            long elapsedMs = stopwatch.ElapsedMilliseconds;

            // Update index index
            if (changes > 0)
            {
                command_index = new_index.OrderBy(x => x).ToList(); ;
                Print($"Indexed {command_index.Count} commands in {elapsedMs}ms");
            }
            else
                Print($"No changes detected. (Checked in {elapsedMs} ms)");
            
        }

        public static void IndexCommandsIfNotIndexed()
        {
            if (console_methods == null || console_variables == null)
            {
                IndexCommands();
            }
        }

        public static void ExecuteCommand(string input)
        {
            try
            {
                CommandParser.ParseCommandWithArgs(input, out var command, out var args);
                ExecuteCommand(command, args);
            }
            catch (InvalidCommandException e) { 
                Error(e);
            }
        }
        /// <summary>
        /// Executes a IConsoleCommand object with given arguments
        /// </summary>
        /// <param name="command">Command to execute</param>
        /// <param name="string_args">String arguments (unparsed)</param>
        /// <returns></returns>
        public static object ExecuteCommand(IConsoleCommand command, string[] string_args)
        {
            object return_result = null;
            try
            {
#if UNITY_EDITOR
                //Edit-Mode check
                if (!Application.isPlaying && !command.CanExecuteInEditMode())
                    throw new InvalidOperationException(
                        $"Command <color=yellow>'{command.GetName()}'</color> cannot be executed in Edit Mode. " +
                        $"Please enter Play Mode to use this command."
                    );
#endif

                if (command is ConsoleMethod)
                {
                    //Cast to ConsoleMethod
                    var conMethod = (ConsoleMethod)command;
                    //Execute the ConsoleMethod
                    return_result = ExecuteConMethod(conMethod, CommandParser.TryCastArguments(string_args, conMethod.MethodInfo.GetParameters()));
                }
                else if (command is ConsoleVariable)
                {
                    //Cast to ConsoleVariable
                    var conVar = (ConsoleVariable)command;
                    //Try to get the inputted value
                    CommandParser.TryCastArgument(string_args.FirstOrDefault(), conVar.VariableType, out object value);
                    //Execute the variable command
                    return_result = ExecuteConVar(conVar, value);
                }
            }
            //Errors thrown by commands
            catch (TargetInvocationException e) // Catch inner error for invocation errors
            {
                // Get the actual exception that was thrown
                Exception innerException = e.InnerException;
                // Show the real error
                Error(innerException);
            }
            //General errors (not generated by the commands themselves)
            catch (Exception e)
            {
                Error(e);
                // Also display in the default console
                UnityEngine.Debug.LogError($"{e.GetType().Name}: {e.Message}");
            }


            //Print out the execution result if its not null
            if (return_result != null)
                DeveloperConsole.Print(return_result.ToString());

            return return_result;
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
            return_object = command.MethodInfo.Invoke(null, method_arguments);
            //Return the commands return object
            return command.MethodInfo.ReturnType == null ? null : return_object;
        }
        public static object ExecuteConVar(ConsoleVariable conVariable, object value)
        {
            //Set the value if needed
            if (value != null)
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

                conVariable.SetValue(singleArg);
            }

            //Get and show the currect value
            return conVariable.GetValue();
        }

        [ConsoleMethod("print", "log a message to the console.", executeInEditMode: true)]
        public static void Print(string message)
        {
            if (message == null)
                throw new ArgumentNullException("message");
            OnLog.Invoke(message, ConsoleLogType.standard);
        }
        [ConsoleMethod("error", "log an error to the console.", executeInEditMode: true)]
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

        [ConsoleMethod("warn", "log an error to the console.", executeInEditMode: true)]
        public static void Warn(string message)
        {
            OnLog.Invoke(message, ConsoleLogType.warning);
        }

        [ConsoleMethod("help", "List all commands, or log the information of a command.", executeInEditMode: true)]
        public static void Help(string commandName = null)
        {
            //If no name is given, list all commands
            if (commandName == null || commandName.Trim() == "")
            {
                Print(string.Join("\n", command_index));
                return;
            }


            var command = CommandParser.ParseCommand(commandName);
            if (command == null)
            {
                Error("Unknown command, Use 'help' to get a list of all available commands.");
                return;
            }

            //Description and Usage
            Print($"Description: \"{command.GetDescription()??"n/a"}\"\n<color=\"yellow\">Usage:\n <color=\"grey\">{command.GetUsage()}");
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
        bool CanExecuteInEditMode();
        string GetName();

        /// <summary>
        /// The amount of variables this console command accepts
        /// </summary>
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
        public bool ExecuteInEditMode { get; private set; }
        public bool CanExecuteInEditMode() => ExecuteInEditMode;

        public MethodInfo MethodInfo { get; set; }

        public ConsoleMethod(string nameOverride = null, string description = null, bool executeInEditMode = false)
        {
            NameOverride = nameOverride.Trim().ToLower();
            Description = description;
            ExecuteInEditMode = executeInEditMode;
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
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public class ConsoleVariable : Attribute, IConsoleCommand
    {
        public string NameOverride { get; private set; }
        public string Description { get; private set; }

        public bool ExecuteInEditMode { get; private set; }
        public bool CanExecuteInEditMode => ExecuteInEditMode;

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

        public ConsoleVariable(string nameOverride = null, string description = null, bool executeInEditMode = false)
        {
            NameOverride = nameOverride.Trim().ToLower();
            Description = description;
            ExecuteInEditMode = executeInEditMode;
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
            if(CanBeSet)
                return($"{GetName()} <{VariableType.Name}(optional)>");
            else
                return (GetName());
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

        bool IConsoleCommand.CanExecuteInEditMode()
        {
            throw new NotImplementedException();
        }
    }

    public class InvalidCommandException : Exception
    {
        public InvalidCommandException() { }
        public InvalidCommandException(string message) : base(message) { }
    }
}

