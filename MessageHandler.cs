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
    /// <summary>
    /// Message stuff!
    /// </summary>
    [Flags]
    public enum Destinations
    {
        DEST_AGENT = 1,
        DEST_GROUP = 2,
        DEST_LOCAL = 4,
        //DEST_CONSOLE_INFO = 8,
        //DEST_ACTION = 16, // DEPRECATED
        DEST_DISCORD = 32
    };
    /// <summary>
    /// Message stuffs
    /// </summary>
    public abstract class Message
    {
        public abstract Destinations GetMessageSource();
        public abstract string GetMessage();
        public abstract UUID GetSender();
        public abstract UUID GetTarget();
        public abstract string GetSenderName();
        public abstract int GetChannel();
        internal abstract void set(Destinations dest, string msg, UUID agentID,string senderName, int channel);
    }
    /// <summary>
    /// IM, local, whatever
    /// </summary>
    public class ChatMessage : Message
    {
        private string Msg;
        private Destinations dest;
        private UUID sender;
        private UUID target;
        private string senderName;
        private int Chn;

        public override UUID GetTarget()
        {
            return target;
        }

        public override int GetChannel()
        {
            return Chn;
        }

        public override string GetMessage()
        {
            return Msg;
        }

        public override Destinations GetMessageSource()
        {
            return dest;
        }

        public override UUID GetSender()
        {
            return sender;
        }

        public override string GetSenderName()
        {
            return senderName;
        }

        internal override void set(Destinations dest, string msg, UUID agentID,string senderName, int channel)
        {
            this.dest = dest;
            Msg = msg;
            sender = agentID;
            this.senderName = senderName;
            Chn = channel;
        }

        public ChatMessage(UUID targetID)
        {
            target = targetID;
        }
    }
    /// <summary>
    /// Includes methods specific to the Group
    /// </summary>
    public class GroupMessage : ChatMessage
    {
        private UUID GroupID;
        private string groupName;
        public UUID GetGroupID()
        {
            return GroupID;
        }
        public string GetGroupName()
        {
            return groupName;
        }
        public GroupMessage(UUID ID) : base(ID)
        {
            GroupID = ID;
        }
    }


    public class MessageFactory
    {

        public static void Post(Destinations dest, string Msg, UUID destID, int chn = 0)
        {

            Message m = null;

            switch (dest)
            {
                case Destinations.DEST_GROUP:
                    m = new GroupMessage(destID);
                    break;
                case Destinations.DEST_DISCORD:
                    m = new ChatMessage(UUID.Zero);
                    break;
                default:
                    m = new ChatMessage(destID);
                    break;
            }

            m.set(dest, Msg, BotSession.Instance.grid.Self.AgentID, BotSession.Instance.grid.Self.Name, chn);

            MessageService.Dispatch(m);
        }
    }


    /// <summary>
    /// Basic messaging factory
    /// </summary>
    public class MessageService
    {
        public MessageService()
        {

        }

        public static void Dispatch(Message M)
        {
            MessageEventArgs MEA = new MessageEventArgs();
            MEA.Timestamp = DateTime.Now;
            MEA.Msg = M;

            BotSession.Instance.MSGSVC.OnMessageEvent(MEA);
        }

        protected virtual void OnMessageEvent(MessageEventArgs e)
        {
            EventHandler<MessageEventArgs> handler = MessageEvent;
            if (handler != null)
            {
                handler(this, e);
            }
        }
        public event EventHandler<MessageEventArgs> MessageEvent;
    }


    public class MessageEventArgs : EventArgs
    {
        public DateTime Timestamp { get; set; }
        public Message Msg { get; set; }
    }



    public class MessageHandler_old // keep the old structure for now
    {
        private List<MessageQueuePacket> MSGQueue = new List<MessageQueuePacket>();
        private List<ActionPacket> ActionQueue = new List<ActionPacket>();
        private List<DiscordAction> DiscordQueue = new List<DiscordAction>();
        public ManualResetEvent GroupJoinWaiter = new ManualResetEvent(false);
        private Logger Log = BotSession.Instance.Logger;


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

        public delegate void MessageHandleEvent(Destinations DType, UUID AgentOrSession, string MSG, int channel = 0);
        public volatile MessageHandleEvent callbacks;
        public void MessageHandle(Destinations DType, UUID AgentOrSession, string MSG, int channel = 0)
        {
            if (DType == Destinations.DEST_DISCORD)
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
