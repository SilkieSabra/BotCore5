using Bot.CommandSystem;
using Newtonsoft.Json;
using OpenMetaverse;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using OpenMetaverse.Packets;
using Bot.Assemble;
using OpenMetaverse.Interfaces;
using System.Security.Cryptography;

namespace Bot
{
    public class Program
    {
        public static SysOut Log = SysOut.Instance;
        public static double BotVer = ASMInfo.BotVer;
        public static string BotStr = ASMInfo.BotName; // internal identifier for linden
        public static MainConfiguration conf;
        public static string Flavor = "Bot"; // inworld identification - must be customized
        public static SerialManager SM = new SerialManager();
        public static string DefaultProgram = "BlankBot.dll"; // default bot - blank will only contain the commands to switch programs. It is a complete blank!
        public static GridClient client = new GridClient();
        public static bool g_iIsRunning = true;
        public static MessageHandler MH;
        public static CommandRegistry registry;
        public static List<IProgram> g_ZPrograms = new List<IProgram>();

        static readonly object _CacheLock = new object();
        //public static License LicenseKey; // Not to be used yet

        public static void msg(MessageHandler.Destinations D, UUID x, string m)
        {
            MH.callbacks(D, x, m);
        }
        public static unsafe void Main(string[] args)
        {
            Console.WriteLine("Setting up Main Configuration");
            ZHash.Instance.NewKey();
            ZHash.Instance.Key = "Test";
            Console.WriteLine("ZHash (Test): " + ZHash.Instance.Key);
            conf = MainConfiguration.Load();
            //MasterObjectCaches = ObjectCaches.Instance;
            Log.debugf(true, "main", args);

            if (args.Length == 2)
            {
                // Check if this is activation command
                if (args[0] == "-a")
                {
                    conf.ActivationCode = args[1];
                    SM.Write<MainConfiguration>("Main", conf);
                    return;
                }
                else if (args[0] == "-m")
                {
                    conf.MainProgramDLL = args[1];
                    SM.Write<MainConfiguration>("Main", conf);
                    return;
                }
            }
            else if (args.Length == 4)
            {
                if (args[0] == "-l")
                {
                    conf.first = args[1];
                    conf.last = args[2];
                    conf.password = args[3];
                    SM.Write<MainConfiguration>("Main", conf);
                    return;
                }
            }
            // Initiate bot login
            // Main thread must be caught in the bot loop so it does not terminate early.
            // Other programs may hook into the bot to control it

            /*
            if (conf.ActivationCode != "")
            {
                
                License L = new License();
                L.NewKey();
                L.InitUniqueMachine();
                L.Key = conf.ActivationCode;
                License def = new License();
                def.NewKey();
                def.InitUniqueMachine();
                string Reply;
                HttpWebRequest _request = (HttpWebRequest)WebRequest.Create("http://bak.cloud.xsinode.net/act_handle.php?r=verify&act=" + conf.ActivationCode + "&LIC=" + L.Key + "&mac="+def.Key);
                using (HttpWebResponse response = (HttpWebResponse)_request.GetResponse())
                using (Stream str = response.GetResponseStream())
                using (StreamReader sr = new StreamReader(str))
                {
                    Reply = sr.ReadToEnd();
                }

                string[] ReplyData = Reply.Split('|');
                if(ReplyData[0] == "deny")
                {
                    WebClient wc = new WebClient();
                    if (File.Exists("Activator.exe")) File.Delete("Activator.exe");
                    wc.DownloadFile("http://bak.cloud.xsinode.net/znibot/Activator.exe", "Activator.exe");
                    int E_CODE = -1;
                    if (ReplyData[1] == "TOO_MANY") E_CODE = 90;
                    else if (ReplyData[1] == "EXPIRED") E_CODE = 32;
                    else if (ReplyData[1] == "INVALID") E_CODE = 100;
                    else E_CODE = 55;
                    
                    string batchContents = "@echo off" +
                        "\ntimeout 5\n" +
                        "cd " + Directory.GetCurrentDirectory()+"\n" +
                        "Activator.exe -d -m " + E_CODE.ToString();
                    File.WriteAllText("denyAct.bat", batchContents);
                    Process p = Process.Start("denyAct.bat");
                    
                    return;

                } else if(ReplyData[0] == "allow")
                {
                    // Handle expiry on server!
                    // Activate now
                }
                else
                {

                    WebClient wc = new WebClient();
                    if (File.Exists("Activator.exe")) File.Delete("Activator.exe");
                    wc.DownloadFile("http://bak.cloud.xsinode.net/znibot/Activator.exe", "Activator.exe");
                    int E_CODE = 55; // Unknown reply. Activator will not permit logging in until the server can be reached safely.

                    string batchContents = "@echo off" +
                        "\ntimeout 5\n" +
                        "cd " + Directory.GetCurrentDirectory() + "\n" +
                        "Activator.exe -d -m " + E_CODE.ToString();
                    File.WriteAllText("denyAct.bat", batchContents);
                    Process p = Process.Start("denyAct.bat");

                    return;
                }
            }
            else
            {
                Console.WriteLine("ERROR: You must have an activation code set prior to running Bot.exe!!\n \n[Please run Activator with the Confirmation Number]");
                Console.ReadKey();
                return;
            }
            */
            MH = new MessageHandler();
            MH.callbacks += MH.MessageHandle;


            string fna = null;
            string lna = null;
            string pwd = null;
            if (conf.first == null)
            {

                if (args.Length == 0)
                {

                    Log.info("Please enter your avatar's first name:  ");
                    fna = Console.ReadLine();

                    Log.info("Please enter the last name: ");
                    lna = Console.ReadLine();
                    Log.info("Now enter your password: ");
                    pwd = Console.ReadLine();

                    conf.MainProgramDLL = DefaultProgram;
                    conf.ConfigFor = "ZBotCore";
                    conf.ConfigVersion = 1.0f;

                }
                else
                {
                    Log.info("Loading...");
                    Log.info("FirstName: " + args[0]);
                    fna = args[0];
                    lna = args[1];
                    pwd = args[2];

                    // Continue boot
                }
                conf.first = fna;
                conf.last = lna;
                conf.password = pwd;
                SM.Write<MainConfiguration>("Main", conf);
                Log.debug("FirstName in Config: " + conf.first);
            }
            else
            {
                fna = conf.first;
                lna = conf.last;
                pwd = conf.password;
            }

            bool startupSeq = true;

            if (File.Exists("ObjectCache.bdf")) File.Delete("ObjectCache.bdf");
            client.Self.ChatFromSimulator += onChatRecv;
            client.Self.GroupChatJoined += onJoinGroupChat;

            //client.Objects.ObjectUpdate += onObjectUpdate;
            //client.Objects.ObjectProperties += onObjectProperties;

            //client.Network.SimChanged += onSimChange; // Recache prims for this sim


            //client.Objects.TerseObjectUpdate += onObjectTerseUpdate;


            client.Settings.OBJECT_TRACKING = true;
            client.Settings.ALWAYS_DECODE_OBJECTS = true;
            client.Settings.USE_ASSET_CACHE = true;
            client.Throttle.Asset = 100000;
            client.Throttle.Land = 100000;
            client.Throttle.Task = 100000;
            client.Throttle.Total = 100000;

            client.Settings.ALWAYS_REQUEST_OBJECTS = true;

            Console.WriteLine("Logging in...");
            
            bool LoggedIn = client.Network.Login(fna, lna, pwd, BotStr, BotVer.ToString());
            Console.WriteLine("Logged In: " + LoggedIn.ToString());

            if (!LoggedIn)
            {
                Console.WriteLine("Check Creds:\n \nFirst Name: '" + fna + "'\nLast Name: '" + lna + "'\nPWD: '" + pwd + "'\nBotStr: '" + BotStr + "'\nBotVer: " + BotVer.ToString()+"\n \nLogin Message: "+client.Network.LoginMessage);
                
                
                if(args[0] == "-x") // debug launch
                    Console.ReadKey();
            }
            if (LoggedIn)
            {
                if (File.Exists("XUP"))
                {
                    File.Delete("XUP");
                    MH.callbacks(MessageHandler.Destinations.DEST_LOCAL, UUID.Zero, "Updated to version " + BotStr + " - "+BotVer.ToString());
                }
                Log.debugf(true, "SL_NET", new[] { "logged_in" });

                // Setup BotSession Singleton!
                BotSession.Instance.grid = client;
                BotSession.Instance.Logger = Log;
                BotSession.Instance.MHE = MH.callbacks;
                BotSession.Instance.MH = MH;
                BotSession.Instance.ConfigurationHandle = conf;
                while (g_iIsRunning)
                {
                    client.Self.RetrieveInstantMessages();
                    if (client.Network.Connected == false) g_iIsRunning = false; // Quit the program and restart immediately!
                    Thread.Sleep(2000);
                    DirectoryInfo lp = new DirectoryInfo("update");



                    if (lp.Exists) g_iIsRunning = false;

                    if (conf.ConfigFor == "Main")
                    {
                        msg(MessageHandler.Destinations.DEST_LOCAL, UUID.Zero, "Alert: Main.json is not fully initialized. Setting default values");
                        conf.ConfigFor = "BOT";
                        conf.ConfigVersion = 1.0f;
                        // data contains nothing at the moment.
                        SM.Write<MainConfiguration>("Main", conf);
                        conf = null;
                        conf = SM.Read<MainConfiguration>("Main");

                        if (conf.ConfigFor == "BOT")
                        {
                            msg(MessageHandler.Destinations.DEST_LOCAL, UUID.Zero, "Main.json has been created");
                            msg(MessageHandler.Destinations.DEST_LOCAL, UUID.Zero, "Continuing with startup");
                        }
                        else
                        {
                            msg(MessageHandler.Destinations.DEST_LOCAL, UUID.Zero, "Main.json does not contain all memory. FAILURE.");
                            g_iIsRunning = false;
                        }
                    }
                    else
                    {
                        Flavor = conf.ConfigFor;
                    }

                    // Check MainConfiguration for a mainProgram handle
                    if (conf.MainProgramDLL == null)
                    {
                        Log.info("Setting main program library");
                        conf.MainProgramDLL = DefaultProgram;
                        SM.Write<MainConfiguration>("Main", conf);

                    }
                    if (File.Exists(conf.MainProgramDLL) == false)
                    {
                        Log.info("MainProgram Library: " + conf.MainProgramDLL + " does not exist");
                        if (conf.MainProgramDLL == DefaultProgram)
                        {
                            Log.info("FATAL: BlankBot.dll must exist to proceed");
                            msg(MessageHandler.Destinations.DEST_LOCAL, UUID.Zero, "BlankBot.dll does not exist. Please place the blank bot program into the same folder as 'Bot.dll'. Load cannot proceed any further Terminating");

                        }
                        g_iIsRunning = false;
                    }
                    else
                    {
                        if (startupSeq)
                        {
                            registry = CommandRegistry.Instance;
                            //ReloadGroupsCache();
                            Log.info("MainProgram exists");

                            try
                            {
                                int programCount = 0;
                                PluginActivator PA = new PluginActivator();
                                PA.LoadLibrary(conf.MainProgramDLL);
                                List<IProgram> plugins = PA.Activate(PA.LoadedASM);

                                foreach (IProgram plugin in plugins)
                                {

                                    plugin.run(client, MH, registry); // simulate constructor and set up other things
                                    g_ZPrograms.Add(plugin);
                                    client.Self.IM += plugin.onIMEvent;
                                    programCount++;

                                    Log.debug("Plugin: " + plugin.ProgramName + " [" + PA.LoadedASM.FullName + "] added to g_ZPrograms");
                                    if (File.Exists(plugin.ProgramName + ".bdf"))
                                        plugin.LoadConfiguration(); // will throw an error if BlankBot tries to load config
                                }

                                Log.debug(g_ZPrograms.Count.ToString() + " programs linked");
                                if (g_ZPrograms.Count > 0) msg(MessageHandler.Destinations.DEST_LOCAL, UUID.Zero, "Default Program [" + conf.MainProgramDLL + "] has been loaded, " + programCount.ToString() + " plugin(s) loaded");
                                registry.LocateCommands();

                                msg(MessageHandler.Destinations.DEST_LOCAL, UUID.Zero, "Commands found: " + registry.Cmds.Count.ToString());

                            }
                            catch (Exception E)
                            {
                                string Msg = E.Message;
                                string STACK = E.StackTrace.Replace("ZNI", "");
                                Msg = Msg.Replace("ZNI", "");
                                Log.debug("Generic Exception Caught: " + Msg + " [0x0A]");
                                int i;
                                int* ptr = &i;
                                IntPtr addr = (IntPtr)ptr;
                                msg(MessageHandler.Destinations.DEST_LOCAL, UUID.Zero, "Generic Exception Caught: " + Msg + " [0x0A, 0x" + addr.ToString("x") + "]\nSTACK: " + STACK);
                            }
                        }


                    }
                    foreach (IProgram plugin in g_ZPrograms)
                    {
                        plugin.getTick(); // Trigger a tick event!!!
                    }

                    string jsonReply = MH.CheckActions();


                    if (jsonReply == "NONE") jsonReply = "";


                    if (jsonReply == "" || jsonReply == null)
                    {
                        //Log.debug("TICK NULL");

                    }
                    else
                    {
                        Log.debug("TICK REPLY: " + jsonReply);
                        dynamic jsonObj = JsonConvert.DeserializeObject(jsonReply);
                        Log.debug("TYPE: " + jsonObj.type);
                        string tp = jsonObj.type;
                        switch (tp)
                        {
                            case "assignProgram":
                                {

                                    client.Self.Chat("Stand by", 0, ChatType.Normal);
                                    string newProg = jsonObj.newProgram;
                                    if (File.Exists(newProg + ".dll"))
                                    {
                                        conf.MainProgramDLL = jsonObj.newProgram + ".dll";
                                        SM.Write<MainConfiguration>("Main", conf);
                                        client.Self.Chat("Restarting bot using new main program", 0, ChatType.Normal);
                                        g_iIsRunning = false;
                                    }
                                    else
                                    {
                                        client.Self.Chat("Error: Program '" + newProg + ".dll' does not exist.", 0, ChatType.Normal);
                                    }
                                    break;
                                }
                            case "exit":
                                {

                                    Log.info("Logging off!");
                                    g_iIsRunning = false;
                                    break;
                                }
                            case "reload_groups":
                                {
                                    ReloadGroupsCache();
                                    break;
                                }
                            case "load_program":
                                {
                                    msg(MessageHandler.Destinations.DEST_LOCAL, UUID.Zero, "Stand by.. loading secondary libraries");
                                    string newProg = jsonObj.newProgram;
                                    if (File.Exists(newProg + ".dll"))
                                    {
                                        newProg += ".dll";
                                        PluginActivator Plugs = new PluginActivator();
                                        Plugs.LoadLibrary(newProg);
                                        List<IProgram> libs = Plugs.Activate(Plugs.LoadedASM);
                                        int programCount = 0;
                                        foreach (IProgram plugin in libs)
                                        {


                                            plugin.run(client, MH, registry); // simulate constructor and set up other things
                                            g_ZPrograms.Add(plugin);
                                            client.Self.IM += plugin.onIMEvent;
                                            programCount++;
                                            Log.debug("Plugin: " + plugin.ProgramName + " [" + Plugs.LoadedASM.FullName + "] added to g_ZPrograms");
                                            if (File.Exists(plugin.ProgramName + ".bdf"))
                                                plugin.LoadConfiguration(); // will throw an error if BlankBot tries to load config
                                        }

                                        msg(MessageHandler.Destinations.DEST_LOCAL, UUID.Zero, "Loaded plugin " + newProg + " with " + programCount.ToString() + " entry points");

                                        registry.LocateCommands();

                                        msg(MessageHandler.Destinations.DEST_LOCAL, UUID.Zero, "Commands found: " + registry.Cmds.Count.ToString());
                                    }
                                    else
                                    {
                                        msg(MessageHandler.Destinations.DEST_LOCAL, UUID.Zero, "ERROR: " + newProg + " could not be located!");
                                    }
                                    break;
                                }
                            default:
                                {

                                    Log.debug("Unknown response code");
                                    break;
                                }
                        }
                    }

                    MH.run(client);
                    //MasterObjectCaches.Save();
                    if (startupSeq) startupSeq = false;



                    //if (MasterObjectCaches.RegionPrims.Count == 0 && client.Network.Connected)
                    //{

                    //    onSimChange(null, new SimChangedEventArgs(client.Network.CurrentSim));
                    //}
                }

                Log.debugf(false, "SL_NET", new[] { "" });

                client.Network.Logout();
            }


            Log.debugf(false, "main", args);
            //System.Console.WriteLine("PAUSING. PRESS ANY KEY TO EXIT");
            //System.Console.ReadKey();
        }

