﻿using Bot.CommandSystem;
using OpenMetaverse;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bot
{
    public sealed class ChatLogger
    {
        private static readonly object writelock = new object();
        private static ChatLogger inst = null;
        static ChatLogger() { }
        public static ChatLogger Instance
        {
            get
            {
                if (inst != null) return inst;
                else
                {
                    if (inst == null)
                    {
                        inst = new ChatLogger();
                    }

                    return inst;
                }
            }
        }



        /// <summary>
        /// Debug function to log chat and IMs incase of errors.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="level"></param>
        /// <param name="grid"></param>
        /// <param name="additionalArgs"></param>
        /// <param name="MHE"></param>
        /// <param name="source"></param>
        /// <param name="registry"></param>
        /// <param name="agentKey"></param>
        /// <param name="agentName"></param>
        [CommandGroup("log_chat", 5, 0, "log_chat - Toggles chat and IM logging", MessageHandler.Destinations.DEST_AGENT | MessageHandler.Destinations.DEST_LOCAL | MessageHandler.Destinations.DEST_CONSOLE_INFO)]
        public void toggleChatLog(UUID client, int level, GridClient grid, string[] additionalArgs, MessageHandler.MessageHandleEvent MHE, MessageHandler.Destinations source, CommandRegistry registry, UUID agentKey, string agentName)
        {
            MHE(source, client, "Toggling");

            MainConfiguration.Instance.LogChatAndIMs = !MainConfiguration.Instance.LogChatAndIMs;
            MainConfiguration.Instance.Save();

            MHE(source, client, "Logging is now set to: " + MainConfiguration.Instance.LogChatAndIMs.ToString());
        }


    }
}