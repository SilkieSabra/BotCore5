using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using OpenMetaverse;


namespace Bot.CommandSystem
{
    public class CommandHelp
    {
        Help h;

        public bool hasGroupFlag()
        {
            if ((h.dests_allowed & MessageHandler.Destinations.DEST_GROUP) == MessageHandler.Destinations.DEST_GROUP) return true;
            else return false;
        }

        public static readonly string NoAdditionalArguments = "This command does not take any arguments";
        public struct Help
        {
            public string Name;
            public int minLevel;
            public int args;
            public string Text;
            public string sources;
            public MessageHandler.Destinations dests_allowed;
        }
        public string GetUsage()
        {
            return "_\nCommand [" + h.Name + "]\n" + h.sources + "\nMinimum Level Required [" + h.minLevel.ToString() + "]\nTotal Arguments [" + h.args.ToString() + "]\nUsage: " + h.Text;

        }
        public string RawUsage()
        {
            return "Usage: " + h.Text;
        }
        public CommandHelp(string CmdName, int minLevel, int argCount, string HelpText, MessageHandler.Destinations DESTS)
        {
            h = new Help();
            string Applicable = "Command can be used in [";
            if ((DESTS & MessageHandler.Destinations.DEST_LOCAL) == MessageHandler.Destinations.DEST_LOCAL) Applicable += "Local, ";
            if ((DESTS & MessageHandler.Destinations.DEST_AGENT) == MessageHandler.Destinations.DEST_AGENT) Applicable += "IM, ";
            if ((DESTS & MessageHandler.Destinations.DEST_GROUP) == MessageHandler.Destinations.DEST_GROUP) Applicable += "Group, ";
            if ((DESTS & MessageHandler.Destinations.DEST_DISCORD) == MessageHandler.Destinations.DEST_DISCORD) Applicable += "Discord, ";

            if (Applicable.Substring(Applicable.Length - 1, 1) == " ") Applicable = Applicable.Substring(0, Applicable.Length - 2) + "]";

            h.dests_allowed = DESTS;
            h.args = argCount;
            h.Name = CmdName;
            h.minLevel = minLevel;
            if (HelpText == "") HelpText = NoAdditionalArguments;
            h.Text = HelpText;
            h.sources = Applicable;
        }
    }
}
