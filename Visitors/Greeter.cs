using Bot.CommandSystem;
using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using System.Linq;
using System.Threading;

namespace Bot.Visitors
{
    class Greeter : BaseCommands, IProgram
    {
        [CommandGroup("greet", 5, "greet [array:Message] - Greets any new visitors to the parcel", Destinations.DEST_AGENT | Destinations.DEST_GROUP | Destinations.DEST_LOCAL)]
        public void enable_greeter_and_set_msg
                            (UUID client, int level, string[] additionalArgs,
                            Destinations source,
                            UUID agentKey, string agentName)
        {
            List<string> greeterMsg = additionalArgs.ToList();
            string msg = string.Join(' ', greeterMsg);


            MainConfiguration.Instance.GreeterEnabled = true;
            MainConfiguration.Instance.GreeterMessage = msg;

            MainConfiguration.Instance.Save();

            MHE(source, client, "Greeter enabled, and message updated. The greeter will only greet for the current parcel the bot is standing in");
        }


        [CommandGroup("visitor_log_on", 5, "visitor_log_on - Enables the visitor log", Destinations.DEST_AGENT | Destinations.DEST_GROUP | Destinations.DEST_LOCAL)]
        public void enable_visitor_log
                            (UUID client, int level, string[] additionalArgs,
                            Destinations source,
                            UUID agentKey, string agentName)
        {
            MainConfiguration.Instance.VisitorLogEnabled = true;
            MainConfiguration.Instance.Save();
            MHE(source, client, "Visitor Logging enabled");
        }
        [CommandGroup("visitor_log_off", 5, "visitor_log_off - Disables the visitor log", Destinations.DEST_AGENT | Destinations.DEST_GROUP | Destinations.DEST_LOCAL)]
        public void disable_visitor_log
                            (UUID client, int level, string[] additionalArgs,
                            Destinations source,
                            UUID agentKey, string agentName)
        {
            MainConfiguration.Instance.VisitorLogEnabled = false;
            MainConfiguration.Instance.Save();
            MHE(source, client, "Visitor Logging disabled");
        }


        [CommandGroup("greet_off", 5, "greet_off - Turns off the greeter", Destinations.DEST_AGENT | Destinations.DEST_GROUP | Destinations.DEST_LOCAL)]
        public void disable_greeter
                            (UUID client, int level, string[] additionalArgs,
                            Destinations source,
                            UUID agentKey, string agentName)
        {


            MainConfiguration.Instance.GreeterEnabled = false;
            MainConfiguration.Instance.GreeterMessage = "";

            MainConfiguration.Instance.Save();
            MHE(source, client, "Greeter has been disabled");
        }



        [CommandGroup("dump_visitors", 5, "dump_visitors", Destinations.DEST_AGENT | Destinations.DEST_GROUP | Destinations.DEST_LOCAL)]
        public void dump_visitors
                            (UUID client, int level, string[] additionalArgs,
                            Destinations source,
                            UUID agentKey, string agentName)
        {
            // dump the visitors
            foreach(Visitor v in VisitorLog.Instance.Visitors)
            {

                List<TimeSpan> Avgs = new List<TimeSpan>();
                foreach (KeyValuePair<DateTime,TimeSpan> kvvp in v.MinutesSeenPerDay)
                {
                    Avgs.Add(kvvp.Value);
                }
                double avg = Avgs.Average(ts => ts.TotalHours);


                MHE(Destinations.DEST_AGENT, agentKey, $"GREETED: {v.Greeted}\nSLURL: secondlife:///app/agent/{v.ID}/about\nLastSeen: {v.LastSeen}\nIs member of my active group: {v.IsMemberOfMyActiveGroup}\nAverage Time on sim: {avg} hours over a period of {v.MinutesSeenPerDay.Count} days, not necessarily consecutive.");
            }
        }