        private static void onJoinGroupChat(object sender, GroupChatJoinedEventArgs e)
        {
            if (e.Success)
                MH.GroupJoinWaiter.Set();
        }

        private static AutoResetEvent ReqObjProperties = new AutoResetEvent(false);
        private static Dictionary<UUID, Primitive.ObjectProperties> ReqObjPropertiesData = new Dictionary<UUID, Primitive.ObjectProperties>();


        [STAThread()]
        private static void onObjectUpdate(object sender, PrimEventArgs e)
        {
            //Console.WriteLine("ObjectUpdate @ " + DateTime.Now);
            /*
             * Disabled until Libremetaverse is fully tested
             * 
            while (Monitor.IsEntered(_CacheLock)) { }
            lock (_CacheLock)
            {

                if (MasterObjectCaches == null)
                {
                    MasterObjectCaches = ObjectCaches.Instance;
                    Console.WriteLine("\n=> Recv: ObjectUpdate; Set: new MasterObjectCache(" + e.Simulator.Name + ")");
                }
                if (MasterObjectCaches.RegionPrims == null) MasterObjectCaches.RegionPrims = new Dictionary<string, Dictionary<UUID, Primitive2>>();
                Dictionary<UUID, Primitive2> NewDictionary = new Dictionary<UUID, Primitive2>();
                if (!MasterObjectCaches.RegionPrims.ContainsKey(e.Simulator.Name))
                {
                    NewDictionary = new Dictionary<UUID, Primitive2>();
                    try
                    {

                        MasterObjectCaches.RegionPrims.Add(e.Simulator.Name, NewDictionary);
                        MasterObjectCaches.MarkDirty();
                    }
                    catch (Exception E)
                    {
                        Console.WriteLine("FAILED TO INITIALIZE MASTER OBJECT CACHE FOR REGION");
                        Console.WriteLine(E.StackTrace);
                    }
                }
                else
                {
                    NewDictionary = MasterObjectCaches.RegionPrims[e.Simulator.Name];
                }
                Primitive p = e.Prim;
                if (!NewDictionary.ContainsKey(e.Prim.ID))
                {
                    NewDictionary.Add(p.ID, new Primitive2(p));
                    MasterObjectCaches.RegionPrims[e.Simulator.Name] = NewDictionary;
                    MasterObjectCaches.MarkDirty();
                    // Check properties val
                    if (p.Properties == null)
                    {
                        client.Objects.SelectObject(client.Network.CurrentSim, p.LocalID);
                        client.Objects.DeselectObject(client.Network.CurrentSim, p.LocalID);
                    }

                    if (p.OwnerID == client.Self.AgentID)
                    {
                        Console.WriteLine("[!] Discovered a prim that i created\n\n");
                    }
                }
                else
                {
                    // Prim is already in list. 
                    // Verify that the properties are still the same
                    if (p.Properties == null)
                    {
                        client.Objects.SelectObject(client.Network.CurrentSim, p.LocalID);
                        client.Objects.DeselectObject(client.Network.CurrentSim, p.LocalID);
                    }

                }
            }
            */
        }

