using System;
using System.Collections.Generic;
using System.Linq;
using OpenMetaverse;
using Newtonsoft.Json;
using System.Reflection;
using System.Threading;


namespace Bot.CommandSystem
{
    public sealed class CommandRegistry
    {
        /*
         * ===============================
         * START SINGLETON PATTERN
         * ===============================
         */
        private static CommandRegistry _instance = null;
        private static readonly object lockhandle = new object();

        static CommandRegistry()
        {

        }

        public static CommandRegistry Instance
        {
            get
            {
                lock (lockhandle)
                {
                    if (_instance == null)
                    {
                        BotSession bs = BotSession.Instance;
                        _instance = new CommandRegistry();
                        _instance.client = bs.grid;
                        _instance.config = bs.ConfigurationHandle;
                        _instance.Log = bs.Logger;
                        _instance.LocateCommands();
                    }
                    return _instance;
                }
            }
        }

        /*
         * ==============================
         * SINGLETON PATTERN END
         * Usage: CommandRegistry reg = CommandRegistry.Instance
         * ==============================
         */



        // Define the registry
        public Dictionary<string, CommandGroup> Cmds = new Dictionary<string, CommandGroup>();
        public GridClient client;
        public Logger Log;
        public IConfig config;
        public void LocateCommands()
        {
            try
            {

                int i = 0;
                // Locate all commands--
                for (i = 0; i < AppDomain.CurrentDomain.GetAssemblies().Length; i++)
                {
                    Assembly A = null;
                    try
                    {
                        A = AppDomain.CurrentDomain.GetAssemblies()[i];
                    }
                    catch (Exception e)
                    {
                        //                    MHEx(MessageHandler.Destinations.DEST_LOCAL, UUID.Zero, "")
                    }
                    if (A != null)
                    {
                        int ii = 0;
                        for (ii = 0; ii < A.GetTypes().Length; ii++)
                        {
                            Type T = null;
                            try
                            {
                                T = A.GetTypes()[ii];
                            }
                            catch (Exception e)
                            {

                            }
                            if (T != null)
                            {

                                if (T.IsClass)
                                {
                                    foreach (MethodInfo MI in T.GetMethods())
                                    {
                                        CommandGroup[] Command = (CommandGroup[])MI.GetCustomAttributes(typeof(CommandGroup), false);
                                        //var CommandO = MI.GetCustomAttributes(typeof(CommandGroup), false);
                                        if (Command.Length > 0)
                                        {
                                            for (int ix = 0; ix < Command.Length; ix++)
                                            {
                                                CommandGroup CG = Command[ix];
                                                CG.AssignedMethod = MI;

                                                if (Cmds.ContainsKey(CG.Command) == false)
                                                {
                                                    Log.info(true, "DISCOVER: " + CG.Command);
                                                    Cmds.Add(CG.Command, CG);
                                                }
                                            }
                                        }


                                    }
                                }
                            }
                        }
                    }
                }
                Log.info(log:"Discovered " + Cmds.Count.ToString() + " total commands");
            }
            catch (ReflectionTypeLoadException e)
            {
                MessageFactory.Post(Destinations.DEST_LOCAL, "FAILURE!!!\n\n[Assembly load failure]", UUID.Zero);
                foreach (Exception X in e.LoaderExceptions)
                {
                    MessageFactory.Post(Destinations.DEST_LOCAL, X.Message + "\n \nSTACK: " + X.StackTrace, UUID.Zero);
                    
                }
            }

        }

        public void PrintHelpAll(Destinations dest, UUID uid)
        {

            for (int i = 0; i < Cmds.Count; i++)
            {


                KeyValuePair<string, CommandGroup> kvp = Cmds.ElementAt(i);

                CommandHelp HE = kvp.Value.cmdUsage;
                if (dest == Destinations.DEST_GROUP)
                {
                    if (!HE.hasGroupFlag())
                    {
                        //return;
                    }
                    else
                    {
                        MessageFactory.Post(dest, HE.GetUsage(), uid);
                        
                    }
                }
                else
                {
                    MessageFactory.Post(dest, HE.GetUsage(), uid);

                }

                //                MHEx(dest, uid, kvp.Value.cmdUsage.GetUsage());

            }
        }

