/*
Copyright © 2019 Tara Piccari (Aria; Tashia Redrose)
Licensed under the AGPL-3.0
*/


using System;
using System.Collections.Generic;
using System.Text;

namespace Bot
{
    public sealed class SysOut
    {

        private static SysOut _Inst = null;
        private static readonly object instloc = new object();

        static SysOut() { }

        public static SysOut Instance
        {
            get
            {
                lock (instloc)
                {
                    if(_Inst == null)
                    {
                        _Inst = new SysOut();
                        _Inst.FLAVOR = "Bot";
                    }
                    return _Inst;
                }
            }
        }
        public int tabs = 0;
        string FLAVOR;

        private static readonly object Locks = new object();


        public void info(string msg)
        {
            lock (Locks)
            {

                Console.ForegroundColor = ConsoleColor.White;
                System.Console.Write("[");
                Console.ForegroundColor = ConsoleColor.Green;
                System.Console.Write("INFO");
                Console.ForegroundColor = ConsoleColor.White;
                System.Console.Write("] [");
                Console.ForegroundColor = ConsoleColor.Red;
                System.Console.Write(FLAVOR);
                Console.ForegroundColor = ConsoleColor.White;
                System.Console.Write("] ");
                Console.ForegroundColor = ConsoleColor.Cyan;
                for (int i = 0; i < tabs; i++) { Console.Write("\t|"); }
                System.Console.Write(msg);
                Console.ForegroundColor = ConsoleColor.White;
                System.Console.Write("\n");
            }
        }

        public void debugf(bool enter, string label, string[] debugParams)
        {
            lock (Locks)
            {

                Console.ForegroundColor = ConsoleColor.White;
                System.Console.Write("[");
                Console.ForegroundColor = ConsoleColor.DarkRed;
                System.Console.Write("DEBUG");
                Console.ForegroundColor = ConsoleColor.White;
                System.Console.Write("] [");
                Console.ForegroundColor = ConsoleColor.Green;
                System.Console.Write(FLAVOR);
                Console.ForegroundColor = ConsoleColor.White;
                System.Console.Write("] ");
                Console.ForegroundColor = ConsoleColor.Magenta;

                if (enter)
                {
                    for (int i = 0; i < tabs; i++) { System.Console.Write("\t"); }
                    System.Console.Write("ENTER ");
                    tabs++;
                    Console.BackgroundColor = ConsoleColor.Cyan;
                    System.Console.Write(label);
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write(" ");
                }
                else
                {
                    tabs--;
                    for (int i = 0; i < tabs; i++) { System.Console.Write("\t"); }
                    System.Console.Write("LEAVE ");
                    Console.BackgroundColor = ConsoleColor.Cyan;
                    System.Console.Write(label);
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write(" ");
                }
                Console.Write("[");
                for (int i = 0; i < debugParams.Length; i++)
                {
                    Console.Write(debugParams[i] + ", ");
                }
                Console.Write("]");

                Console.ForegroundColor = ConsoleColor.White;
                System.Console.Write("\n");
                Console.ResetColor();
            }
        }

        public void debug(string m)
        {
            lock (Locks)
            {

                Console.ForegroundColor = ConsoleColor.White;
                System.Console.Write("[");
                Console.ForegroundColor = ConsoleColor.DarkRed;
                System.Console.Write("DEBUG");
                Console.ForegroundColor = ConsoleColor.White;
                System.Console.Write("] [");
                Console.ForegroundColor = ConsoleColor.Green;
                System.Console.Write(FLAVOR);
                Console.ForegroundColor = ConsoleColor.White;
                System.Console.Write("] ");
                Console.ForegroundColor = ConsoleColor.Magenta;

                for (int i = 0; i < tabs; i++)
                {
                    Console.Write("\t|");
                }
                Console.Write(" " + m);
                Console.Write("\n");
            }

        }
    }
}