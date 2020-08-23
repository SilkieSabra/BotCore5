using System;
using System.Collections.Generic;
using System.Text;
using Bot.CommandSystem;
using Bot.NonCommands;
using OpenMetaverse;

namespace Bot
{
    /// <summary>
    /// A extremely basic implementation of RLV just to pass checks on whether it is active or not in the viewer
    /// </summary>
    class RLV : BaseCommands, nCMD
    {
        [NotCommand(SourceType = Destinations.DEST_LOCAL)]
        public void handle(string text, UUID User, string agentName, Destinations src, UUID originator)
        {
            if (text.Substring(0, 1) == "@")
            {
                string[] arguments = text.Substring(1).Split(new[] { ':', '=' });
                if(arguments[0] == "version")
                {
                    BotSession.Instance.grid.Self.Chat("RLV 0", Convert.ToInt32(arguments[1]), ChatType.RegionSay);
                } else if(arguments[0] == "versionnew")
                {
                    BotSession.Instance.grid.Self.Chat("RLV 0", Convert.ToInt32(arguments[1]), ChatType.RegionSay);
                }
            }
        }
    }
}
