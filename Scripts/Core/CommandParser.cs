using System;
using System.Linq;
using System.Reflection;

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
        public static void ParseCommandWithArgs(string input, out IConsoleCommand command, out string[] string_args)
        {
            if (input == "")
            {
                command = null;
                string_args = null;
                return;
            }

            string[] parts = input.Split(' '); //split the input string into parts
            string commandName = parts[0]; //get just the command name

            // Attempt to parse this command
            command = ParseCommand(commandName);
            if(command == null)
                throw new InvalidCommandException("command not found, use 'help' to see a all available commands.");

            string_args = ParseArgs(parts, command.GetParametersLength());
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
                TryCastArgument(args[i], methodParameters[i].ParameterType, out result[i]);

            return result;
        }

        /// <summary>
        /// Attempt to cast/parse a set of string arguments into any possible primitive types
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
        /// <param name="arg_string">The string to cast</param>
        /// <param name="result">The cast result</param>
        /// <returns>True if casting succeeded</returns>
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


        /// <summary>
        /// Attempt to cast a string parameter into other primitive types
        /// </summary>
        /// <param name="arg_string">The string to cast</param>
        /// <param name="targetType">The target type to cast to</param>
        /// <param name="result">The cast result</param>
        /// <returns>True if casting succeeded</returns>
        public static bool TryCastArgument(string arg_string, Type targetType, out object result)
        {
            result = null;

            // Handle null or empty
            if (string.IsNullOrEmpty(arg_string))
                return false;

            // Check if target type is string (just return the string)
            if (targetType == typeof(string))
            {
                result = arg_string;
                return true;
            }

            // Boolean
            if (targetType == typeof(bool) && bool.TryParse(arg_string, out bool result_bool))
            {
                result = result_bool;
                return true;
            }

            // Integer (also handle other integer types)
            if (targetType == typeof(int) && int.TryParse(arg_string, out int result_int))
            {
                result = result_int;
                return true;
            }
            if (targetType == typeof(short) && short.TryParse(arg_string, out short result_short))
            {
                result = result_short;
                return true;
            }
            if (targetType == typeof(byte) && byte.TryParse(arg_string, out byte result_byte))
            {
                result = result_byte;
                return true;
            }

            // Float (also handle double and decimal)
            if (targetType == typeof(float) && float.TryParse(arg_string, out float result_float))
            {
                result = result_float;
                return true;
            }
            if (targetType == typeof(double) && double.TryParse(arg_string, out double result_double))
            {
                result = result_double;
                return true;
            }
            if (targetType == typeof(decimal) && decimal.TryParse(arg_string, out decimal result_decimal))
            {
                result = result_decimal;
                return true;
            }

            // Enum
            if (targetType.IsEnum)
            {
                try
                {
                    // Try by name (case-insensitive)
                    if (Enum.TryParse(targetType, arg_string, true, out object enumResult))
                    {
                        result = enumResult;
                        return true;
                    }

                    // Try by numeric value
                    if (int.TryParse(arg_string, out int intValue) && Enum.IsDefined(targetType, intValue))
                    {
                        result = Enum.ToObject(targetType, intValue);
                        return true;
                    }

                    // For flags enum with comma-separated values
                    if (arg_string.Contains(","))
                    {
                        long combinedValue = 0;
                        foreach (string flag in arg_string.Split(','))
                        {
                            if (Enum.TryParse(targetType, flag.Trim(), true, out object flagResult))
                            {
                                combinedValue |= Convert.ToInt64(flagResult);
                            }
                            else
                            {
                                return false;
                            }
                        }
                        result = Enum.ToObject(targetType, combinedValue);
                        return true;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }

            // Any other type (fallback)
            try
            {
                result = Convert.ChangeType(arg_string, targetType);
                return true;
            }
            catch
            {
                result = arg_string;
                return false;
            }
        }
    }
}