        public void PrintHelp(Destinations dest, string cmd, UUID uid)
        {
            try
            {

                CommandHelp HE = Cmds[cmd].cmdUsage;
                if (dest == Destinations.DEST_GROUP)
                {
                    if (!HE.hasGroupFlag())
                    {
                        //return; // DO NOT SCHEDULE THIS HELP INFO FOR GROUP!!!
                    }
                }
                MessageFactory.Post(dest, Cmds[cmd].cmdUsage.GetUsage(), uid);
                
            }
            catch (Exception e)
            {
                MessageFactory.Post(dest, "Error: Unknown command", uid);
                
            }
        }

        public void RunCommand(string cmdString, UUID user, int level, Destinations source, UUID agentKey, string agentName)
        {
            int pos = 0;
            string[] cmdStruct = cmdString.Split(' ');
            int IgnoreCount = 0;
            foreach (string S in cmdStruct)
            {
                if (IgnoreCount > 0) { IgnoreCount--; }
                else
                {

                    // Search for Command
                    if (Cmds.ContainsKey(S))
                    {
                        // this must be a command
                        // argument types will ALWAYS be structured like this:
                        // UUID, int, GridClient, [args up to argCount], optional:{SysOut}

                        CommandGroup cgX = Cmds[S];
                        if (level >= cgX.minLevel)
                        {
                            // Check that the destination is allowed.
                            // If not then skip this command entirely
                            Destinations dests = cgX.CommandSource;
                            bool Allowed = false;
                            if ((dests & Destinations.DEST_AGENT) == source) Allowed = true;
                            if ((dests & Destinations.DEST_GROUP) == source) Allowed = true;
                            if ((dests & Destinations.DEST_LOCAL) == source) Allowed = true;
                            if ((dests & Destinations.DEST_DISCORD) == source) Allowed = true; 

                            if (!Allowed)
                            {
                                IgnoreCount = cgX.arguments;

                            }
                            else
                            {
                                if (MainConfiguration.Instance.DisabledCommands.Contains(cgX.Command))
                                {
                                    BaseCommands.MH(source, user, "Function: '"+cgX.AssignedMethod.Name+"' associated with command '"+cgX.Command+"' is disabled by a administrator");
                                    return;
                                }
                                var ovj = Activator.CreateInstance(cgX.AssignedMethod.DeclaringType);
                                string[] additionalArgs = new string[cgX.arguments];
                                IgnoreCount = cgX.arguments;
                                for (int i = 1; i <= cgX.arguments; i++)
                                {
                                    additionalArgs[i - 1] = cmdStruct[pos + i];
                                }
                                pos++;
                                //(UUID client, int level, string[] additionalArgs,
                                //Destinations source,
                                //UUID agentKey, string agentName)
                                try
                                {
                                    Thread CMDThread = new Thread(() =>
                                    {
                                        try
                                        {
                                            cgX.AssignedMethod.Invoke(ovj, new object[] { user,level,additionalArgs, source,agentKey, agentName });
                                        }catch(Exception e)
                                        {
                                            BotSession.Instance.grid.Self.Chat("Exception caught when executing a command\n" + e.Message + "\nStacktrace: " + e.StackTrace, 0, ChatType.Shout);
                                            //MessageFactory.Post(Destinations.DEST_LOCAL, "Exception caught when executing a command\n" + e.Message + "\nStacktrace: " + e.StackTrace, UUID.Zero);
                                        }
                                    });
                                    CMDThread.Start();
                                }catch(Exception e)
                                {
                                    MessageFactory.Post(Destinations.DEST_LOCAL, "EXCEPTION CAUGHT WHEN EXECUTING COMMAND\n\n" + e.Message + "\nSTACK\n" + e.StackTrace, UUID.Zero);
                                }
                            }
                        }
                    }
                }
            }
        }



    }
}
