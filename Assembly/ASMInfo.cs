using System;
using System.Collections.Generic;
using System.Text;

namespace Bot.Assemble
{
    public class ASMInfo
    {
        public static string BotName = "ZBotCore";
        public static double BotVer = 5.2;
        public static string GitPassword
        {
            get
            {
                return MainConfiguration.Load().GitPassword;
            }
        }
    }
}
