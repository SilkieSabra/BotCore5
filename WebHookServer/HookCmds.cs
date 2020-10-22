using Bot.CommandSystem;
using OpenMetaverse;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace Bot.WebHookServer
{
    class HookCmds : BaseCommands
    {

        public HttpListener listener;
        public void OnWebHook(IAsyncResult ar)
        {
            HttpListenerContext CTX = null;
            try
            {
                CTX = listener.EndGetContext(ar);
            }
            catch (Exception e)
            {
                BotSession.Instance.Logger.info(log: "ERROR: Getting the end context for the listener failed");
                return;
            }
            listener.BeginGetContext(OnWebHook, null);


            Stream body = CTX.Request.InputStream;
            StreamReader SR = new StreamReader(body, CTX.Request.ContentEncoding);
            string Response = SR.ReadToEnd();

            if (!Directory.Exists("BotData/request_log")) Directory.CreateDirectory("BotData/request_log");


            string RequestPath = CTX.Request.RawUrl;
            if (RequestPath.EndsWith("/")) RequestPath = RequestPath.Substring(0, RequestPath.Length - 1);

            string CustomReplyStr = "";

            WebhookRegistry.HTTPResponseData reply = WebhookRegistry.Instance.RunCommand(RequestPath, Response, CTX.Request.Headers, CTX.Request.HttpMethod);


            CustomReplyStr = reply.ReplyString;
            byte[] buffer = Encoding.UTF8.GetBytes("\n" + CustomReplyStr);
            CTX.Response.ContentLength64 = buffer.Length;
            CTX.Response.AddHeader("Server", "1.7");
            CTX.Response.StatusCode = reply.Status;
            if (reply.ReturnContentType != "" && reply.ReturnContentType != null)
            {
                CTX.Response.ContentType = reply.ReturnContentType;
            }
            Stream output = CTX.Response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            output.Close();

        }


        [CommandGroup("webhook_auth", 4, "webhook_auth [github_name] [y/n]", Destinations.DEST_AGENT | Destinations.DEST_LOCAL | Destinations.DEST_GROUP)]
        public void WebHookAuthMgr(UUID client, int level, string[] additionalArgs,  Destinations source, UUID agentKey, string agentName)
        {
            MainConfiguration cfg = MainConfiguration.Instance;

            MHE(source, client, "Checking..");
            

            if (cfg.Authed(additionalArgs[0]))
            {
                if (additionalArgs[1] == "y")
                {
                    MHE(source, client, "Not modified. Already authorized");
                }
                else
                {
                    MHE(source, client, "Authorization revoked - git alerts from this user will not be whitelisted");
                    cfg.AuthedGithubUsers.Remove(additionalArgs[0]);
                }
            }
            else
            {
                if (additionalArgs[1] == "y")
                {
                    cfg.AuthedGithubUsers.Add(additionalArgs[0]);
                    MHE(source, client, "Authorized.");
                }
                else
                {
                    MHE(source, client, "Not modified. Already  not whitelisted");
                }
            }


            cfg.Save();
        }
    }
}
