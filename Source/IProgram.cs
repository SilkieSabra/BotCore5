﻿/*
Copyright © 2019 Tara Piccari (Aria; Tashia Redrose)
Licensed under the AGPL-3.0
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenMetaverse;

namespace Bot
{
    public interface IProgram
    {
        void run(GridClient client, MessageHandler MH, CommandSystem.CommandRegistry registry); // Define the run command since a thread needs a entry point

        string getTick(); // Run every second to check for queued data. If queue exists, then it will be returned as a JSON string.
        // getTick can reply with data for the serializer for instance.

        void passArguments(string data); // json!!

        string ProgramName { get; }
        float ProgramVersion { get; }

        void LoadConfiguration();
        void onIMEvent(object sender, InstantMessageEventArgs e);
    }
}
