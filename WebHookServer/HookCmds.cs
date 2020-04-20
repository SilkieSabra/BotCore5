using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace Bot.WebHookServer
{
    class HookCmds
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

            if (!Directory.Exists("request_log")) Directory.CreateDirectory("request_log");


            string RequestPath = CTX.Request.RawUrl;
            if (RequestPath.EndsWith("/")) RequestPath = RequestPath.Substring(0, RequestPath.Length - 1);

            string CustomReplyStr = "";

            WebhookRegistry.HTTPResponseData reply = WebhookRegistry.Instance.RunCommand(RequestPath, Response, CTX.Request.Headers, CTX.Request.HttpMethod);


            CustomReplyStr = reply.ReplyString;
            byte[] buffer = Encoding.UTF8.GetBytes("\n" + CustomReplyStr);
            CTX.Response.ContentLength64 = buffer.Length;
            CTX.Response.AddHeader("Server", "1.6");
            CTX.Response.StatusCode = reply.Status;
            if (reply.ReturnContentType != "" && reply.ReturnContentType != null)
            {
                CTX.Response.ContentType = reply.ReturnContentType;
            }
            Stream output = CTX.Response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            output.Close();

        }
    }
}
