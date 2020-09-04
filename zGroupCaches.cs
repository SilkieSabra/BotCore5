/*

Copyright © 2019 Tara Piccari (Aria; Tashia Redrose)
Licensed under the GPLv2

*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bot;
using OpenMetaverse;
using Newtonsoft.Json;
using System.Reflection;
using System.Threading;
using System.IO;



namespace Bot
{
    [Serializable()]
    public class zGroupCaches : IConfig
    {
        public float ConfigVersion { get { return 1.4f; } set { } }
        public string ConfigFor { get { return "GroupCache"; } set { } }
        public List<string> Data { get; set; }

        public UUID GroupID { get; set; }

        [Serializable()]
        public struct GroupMemoryData
        {
            public string RoleName;
            public UUID roleID;
            public string Title;
            public GroupPowers Powers;
            public string GroupName;
        }

        public List<GroupMemoryData> GMD { get; set; }


        public void Save(string CustomName)
        {


            //if (!File.Exists("OpenCollarBot.bdf")) return;
            SerialManager sm = new SerialManager();
            sm.Write<zGroupCaches>("zGroupCache/" + CustomName, this);
            sm = null;
        }

        public static zGroupCaches Reload(string CustomName)
        {
            if (!File.Exists("BotData/zGroupCache/" + CustomName + ".json")) return new zGroupCaches();
            SerialManager sm = new SerialManager();
            zGroupCaches ocb = sm.Read<zGroupCaches>("zGroupCache/" + CustomName);
            if (ocb == null)
            {
                return new zGroupCaches();
            }
            return ocb;
        }
        public zGroupCaches()
        {

            GMD = new List<GroupMemoryData>();
        }
    }
}
