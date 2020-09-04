using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Newtonsoft.Json;



namespace Bot
{
    public class SerialManager // Handles saving a large amount of data to a binary file or vise-versa
    {
        /*
        public void Write<T>(string Name, T ObjectData)
        {
            Stream F = null;
            BinaryFormatter BinaryFormat = new BinaryFormatter();
            try
            {
                if (File.Exists(Name + ".bdf")) File.Copy(Name + ".bdf", Name + ".bdf.bak", true);
                F = new FileStream(Name + ".bdf", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                BinaryFormat.Serialize(F, ObjectData);
            }
            catch (SerializationException e)
            {
                Console.WriteLine(e.Message);
            }
            F.Close();
        }

        public T Read<T>(string Name)
        {

            if (File.Exists(Name + ".bdf") == false) throw new FileNotFoundException();

            Stream F = new FileStream(Name + ".bdf", FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            BinaryFormatter BinaryFormat = new BinaryFormatter();
            T deserial = default(T);
            try
            {
                deserial = (T)BinaryFormat.Deserialize(F);
            }
            catch (SerializationException e)
            {
                Console.WriteLine(e.Message);
            }
            catch (Exception e)
            {
                // 
                Console.WriteLine(e.Message);
            }

            F.Close();
            if (deserial == null) deserial = default(T);

            Console.WriteLine("Returning deserialized class");
            return deserial;
        }

    */
        private static readonly object _fileAccess = new object();
        public void Write<T>(string Name, T ObjectData)
        {
            string Json = JsonConvert.SerializeObject(ObjectData, Formatting.Indented);
            lock (_fileAccess)
            {

                try
                {
                    File.WriteAllText("BotData/"+Name + ".json", Json);
                } catch(Exception E)
                {
                    BotSession.Instance.Logger.info(true, E.Message);
                    
                }
            }

        }

        public T Read<T> (string Name)
        {
            lock(_fileAccess){

                try
                {

                    T obj = default(T);
                    string serial = File.ReadAllText("BotData/"+Name + ".json");
                    
                    obj = (T)JsonConvert.DeserializeObject<T>(serial);
                    BotSession.Instance.Logger.info(true, "Returning class object");
                    

                    if (obj == null) obj = default(T);
                    return obj;
                }
                catch (Exception e)
                {
                    BotSession.Instance.Logger.info(true, e.Message);
                    
                    throw new FileNotFoundException();
                }
            }

        }
    }
}
