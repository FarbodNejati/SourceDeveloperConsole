using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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

        /// <summary>
        /// Subscribe to this event to recieve logs in your console gui.
        /// </summary>
        public static event Action<string, ConsoleLogType> OnLog;

        /// <summary>
        /// Subscribe to this event to know when to clear your consone gui's logs.
        /// </summary>
        public static event Action OnClearLog;

        /// <summary>
        /// Should exceptions and errors logged to the console include the full stack trace?
        /// </summary>
        [ConsoleVariable("log_full_stack", "should the console show a full error traceback, or simply the name and message.", true)]
        public static bool ShowFullErrorStackTrace { get; set; } = false;


        /// <summary>
        /// Find every command and index their names.
        /// </summary>
        /// <exception cref="RepeatedCommandException"></exception>
        [ConsoleMethod("reindex", "regenerate the index of console commands", true)]
        public static void IndexCommands()
        {
            var stopwatch = Stopwatch.StartNew();//Measure operation time

            var assemblies = GetAssemblies();

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

        /// <summary>
        /// Indexes commands if the command index is empty
        /// </summary>
        public static void IndexCommandsIfNotIndexed()
        {
            if (console_methods == null || console_variables == null)
            {
                IndexCommands();
            }
        }

        /// <summary>
        /// Parse and execute the provided input
        /// </summary>
        /// <param name="input"></param>
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
                UnityEngine.Debug.LogException(e);
                Error(e);
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
        
        /// <summary>
        /// Return the value of a console variable, and set it ig a value is provided.
        /// </summary>
        /// <param name="conVariable"></param>
        /// <param name="value"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Log a standard message to the console.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        [ConsoleMethod("print", "log a message to the console.", executeInEditMode: true)]
        public static void Print(object message)
        {
            if (message == null)
                throw new ArgumentNullException("message");
            OnLog.Invoke(message.ToString(), ConsoleLogType.standard);
        }

        /// <summary>
        /// Log a message with error formatting to the console.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        [ConsoleMethod("error", "log an error to the console.", executeInEditMode: true)]
        public static void Error(string message)
        {
            if (message == null)
                throw new ArgumentNullException("message");
            OnLog.Invoke(message, ConsoleLogType.error);
        }

        /// <summary>
        /// Log an exception to the console.
        /// </summary>
        public static void Error(System.Exception exception)
        {
            if (ShowFullErrorStackTrace)
                Error(exception.ToString());
            else
                Error($"{exception.GetType().Name}: {exception.Message}");
        }


        /// <summary>
        /// Log a messge with warning formatting to the console.
        /// </summary>
        /// <param name="message"></param>
        /// <exception cref="ArgumentNullException"></exception>
        [ConsoleMethod("warn", "log an error to the console.", executeInEditMode: true)]
        public static void Warn(string message)
        {
            if (message == null)
                throw new ArgumentNullException("message");
            OnLog.Invoke(message, ConsoleLogType.warning);
        }

        /// <summary>
        /// ist all commands, Or etrieve information for a specific command
        /// </summary>
        /// <param name="command_name"></param>
        [ConsoleMethod("help", "List all commands, or log the information of a command.", executeInEditMode: true)]
        public static string Help(string command_name = null)
        {
            //If no name is given, list all commands
            if (command_name == null || command_name.Trim() == "")
            {
                string commandsList = string.Join("\n", command_index);
                return $"use `help {command_name}` to get info on a specific command " +
                    $"\n\nCommands: <color=\"yellow\">\n" +
                    commandsList;
            }


            var command = CommandParser.ParseCommand(command_name);
            if (command == null)
            {
                throw new ArgumentException("Unknown command, Use 'help' to get a list of all available commands.");
            }

            //Description and Usage
            return $"Description: \"{command.GetDescription()??"n/a"}\"\n<color=\"yellow\">Usage: <color=\"grey\">{command.GetUsage()}";
        }

        /// <summary>
        /// Clears the log of console GUIs subscribed to the OnClearLog event
        /// </summary>
        [ConsoleMethod("clear", "Clear all terminals.", executeInEditMode: true)]
        public static void ClearLog()
        {
            OnClearLog.Invoke();
        }

        /// <summary>
        /// Retrieves all ConsoleMethods in the provided assemblies
        /// </summary>
        /// <param name="assemblies"></param>
        /// <returns></returns>
        /// <exception cref="NonStaticCommandException"></exception>
        private static ConsoleMethod[] FindAllMethodAttributes(Assembly[] assemblies)
        {
            List<ConsoleMethod> result = new();
            foreach (var assembly in assemblies)
            {
                var methods = assembly.GetTypes().SelectMany(t => t.GetMethods()).Where(m => m.GetCustomAttributes(typeof(ConsoleMethod), true).Length > 0);
                foreach (var method in methods)
                {
                    ConsoleMethod attribute = GetAttribute<ConsoleMethod>(method);
                    attribute.MethodInfo = method;

                    //Skip non static methods
                    if (!ValidateMethodAttribute(attribute))
                        continue;

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

        /// <summary>
        /// Retrieves all ConsoleMethods in the provided assemblies
        /// </summary>
        /// <param name="assemblies"></param>
        /// <returns></returns>
        private static ConsoleVariable[] FindAllConVarAttributes(Assembly[] assemblies)
        {
            List<ConsoleVariable> result = new();

            foreach (var assembly in assemblies)
            {
                // --- Properties ---
                var props = assembly.GetTypes()
                    .SelectMany(t => t.GetProperties())
                    .Where(m => m.GetCustomAttributes(typeof(ConsoleVariable), true).Length > 0);

                foreach (var prop in props)
                {
                    var attribute = GetConVarAttribute<ConsoleVariable>(prop);
                    attribute.PropertyInfo = prop;

                    //Skip non static methods
                    if (!ValidatePropertyAttribute(attribute))
                        continue;

                    result.Add(attribute);
                }

                // --- Fields ---
                var fields = assembly.GetTypes()
                    .SelectMany(t => t.GetFields())
                    .Where(m => m.GetCustomAttributes(typeof(ConsoleVariable), true).Length > 0);

                foreach (var field in fields)
                {
                    var attribute = GetConVarAttribute<ConsoleVariable>(field);
                    attribute.FieldInfo = field;

                    // Check static-ness
                    if (!ValidateFieldAttribute(attribute))
                        continue;

                    result.Add(attribute);
                }
            }

            return result.ToArray();
        }

        private static Assembly[] GetAssemblies()
        {
            var consoleAssembly = typeof(DeveloperConsole).Assembly;
            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetReferencedAssemblies().Any(r => r.FullName == consoleAssembly.FullName)
                        || a == consoleAssembly)
            .ToArray();
            return assemblies;
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


        private static bool ValidateMethodAttribute(ConsoleMethod attribute)
        {
            if (attribute.MethodInfo.IsStatic)
                return true;

#if UNITY_EDITOR
            ShowErrorMessageWithPath(attribute, attribute.MethodInfo, attribute.sourceFilePath, attribute.sourceLine);
#endif
            return false;
        }

        private static bool ValidatePropertyAttribute(ConsoleVariable attribute)
        {
            var prop = attribute.PropertyInfo;
            if (prop == null)
                return false;

            var getMethod = prop.GetGetMethod();

            //Return true if static
            if (getMethod != null && getMethod.IsStatic)
                return true;

#if UNITY_EDITOR
            ShowErrorMessageWithPath(attribute, prop, attribute.sourceFilePath, attribute.sourceLine);
#endif
            return false;
        }

        private static bool ValidateFieldAttribute(ConsoleVariable attribute)
        {
            var field = attribute.FieldInfo;

            //Return true if static
            if (field != null && field.IsStatic)
                return true;

#if UNITY_EDITOR
            ShowErrorMessageWithPath(attribute, field, attribute.sourceFilePath, attribute.sourceLine);
#endif
            return false;
        }

        private static void ShowErrorMessageWithPath(IConsoleCommand command,MemberInfo info, string path, int line)
        {
            Warn($"Skipping non static command '{command.GetName()}'");

            string link = $"<color=#40a0ff><link=\"href='{path}' line='{line}'\">{path}:{line}</link></color>";
            UnityEngine.Debug.LogError($"The command {info.Name} is not static.\n(at {link})");
        }
    }
    public class RepeatedCommandException : Exception
    {
        public RepeatedCommandException(string message) : base(message) { }
    }
    public class NonStaticCommandException : Exception
    {
        public NonStaticCommandException(string message) : base(message) { }
    }

    public class InvalidCommandException : Exception
    {
        public InvalidCommandException() { }
        public InvalidCommandException(string message) : base(message) { }
    }
}