        [STAThread()]
        private static void onObjectTerseUpdate(object sender, TerseObjectUpdateEventArgs e)
        {
            //Console.WriteLine("TerseObjectUpdate @ " + DateTime.Now);
            PrimEventArgs ex = new PrimEventArgs(e.Simulator, e.Prim, e.TimeDilation, false, false);
            onObjectUpdate(sender, ex);
        }

        [STAThread()]
        private static void onObjectProperties(object sender, ObjectPropertiesEventArgs e)
        {
            //Console.WriteLine("ObjectProperties @ " + DateTime.Now);
            //Console.WriteLine("\n=> Got prim properties <=\n");
            /*
            Dictionary<UUID, Primitive2> PrimList = MasterObjectCaches.RegionPrims[e.Simulator.Name];
            UUID id = e.Properties.ObjectID;
            if (PrimList.ContainsKey(id))
            {

                Primitive2 prim = PrimList[id];
                if (prim.Properties != new Primitive2Properties(e.Properties))
                {

                    prim.Properties = new Primitive2Properties(e.Properties);
                    PrimList[id] = prim;
                    MasterObjectCaches.RegionPrims[e.Simulator.Name] = PrimList;
                    MasterObjectCaches.MarkDirty();
                }
            }
            else
            {
                // Skip. There is nothing we can do
            }*/
            //ReqObjPropertiesData.Add(e.Properties.ObjectID, e.Properties);
            // This function is disabled until LibreMetaverse is fully tested
            //ReqObjProperties.Set();
        }

