using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse;
using System.Threading.Tasks;

namespace Bot
{
    public sealed class BotSession
    {
        private static BotSession _inst = null;
        private static readonly object lockHandle = new object();

        static BotSession()
        {

        }

        public static BotSession Instance
        {
            get
            {
                lock (lockHandle)
                {
                    if (_inst == null)
                    {
                        _inst = new BotSession();
                    }
                    return _inst;
                }
            }
        }


        public GridClient grid { get; set; }
        public Logger Logger { get; set; }
        public MessageHandler.MessageHandleEvent MHE;
        public MessageHandler MH;

        public MainConfiguration ConfigurationHandle { 
            get {
                return MainConfiguration.Instance;
            } 
        }


        public DateTime LaunchTime { get; set; } = DateTime.Now;
        public bool WaitForFiveMinutes = false;
    }
}
