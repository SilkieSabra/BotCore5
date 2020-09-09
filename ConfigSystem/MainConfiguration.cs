using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using OpenMetaverse;
using Newtonsoft.Json;

namespace Bot
{
    [Serializable()]
    public sealed class MainConfiguration : IConfig
    {
        private static readonly object lck = new object();
        private static MainConfiguration inst = null;

        static MainConfiguration() { }

        public static MainConfiguration Instance
        {
            get
            {
                if(inst == null)
                {
                    lock (lck)
                    {
                        inst = new MainConfiguration();
                        inst.Load();
                        return inst;
                    }
                }
                else
                {
                    return inst;
                }
            }
        }
        public float ConfigVersion
        {
            get; set;
        }
        public string ConfigFor { get; set; }
        public List<string> Data
        {
            get; set;
        }

        public string first { get; set; } = "";
        public string last { get; set; } = "";
        public string password { get; set; } = "";

        public string SecretNewVerCode { get; set; } = "configure_me";

        //public License LicenseKey { get; set; }
        public string ActivationCode { get; set; } = "";

        public string GitPassword { get; set; } = "NOT_SET";

        [JsonProperty(PropertyName = "RelogAfter")]
        public double AutoRelogAfterHours = 12;

        public int WebServerPort { get; set; } = 35591;
        public string WebServerIP { get; set; } = "zontreck.dev";

        public bool UseSSL { get; set; } = false;
        public string SSLCertificatePFX { get; set; } = "certificate.pfx";
        public string SSLCertificatePWD { get; set; } = "";

        public bool LogChatAndIMs { get; set; } = false;
        public string IMAndChatFormat { get; set; } = "[%TIME%] (%NAME%): %MESSAGE%";


        public bool GreeterEnabled { get; set; } = false;
        public string GreeterMessage { get; set; } = "";
        public bool VisitorLogEnabled { get; set; } = false;

        public Dictionary<UUID, int> BotAdmins { get; set; } = new Dictionary<UUID, int>();

        public List<string> AuthedGithubUsers { get; set; } = new List<string>();

        public List<string> DisabledCommands { get; set; } = new List<string>();

        public bool Authed(string GHLogin)
        {
            if (AuthedGithubUsers.Contains(GHLogin)) return true;
            else return false;
        }

        public void Load()
        {
            if (File.Exists("Main.json")) File.Move("Main.json", Path.Combine("BotData", "Main.json"));
            inst = new MainConfiguration();
            SerialManager sm = new SerialManager();
            try
            {
                inst = sm.Read<MainConfiguration>("Main");


            }
            catch (FileNotFoundException e)
            {
                BotSession.Instance.Logger.info(true, "Main.json does not exist");

            }
        }

        public void Save()
        {
            SerialManager sm = new SerialManager();
            sm.Write<MainConfiguration>("Main", this);
            sm = null;
        }
    }
}
