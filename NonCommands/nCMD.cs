using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;



namespace Bot.NonCommands
{
    public interface nCMD
    {
        public void handle(string text, UUID User, string agentName, MessageHandler.Destinations src, UUID originator);
    }
}
