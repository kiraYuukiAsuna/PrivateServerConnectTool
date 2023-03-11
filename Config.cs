using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;

namespace PrivateServerConnectTool
{
    public class Config
    {
        public bool useHTTPS { get; set; }
        public string serverIPAddress { get; set; }
        public int proxyPort { get; set; }

        public string gameExeFilePath { get; set; }
    }

    public class ConfigManager
    {
        public bool SaveConfigToFile(ref Config config, string fileName)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string jsonString = JsonSerializer.Serialize(config, options);

                File.WriteAllText(fileName, jsonString);

                Console.WriteLine("Write Config To File:");
                Console.WriteLine(jsonString);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                return false;
            }
        }

        public bool LoadConfigFromFile(ref Config config, string fileName)
        {
            try
            {
                if (File.Exists(fileName))
                {
                    string jsonString = File.ReadAllText(fileName);
                    config =  JsonSerializer.Deserialize<Config>(jsonString);

                    Console.WriteLine("Read Config From File:");
                    Console.WriteLine(jsonString);
                }


                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                return false;
            }
        }

    }
}
