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
using Bot.CommandSystem;
using OpenMetaverse;
using System.IO;
using System.Net.Http;
using System.Net;
using Newtonsoft.Json;

namespace Bot.WebHookServer
{
    class GitServer : IProgram
    {
        public HttpListener listener;
        public MessageHandler.MessageHandleEvent MHEx;
        public string ProgramName
        {
            get { return "GitServer"; }
        }

        public float ProgramVersion
        {
            get { return 1.6f; }
        }

        public string getTick()
        {

            return "";
        }

        public void passArguments(string data)
        {
            // dont throw, just silently do nothing
        }

        public void LoadConfiguration()
        {

        }

        public void onIMEvent(object sender, InstantMessageEventArgs e)
        {
        }
        public void run(GridClient client, MessageHandler MH, CommandRegistry registry)
        {
            try
            {
                listener = new HttpListener();
                MHEx = MH.callbacks;
                if (MainConfiguration.Instance.UseSSL)
                    listener.Prefixes.Add($"https://*:{MainConfiguration.Instance.WebServerPort}/");
                else
                    listener.Prefixes.Add($"http://*:{MainConfiguration.Instance.WebServerPort}/");
                
                listener.Start();
                var hc = new HookCmds();
                hc.listener = listener;

                listener.BeginGetContext(hc.OnWebHook, null);

            }catch(Exception e)
            {
                BotSession.Instance.MHE(MessageHandler.Destinations.DEST_LOCAL, UUID.Zero, "Error: Program could not escalate to Admin Privileges. WebHook engine not running");
            }
        }
    }
}
