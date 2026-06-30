using System;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace Farbod.DeveloperConsole
{
    public static class CommandParser
    {
        public static IConsoleCommand ParseCommand(string input)
        {
            return (IConsoleCommand)ParseConsoleMethod(input) ?? ParseConsoleVariable(input);
        }
        public static ConsoleMethod ParseConsoleMethod(string input)
        {
            return DeveloperConsole.console_methods.Find(cmd => cmd.GetName().Trim() == input.ToLower().Trim());
        }
        public static ConsoleVariable ParseConsoleVariable(string input)
        {
            return DeveloperConsole.console_variables.Find(v => v.GetName().Trim() == input.ToLower().Trim());
        }


        /// <summary>
        /// Given a string, executes it normally as if it were a console-input
        /// </summary>
        /// <param name="input"></param>
        public static void ExecuteString(string input)
        {
            if (input == "") return;

            string[] parts = input.Split(' '); //split the input string into parts
            string commandName = parts[0]; //get just the command name

            // Attempt to parse this command
            var command = ParseCommand(commandName);
            if(command == null)
            {
                DeveloperConsole.Error("Unknown command");
                return;
            }

            string[] string_args = ParseArgs(parts, command.GetParametersLength());
            object return_result = null;

            if (command is ConsoleMethod)
            {
                var conMethod  = (ConsoleMethod)command;
                return_result = DeveloperConsole.ExecuteConMethod(conMethod, TryCastArguments(string_args, conMethod.MethodInfo.GetParameters()));
            }
            else if (command is ConsoleVariable)
            {
                var conVar = (ConsoleVariable)command;
                return_result = DeveloperConsole.ExecuteConVar(conVar, TryCastArguments(string_args));
            }

            if (return_result != null)
                DeveloperConsole.Print(return_result.ToString());
        }

        public static string[] ParseArgs(string[] commandParts, int maxArgs)
        {
            // Remove command from command parts (first element
            var excludingCommand = commandParts.ToList();

            //Remove the command itself, leaving only the arguments
            excludingCommand.RemoveAt(0);

            string rejoinedArgs = string.Join(' ', excludingCommand);

            var newPartsArray = rejoinedArgs.Split('"', '\'')
                     .Select((element, index) => index % 2 == 0  // If even index
                                           ? element.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)  // Split the item
                                           : new string[] { element })  // Keep the entire item
                     .SelectMany(element => element).ToList();

            if (newPartsArray.Count > maxArgs)
                newPartsArray.RemoveRange(newPartsArray.Count - (newPartsArray.Count - maxArgs), newPartsArray.Count - maxArgs);

            return newPartsArray.ToArray();
        }

        /// <summary>
        /// Attempt to cast/parse a set of string arguments
        /// </summary>
        public static object[] TryCastArguments(string[] args, ParameterInfo[] methodParameters)
        {
            object[] result = new object[args.Length];
            for (int i = 0; i < args.Length; i++)
            {

                //First make sure the param is not a string
                var t = methodParameters[i].ParameterType;
                if (t == typeof(string))
                    result[i] = args[i];
                else
                    TryCastArgument(args[i], out result[i]);
            }

            return result;
        }

        /// <summary>
        /// Attempt to cast/parse a set of string arguments into any other primitive types
        /// </summary>
        public static object[] TryCastArguments(string[] args)
        {
            if(args == null || args.Length == 0) 
                return null;

            object[] result = new object[args.Length];
            for (int i = 0; i < args.Length; i++)
                TryCastArgument(args[i], out result[i]);

            return result;
        }

        /// <summary>
        /// Attempt to cast a string parameter into other primitive types
        /// </summary>
        /// <param name="arg_string"></param>
        /// <returns>Cast to non-string success</returns>
        public static bool TryCastArgument(string arg_string, out object result)
        {
            //Boolean
            if (bool.TryParse(arg_string, out bool result_bool))
            {
                result = result_bool;
                return true;
            }

            //Integer
            if (int.TryParse(arg_string, out int result_integer))
            {
                result = result_integer;
                return true;
            }

            //Float
            if (float.TryParse(arg_string, out float result_float))
            {
                result = result_float;
                return true;
            }

            //String
            result = arg_string;
            return false;
        }

    }
}

