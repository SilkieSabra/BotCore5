using Newtonsoft.Json;
using OpenMetaverse;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Bot.Visitors
{
    public sealed class VisitorLog
    {
        private static readonly object lck = new object();
        private static VisitorLog l;
        static VisitorLog() { }

        public static VisitorLog Instance
        {
            get
            {
                if (l != null) return l;
                else
                {
                    lock (lck)
                    {
                        if (l == null) l = LoadMemory();
                        return l;
                    }
                }
            }
        }

        private static readonly object fileLock = new object();

        public static VisitorLog LoadMemory()
        {
            lock (fileLock)
            {
                if (File.Exists("VisitorLog.json"))
                    return JsonConvert.DeserializeObject<VisitorLog>(File.ReadAllText("VisitorLog.json"));
                else return new VisitorLog();
            }
        }

        public static void SaveMemory()
        {
            lock (fileLock)
            {
                File.WriteAllText("VisitorLog.json", JsonConvert.SerializeObject(l, Formatting.Indented));
            }
        }

        public List<Visitor> Visitors { get; set; } = new List<Visitor>();
    }

    public class Visitor
    {
        public UUID ID { get; set} = UUID.Zero;
        public string username { get; set; } = "first.last";
        public DateTime LastSeen { get; set; } = DateTime.Now;
        public Dictionary<DateTime, TimeSpan> MinutesSeenPerDay { get; set; } = new Dictionary<DateTime, TimeSpan>();


        internal Visitor(UUID kID)
        {
            ID = kID;
            username = "";
            LastSeen = DateTime.Now;
            MinutesSeenPerDay = new Dictionary<DateTime, TimeSpan>();
            MinutesSeenPerDay.Add(DateTime.Today, TimeSpan.FromMinutes(0));
        }
    }
}
