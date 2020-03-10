using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using OpenMetaverse;

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

        public string MainProgramDLL = "BlankBot.dll";
        public string first { get; set; } = "";
        public string last { get; set; } = "";
        public string password { get; set; } = "";

        //public License LicenseKey { get; set; }
        public string ActivationCode { get; set; } = "";

        public string GitPassword { get; set; } = "NOT_SET";

        public Dictionary<UUID, int> BotAdmins { get; set; } = new Dictionary<UUID, int>();


        public void Load()
        {
            MainConfiguration X = new MainConfiguration();
            SerialManager sm = new SerialManager();
            try
            {
                X = sm.Read<MainConfiguration>("Main");

                MainProgramDLL = X.MainProgramDLL;
                first = X.first;
                last = X.last;
                ActivationCode = X.ActivationCode;
                password = X.password;
                GitPassword = X.GitPassword;
                BotAdmins = X.BotAdmins;

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
