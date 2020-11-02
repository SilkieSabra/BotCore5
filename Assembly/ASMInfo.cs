using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using LibZNI;

[assembly: AssemblyCompany("ZNI")]
[assembly: AssemblyAlgorithmId(System.Configuration.Assemblies.AssemblyHashAlgorithm.MD5)]
[assembly: AssemblyCopyright("(C) 2020 Tara Piccari")]
[assembly: AssemblyFileVersion("5.0.5.1261")]
[assembly: AssemblyDescription("Second Life Bot - BotCore5")]
[assembly: AutoUpdater("/job/Bot", "!os!.tar")]

namespace Bot.Assemble
{
    public class ASMInfo
    {
        public static string BotName = "ZBotCore";
        public static string BotVer = "5.0.5.1261";
        public static string GitPassword
        {
            get
            {
                return MainConfiguration.Instance.GitPassword;
            }
        }
    }
}
