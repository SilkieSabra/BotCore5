/*

Copyright © 2019 Tara Piccari (Aria; Tashia Redrose)
Licensed under the GPLv2

*/

using OpenMetaverse;
using Bot;
using Bot.CommandSystem;



namespace Bot
{
    class Auth
    {

        [CommandGroup("auth_user", 5, 2, "Authorizes a user to have command access. Arguments are user (UUID), and Level (int)", MessageHandler.Destinations.DEST_AGENT | MessageHandler.Destinations.DEST_LOCAL)]
        public void set_auth(UUID client, int level, GridClient grid, string[] additionalArgs,
                                MessageHandler.MessageHandleEvent MHE, MessageHandler.Destinations source,
                                CommandRegistry registry, UUID agentKey, string agentName)
        {
            MainConfiguration mem = MainConfiguration.Instance;
            BotSession.Instance.Logger.info(log:"Existing Admins: " + mem.BotAdmins.Count.ToString());
            if (level < 5 && mem.BotAdmins.Count > 0)
            {
                MHE(source, UUID.Zero, "Authorization failure. You do not have the proper permission level");
                //grid.Self.Chat(, 0, ChatType.Normal);
                return;
            }
            MHE(source, UUID.Zero, "Authorizing..");
            //grid.Self.Chat("Authorizing user..", 0, ChatType.Normal);
            UUID user = UUID.Parse(additionalArgs[0]);
            int NewLevel = int.Parse(additionalArgs[1]);
            if (NewLevel <= 0)
            {
                mem.BotAdmins.Remove(user);
                MHE(MessageHandler.Destinations.DEST_AGENT, user, "Your access to the main bot has been removed. You will still have access to any command that does not require a access level higher than 0");
                MHE(MessageHandler.Destinations.DEST_LOCAL, UUID.Zero, "Access Removed");
                mem.Save();
                return;
            }

            if (NewLevel > level && mem.BotAdmins.Count > 0)
            {
                MHE(source, client, "Cannot authorize higher than your own level");
                return;
            }


            if (!mem.BotAdmins.ContainsKey(user))
                mem.BotAdmins.Add(user, NewLevel);
            else
                mem.BotAdmins[user] = NewLevel;
            MHE(MessageHandler.Destinations.DEST_AGENT, user, "You have been granted authorization level " + NewLevel.ToString());
            MHE(source, UUID.Zero, "Authorized");
            mem.Save();

        }
    }
}