        [CommandGroup("dump_visitors_local", 5, "dump_visitors_local", Destinations.DEST_AGENT | Destinations.DEST_GROUP | Destinations.DEST_LOCAL)]
        public void dump_visitors_local
                            (UUID client, int level, string[] additionalArgs,
                            Destinations source,
                            UUID agentKey, string agentName)
        {
            // dump the visitors
            foreach (Visitor v in VisitorLog.Instance.Visitors)
            {

                List<TimeSpan> Avgs = new List<TimeSpan>();
                foreach (KeyValuePair<DateTime, TimeSpan> kvvp in v.MinutesSeenPerDay)
                {
                    Avgs.Add(kvvp.Value);
                }
                double avg = Avgs.Average(ts => ts.TotalHours);


                MHE(Destinations.DEST_LOCAL, agentKey, $"GREETED: {v.Greeted}\nSLURL: secondlife:///app/agent/{v.ID}/about\nLastSeen: {v.LastSeen}\nIs member of my active group: {v.IsMemberOfMyActiveGroup}\nAverage Time on sim: {avg} hours over a period of {v.MinutesSeenPerDay.Count} days, not necessarily consecutive.");
            }
        }


        [CommandGroup("rm_non_group_visitors", 5, "rm_non_group_visitors", Destinations.DEST_AGENT | Destinations.DEST_GROUP | Destinations.DEST_LOCAL)]
        public void rm_non_group_vis
                            (UUID client, int level, string[] additionalArgs,
                            Destinations source,
                            UUID agentKey, string agentName)
        {
            List<Visitor> toRemove = new List<Visitor>();
            // dump the visitors
            foreach (Visitor v in VisitorLog.Instance.Visitors)
            {

                if (v.IsMemberOfMyActiveGroup) continue;
                else
                {
                    toRemove.Add(v);
                }
            }

            foreach(Visitor v in toRemove)
            {
                VisitorLog.Instance.Visitors.Remove(v);
                VisitorLog.SaveMemory();
            }

            MHE(source, client, "Operations complete");
        }


        private ManualResetEvent WaitForInventory = new ManualResetEvent(false);
        private UUID FoundInventoryItem = UUID.Zero;

        public string ProgramName => "Greeter";

        public float ProgramVersion => 1.0f;

        [CommandGroup("set_give_on_greet", 5, "set_give_on_greet [array:InventoryPath] - Finds and sets the inventory item to give when greeting someone", Destinations.DEST_AGENT | Destinations.DEST_GROUP | Destinations.DEST_LOCAL)]
        public void set_greeter_item
                            (UUID client, int level, string[] additionalArgs,
                            Destinations source,
                            UUID agentKey, string agentName)
        {

            List<string> greeterMsg = additionalArgs.ToList();
            string sItem = string.Join(' ', greeterMsg);

            MHE(source, client, "Searching for: " + sItem);
            BotSession.Instance.grid.Inventory.FindObjectByPathReply += Inventory_FindObjectByPathReply;
            WaitForInventory.Reset();

            FoundInventoryItem = BotSession.Instance.grid.Inventory.FindObjectByPath(BotSession.Instance.grid.Inventory.Store.RootFolder.UUID, BotSession.Instance.grid.Self.AgentID, sItem, 10000);



            if(FoundInventoryItem == UUID.Zero)
            {
                MHE(source, client, "Nothing found");
                return;
            }

            if (WaitForInventory.WaitOne(5001))
            {

                MHE(source, client, "Timed out on searching inventory for that item");
            }
            // check if the item has been cached
            if (BotSession.Instance.grid.Inventory.Store.Contains(FoundInventoryItem) && FoundInventoryItem != UUID.Zero)
            {
                MHE(source, client, "Inventory item found. Sending you a copy to ensure it is correct!");
                InventoryNode item = BotSession.Instance.grid.Inventory.Store.Items[FoundInventoryItem];
                BotSession.Instance.grid.Inventory.GiveItem(FoundInventoryItem, item.Data.Name, AssetType.Unknown, client, true);
                return;
            }
        }

