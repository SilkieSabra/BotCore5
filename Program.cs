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
using System.Reflection;

namespace Bot
{
    public class Program : BaseCommands
    {
        public static Logger Log;
        public static string BotVer = ASMInfo.BotVer;
        public static string BotStr = ASMInfo.BotName; // internal identifier for linden
        public static string Flavor = "Bot"; // inworld identification - must be customized
        public static SerialManager SM = new SerialManager();
        public static GridClient client = new GridClient();
        public static bool g_iIsRunning = true;
        public static CommandRegistry registry;
        public static List<IProgram> g_ZPrograms = new List<IProgram>();
        public static CommandManager CM = null;

        static readonly object _CacheLock = new object();
        //public static License LicenseKey; // Not to be used yet

        public static void msg(Destinations D, UUID x, string m)
        {
            MessageFactory.Post(D, m, x);
        }


        public static void passArguments(string data)
        {
            
            CM.RunChatCommand(data);
        }

        public static unsafe void Main(string[] args)
        {
            File.WriteAllText("PID.lock", Process.GetCurrentProcess().Id.ToString());
            Console.WriteLine("Setting up Main Configuration");
            Log = new Logger("BotCore5");
            BotSession.Instance.Logger = Log;
            BotSession.Instance.LaunchTime = DateTime.Now;
            ZHash.Instance.NewKey();
            ZHash.Instance.Key = "Test";
            Console.WriteLine("ZHash (Test): " + ZHash.Instance.Key);
            MainConfiguration.Instance.Load();
            MainConfiguration conf = MainConfiguration.Instance;
            //MasterObjectCaches = ObjectCaches.Instance;

            if (args.Length == 2)
            {
                // Check if this is activation command
                if (args[0] == "-a")
                {
                    MainConfiguration.Instance.ActivationCode = args[1];
                    MainConfiguration.Instance.Save();
                    return;
                }
            }
            else if (args.Length == 4)
            {
                if (args[0] == "-l")
                {
                    MainConfiguration.Instance.first = args[1];
                    MainConfiguration.Instance.last = args[2];
                    MainConfiguration.Instance.password = args[3];
                    MainConfiguration.Instance.Save();
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
            BotSession.Instance.MSGSVC.MessageEvent += MSGSVC_onChat;
            BotSession.Instance.MSGSVC.MessageEvent += MSGSVC_onIM;
            BotSession.Instance.MSGSVC.MessageEvent += MSGSVC_onGroupMessage;


            string fna = null;
            string lna = null;
            string pwd = null;
            if (conf.first == null || conf.first == "")
            {

                if (args.Length == 0)
                {

                    Log.info(false, "Please enter your avatar's first name:  ");
                    fna = Console.ReadLine();

                    Log.info(false, "Please enter the last name: ");
                    lna = Console.ReadLine();
                    Log.info(false, "Now enter your password: ");
                    pwd = Console.ReadLine();

                    conf.ConfigFor = "ZBotCore";
                    conf.ConfigVersion = 1.0f;

                }
                else
                {
                    Log.info(false, "Loading...");
                    Log.info(false, "FirstName: " + args[0]);
                    fna = args[0];
                    lna = args[1];
                    pwd = args[2];

                    // Continue boot
                }
                conf.first = fna;
                conf.last = lna;
                conf.password = pwd;
                conf.Save();
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
            client.Self.IM += onIMEvent;
            client.Groups.GroupRoleDataReply += CacheGroupRoles;
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
            client.Settings.LOG_RESENDS = false;
            client.Settings.LOG_ALL_CAPS_ERRORS = false;
            client.Settings.LOG_DISKCACHE = false;
            


            client.Settings.ALWAYS_REQUEST_OBJECTS = true;

            Console.WriteLine("Logging in...");
            
            bool LoggedIn = client.Network.Login(fna, lna, pwd, BotStr, BotVer.ToString());
            Console.WriteLine("Logged In: " + LoggedIn.ToString());

            if (!LoggedIn)
            {
                Console.WriteLine("Check Creds:\n \nFirst Name: '" + fna + "'\nLast Name: '" + lna + "'\nPWD: '" + pwd + "'\nBotStr: '" + BotStr + "'\nBotVer: " + BotVer.ToString()+"\n \nLogin Message: "+client.Network.LoginMessage);
                
            }
            if (LoggedIn)
            {

                
                // Setup BotSession Singleton!
                BotSession.Instance.grid = client;
                BotSession.Instance.Logger = Log;

                Thread prompter = new Thread(() => {
                    BotSession.Instance.Logger.DoPrompt();
                });

                prompter.Start();
                CM = new CommandManager();
                
                MainConfiguration.Instance.Save(); // Flush the config, to update the file format

                g_ZPrograms = new List<IProgram>();
                // Scan folder for plugins, then load
                FileInfo[] files = new DirectoryInfo(Directory.GetCurrentDirectory()).GetFiles();
                foreach(FileInfo fi in files)
                {
                    try
                    {

                        if (fi.Extension.ToLower() == ".dll")
                        {
                            PluginActivator PA = new PluginActivator();
                            Assembly asm = PA.LoadLibrary(fi.FullName);
                            List<IProgram> plugins = PA.Activate(asm);
                            foreach (IProgram prog in plugins)
                            {
                                try
                                {
                                    if (!g_ZPrograms.Contains(prog))
                                    {

                                        Console.WriteLine("Plugin [" + prog.ProgramName + "] found (" + fi.FullName + ") loaded and activated");
                                        prog.run();
                                        g_ZPrograms.Add(prog);
                                    }
                                }
                                catch (Exception e) { }
                            }
                        }
                    }catch(Exception e)
                    {
                        Console.WriteLine("Could not load file: " + fi.FullName+" as a Bot Plugin");
                    }
                }

                CommandRegistry.Instance.LocateCommands();
                if (!File.Exists("Inventory.blob"))
                {
                    BotSession.Instance.grid.Inventory.RequestFolderContents(BotSession.Instance.grid.Inventory.Store.RootFolder.OwnerID, BotSession.Instance.grid.Inventory.Store.Owner, true, true, InventorySortOrder.SystemFoldersToTop);

                    BotSession.Instance.grid.Inventory.RequestFolderContents(BotSession.Instance.grid.Inventory.FindFolderForType(AssetType.Animation), BotSession.Instance.grid.Inventory.Store.Owner, true, true, InventorySortOrder.SystemFoldersToTop);
                    BotSession.Instance.grid.Inventory.RequestFolderContents(BotSession.Instance.grid.Inventory.FindFolderForType(AssetType.Bodypart), BotSession.Instance.grid.Inventory.Store.Owner, true, true, InventorySortOrder.SystemFoldersToTop);
                    BotSession.Instance.grid.Inventory.RequestFolderContents(BotSession.Instance.grid.Inventory.FindFolderForType(AssetType.CallingCard), BotSession.Instance.grid.Inventory.Store.Owner, true, true, InventorySortOrder.SystemFoldersToTop);
                    BotSession.Instance.grid.Inventory.RequestFolderContents(BotSession.Instance.grid.Inventory.FindFolderForType(AssetType.Clothing), BotSession.Instance.grid.Inventory.Store.Owner, true, true, InventorySortOrder.SystemFoldersToTop);
                    BotSession.Instance.grid.Inventory.RequestFolderContents(BotSession.Instance.grid.Inventory.FindFolderForType(AssetType.Folder), BotSession.Instance.grid.Inventory.Store.Owner, true, true, InventorySortOrder.SystemFoldersToTop);
                    BotSession.Instance.grid.Inventory.RequestFolderContents(BotSession.Instance.grid.Inventory.FindFolderForType(AssetType.Gesture), BotSession.Instance.grid.Inventory.Store.Owner, true, true, InventorySortOrder.SystemFoldersToTop);
                    BotSession.Instance.grid.Inventory.RequestFolderContents(BotSession.Instance.grid.Inventory.FindFolderForType(AssetType.ImageJPEG), BotSession.Instance.grid.Inventory.Store.Owner, true, true, InventorySortOrder.SystemFoldersToTop);
                    BotSession.Instance.grid.Inventory.RequestFolderContents(BotSession.Instance.grid.Inventory.FindFolderForType(AssetType.ImageTGA), BotSession.Instance.grid.Inventory.Store.Owner, true, true, InventorySortOrder.SystemFoldersToTop);
                    BotSession.Instance.grid.Inventory.RequestFolderContents(BotSession.Instance.grid.Inventory.FindFolderForType(AssetType.Landmark), BotSession.Instance.grid.Inventory.Store.Owner, true, true, InventorySortOrder.SystemFoldersToTop);
                    BotSession.Instance.grid.Inventory.RequestFolderContents(BotSession.Instance.grid.Inventory.FindFolderForType(AssetType.Link), BotSession.Instance.grid.Inventory.Store.Owner, true, true, InventorySortOrder.SystemFoldersToTop);
                    BotSession.Instance.grid.Inventory.RequestFolderContents(BotSession.Instance.grid.Inventory.FindFolderForType(AssetType.LinkFolder), BotSession.Instance.grid.Inventory.Store.Owner, true, true, InventorySortOrder.SystemFoldersToTop);
                    BotSession.Instance.grid.Inventory.RequestFolderContents(BotSession.Instance.grid.Inventory.FindFolderForType(AssetType.LSLBytecode), BotSession.Instance.grid.Inventory.Store.Owner, true, true, InventorySortOrder.SystemFoldersToTop);
                    BotSession.Instance.grid.Inventory.RequestFolderContents(BotSession.Instance.grid.Inventory.FindFolderForType(AssetType.LSLText), BotSession.Instance.grid.Inventory.Store.Owner, true, true, InventorySortOrder.SystemFoldersToTop);
                    BotSession.Instance.grid.Inventory.RequestFolderContents(BotSession.Instance.grid.Inventory.FindFolderForType(AssetType.Mesh), BotSession.Instance.grid.Inventory.Store.Owner, true, true, InventorySortOrder.SystemFoldersToTop);
                    BotSession.Instance.grid.Inventory.RequestFolderContents(BotSession.Instance.grid.Inventory.FindFolderForType(AssetType.Notecard), BotSession.Instance.grid.Inventory.Store.Owner, true, true, InventorySortOrder.SystemFoldersToTop);
                    BotSession.Instance.grid.Inventory.RequestFolderContents(BotSession.Instance.grid.Inventory.FindFolderForType(AssetType.Object), BotSession.Instance.grid.Inventory.Store.Owner, true, true, InventorySortOrder.SystemFoldersToTop);
                    BotSession.Instance.grid.Inventory.RequestFolderContents(BotSession.Instance.grid.Inventory.FindFolderForType(AssetType.Person), BotSession.Instance.grid.Inventory.Store.Owner, true, true, InventorySortOrder.SystemFoldersToTop);
                    BotSession.Instance.grid.Inventory.RequestFolderContents(BotSession.Instance.grid.Inventory.FindFolderForType(AssetType.Simstate), BotSession.Instance.grid.Inventory.Store.Owner, true, true, InventorySortOrder.SystemFoldersToTop);
                    BotSession.Instance.grid.Inventory.RequestFolderContents(BotSession.Instance.grid.Inventory.FindFolderForType(AssetType.Sound), BotSession.Instance.grid.Inventory.Store.Owner, true, true, InventorySortOrder.SystemFoldersToTop);
                    BotSession.Instance.grid.Inventory.RequestFolderContents(BotSession.Instance.grid.Inventory.FindFolderForType(AssetType.SoundWAV), BotSession.Instance.grid.Inventory.Store.Owner, true, true, InventorySortOrder.SystemFoldersToTop);
                    BotSession.Instance.grid.Inventory.RequestFolderContents(BotSession.Instance.grid.Inventory.FindFolderForType(AssetType.Texture), BotSession.Instance.grid.Inventory.Store.Owner, true, true, InventorySortOrder.SystemFoldersToTop);
                    BotSession.Instance.grid.Inventory.RequestFolderContents(BotSession.Instance.grid.Inventory.FindFolderForType(AssetType.TextureTGA), BotSession.Instance.grid.Inventory.Store.Owner, true, true, InventorySortOrder.SystemFoldersToTop);
                    BotSession.Instance.grid.Inventory.RequestFolderContents(BotSession.Instance.grid.Inventory.FindFolderForType(AssetType.Unknown), BotSession.Instance.grid.Inventory.Store.Owner, true, true, InventorySortOrder.SystemFoldersToTop);
                    BotSession.Instance.grid.Inventory.RequestFolderContents(BotSession.Instance.grid.Inventory.FindFolderForType(AssetType.Widget), BotSession.Instance.grid.Inventory.Store.Owner, true, true, InventorySortOrder.SystemFoldersToTop);





                }
                else
                    BotSession.Instance.grid.Inventory.Store.RestoreFromDisk("Inventory.blob");


                int iLastInvLength = 0;
                while (g_iIsRunning)
                {

                    if(iLastInvLength != BotSession.Instance.grid.Inventory.Store.Items.Count)
                    {
                        BotSession.Instance.grid.Inventory.Store.SaveToDisk("Inventory.blob");
                        iLastInvLength = BotSession.Instance.grid.Inventory.Store.Items.Count;
                    }
                    string consoleCmd = "N/A";
                    try
                    {
                        consoleCmd = BotSession.Instance.Logger.CheckForNewCmd();
                    }catch(Exception e)
                    {
                        // no command is set yet!
                    }

                    switch (consoleCmd)
                    {
                        case "N/A":
                            {
                                break;
                            }
                        default:
                            {
                                // Run command!
                                Dictionary<string, string> argsx = new Dictionary<string, string>();
                                argsx.Add("type", "console");
                                argsx.Add("source", "null");
                                argsx.Add("request", consoleCmd);
                                argsx.Add("from", "");
                                argsx.Add("from_sess", "");
                                argsx.Add("fromName", "CONSOLE");
                                passArguments(JsonConvert.SerializeObject(argsx));
                                break;
                            }
                    }
                    // Pass to the command handlers

                    client.Self.RetrieveInstantMessages();
                    if (client.Network.Connected == false) g_iIsRunning = false; // Quit the program and restart immediately!
                    Thread.Sleep(1000);


                    if (conf.ConfigFor == "Main")
                    {
                        MessageFactory.Post(Destinations.DEST_LOCAL, "Alert: Main.json is not fully initialized. Setting default values", UUID.Zero);
                        
                        conf.ConfigFor = "BOT";
                        conf.ConfigVersion = 1.0f;
                        // data contains nothing at the moment.
                        SM.Write<MainConfiguration>("Main", conf);
                        conf = null;
                        conf = SM.Read<MainConfiguration>("Main");

                        if (conf.ConfigFor == "BOT")
                        {
                            MessageFactory.Post(Destinations.DEST_LOCAL, "Main.json has been created", UUID.Zero);
                        }
                        else
                        {
                            MessageFactory.Post(Destinations.DEST_LOCAL, "Main.json is invalid. Cannot continue", UUID.Zero);
                            g_iIsRunning = false;
                        }
                    }
                    else
                    {
                        Flavor = conf.ConfigFor;
                    }

                    //msg(MessageHandler.Destinations.DEST_LOCAL, UUID.Zero, "Commands found: " + registry.Cmds.Count.ToString());


                    if (startupSeq)
                    {
                        registry = CommandRegistry.Instance;
                        registry.LocateCommands();
                        GroupsCache = new Dictionary<UUID, Group>();
                        ReloadGroupsCache();
                        //ReloadGroupsCache();
                        try
                        {

                            Log.info(true, g_ZPrograms.Count.ToString() + " programs linked");
                            //if (g_ZPrograms.Count > 0) msg(MessageHandler.Destinations.DEST_LOCAL, UUID.Zero, "Default Program [" + conf.MainProgramDLL + "] has been loaded, " + programCount.ToString() + " plugin(s) loaded");
                            registry.LocateCommands();

                            //msg(MessageHandler.Destinations.DEST_LOCAL, UUID.Zero, "Commands found: " + registry.Cmds.Count.ToString());

                            GroupsCache = new Dictionary<UUID, Group>();
                            ReloadGroupsCache();
                        }
                        catch (Exception E)
                        {
                            string Msg = E.Message;
                            string STACK = E.StackTrace.Replace("ZNI", "");
                            Msg = Msg.Replace("ZNI", "");
                            Log.info(true, "Generic Exception Caught: " + Msg + " [0x0A]");
                            int i;
                            int* ptr = &i;
                            IntPtr addr = (IntPtr)ptr;
                            MessageFactory.Post(Destinations.DEST_LOCAL, "Generic Exception Caught: " + Msg + " [0x0A, 0x" + addr.ToString("x") + "]\nSTACK: " + STACK, UUID.Zero);
                        }
                        if (File.Exists("XUP"))
                        {
                            File.Delete("XUP");

                            MessageFactory.Post(Destinations.DEST_LOCAL, $"Updated to version {BotStr} - {BotVer}", UUID.Zero);
                        }
                    }

                    foreach (IProgram plugin in g_ZPrograms)
                    {
                        plugin.getTick(); // Trigger a tick event!!!
                    }

                    
                    
                    //MasterObjectCaches.Save();
                    if (startupSeq) startupSeq = false;


                    if(BotSession.Instance.LaunchTime.AddHours(MainConfiguration.Instance.AutoRelogAfterHours) < DateTime.Now)
                    {
                        // Initiate a relog
                        try
                        {
                            prompter.Interrupt();
                            prompter.Abort();
                        }catch(Exception e)
                        {
                            client.Self.Chat(e.Message, 0, ChatType.Normal);
                        }
                        client.Self.Chat("Automatic relog in progress", 0, ChatType.Whisper);
                        g_iIsRunning = false;
                        client.Network.Logout();
                    }
                    //if (MasterObjectCaches.RegionPrims.Count == 0 && client.Network.Connected)
                    //{

                    //    onSimChange(null, new SimChangedEventArgs(client.Network.CurrentSim));
                    //}

                    if (BotSession.Instance.EnqueueExit) g_iIsRunning = false;

                    if (BotSession.Instance.EnqueueGroupRefresh)
                    {
                        BotSession.Instance.EnqueueGroupRefresh = false;
                        ReloadGroupsCache();
                    }

                    BotSession.Instance.MSGSVC.PopMessage();
                }

                prompter.Interrupt();
                client.Network.Logout();
            }
            while (client.Network.Connected) { }

            if (BotSession.Instance.WaitForFiveMinutes)
            {
                AutoResetEvent are = new AutoResetEvent(false);
                are.WaitOne(TimeSpan.FromMinutes(5));
            }
            File.Delete("PID.lock");
            Environment.Exit(0);

            //System.Console.WriteLine("PAUSING. PRESS ANY KEY TO EXIT");
            //System.Console.ReadKey();
        }
        private static ManualResetEvent GroupJoinWaiter = new ManualResetEvent(false);
        private static void MSGSVC_onGroupMessage(object sender, MessageEventArgs e)
        {
            // not implemented, yet, but do not throw error
            switch (e.Msg.GetMessageSource())
            {
                case Destinations.DEST_GROUP:
                    BotSession.Instance.grid.Self.InstantMessageGroup(e.Msg.GetTarget(), e.Msg.GetMessage());
                    break;
                default:
                    return;
            }
        }

        private static void MSGSVC_onIM(object sender, MessageEventArgs e)
        {
            switch (e.Msg.GetMessageSource())
            {
                case Destinations.DEST_AGENT:
                    BotSession.Instance.grid.Self.InstantMessage(e.Msg.GetTarget(), e.Msg.GetMessage());
                    break;
                default:
                    return;
            }
        }

        private static void MSGSVC_onChat(object sender, MessageEventArgs e)
        {
            switch (e.Msg.GetMessageSource())
            {
                case Destinations.DEST_LOCAL:
                    BotSession.Instance.grid.Self.Chat(e.Msg.GetMessage(), e.Msg.GetChannel(), ChatType.Normal);
                    break;
                default:
                    return;
            }
        }

        private static void onJoinGroupChat(object sender, GroupChatJoinedEventArgs e)
        {
            if (e.Success)
                GroupJoinWaiter.Set();
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

        private ManualResetEvent profile_get = new ManualResetEvent(false);
        private Avatar.AvatarProperties Properties_AV;
        [CommandGroup("set_profile_text", 6, "set_profile_text [text:Base64] - Sets the profile text", Destinations.DEST_AGENT | Destinations.DEST_LOCAL)]
        public void setProfileText(UUID client, int level, string[] additionalArgs, Destinations source, UUID agentKey, string agentName)
        {
            MHE(source, client, "Setting...");

            BotSession.Instance.grid.Avatars.AvatarPropertiesReply += Avatars_AvatarPropertiesReply;
            BotSession.Instance.grid.Avatars.RequestAvatarProperties(BotSession.Instance.grid.Self.AgentID);
            profile_get.Reset();

            if (profile_get.WaitOne(TimeSpan.FromSeconds(30)))
            {
                Properties_AV.AboutText = Encoding.UTF8.GetString(Convert.FromBase64String(additionalArgs[0]));
                
                BotSession.Instance.grid.Self.UpdateProfile(Properties_AV);
                MHE(source, client, "Profile text set");
            }
            else
            {
                MHE(source, client, "The profile text could not be set. Timeout experienced");

                BotSession.Instance.grid.Avatars.AvatarPropertiesReply -= Avatars_AvatarPropertiesReply;
            }
        
        }

        private void Avatars_AvatarPropertiesReply(object sender, AvatarPropertiesReplyEventArgs e)
        {
            Properties_AV = e.Properties;

            profile_get.Set();

            BotSession.Instance.grid.Avatars.AvatarPropertiesReply -= Avatars_AvatarPropertiesReply;
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

            string eMe = e.Message;
            Dictionary<string, string> dstuf = new Dictionary<string, string>();
            //Log.debugf(true, "onChatRecv", new[] { e.Message });

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

            passArguments(JsonConvert.SerializeObject(dstuf));
            //Log.debugf(false, "onChatRecv", new[] { "" });

        }





        public static void onIMEvent(object sender, InstantMessageEventArgs e)
        {

            if (e.IM.FromAgentID == client.Self.AgentID) return;

            MainConfiguration mem = MainConfiguration.Instance;

            UUID SentBy = e.IM.FromAgentID;
            int Level = 0;
            if (mem.BotAdmins.ContainsKey(SentBy)) Level = mem.BotAdmins[SentBy];
            if (e.IM.Dialog == InstantMessageDialog.GroupInvitation)
            {
                if (Level >= 4)
                {
                    client.Self.GroupInviteRespond(e.IM.FromAgentID, e.IM.IMSessionID, true);
                }
                else
                {
                    client.Self.GroupInviteRespond(e.IM.FromAgentID, e.IM.IMSessionID, false);
                    MH(Destinations.DEST_AGENT, e.IM.FromAgentID, "You lack the proper permissions to perform this action");
                }
            }
            else if (e.IM.Dialog == InstantMessageDialog.FriendshipOffered)
            {
                if (Level >= 4)
                {
                    client.Friends.AcceptFriendship(e.IM.FromAgentID, e.IM.IMSessionID);
                    MH(Destinations.DEST_AGENT, e.IM.FromAgentID, "Welcome to my friends list!");
                }
                else
                {
                    MH(Destinations.DEST_AGENT, e.IM.FromAgentID, "You lack proper permission");
                }
            }
            else if (e.IM.Dialog == InstantMessageDialog.RequestTeleport)
            {
                if (Level >= 3)
                {
                    client.Self.TeleportLureRespond(e.IM.FromAgentID, e.IM.IMSessionID, true);
                    MH(Destinations.DEST_AGENT, e.IM.FromAgentID, "Teleporting...");
                }
                else
                {
                    client.Self.TeleportLureRespond(e.IM.FromAgentID, e.IM.IMSessionID, false);
                    MH(Destinations.DEST_AGENT, e.IM.FromAgentID, "You lack permission");
                }
            }
            else if (e.IM.Dialog == InstantMessageDialog.MessageFromObject)
            {
                if (Level >= 5)
                {
                    // For this to work the object must have been granted auth!!!!!
                    Dictionary<string, string> args = new Dictionary<string, string>();
                    args.Add("type", "im");
                    args.Add("source", "obj");
                    args.Add("request", e.IM.Message);
                    args.Add("from", e.IM.FromAgentID.ToString());
                    args.Add("from_sess", e.IM.IMSessionID.ToString());
                    args.Add("fromName", e.IM.FromAgentName);
                    passArguments(JsonConvert.SerializeObject(args));
                }
                else
                {
                    // If auth is insufficient, ignore it. 
                }
            }
            else if (e.IM.Dialog == InstantMessageDialog.MessageFromAgent || e.IM.Dialog == InstantMessageDialog.SessionSend)
            {
                //if (e.IM.Message.Substring(0, 1) != "!") return;
                string msgs = e.IM.Message;
                //string msgs = e.IM.Message.Substring(1);
                // Perform a few tests before live deployment
                if (IsGroup(e.IM.IMSessionID))
                {

                    Dictionary<string, string> args = new Dictionary<string, string>();
                    args.Add("type", "group");
                    args.Add("source", "agent");
                    args.Add("request", msgs);
                    args.Add("from", e.IM.FromAgentID.ToString());
                    args.Add("from_sess", e.IM.IMSessionID.ToString());
                    args.Add("fromName", e.IM.FromAgentName);
                    passArguments(JsonConvert.SerializeObject(args));
                }
                else
                {
                    Dictionary<string, string> args = new Dictionary<string, string>();
                    args.Add("type", "im");
                    args.Add("source", "agent");
                    args.Add("request", msgs);
                    args.Add("from", e.IM.FromAgentID.ToString());
                    args.Add("from_sess", "");
                    args.Add("fromName", e.IM.FromAgentName);
                    passArguments(JsonConvert.SerializeObject(args));
                }
            }

        }


        private static void CacheGroupRoles(object sender, GroupRolesDataReplyEventArgs e)
        {
            //MHE(MessageHandler.Destinations.DEST_LOCAL, UUID.Zero, "[debug] role_reply");
            if (!Directory.Exists("zGroupCache")) Directory.CreateDirectory("zGroupCache"); // this should be purged at every bot restart!!!

            //MHE(MessageHandler.Destinations.DEST_LOCAL, UUID.Zero, "[debug] generating groupcache file");
            zGroupCaches newCache = new zGroupCaches();
            zGroupCaches.GroupMemoryData gmd = new zGroupCaches.GroupMemoryData();
            foreach (KeyValuePair<UUID, GroupRole> roleData in e.Roles)
            {
                gmd.roleID = roleData.Value.ID;
                gmd.RoleName = roleData.Value.Name;
                gmd.Title = roleData.Value.Title;
                gmd.Powers = roleData.Value.Powers;


                newCache.GMD.Add(gmd);

            }
            newCache.GroupID = e.GroupID;
            newCache.Save(e.GroupID.ToString());
            RoleReply.Set();
            FileInfo fi = new FileInfo("GroupCache/" + e.GroupID.ToString() + ".json");

            //MHE(MessageHandler.Destinations.DEST_LOCAL, UUID.Zero, "[debug] Roles for secondlife:///app/group/" + e.GroupID.ToString() + "/about have been saved to: GroupCache/" + e.GroupID.ToString() + ".bdf\nFileSize: "+fi.Length.ToString(), 55);


        }
        private static Dictionary<UUID, Group> GroupsCache = null;
        private static ManualResetEvent GroupsEvent = new ManualResetEvent(false);
        private static ManualResetEvent RoleReply = new ManualResetEvent(false);
        private static void Groups_CurrentGroups(object sender, CurrentGroupsEventArgs e)
        {
            if (null == GroupsCache)
                GroupsCache = e.Groups;
            else
                lock (GroupsCache) { GroupsCache = e.Groups; }
            GroupsEvent.Set();

            foreach (KeyValuePair<UUID, Group> DoCache in GroupsCache)
            {
                bool Retry = true;
                int count = 0;
                while (Retry)
                {
                    client.Groups.RequestGroupRoles(DoCache.Value.ID);
                    if (RoleReply.WaitOne(TimeSpan.FromSeconds(30), false)) { Retry = false; }
                    else
                    {
                        count++;
                        //MH.callbacks(MessageHandler.Destinations.DEST_LOCAL, UUID.Zero, "There appears to have been a failure requesting the group roles for secondlife:///app/group/" + DoCache.Value.ID.ToString() + "/about - Trying again");

                        if(count >= 5)
                        {
                            MH(Destinations.DEST_LOCAL, UUID.Zero, "Aborting group refresh attempt. Too many errors - Resetting cache and retrying");
                            GroupsEvent.Reset();
                            GroupsCache = new Dictionary<UUID, Group>();
                            client.Groups.CurrentGroups -= Groups_CurrentGroups;

                            ReloadGroupsCache();

                            return;
                        }

                    }
                }
            }
        }
        private static void ReloadGroupsCache()
        {
            client.Groups.CurrentGroups += Groups_CurrentGroups;
            client.Groups.RequestCurrentGroups();
            GroupsEvent.WaitOne(10000, false);
            client.Groups.CurrentGroups -= Groups_CurrentGroups;
            GroupsEvent.Reset();
        }

        private UUID GroupName2UUID(String groupName)
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

        private static bool IsGroup(UUID grpKey)
        {
            // For use in IMs since it appears partially broken at the moment
            return GroupsCache.ContainsKey(grpKey);
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
            string valid = "abcdefghijklmnopqrstuvwxyz1234567890ABCDEFGHIJKLMNOPQRSTUVWXYZ=.+/\\][{}';:?><,_-)(*&^%$#@!`~|";
            while(valid.Length < K.Length)
            {
                valid += valid;
            }
            StringBuilder tmp = new StringBuilder(_key);

            for (int i = 0; i < _key.Length; i++)
            {
                char V = _key[i];
                if (V != ':')
                {
                    MD5 MDHash = MD5.Create();
                    for (int ii = 0; ii < K.Length; ii++)
                    {
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
