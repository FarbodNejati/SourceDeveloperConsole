using System;
using System.Linq;
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

            var command = ParseCommand(commandName);
            if (command != null) //if the command exists
            {
                string[] cleanParts = ParseArgs(parts, command.GetParametersLength());

                if (command is ConsoleMethod)
                {
                    //TODO
                }
                if (command is ConsoleVariable)
                {
                    //TODO
                }
            }
        }

        public static string[] ParseArgs(string[] commandParts, int maxArgs)
        {
            // Remove command from command parts (first element
            var excludingCommand = commandParts.ToList();
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
    }
}

