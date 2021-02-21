using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bot;
using Bot.CommandSystem;
using OpenMetaverse;

namespace BotCore5.NonCommands
{
    public class IgnoreNonCommands : BaseCommands
    {
        [CommandGroup("ignorethisgroup", 5, "ignorethisgroup - Ignores the group this command was issued in", Destinations.DEST_GROUP)]
        public void ignoreMe(UUID client, int level, string[] additionalArgs,
                            Destinations source,
                            UUID agentKey, string agentName)
        {
            // Check the main config for this value already being there
            // If it is not there, add it. Otherwise give the error and dont modify it
            if (MainConfiguration.Instance.IgnoreGroups.Contains(client))
            {
                MHE(source, client, "ERROR: This group is already being ignored for non-commands.");
            }
            else
            {
                MainConfiguration.Instance.IgnoreGroups.Add(client);
                MainConfiguration.Instance.Save();
                MHE(source, client, "Success. Any message that is not a command will now be ignored by all bot operations in this group");
            }
        }


        [CommandGroup("unignorethisgroup", 5, "unignorethisgroup - Ignores the group this command was issued in", Destinations.DEST_GROUP)]
        public void UnignoreMe(UUID client, int level, string[] additionalArgs,
                            Destinations source,
                            UUID agentKey, string agentName)
        {
            if (MainConfiguration.Instance.IgnoreGroups.Contains(client))
            {
                MainConfiguration.Instance.IgnoreGroups.Remove(client);
                MainConfiguration.Instance.Save();
                MHE(source, client, "Success. The bot will no longer ignore non-commands from this group, such as auto responses.");
            }
            else
            {
                MHE(source, client, "ERROR: This group was not being ignored");
            }
        }
    }
}
