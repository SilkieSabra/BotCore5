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

            BotSession.Instance.grid.Inventory.RequestFindObjectByPath(BotSession.Instance.grid.Inventory.Store.RootFolder.UUID, BotSession.Instance.grid.Self.AgentID, sItem);

            if (WaitForInventory.WaitOne(30001))
            {
                // check if the item has been cached
                if (BotSession.Instance.grid.Inventory.Store.Contains(FoundInventoryItem) && FoundInventoryItem!=UUID.Zero)
                {
                    MHE(source, client, "Inventory item found. Sending you a copy to ensure it is correct!");
                    InventoryNode item = BotSession.Instance.grid.Inventory.Store.Items[FoundInventoryItem];
                    BotSession.Instance.grid.Inventory.GiveItem(FoundInventoryItem, item.Data.Name, AssetType.Unknown, client, true);
                    return;
                }

                MHE(source, client, "Timed out on searching inventory for that item");
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
            // Every 2 seconds, tick
            // cant implement yet- visitor log needed
            if (tickCounter == null)
            {
                tickCounter = new TimeSpan();
                tickCounter.Add(TimeSpan.FromSeconds(2));
            }


            if(tickCounter > TimeSpan.FromMinutes(1))
            {
                // update everyones' seen minute count who is on sim


            }
        }

        public void getTick()
        {
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
