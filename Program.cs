using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Web.Script.Serialization;

namespace DSDeaths {

    public class Game {
        public string process { get; set; }
        public int[] offsets { get; set; }
        public bool isWow64 { get; set; } // true = game is a 32-bit process and runs in 64-bit emulation
    }

    class Program {

        const int PROCESS_WM_READ = 0x0010;

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool ReadProcessMemory(
            IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWow64Process(
            [In] IntPtr hProcess,
            [Out] out bool wow64Process
        );

        static bool WriteDeaths(in string template, in string formatToken, in int deaths) {
            try {
                string s = template.Replace(formatToken, deaths.ToString());
                File.WriteAllText("deaths.txt", s);
            } catch (IOException) {
                return false;
            }
            return true;
        }

        static bool PeekMemory(in IntPtr handle, in IntPtr baseAddress, in Game game, ref int value) {
            long address = baseAddress.ToInt64();
            byte[] buffer = new byte[8];
            int discard = 0;

            foreach (int offset in game.offsets) {
                address += offset;

                if (!ReadProcessMemory(handle, (IntPtr)address, buffer, 8, ref discard)) {
                    return false;
                }

                address = game.isWow64 ? BitConverter.ToInt32(buffer, 0) : BitConverter.ToInt64(buffer, 0);
                if (address == 0) {
                    return false;
                }
            }
            value = (int)address;
            return true;
        }

        static bool ScanProcesses(in Dictionary<string, Game> games, ref Process proc, ref Game game) {
            foreach (var kv in games) {
                Game g = kv.Value;
                Process[] process = Process.GetProcessesByName(g.process);
                if (process.Length != 0) {
                    IsWow64Process(process[0].Handle, out bool isWow64);
                    // used to differtiate between regular ds2 and sotfs ds2
                    if (isWow64 != g.isWow64)
                    {
                        continue;
                    }

                    Print("Found: " + kv.Key);
                    proc = process[0];
                    game = g;
                    return true;
                }
            }
            return false;
        }

        static void Print(in string msg)
        {
            string time = DateTime.Now.ToString("t");
            Console.WriteLine(time + " " + msg);
        }

        static void Main() {

            const string configPath = "config.json";
            const string templatePath = "template.txt";
            const string templateFormatToken = "$deaths";
            string template = "";

            Console.WriteLine("-----------------------------------WARNING-----------------------------------");
            Console.WriteLine(" Does NOT work with Elden Ring if Easy Anti-Cheat (EAC) is running.");
            Console.WriteLine(" Possible risk of BANS by trying to use with EAC enabled or disabling EAC.");
            Console.WriteLine(" USE AT YOUR OWN RISK.");
            Console.WriteLine("-----------------------------------WARNING-----------------------------------\n");
            Console.WriteLine("DSDeaths - https://github.com/Quidrex/DSDeaths");
            Console.WriteLine("INFO: Use the template.txt file to define a custom text.");
            Console.WriteLine("      " + templateFormatToken + " will be replaced with the actual deaths.\n");


            // try to load the game configs
            string json;
            if (File.Exists(configPath))
            {
                // file found
                json = File.ReadAllText(configPath);
            }
            else
            {
                // file not found, create default config file
                json = Config.json;
                Print(configPath + " created");
                File.WriteAllText(configPath, json);
            }

            // convert the json to an c# object
            JavaScriptSerializer jss = new JavaScriptSerializer();
            Dictionary<string, Game> games;
            try
            {
               games = jss.Deserialize<Dictionary<string, Game>>(json);
            }
            catch(Exception)
            {
                Print("Could not load " + configPath + ". Using default config.");
                games = jss.Deserialize<Dictionary<string, Game>>(Config.json);
            }

            // try to load the template text
            if (File.Exists(templatePath))
            {
                // file found
                template = File.ReadAllText(templatePath);
            }
            else
            {
                // file not found, create empty template text file
                Print(templatePath + " created.");
                File.Create(templatePath).Dispose();
            }

            // template file is empty, use no template text
            if (string.IsNullOrEmpty(template)){
                template = templateFormatToken;
            }

            Console.CancelKeyPress += delegate {
                WriteDeaths(template, templateFormatToken, 0);
            };

            while (true) {
                WriteDeaths(template, templateFormatToken, 0);
                Print("Waiting for the game to start...");

                Process proc = null;
                Game game = null;

                while (!ScanProcesses(games, ref proc, ref game)) {
                    Thread.Sleep(500);
                }

                IntPtr handle = OpenProcess(PROCESS_WM_READ, false, proc.Id);
                IntPtr baseAddress = proc.MainModule.BaseAddress;
                int oldValue = 0, value = 0;

                while (!proc.HasExited) {
                    if (PeekMemory(handle, baseAddress, game, ref value)) {
                        if (value != oldValue) {
                            oldValue = value;
                            WriteDeaths(template, templateFormatToken, value); ;
                        }
                        Thread.Sleep(500);
                    }
                }
                Print("Game was closed.\n");
                Thread.Sleep(2000);
            }
        }
    }
}
