using OpenMetaverse;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Reflection;


namespace Bot.NonCommands
{
    public class nRegistry
    {
        public static void Dispatch(string request, UUID agentKey, string agentName, MessageHandler.Destinations sourceLoc, UUID originator)
        {

            foreach(Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach(Type t in a.GetTypes())
                {
                    if (t.IsClass)
                    {
                        foreach (NotCommand NC in (NotCommand[])t.GetCustomAttributes(false))
                        {
                            MethodInfo mi = t.GetMethod("handle");

                            ThreadStart work = delegate
                            {
                                mi.Invoke(Activator.CreateInstance(mi.DeclaringType), new object[] { request, agentKey, agentName, sourceLoc, originator });
                            };
                            Thread T = new Thread(work);


                               // _mi.Invoke(Activator.CreateInstance(_mi.DeclaringType), new object[] {  });
                            T.Start();
                        }
                    }
                }
            }
        }
    }
}
