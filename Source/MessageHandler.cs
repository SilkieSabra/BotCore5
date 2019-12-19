
/*
Copyright © 2019 Tara Piccari (Aria; Tashia Redrose)
Licensed under the AGPL-3.0
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenMetaverse;
using Newtonsoft.Json;
using System.IO;
using System.Threading;


namespace Bot
{
    public class MessageHandler
    {
        private List<MessageQueuePacket> MSGQueue = new List<MessageQueuePacket>();
        private List<ActionPacket> ActionQueue = new List<ActionPacket>();
        private List<DiscordAction> DiscordQueue = new List<DiscordAction>();
        public ManualResetEvent GroupJoinWaiter = new ManualResetEvent(false);
        private SysOut Log = SysOut.Instance;


        [Flags]
        public enum Destinations
        {
            DEST_AGENT = 1,
            DEST_GROUP = 2,
            DEST_LOCAL = 4,
            DEST_CONSOLE_DEBUG = 8,
            DEST_CONSOLE_INFO = 16,
            DEST_ACTION = 32,
            DEST_DISCORD = 64
        };

        public struct MessageQueuePacket
        {
            public Destinations Dest;
            public UUID DestID;
            public string Msg;
            public int channel;
        }

        public struct ActionPacket
        {
            public Destinations Dest;
            public string ActionStr;
        }

        public struct DiscordAction
        {
            public string Action;
        }

        public delegate void MessageHandleEvent(MessageHandler.Destinations DType, UUID AgentOrSession, string MSG, int channel = 0);
        public volatile MessageHandleEvent callbacks;
        public void MessageHandle(Destinations DType, UUID AgentOrSession, string MSG, int channel = 0)
        {
            if (DType == Destinations.DEST_ACTION)
            {
                if (MSG == "RESET_QUEUE")
                {
                    ClearQueues();
                    return;
                }
                ActionPacket PKT = new ActionPacket();
                PKT.Dest = DType;
                PKT.ActionStr = MSG;
                ActionQueue.Add(PKT);
                return;
            }
            else if (DType == Destinations.DEST_DISCORD)
            {
                DiscordAction DA = new DiscordAction();
                DA.Action = MSG;
                DiscordQueue.Add(DA);
                return; // Do nothing
            }
            MessageQueuePacket pkt = new MessageQueuePacket();
            pkt.channel = channel;
            pkt.Dest = DType;
            pkt.DestID = AgentOrSession;
            pkt.Msg = MSG;

            if (MSGQueue != null)
                MSGQueue.Add(pkt);
        }


        public void ClearQueues()
        {
            MSGQueue = new List<MessageQueuePacket>();
            DiscordQueue = new List<DiscordAction>();
        }

        public void run(GridClient client)
        {
            // Execute one queue item
            if (MSGQueue.Count == 0) return;
            MessageQueuePacket pkt = MSGQueue.First();
            MSGQueue.RemoveAt(MSGQueue.IndexOf(pkt));
            if (pkt.Dest == Destinations.DEST_AGENT)
            {
                client.Self.InstantMessage(pkt.DestID, "[" + MSGQueue.Count.ToString() + "] " + pkt.Msg);
            }
            else if (pkt.Dest == Destinations.DEST_CONSOLE_DEBUG)
            {
                Log.debug("[" + MSGQueue.Count.ToString() + "] " + pkt.Msg);
            }
            else if (pkt.Dest == Destinations.DEST_CONSOLE_INFO)
            {
                Log.info("[" + MSGQueue.Count.ToString() + "] " + pkt.Msg);
            }
            else if (pkt.Dest == Destinations.DEST_GROUP)
            {
                if (client.Self.GroupChatSessions.ContainsKey(pkt.DestID))
                    client.Self.InstantMessageGroup(pkt.DestID, "[" + MSGQueue.Count.ToString() + "] " + pkt.Msg);
                else
                {
                    GroupJoinWaiter.Reset();
                    client.Groups.ActivateGroup(pkt.DestID);
                    client.Self.RequestJoinGroupChat(pkt.DestID);
                    //callbacks(Destinations.DEST_LOCAL, UUID.Zero, "Attempting to join group chat for secondlife:///app/group/" + pkt.DestID.ToString() + "/about");

                    if (GroupJoinWaiter.WaitOne(TimeSpan.FromSeconds(20), false))
                    {

                        client.Self.InstantMessageGroup(pkt.DestID, "[" + MSGQueue.Count.ToString() + "] " + pkt.Msg);
                    }
                    else
                    {
                        MSGQueue.Add(pkt); // Because we failed to join the group chat we'll tack this onto the end of the queue and try again

                    }
                }
            }
            else if (pkt.Dest == Destinations.DEST_LOCAL)
            {
                client.Self.Chat("[" + MSGQueue.Count.ToString() + "] " + pkt.Msg, pkt.channel, ChatType.Normal);
            }
        }

        public string CheckActions()
        {
            string RETURNStr = "";
            if (ActionQueue.Count == 0) return "NONE";
            else
            {
                RETURNStr = ActionQueue.First().ActionStr;
                ActionQueue.Remove(ActionQueue.First());
                return RETURNStr;
            }
        }

        public string CheckDiscordActions()
        {
            if (DiscordQueue.Count == 0) return "NONE";
            else
            {
                string RET = DiscordQueue.First().Action;
                DiscordQueue.Remove(DiscordQueue.First());
                return RET;
            }
        }
    }
}
