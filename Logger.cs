using System;

namespace Bot
{
    public class Logger
    {
        string LogName;
        public Logger(string Name)
        {
            LogName = Name;
            info(log: "Logger initialized for " + Name);
            BotSession.Instance.Logger = this;
        }

        string InputData = ""; // Data entered into the console
        string ReturnData = "";

        public void info(bool Restore = true, params string[] log)
        {
            foreach (string val in log)
            {
                string pad = "";
                if (InputData.Length > 0)
                {
                    foreach (char v in InputData)
                    {
                        pad += " ";
                    }
                }
                Console.Write($"\r{pad}   \r[{LogName}] [INFO] : {val}\n");
                if (InputData.Length > 0 && Restore)
                    Console.Write("\r> " + InputData);
                else
                    Console.Write("\r> ");
            }
        }

        public void DoPrompt()
        {
            var newKey = Console.ReadKey();
            switch (newKey.Key)
            {
                case ConsoleKey.Enter:
                    {
                        ReturnData = InputData;
                        InputData = "";
                        if (ReturnData.Length > 0)
                            Console.WriteLine();
                        else Console.Write("\r> ");
                        break;
                    }
                case ConsoleKey.Escape:
                    {
                        ReturnData = "";
                        info(Restore: false, "Cmd Canceled");
                        InputData = "";
                        break;
                    }
                case ConsoleKey.Backspace:
                    {
                        string pad = "   ";
                        foreach (char v in InputData)
                        {
                            pad += " ";
                        }
                        Console.Write("\r" + pad);
                        if (InputData.Length > 0)
                            InputData = InputData.Substring(0, InputData.Length - 1);
                        Console.Write("\r> " + InputData);

                        break;
                    }
                case ConsoleKey.Tab:
                    {
                        InputData += "\t";
                        break;
                    }
                default:
                    {

                        InputData += newKey.KeyChar;
                        break;
                    }
            }
            DoPrompt();
        }

        public string CheckForNewCmd()
        {
            if (ReturnData.Length != 0)
            {
                string Tmp = new string(ReturnData);
                ReturnData = "";
                return Tmp;
            }
            else
            {
                throw new Exception("Data not yet available");
            }
        }
    }
}
