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
        public Destinations SourceType = Destinations.DEST_AGENT | Destinations.DEST_DISCORD | Destinations.DEST_GROUP | Destinations.DEST_LOCAL;

        public NotCommand()
        {
            // Not Command, this just marks a class
        }

    }
}
