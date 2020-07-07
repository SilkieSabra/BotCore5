using Bot.Assemble;
using Bot.WebHookServer;
using OpenMetaverse;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Bot.CommandSystem
{
    class BaseHooks : BaseCommands
    {

        [WebhookAttribs("/help")]
        public WebhookRegistry.HTTPResponseData showHelp(List<string> arguments, string body, string method, NameValueCollection headers)
        {
            WebhookRegistry.HTTPResponseData httpReply = new WebhookRegistry.HTTPResponseData();
            CommandRegistry reg = CommandRegistry.Instance;

            string Final = "<body bgcolor='black'><style type='text/css'>table.HelpTable {  border: 5px solid #1C6EA4;" +
                "  background - color: #000000; " +
                "  width: 100 %;            text - align: left;            border - collapse: collapse;" +
                "        }        table.HelpTable td, table.HelpTable th        {            border: 3px solid #AAAAAA;" +
                "  padding: 3px 2px;        }        table.HelpTable tbody td {  font-size: 19px;  color: #69FAF7;" +
                "}    table.HelpTable tr:nth-child(even)    {    background: #000000;}    table.HelpTable thead" +
                "    {        background: #26A486;  background: -moz-linear-gradient(top, #5cbba4 0%, #3bad92 66%, #26A486 100%);" +
                "  background: -webkit-linear-gradient(top, #5cbba4 0%, #3bad92 66%, #26A486 100%);" +
                "  background: linear-gradient(to bottom, #5cbba4 0%, #3bad92 66%, #26A486 100%);" +
                "  border-bottom: 2px solid #444444;}    table.HelpTable thead th {  font-size: 25px;" +
                "  font-weight: bold;  color: #FFFFFF;  text-align: center;  border-left: 2px solid #D0E4F5;" +
                "}table.HelpTable thead th:first-child {  border-left: none;}table.HelpTable tfoot td {  font-size: 14px;" +
                "}table.HelpTable tfoot.links{    text-align: right;}table.HelpTable tfoot.links a{display: inline - block;" +
                "background: #1C6EA4;  color: #FFFFFF;  padding: 2px 8px;    border - radius: 5px;}</style>";

            Final += "<table class='HelpTable'><thead><tr><th>Bot Version</th><th>"+ASMInfo.BotVer+"</th></tr></table><br/>";

            Final += "<table class='HelpTable'><thead><tr><th>Command</th><th>Minimum Level Required</th><th>Usage</th><th>Allowed Sources</th><th>Number of Arguments required</th></thead><tbody>";
            foreach (KeyValuePair<string, CommandGroup> cmd in reg.Cmds)
            {
                // Command
                Final += "<tr><td>" + cmd.Value.Command + "</td>";
                // Level
                Final += "<td>" + cmd.Value.minLevel.ToString() + "</td>";
                // Usage
                Final += "<td>" + cmd.Value.cmdUsage.RawUsage() + "</td>";
                // Allowed Sources
                Final += "<td>" + cmd.Value.CommandSource + "</td>";
                // # Arguments
                Final += "<td>" + cmd.Value.arguments.ToString() + "</td></tr>";
            }
            Final += "</tbody></table>";

            Final += "<table class='HelpTable'><thead><tr><th>Hook Path</th><tr></thead><tbody>";
            WebhookRegistry regx = WebhookRegistry.Instance;
            foreach(KeyValuePair<string, WebhookAttribs> hooks in regx.hooks)
            {
                Final += "<tr><td>" + hooks.Value.Path + "</td></tr>";
            }
            Final += "</tbody></table>";

            Final += "<br/><table class='HelpTable'><thead><tr><th>Assembly</th><th>Version</th><th># Of Commands</th><th>Total Classes</th></tr></thead><tbody>";

            foreach (Assembly A in AppDomain.CurrentDomain.GetAssemblies())
            {
                Final += "<tr><td>" + A.GetName().Name + "</td><td>" + A.GetName().Version + "</td>";
                int TotalCommandsContained = 0;
                int TotalClasses = 0;
                foreach (Type T in A.GetTypes())
                {
                    if (T.IsClass)
                    {
                        TotalClasses++;
                        foreach (MethodInfo MI in T.GetMethods())
                        {
                            CommandGroup[] CG = (CommandGroup[])MI.GetCustomAttributes(typeof(CommandGroup), false);
                            TotalCommandsContained += CG.Length;
                        }
                    }
                }

                Final += "<td>" + TotalCommandsContained.ToString() + "</td><td>" + TotalClasses.ToString() + "</td></tr>";
            }
            Final += "</tbody></table>";


            httpReply.ReplyString = Final;
            httpReply.Status = 200;
            httpReply.ReturnContentType = "text/html";

            return httpReply;
        }



        [CommandGroup("show_level", 0, 0, "This command shows your current auth level if any.", Destinations.DEST_AGENT | Destinations.DEST_DISCORD | Destinations.DEST_LOCAL | Destinations.DEST_GROUP)]
        public void show_level(UUID client, int level, string[] additionalArgs,
                                Destinations source,
                                UUID agentKey, string agentName)
        {
            MHE(source, client, "Hi secondlife:///app/agent/" + agentKey.ToString() + "/about !! Your authorization level is " + level.ToString());
        }

        [CommandGroup("show_version", 0, 0, "Outputs the bot version", Destinations.DEST_AGENT | Destinations.DEST_LOCAL)]
        public void show_version(UUID client, int level,  string[] additionalArgs,
                                 Destinations source,
                                UUID agentKey, string agentName)
        {
            MHE(source, client, "Version " + ASMInfo.BotVer.ToString());
        }



        [CommandGroup("show_admins", 4, 0, "Outputs all admin users", Destinations.DEST_AGENT | Destinations.DEST_LOCAL)]
        public void show_admins(UUID client, int level, string[] additionalArgs, Destinations source,
                                UUID agentKey, string agentName)
        {

            for (int i = 0; i < MainConfiguration.Instance.BotAdmins.Count; i++)
            {
                MHE(source, client, "secondlife:///app/agent/" + MainConfiguration.Instance.BotAdmins.ElementAt(i).Key.ToString() + "/about [" + MainConfiguration.Instance.BotAdmins.ElementAt(i).Value.ToString() + "] " + MainConfiguration.Instance.BotAdmins.ElementAt(i).Key.ToString());
            }
        }


        [CommandGroup("terminate_bot", 5, 0, "", Destinations.DEST_LOCAL | Destinations.DEST_AGENT | Destinations.DEST_DISCORD | Destinations.DEST_GROUP)]
        public void PerformExit(UUID client, int level, string[] additionalArgs, Destinations source, UUID agentKey, string agentName)
        {
            MHE(source, client, "Bot exit initiated.");
            BotSession.Instance.EnqueueExit = true;
        }
        // !!help
        [CommandGroup("!help", 1, 0, "Prints the entire help registry", Destinations.DEST_AGENT |Destinations.DEST_LOCAL | Destinations.DEST_GROUP)]
        [CommandGroup("bot.help", 1, 0, "Alias to !help", Destinations.DEST_AGENT | Destinations.DEST_LOCAL | Destinations.DEST_GROUP)]
        public void PrintAllHelp(UUID client, int level, string[] additionalArgs, Destinations source, UUID agentKey, string agentName)
        {
            if (MainConfiguration.Instance.UseSSL)
                MHE(source, client, $"All commands viewable at: https://{MainConfiguration.Instance.WebServerIP}:{MainConfiguration.Instance.WebServerPort}/help");
            else
                MHE(source, client, $"All commands viewable at: http://{MainConfiguration.Instance.WebServerIP}:{MainConfiguration.Instance.WebServerPort}/help");
        }
        // !help "command"
        [CommandGroup("help", 0, 1, "Prints help for one command", Destinations.DEST_AGENT | Destinations.DEST_LOCAL | Destinations.DEST_GROUP )]
        public void PrintHelp(UUID client, int level, string[] additionalArgs, Destinations source, UUID agentKey, string agentName)
        {
            CommandRegistry.Instance.PrintHelp(source, additionalArgs[0], client);
        }
    }
}
