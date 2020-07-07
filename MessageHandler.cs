using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenMetaverse;
using Newtonsoft.Json;
using System.IO;
using System.Threading;
using Microsoft.VisualBasic;

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
    public class DiscordMiscDataPacket
    {
        public ulong ServerID;
        public ulong ChannelID;
        public bool originateFromDiscord { get; set; } = false;
        public string DiscordUserName;
        public ulong DiscordMessageID;
    }
    public class DiscordMessage : Message
    {
        private string Msg;
        public string ServerName;
        public string ChannelName;
        private UUID SenderID;
        /// <summary>
        /// This is used to store misc data that could be identifying to where the data should go
        /// </summary>
        public DiscordMiscDataPacket PKT;
        
        public override int GetChannel()
        {
            return 0;
        }

        public override string GetMessage()
        {
            return Msg;
        }

        public override Destinations GetMessageSource()
        {
            return Destinations.DEST_DISCORD;
        }

        public override UUID GetSender()
        {
            // This is always UUID.Zero with the bot's discord plugin
            return SenderID;
        }

        public override string GetSenderName()
        {
            return "";
        }

        public override UUID GetTarget()
        {
            return UUID.Zero;
        }

        internal override void set(Destinations dest, string msg, UUID agentID, string senderName, int channel)
        {
            Msg = msg;
            return;
        }

        public DiscordMessage(string Msg, UUID Sender)
        {
            this.Msg = Msg;
            ServerName = "MAP_NOT_KNOWN";
            ChannelName = "MAP_NOT_KNOWN";
            SenderID = Sender;
        }

        public DiscordMessage(string Msg, string Server, string Channel, UUID Sender,DiscordMiscDataPacket pkt)
        {
            this.Msg = Msg;
            ServerName = Server;
            ChannelName = Channel;
            SenderID = Sender;
            PKT = pkt;
        }
    }


    public class MessageFactory
    {

        public static void Post(Destinations dest, string Msg, UUID destID, int chn = 0,string ServerName="MAP_NOT_KNOWN", string ChannelName="MAP_NOT_KNOWN",DiscordMiscDataPacket packet=null)
        {

            Message m = null;

            switch (dest)
            {
                case Destinations.DEST_GROUP:
                    m = new GroupMessage(destID);
                    break;
                case Destinations.DEST_DISCORD:
                    m = new DiscordMessage(Msg,ServerName, ChannelName,destID,packet);
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
        public List<MessageEventArgs> QUEUE = new List<MessageEventArgs>();
        public MessageService()
        {

        }

        public void DoSend(MessageEventArgs args)
        {
            OnMessageEvent(args);
        }

        public static void Dispatch(Message M)
        {
            MessageEventArgs MEA = new MessageEventArgs();
            MEA.Timestamp = DateTime.Now;
            MEA.Msg = M;


            BotSession.Instance.MSGSVC.QUEUE.Add(MEA);
            //BotSession.Instance.MSGSVC.OnMessageEvent(MEA);
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


        public void PopMessage()
        {
            if (QUEUE.Count == 0) return;

            DoSend(QUEUE.First());
            QUEUE.Remove(QUEUE.First());
        }
    }


    public class MessageEventArgs : EventArgs
    {
        public DateTime Timestamp { get; set; }
        public Message Msg { get; set; }
    }



}
