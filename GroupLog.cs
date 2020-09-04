using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Bot.CommandSystem;
using OpenMetaverse;
using Bot.WebHookServer;
using System.Collections.Specialized;

namespace Bot
{
    public sealed class GroupLog : BaseCommands
    {

        private static readonly object _lock = new object();
        private static GroupLog _in;
        private static readonly object _writeLock = new object();

        static GroupLog() { }

        public static GroupLog Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_in == null) _in = new GroupLog();
                    return _in;
                }
            }
        }

        public void WriteLogEntry(string LogName, string ToAppend)
        {
            // Log filename will ALWAYS contain the date
            try
            {
                lock (_writeLock)
                {

                    string date = DateTime.Now.ToString("M-d-yyyy");
                    date += " " + LogName + ".log";

                    if (!Directory.Exists("BotData/GroupChatLogs")) Directory.CreateDirectory("BotData/GroupChatLogs");

                    date = "BotData/GroupChatLogs/" + date;

                    File.AppendAllText(date, "[" + DateTime.Now.ToString("hh:mm:ss") + "]: " + ToAppend + "\n");
                }
            }
            catch (Exception e) { }
        }

        public void WriteLogEntry(bool isLocal, bool isIM, string SenderName, UUID SenderID, string Message)
        {
            string filename = "";
            if (isLocal) filename = DateTime.Now.ToString("M-d-yyyy")+"-LocalChat.log";
            else  filename = DateTime.Now.ToString("M-d-yyyy") + "-"+SenderName + ".log";

            string LogFormat = MainConfiguration.Instance.IMAndChatFormat;
            LogFormat=LogFormat.Replace("%TIME%", DateTime.Now.ToString("hh:mm:ss"));
            LogFormat = LogFormat.Replace("%NAME%", SenderName);
            LogFormat = LogFormat.Replace("%MESSAGE%", Message);
            LogFormat = LogFormat.Replace("%UUID%", SenderID.ToString());

            filename = "BotData/GroupChatLogs/" + filename;
            try
            {
                lock (_writeLock)
                {
                    if (!Directory.Exists("BotData/GroupChatLogs")) Directory.CreateDirectory("BotData/GroupChatLogs");
                    File.AppendAllText(filename, LogFormat + "\n");
                }
            }catch(Exception e)
            {
                Console.WriteLine("Could not write a log using overload1 (chat or IM)\nError code: " + e.Message + "\nStack: " + e.StackTrace);
            }

        }

        private static readonly object _fileRead = new object();
        [CommandGroupMaster("Logging")]
        [CommandGroup("search_log", 5, "search_log [groupName] [search_term]  -  Searches for the search term in all logs relating to the group name (Use a underscore to show where spaces are!). The search term may also include the pipe (|) delimiter to include more than 1 word.", Destinations.DEST_AGENT | Destinations.DEST_LOCAL | Destinations.DEST_GROUP | Destinations.DEST_DISCORD)]
        public void search_log(UUID client, int level, string[] additionalArgs,
                                Destinations source,
                                UUID agentKey, string agentName)
        {
            string GrpName = additionalArgs[0].Replace('_', ' ');
            string[] search = additionalArgs[1].Split('|');

            DirectoryInfo di = new DirectoryInfo("BotData/GroupChatLogs");
            foreach (FileInfo fi in di.GetFiles())
            {
                // check if filename contains the group name
                string onlyName = Path.GetFileNameWithoutExtension(fi.Name);

                if (onlyName.Contains(GrpName))
                {
                    // read file
                    lock (_fileRead)
                    {
                        foreach (string S in File.ReadLines("BotData/GroupChatLogs/" + onlyName + ".log"))
                        {
                            foreach (string V in search)
                            {
                                if (S.Contains(V, StringComparison.OrdinalIgnoreCase))
                                {

                                    MHE(source, client, "{[https://zontreck.dev:35591/viewlog/" + Uri.EscapeUriString(onlyName) + " " + onlyName + "]} " + S);
                                }
                            }
                        }


                    }
                }

            }

            MHE(source, client, ".\n \n[Search Completed]");
        }



        [WebhookAttribs("/viewlog/%", HTTPMethod = "GET")]
        public WebhookRegistry.HTTPResponseData View_Log(List<string> arguments, string body, string method, NameValueCollection headers)
        {
            WebhookRegistry.HTTPResponseData rd = new WebhookRegistry.HTTPResponseData();

            string FinalOutput = "";
            lock (_fileRead)
            {
                try
                {

                    foreach (string s in File.ReadLines("BotData/GroupChatLogs/" + Uri.UnescapeDataString(arguments[0]) + ".log"))
                    {
                        string tmp = s;
                        string[] Ltmp = tmp.Split(' ');
                        tmp = "";
                        foreach (string K in Ltmp)
                        {
                            if (K.StartsWith("secondlife://"))
                            {
                                // DO NOT ADD TO OUTPUT
                            }
                            else
                            {
                                tmp += K + " ";
                            }
                        }

                        FinalOutput += tmp + "<br/>";

                    }
                    rd.Status = 200;
                    rd.ReplyString = FinalOutput;
                    rd.ReturnContentType = "text/html";
                }
                catch (Exception e)
                {
                    rd.Status = 418;
                    rd.ReplyString = "You burned... the tea";
                }
            }

            return rd;
        }

        [WebhookAttribs("/logs", HTTPMethod = "GET")]
        public WebhookRegistry.HTTPResponseData List_Logs(List<string> arguments, string body, string method, NameValueCollection headers)
        {
            WebhookRegistry.HTTPResponseData hrd = new WebhookRegistry.HTTPResponseData();
            hrd.Status = 200;
            hrd.ReplyString = "<center><h2>Group Chat Logs</h2></center>";
            DirectoryInfo di = new DirectoryInfo("BotData/GroupChatLogs");
            foreach (FileInfo fi in di.GetFiles())
            {
                hrd.ReplyString += "<br/><a href='/viewlog/" + Path.GetFileNameWithoutExtension(fi.Name) + "'> " + fi.Name + "</a>";
            }
            hrd.ReturnContentType = "text/html";

            return hrd;
        }
    }
}