        private static void onSimChange(object sender, SimChangedEventArgs e)
        {
            // Request object data for all prims on sim!
            //Dictionary<uint, Primitive> simPrims = client.Network.CurrentSim.ObjectsPrimitives.Copy();
            //ManualResetEvent mreWaiter = new ManualResetEvent(false);
            //mreWaiter.Reset();
            //foreach(KeyValuePair<uint, Primitive> kvp in simPrims)
            //{
            //    onObjectUpdate(null, new PrimEventArgs(client.Network.CurrentSim, kvp.Value, 0, false, false));
            //    mreWaiter.WaitOne(TimeSpan.FromMilliseconds(500));
            //}

        }

        private static void onChatRecv(object sender, ChatEventArgs e)
        {
            if (e.Message == "" || e.Message == "typing") return;
            if (e.SourceID == client.Self.AgentID) return;

            string eMe = e.Message;
            Dictionary<string, string> dstuf = new Dictionary<string, string>();
            Log.debugf(true, "onChatRecv", new[] { e.Message });

            dstuf.Add("type", "chat");
            string SRC = "";
            if (e.SourceType == ChatSourceType.Agent) SRC = "agent";
            else if (e.SourceType == ChatSourceType.Object) SRC = "obj";
            else if (e.SourceType == ChatSourceType.System) SRC = "sys";
            dstuf.Add("source", SRC);
            dstuf.Add("request", eMe);
            dstuf.Add("from", e.SourceID.ToString());
            dstuf.Add("from_sess", "");
            dstuf.Add("fromName", e.FromName);

            foreach (IProgram P in g_ZPrograms)
            {
                Log.debug(JsonConvert.SerializeObject(dstuf));
                Thread X = new Thread(() => P.passArguments(JsonConvert.SerializeObject(dstuf)));
                X.Name = "T_" + eMe;
                X.Start();
            }
            Log.debugf(false, "onChatRecv", new[] { "" });

        }











