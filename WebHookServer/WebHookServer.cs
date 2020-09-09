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
using System.Security.Cryptography.X509Certificates;
using System.Reflection;

namespace Bot.WebHookServer
{
    class GitServer : IProgram
    {
        public HttpListener listener;
        public string ProgramName
        {
            get { return "GitServer"; }
        }

        public float ProgramVersion
        {
            get { return 1.7f; }
        }

        public void getTick()
        {
            return;
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
        public void run()
        {
            if (listener != null) return;// Already had run triggered
            try
            {
                listener = new HttpListener();

                if (MainConfiguration.Instance.UseSSL)
                {

                    X509Certificate cert = new X509Certificate2("BotData/"+MainConfiguration.Instance.SSLCertificatePFX, MainConfiguration.Instance.SSLCertificatePWD);

                    Type hepmType = Type.GetType("System.Net.HttpEndPointManager, System.Net.HttpListener");
                    Type heplType = Type.GetType("System.Net.HttpEndPointListener, System.Net.HttpListener");
                    MethodInfo getEPListener = hepmType.GetMethod("GetEPListener", BindingFlags.Static | BindingFlags.NonPublic);
                    FieldInfo heplCert = heplType.GetField("_cert", BindingFlags.NonPublic | BindingFlags.Instance);
                    object epl = getEPListener.Invoke(null, new object[] { "+", MainConfiguration.Instance.WebServerPort, listener, true });
                    heplCert.SetValue(epl, cert);
                    listener.Prefixes.Add($"https://*:{MainConfiguration.Instance.WebServerPort}/");
                }
                else
                    listener.Prefixes.Add($"http://*:{MainConfiguration.Instance.WebServerPort}/");


                
                listener.Start();
                var hc = new HookCmds();
                hc.listener = listener;

                listener.BeginGetContext(hc.OnWebHook, null);

                AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            }catch(Exception e)
            {
                //MessageFactory.Post(Destinations.DEST_LOCAL, "Error: Program could not escalate to Admin Privileges. WebHook engine not running\n\n" + e.Message + "\n" + e.StackTrace, UUID.Zero);
                
            }
        }

        private void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            listener.Stop();
        }
    }
}
