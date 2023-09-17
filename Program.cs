using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DSDeaths
{
    class Game
    {
        public readonly string name;
        public readonly int[] offsets32;
        public readonly int[] offsets64;

        public Game(in string name, in int[] offsets32, in int[] offsets64)
        {
            this.name = name;
            this.offsets32 = offsets32;
            this.offsets64 = offsets64;
        }
    }

    class Program
    {
        const int PROCESS_WM_READ = 0x0010;
        const int PROCESS_QUERY_INFORMATION = 0x0400;

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool IsWow64Process(IntPtr hProcess, ref bool Wow64Process);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool ReadProcessMemory(
            IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);

        static readonly Game[] games =
        {
            new Game("DARKSOULS", new int[] {0xF78700, 0x5C}, null),
            new Game("DarkSoulsII", new int[] {0x1150414, 0x74, 0xB8, 0x34, 0x4, 0x28C, 0x100}, new int[] {0x16148F0, 0xD0, 0x490, 0x104}),
            new Game("DarkSoulsIII", null, new int[] {0x47572B8, 0x98}),
            new Game("DarkSoulsRemastered", null, new int[] {0x1C8A530, 0x98}),
            new Game("Sekiro", null, new int[] {0x3D5AAC0, 0x90}),
            new Game("eldenring", null, new int[] {0x3CD4D88, 0x94})
        };

        static bool Write(int value)
        {
            try
            {
                File.WriteAllText(Properties.Settings.Default.FileName, value.ToString());
                if (Properties.Settings.Default.EnableSammi)
                {
                    //Trigger SAMMI Button
                    SignalSAMMIAsync(value);
                }
            }
            catch (IOException)
            {
                Console.WriteLine("Could not write to DSDeaths.txt.");
                return false;
            }
            return true;
        }

        static async Task SignalSAMMIAsync(int value = 0)
        {
            System.Net.ServicePointManager.Expect100Continue = false;
            HttpClient client = new HttpClient();
            string json = string.Empty;
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            if (string.IsNullOrWhiteSpace(Properties.Settings.Default.SammiVariableName) == false)
            {
                json = JsonConvert.SerializeObject(new
                {
                    request = "setVariable",
                    name = Properties.Settings.Default.SammiVariableName,
                    value = value.ToString(),
                    buttonID = Properties.Settings.Default.SammiButtonID
                });
                httpContent = new StringContent(json, Encoding.UTF8, "application/json");
                await client.PostAsync(Properties.Settings.Default.SammiUrl, httpContent);
            }

            if (string.IsNullOrWhiteSpace(Properties.Settings.Default.SammiButtonID) == false)
            {
                json = JsonConvert.SerializeObject(new
                {
                    request = "triggerButton",
                    buttonID = Properties.Settings.Default.SammiButtonID
                });
                httpContent = new StringContent(json, Encoding.UTF8, "application/json");
                await client.PostAsync(Properties.Settings.Default.SammiUrl, httpContent);
            }
        }

        static bool PeekMemory(in IntPtr handle, in IntPtr baseAddress, bool isX64, in int[] offsets, ref int value)
        {
            long address = baseAddress.ToInt64();
            byte[] buffer = new byte[8];
            int discard = 0;

            foreach (int offset in offsets)
            {
                if (address == 0)
                {
                    Console.WriteLine("Encountered null pointer.");
                    return false;
                }

                address += offset;

                if (!ReadProcessMemory(handle, (IntPtr)address, buffer, 8, ref discard))
                {
                    Console.WriteLine("Could not read game memory.");
                    return false;
                }

                address = isX64 ? BitConverter.ToInt64(buffer, 0) : BitConverter.ToInt32(buffer, 0);
            }

            value = (int)address;
            return true;
        }

        static bool ScanProcesses(ref Process proc, ref Game game)
        {
            foreach (Game g in games)
            {
                Process[] process = Process.GetProcessesByName(g.name);
                if (process.Length != 0)
                {
                    Console.WriteLine("Found: " + g.name);
                    proc = process[0];
                    game = g;
                    return true;
                }
            }
            return false;
        }

        static void Main()
        {
            Console.WriteLine($"Application Configuration is located in {AppDomain.CurrentDomain.SetupInformation.ConfigurationFile}");
            Console.CancelKeyPress += delegate
            {
                Write(0);
            };

            Console.WriteLine("-----------------------------------WARNING-----------------------------------");
            Console.WriteLine(" Does NOT work with Elden Ring if Easy Anti-Cheat (EAC) is running.");
            Console.WriteLine(" Possible risk of BANS by trying to use with EAC enabled.");
            Console.WriteLine(" USE AT YOUR OWN RISK.");
            Console.WriteLine("-----------------------------------WARNING-----------------------------------");
            Console.WriteLine();

            while (true)
            {
                Write(0);
                Console.WriteLine("Looking for Dark Souls process...");

                Process proc = null;
                Game game = null;

                while (!ScanProcesses(ref proc, ref game))
                {
                    Thread.Sleep(500);
                }

                IntPtr handle = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_WM_READ, false, proc.Id);
                IntPtr baseAddress = proc.MainModule.BaseAddress;
                int oldValue = 0, value = 0;

                bool isWow64 = false;
                if (IsWow64Process(handle, ref isWow64))
                {
                    Console.WriteLine("Found " + (isWow64 ? "32" : "64") + " bit variant.");
                    int[] offsets = isWow64 ? game.offsets32 : game.offsets64;

                    while (!proc.HasExited)
                    {
                        if (PeekMemory(handle, baseAddress, !isWow64, offsets, ref value))
                        {
                            if (value != oldValue)
                            {
                                oldValue = value;
                                Write(value);
                            }
                        }
                        Thread.Sleep(500);
                    }
                }

                Console.WriteLine("Process has exited.");
                Thread.Sleep(2000);
            }
        }
    }
}
