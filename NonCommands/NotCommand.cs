using System;
using System.Collections.Generic;
using System.Text;

namespace Bot.NonCommands
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class NotCommand : Attribute
    {
        /// <summary>
        /// Defaults to all except action
        /// </summary>
        public MessageHandler.Destinations SourceType = MessageHandler.Destinations.DEST_AGENT | MessageHandler.Destinations.DEST_CONSOLE_INFO | MessageHandler.Destinations.DEST_DISCORD | MessageHandler.Destinations.DEST_GROUP | MessageHandler.Destinations.DEST_LOCAL;
        public NotCommand()
        {
            // Not Command, this just marks a class
        }
    }
}
