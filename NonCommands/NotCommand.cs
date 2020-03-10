using System;
using System.Collections.Generic;
using System.Text;

namespace Bot.NonCommands
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class NotCommand : Attribute
    {
        public NotCommand()
        {
            // Not Command, this just marks a class
        }
    }
}
