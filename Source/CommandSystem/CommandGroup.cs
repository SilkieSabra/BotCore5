/*
Copyright © 2019 Tara Piccari (Aria; Tashia Redrose)
Licensed under the AGPL-3.0
*/



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
        public int arguments = 0;
        public CommandHelp cmdUsage;
        public MessageHandler.Destinations CommandSource;

        public CommandGroup(string Command, int minLevel, int argCount, string HelpText, MessageHandler.Destinations SourceType)
        {
            this.Command = Command;
            this.minLevel = minLevel;
            arguments = argCount;
            CommandSource = SourceType;
            cmdUsage = new CommandHelp(Command, minLevel, argCount, HelpText, SourceType);

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
