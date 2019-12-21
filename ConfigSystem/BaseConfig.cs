using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot
{
    class BaseConfig : IConfig
    {
        public float ConfigVersion { get; set; }
        public string ConfigFor { get; set; }
        public List<string> Data { get; set; }
    }
}