        private static Dictionary<UUID, Group> GroupsCache = null;
        private static ManualResetEvent GroupsEvent = new ManualResetEvent(false);
        private static void Groups_CurrentGroups(object sender, CurrentGroupsEventArgs e)
        {
            if (null == GroupsCache)
                GroupsCache = e.Groups;
            else
                lock (GroupsCache) { GroupsCache = e.Groups; }
            GroupsEvent.Set();
        }
        private static void ReloadGroupsCache()
        {
            client.Groups.CurrentGroups += Groups_CurrentGroups;
            client.Groups.RequestCurrentGroups();
            GroupsEvent.WaitOne(10000, false);
            client.Groups.CurrentGroups -= Groups_CurrentGroups;
            GroupsEvent.Reset();
        }

        private static UUID GroupName2UUID(String groupName)
        {
            UUID tryUUID;
            if (UUID.TryParse(groupName, out tryUUID))
                return tryUUID;
            if (null == GroupsCache)
            {
                ReloadGroupsCache();
                if (null == GroupsCache)
                    return UUID.Zero;
            }
            lock (GroupsCache)
            {
                if (GroupsCache.Count > 0)
                {
                    foreach (Group currentGroup in GroupsCache.Values)
                        if (currentGroup.Name.ToLower() == groupName.ToLower())
                            return currentGroup.ID;
                }
            }
            return UUID.Zero;
        }
    }

