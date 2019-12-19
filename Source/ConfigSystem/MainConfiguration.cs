/*
Copyright © 2019 Tara Piccari (Aria; Tashia Redrose)
Licensed under the AGPL-3.0
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Bot
{
    [Serializable()]
    public class MainConfiguration : IConfig
    {
        public float ConfigVersion
        {
            get; set;
        }
        public string ConfigFor { get; set; }
        public List<string> Data
        {
            get; set;
        }

        public string MainProgramDLL;
        public string first { get; set; }
        public string last { get; set; }
        public string password { get; set; }

        //public License LicenseKey { get; set; }
        public string ActivationCode { get; set; }

        public MainConfiguration()
        {

        }

        public static MainConfiguration Load()
        {
            MainConfiguration X = new MainConfiguration();
            SerialManager sm = new SerialManager();
            try
            {
                X = sm.Read<MainConfiguration>("Main");
                return X;
            }
            catch (FileNotFoundException e)
            {
                Console.WriteLine("Main.json was not found");
                return new MainConfiguration();
            }
        }
    }
}
