/*
Copyright © 2019 Tara Piccari (Aria; Tashia Redrose)
Licensed under the AGPL-3.0
*/


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot
{
    public interface IConfig
    {
        float ConfigVersion { get; set; }

        string ConfigFor { get; set; }

        List<string> Data { get; set; }
    }
}