    public class Tools
    {
        public static Int32 getTimestamp()
        {
            return int.Parse(DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
        }

        public static string Hash2String(byte[] Hash)
        {
            StringBuilder sb = new StringBuilder();
            foreach(byte b in Hash)
            {
                sb.Append(b.ToString("X2"));
            }
            return sb.ToString();
        }

        public static string MD5Hash(string ToHash)
        {
            byte[] Source = UTF8Encoding.UTF8.GetBytes(ToHash);
            byte[] Hash = new MD5CryptoServiceProvider().ComputeHash(Source);
            return Tools.Hash2String(Hash);
        }

        public static string MD5Hash(byte[] ToHash)
        {
            return Tools.Hash2String(new MD5CryptoServiceProvider().ComputeHash(ToHash));
        }

        public static string SHA256Hash(string ToHash)
        {
            SHA256 hasher = SHA256.Create();
            return Tools.Hash2String(hasher.ComputeHash(UTF8Encoding.UTF8.GetBytes(ToHash)));
        }

        public static string SHA256Hash(byte[] ToHash)
        {
            SHA256 Hasher = SHA256.Create();
            return Tools.Hash2String(Hasher.ComputeHash(ToHash));
        }

        public static string ZHX(string ToHash)
        {
            ZHash.Instance.NewKey();
            ZHash.Instance.Key = ToHash;
            return ZHash.Instance.Key;
        }
        
        public static string Base64Encode(string plainText) {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }
        public static string Base64Decode(string base64EncodedData) {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }
    }

