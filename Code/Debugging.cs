using System;
using System.Text;
using UnityEngine;


namespace BOB
{
    /// <summary>
    /// Debugging utility class.
    /// </summary>
    internal static class Debugging
    {
        /// <summary>
        /// Prints a single-line debugging message to the Unity output log.
        /// </summary>
        /// <param name="message">Message to log</param>
        internal static void Message(string message)
        {
            Debug.Log(BOBMod.ModName + ": " + message + ".");
        }


        /// <summary>
        /// Prints an exception message to the Unity output log.
        /// </summary>
        /// <param name="message">Message to log</param>
        internal static void LogException(Exception exception)
        {
            // Use StringBuilder for efficiency since we're doing a lot of manipulation here.
            StringBuilder message = new StringBuilder();

            message.AppendLine("caught exception!");
            message.AppendLine("Exception:");
            message.AppendLine(exception.Message);
            message.AppendLine(exception.Source);
            message.AppendLine(exception.StackTrace);

            // Log inner exception as well, if there is one.
            if (exception.InnerException != null)
            {
                message.AppendLine("Inner exception:");
                message.AppendLine(exception.InnerException.Message);
                message.AppendLine(exception.InnerException.Source);
                message.AppendLine(exception.InnerException.StackTrace);
            }

            // Write to log.
            Message(message.ToString());
        }
    }
}