        private void Inventory_FindObjectByPathReply(object sender, FindObjectByPathReplyEventArgs e)
        {
            FoundInventoryItem = e.InventoryObjectID;
            BotSession.Instance.grid.Inventory.FindObjectByPathReply -= Inventory_FindObjectByPathReply;
            BotSession.Instance.grid.Inventory.RequestFetchInventory(FoundInventoryItem, BotSession.Instance.grid.Self.AgentID);
            
        }
        TimeSpan tickCounter = new TimeSpan();
        public void run()
        {

        }

        public void getTick()
        {
            if (MainConfiguration.Instance.VisitorLogEnabled == false) return; // We can't even use the greeter without the log
            // Every 2 seconds, tick
            // cant implement yet- visitor log needed
            if (tickCounter == null)
            {
                tickCounter = new TimeSpan();
                tickCounter = tickCounter.Add(TimeSpan.FromSeconds(2));
            }
            else
            {
                tickCounter=tickCounter.Add(TimeSpan.FromSeconds(2));
            }


            if (tickCounter > TimeSpan.FromMinutes(1))
            {
                tickCounter = new TimeSpan();
                // update everyones' seen minute count who is on sim
                Dictionary<UUID, Vector3> dict = BotSession.Instance.grid.Network.CurrentSim.AvatarPositions.Copy();

                foreach (KeyValuePair<UUID, Vector3> kvp in dict)
                {
                    if (VisitorLog.Instance.Visitors.Where(x => x.ID == kvp.Key).Count() > 0)
                    {

                        Visitor V = VisitorLog.Instance.Visitors.Where(x => x.ID == kvp.Key).First();
                        int index = VisitorLog.Instance.Visitors.IndexOf(V);
                        V.LastSeen = DateTime.Now;
                        // get today's entry if it exists
                        if (V.MinutesSeenPerDay.ContainsKey(DateTime.Today))
                        {
                            V.MinutesSeenPerDay[DateTime.Today] = V.MinutesSeenPerDay[DateTime.Today].Add(TimeSpan.FromMinutes(1));
                        }
                        else
                        {
                            V.MinutesSeenPerDay.Add(DateTime.Today, TimeSpan.FromMinutes(1));
                        }
                        if (!V.IsMemberOfMyActiveGroup)
                        {
                            UUID botgroup = BotSession.Instance.grid.Self.ActiveGroup;

                            if (BotSession.Instance.grid.Network.CurrentSim.ObjectsAvatars.Copy().Where(x => x.Value.ID == kvp.Key).Count() > 0)
                            {
                                // the avatar's instance was found
                                Avatar avi = BotSession.Instance.grid.Network.CurrentSim.ObjectsAvatars.Copy().Where(x => x.Value.ID == kvp.Key).First().Value;

                                if(botgroup == avi.GroupID || avi.Groups.Contains(botgroup))
                                {
                                    V.IsMemberOfMyActiveGroup = true;
                                }
                            }
                        }
                        VisitorLog.Instance.Visitors[index] = V;
                    }
                    else
                    {
                        // does not yet contain this visitor
                        Visitor V = new Visitor(kvp.Key);

                        VisitorLog.Instance.Visitors.Add(V);
                    }



                    if (MainConfiguration.Instance.GreeterEnabled)
                    {
                        Visitor V = VisitorLog.Instance.Visitors.Where(x => x.ID == kvp.Key).First();
                        int index = VisitorLog.Instance.Visitors.IndexOf(V);
                        if (!V.Greeted)
                        {
                            MHE(Destinations.DEST_AGENT, V.ID, MainConfiguration.Instance.GreeterMessage);
                            V.Greeted = true;
                            VisitorLog.Instance.Visitors[index] = V;
                            // This is also where the give on greet action will go
                        }
                    }
                }



                VisitorLog.SaveMemory();
            }

        }

        public void passArguments(string data)
        {
        }

        public void LoadConfiguration()
        {
        }

        public void onIMEvent(object sender, InstantMessageEventArgs e)
        {
            BotSession.Instance.grid.Self.IM -= onIMEvent;
        }
    }
}
