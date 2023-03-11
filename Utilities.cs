using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace PrivateServerConnectTool
{
    class Utilities
    {
        public static string CalculateMD5(string filename)
        {
            try
            {
                using (var md5 = MD5.Create())
                {
                    using (var stream = File.OpenRead(filename))
                    {
                        var hash = md5.ComputeHash(stream);
                        return BitConverter.ToString(hash).Replace("-", "");
                    }
                }
            }
            catch (Exception)
            {
                return "Unknown";
            }

        }

        public static void EndProcess(string taskname)
        {
            var Processes = Process.GetProcesses().Where(pr => pr.ProcessName == taskname);
            foreach (var process in Processes)
            {
                process.Kill();
            }
        }

        public static void ExecuteCMD(String strCommand)
        {
            try
            {
                Console.WriteLine(strCommand);
                ProcessStartInfo commandInfo = new ProcessStartInfo();
                commandInfo.CreateNoWindow = true;
                commandInfo.UseShellExecute = false;
                commandInfo.RedirectStandardInput = false;
                commandInfo.RedirectStandardOutput = false;
                commandInfo.FileName = "cmd.exe";
                commandInfo.Arguments = strCommand;
                if (commandInfo != null)
                {
                    Process process = Process.Start(commandInfo);
                    if (process != null)
                    {
                        //process.WaitForExit();
                        process.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                // skip
            }
        }

        public static void Log(string message, ConsoleColor color = ConsoleColor.White)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " :" + message);
            Console.ResetColor();
        }


    }
}
