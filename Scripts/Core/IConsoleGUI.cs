namespace Farbod.DeveloperConsole
{
    public interface IConsoleGUI
    {
        /// <summary>
        /// Clears this console's logs.
        /// </summary>
        public void ClearLogs();

        /// <summary>
        /// Create a new entry in the log view.
        /// </summary>
        /// <param name="text">The text of this entry</param>
        /// <param name="logType">How this entry is displayed</param>
        public void CreateLogEntry(string text, ConsoleLogType logType = ConsoleLogType.standard);
    }
}
