using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

[assembly: AssemblyCompany("ZNI")]
[assembly: AssemblyAlgorithmId(System.Configuration.Assemblies.AssemblyHashAlgorithm.MD5)]
[assembly: AssemblyCopyright("(C) 2020 Tara Piccari")]
[assembly: AssemblyFileVersion("5.4.014")]
[assembly: AssemblyDescription("Second Life Bot - BotCore5")]
 

namespace Bot.Assemble
{
    public class ASMInfo
    {
        public static string BotName = "ZBotCore";
        public static string BotVer = "5.4.014";
        public static string GitPassword
        {
            get
            {
                return MainConfiguration.Instance.GitPassword;
            }
        }
    }
}
