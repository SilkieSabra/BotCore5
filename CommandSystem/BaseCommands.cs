using OpenMetaverse;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bot.CommandSystem
{
    public class BaseCommands
    {

        public void MHE(Destinations dest, UUID client, string msg)
        {
            MessageFactory.Post(dest, msg, client);
        }
        public static void MH(Destinations dest, UUID client, string msg)
        {
            MessageFactory.Post(dest, msg, client);
        }
    }
}
