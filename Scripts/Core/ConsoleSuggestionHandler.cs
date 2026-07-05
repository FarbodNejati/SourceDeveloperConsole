using System;
using System.Collections.Generic;
using System.Linq;

namespace Farbod.DeveloperConsole
{
    public class ConsoleSuggestionHandler
    {
        public bool commandUsageTips;
        public bool tipsRichTextHighlighting;
        public List<string> suggestions { get; protected set; } = new();
        public string current_hint;

        public ConsoleSuggestionHandler(bool tips, bool tipsRichTextHighlighting = false)
        {
            this.commandUsageTips = tips;
            this.tipsRichTextHighlighting = tipsRichTextHighlighting;
        }
        public string ApplySuggestion(string raw_input, string suggestion = null)
        {
            //If no suggestion is provided, simply choose the latest top suggestion
            if (string.IsNullOrEmpty(suggestion) && suggestions.Count > 0)
                suggestion = suggestions[0];

            //If no part of the suggestion has been entered, add it to the end of the input
            if (raw_input.EndsWith(" "))
                return raw_input + suggestion;

            //Replace the last word in input field, with the suggestion string.
            var segments = raw_input.Trim().Split(' ');
            segments[segments.Length - 1] = suggestion; //Replace last word

            return string.Join(' ', segments)+" ";
        }
        /// <summary>
        /// Generate a list of suggestions based on the string input
        /// </summary>
        /// <param name="raw_input">Command prompt input</param>
        /// <returns>Whether we have any suggestions or not</returns>
        public bool UpdateSuggestions(string raw_input)
        {
            if (string.IsNullOrEmpty(raw_input.Trim()))
                return false;

            //Split the input into parts
            var segments = raw_input.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            //The items we will search in, to suggest a string to the user.
            //These items can be commands, enum types, or something else.
            var suggestionSpace = GetSuggestionSpace(raw_input, segments);

            suggestions = FilterSuggestionSpace(suggestionSpace, raw_input, segments)?.ToList();
            return suggestions?.Count > 0 || !string.IsNullOrEmpty(current_hint);
        }
        /// <summary>
        /// Get the space of items to search and filter through, based on the input (list of commands, possible parameter values, etc)
        /// </summary>
        /// <param name="input">The raw input</param>
        /// <param name="input_parts">The segmented input</param>
        /// <returns>Generated suggestion space</returns>
        public List<string> GetSuggestionSpace(string input, string[] input_parts)
        {
            bool endsWithWhitespace = input.EndsWith(" ");
            var commands = DeveloperConsole.command_index;
            current_hint = "";

            // If a command has already been entered, this segment must be a parameter
            if (commands.Contains(input_parts[0]))
            {
                IConsoleCommand command = CommandParser.ParseCommand(input_parts[0]);

                // Show usage with parameter validation
                if (commandUsageTips)
                    current_hint = tipsRichTextHighlighting? GetHighlightedUsage(command, input_parts, endsWithWhitespace) : command.GetUsage();

                // Help command special case
                if (input_parts[0] == "help" && (
                    input_parts.Length == 2 && !endsWithWhitespace ||
                    input_parts.Length == 1 && endsWithWhitespace))
                {
                    return commands;
                }

                // Command parameters
                if ((input_parts.Length == 1 && endsWithWhitespace) || input_parts.Length > 1)
                {
                    int paramIndex = endsWithWhitespace ? input_parts.Length - 1 : input_parts.Length - 2;

                    if (paramIndex >= 0 && paramIndex < command.GetParametersLength())
                    {
                        Type paramType = null;
                        string currentValue = input_parts.Length > paramIndex + 1 ? input_parts[paramIndex + 1] : "";

                        if (command is ConsoleMethod method)
                            paramType = method.MethodInfo.GetParameters()[paramIndex].ParameterType;
                        else if (command is ConsoleVariable variable)
                            paramType = variable.VariableType;

                        var possibleValuesForArg = GetPossibleValues(paramType);
                        if (possibleValuesForArg != null)
                            return possibleValuesForArg;
                    }
                }
            }

            // If we're just typing the first word, suggest commands
            if (input_parts.Length == 1 && !endsWithWhitespace)
            {
                if (commands.Contains(input_parts.Last()))
                    return commands.Where(cmd => cmd != input_parts.Last()).ToList();
                return commands;
            }

            return null;
        }
        private List<string> GetPossibleValues(Type arg_type)
        {
            // If boolean, return enum values as suggestions
            if (arg_type != null && arg_type == typeof(bool))
                return new(2) { "true", "false"};

            // If enum, return enum values as suggestions
            if (arg_type != null && arg_type.IsEnum)
                return Enum.GetNames(arg_type).Select(name => name.ToLower()).ToList();

            return null;
        }
        private string GetHighlightedUsage(IConsoleCommand command, string[] input_parts, bool endsWithWhitespace)
        {
            string usage_text = command.GetUsage();
            string[] usage_segments = usage_text.Split(' ');

            //Skip highlighting if:
            //The command does not take any arguments
            //Or if we are typing in too many arguments
            if (usage_segments?.Length <= 1 || input_parts.Length > usage_segments.Length) return command.GetUsage(); 

            //Get target argument types
            Type[] arg_types = null;
            if(command is ConsoleMethod conMethod)
            {
                arg_types = conMethod.MethodInfo.GetParameters()?.Select((p, i) => p.ParameterType).ToArray();
            }
            else if (command is ConsoleVariable conVar && conVar.CanBeSet)
            {
                arg_types = new Type[1] { conVar.VariableType };
            }


            if (arg_types?.Length == 0) return command.GetUsage();

            //First, calculate which part, of this usage hint is being typed out right now
            int inProgressSegment = endsWithWhitespace? input_parts.Length: input_parts.Length - 1;

            //Now go through each usage segment, and color code it based on weather its being typed out, and its validity
            //Ignore the first segment tho (command name)
            for (int i = 1; i < usage_segments.Length; i++)
            {
                string color = null;

                //Color code the segment that is in progress with yellow
                if (i == inProgressSegment)
                {
                    color = "yellow";
                }
                //Color code all segments that have a corresponding value in our input based on input type validity
                //(they are before the seg that is being typed out)
                else if (i<inProgressSegment)
                {
                    //We will check if we can successfully cast them to the needed type.
                    string value = input_parts[i];
                    bool isValid = CommandParser.TryCastArgument(value, arg_types[i-1], out _);
                    color = isValid ? "#89CA9A" : "#FF000D";
                }

                //Add the color with rich text
                if(color!=null)
                    usage_segments[i] = $"<color={color}>{usage_segments[i]}</color>";
            }
            
            

            return string.Join(' ', usage_segments);
        }

        public virtual IEnumerable<string> FilterSuggestionSpace(IEnumerable<string> suggestionSpace, string raw_input, string[] input_parts)
        {
            if (suggestionSpace == null)
                return null;

            string query_word = input_parts.Last();


            if (raw_input.EndsWith(' '))
                return OrderSuggestions(suggestionSpace, "");
            else
                return OrderSuggestions(suggestionSpace.Where(cmd => cmd.StartsWith(query_word)), query_word);
        }

        public virtual IEnumerable<string> OrderSuggestions(IEnumerable<string> source, string query_word)
        {
            return source.OrderByDescending(s => s.StartsWith(query_word)) // Exact prefix match first
                .ThenBy(s => s.Length) // Shortest first (likely most relevant)
                .ThenBy(s => s); // Then alphabetical
        }
    }

}
