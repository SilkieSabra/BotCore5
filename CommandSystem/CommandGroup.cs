using System;
using System.Reflection;


namespace Bot.CommandSystem
{

    [System.AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class CommandGroup : Attribute
    {
        public string Command;
        public int minLevel;
        public MethodInfo AssignedMethod;
        public CommandHelp cmdUsage;
        public Destinations CommandSource;

        public CommandGroup(string Command, int minLevel, string HelpText, Destinations SourceType)
        {
            this.Command = Command;
            this.minLevel = minLevel;
            CommandSource = SourceType;
            cmdUsage = new CommandHelp(Command, minLevel, HelpText, SourceType);

        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class CommandGroupMaster : Attribute
    {
        public string CommandGroupName;
        public CommandGroupMaster(string CmdGroupName)
        {
            CommandGroupName = CmdGroupName;
        }
    }
}