    public sealed class ZHash
    {
        private static readonly object _lock = new object();
        private static ZHash _inst = new ZHash();
        static ZHash() { }

        public static ZHash Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_inst == null) _inst = new ZHash();
                    return _inst;
                }
            }
        }


        public string _key;
        public string Key
        {
            set
            {
                lock(_lock)
                {

                    if (value != "")
                        CalculateKey(value);
                    else NewKey();
                }
            }
            get
            {
                return _key;
            }
        }


        public void CalculateKey(string K)
        {
            string valid = "abcdefghijklmnopqrstuvwxyz1234567890ABCDEFGHIJKLMNOPQRSTUVWXYZ=.+/\\";

            StringBuilder tmp = new StringBuilder(_key);

            for (int i = 0; i < _key.Length; i++)
            {
                char V = _key[i];
                if (V != ':')
                {
                    MD5 MDHash = MD5.Create();
                    for (int ii = 0; ii < K.Length; ii++)
                    {
                        int ixi = ii;
                        while(ixi >= valid.Length)
                        {
                            ixi = ixi / 2;
                        }
                        if (ixi < 0) ixi = valid[1];
                        byte[] md5Data = MDHash.ComputeHash(Encoding.UTF8.GetBytes((K + i.ToString() + valid[i].ToString() + valid[ii].ToString()).ToCharArray()));
                        // Replace digit with MD5'd  char from String K encoded alongside (i)
                        StringBuilder hashData = new StringBuilder();
                        foreach (byte b in md5Data)
                        {
                            hashData.Append(b.ToString("X2"));
                        }
                        string Hash = hashData.ToString();
                        tmp[i] = Hash[(i > 31 ? 1 : i)];
                        Console.Write("\r" + tmp.ToString() + "\r");
                    }
                }
            }
            Console.WriteLine("\r\n");
            _key = tmp.ToString();
        }

        public void NewKey()
        {
            lock(_lock)
            {

                _key = "".PadLeft(10, '0');
                _key += ":";
                _key += "".PadRight(4, '0');
                _key += ":";
                _key += "".PadRight(6, '0');
                _key += ":";
                _key += "".PadRight(8, '0');
            }
        }

        public void SetKey(string Key)
        {
            _key = Key;
        }
    }
}
