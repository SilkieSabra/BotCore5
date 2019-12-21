using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Bot
{
    public class PluginActivator
    {
        public Assembly LoadedASM = null;
        public void LoadLibrary(string DLL)
        {

            LoadedASM = Assembly.LoadFrom(DLL);
        }

        public List<IProgram> Activate(Assembly asm)
        {
            List<IProgram> Plugins = new List<IProgram>();
            foreach (Type A in asm.GetTypes())
            {
                Type check = A.GetInterface("IProgram");
                if (check == null)
                {
                    //return null;
                }
                else
                {
                    IProgram plugin = Activator.CreateInstance(A) as IProgram;
                    Plugins.Add(plugin);
                }
            }
            return Plugins;
        }
    }
}
