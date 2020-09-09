using Bot.CommandSystem;
using OpenMetaverse;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

namespace Bot.WebHookServer
{
    class NewVersionWatcher
    {
        [WebhookAttribs("/NewVersionAvailable", HTTPMethod = "POST")]
        public WebhookRegistry.HTTPResponseData a_new_version_is_available(List<string> args, string body, string method, NameValueCollection headers)
        {
            if(MainConfiguration.Instance.SecretNewVerCode == body)
            {

                WebhookRegistry.HTTPResponseData hrd = new WebhookRegistry.HTTPResponseData();
                hrd.ReplyString = "OK";
                hrd.ReturnContentType = "text/plain";
                hrd.Status = 200;

                BaseCommands.MH(Destinations.DEST_LOCAL, UUID.Zero, "Alert: A new version is available. Restart required");

                BotSession.Instance.EnqueueExit = true;

                return hrd;
            } else
            {
                WebhookRegistry.HTTPResponseData hrd = new WebhookRegistry.HTTPResponseData();
                hrd.ReplyString = "Not authorized";
                hrd.ReturnContentType = "text/plain";
                hrd.Status = 500;
                return hrd;
            }
        }
    }
}
