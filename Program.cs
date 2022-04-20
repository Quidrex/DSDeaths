using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;


namespace DSDeaths {
    class Game {
        public readonly string name;
        public readonly int[] offset;
        public readonly bool longType;

        public Game(in string name, in int[] offset, bool longType=true) {
            this.name = name;
            this.offset = offset;
            this.longType = longType;
        }
    }

    class Program {
        const int PROCESS_WM_READ = 0x0010;

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool ReadProcessMemory(
            IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);

        static readonly Game[] games =
        {
            new Game("DARKSOULS", new int[] {0xF78700, 0x5c}, false),
            new Game("DarkSoulsII", new int[] {0x160B8D0, 0xD0, 0x490, 0x104}),
            new Game("DarkSoulsIII", new int[] {0x4740178, 0x98}),
            new Game("DarkSoulsRemastered", new int[] {0x1D278F0, 0x98}),
            new Game("Sekiro", new int[] {0x3D5AAC0, 0x90}),
            new Game("eldenring", new int[] {0x3C00028, 0x94})
        };

        static bool Write(int value) {
            try {
                File.WriteAllText("DSDeaths.txt", value.ToString());
            } catch (IOException) {
                return false;
            }
            return true;
        }

        static bool PeekMemory(in IntPtr handle, in IntPtr baseAddress, in Game game, ref int value) {
            long address = baseAddress.ToInt64();
            byte[] buffer = new byte[8];
            int discard = 0;

            foreach (int offset in game.offset) {
                address += offset;

                if (!ReadProcessMemory(handle, (IntPtr)address, buffer, 8, ref discard)) {
                    return false;
                }

                address = game.longType ? BitConverter.ToInt64(buffer, 0) : BitConverter.ToInt32(buffer, 0);
                if (address == 0) {
                    return false;
                }
            }
            value = (int)address;
            return true;
        }

        static bool ScanProcesses(ref Process proc, ref Game game) {
            foreach (Game g in games) {
                Process[] process = Process.GetProcessesByName(g.name);
                if (process.Length != 0) {
                    Console.WriteLine("Found: " + g.name);
                    proc = process[0];
                    game = g;
                    return true;
                }
            }
            return false;
        }

        static void Main() {
            Console.CancelKeyPress += delegate {
                Write(0);
            };

            Console.WriteLine("-----------------------------------WARNING-----------------------------------");
            Console.WriteLine(" Does NOT work with Elden Ring if Easy Anti-Cheat (EAC) is running.");
            Console.WriteLine(" Possible risk of BANS by trying to use with EAC enabled or disabling EAC.");
            Console.WriteLine(" USE AT YOUR OWN RISK.");
            Console.WriteLine("-----------------------------------WARNING-----------------------------------");
            Console.WriteLine();

            while (true) {
                Write(0);
                Console.WriteLine("Looking for Dark Souls process...");

                Process proc = null;
                Game game = null;

                while (!ScanProcesses(ref proc, ref game)) {
                    Thread.Sleep(500);
                }

                IntPtr handle = OpenProcess(PROCESS_WM_READ, false, proc.Id);
                IntPtr baseAddress = proc.MainModule.BaseAddress;
                int oldValue = 0, value = 0;

                while (!proc.HasExited) {
                    if (PeekMemory(handle, baseAddress, game, ref value)) {
                        if (value != oldValue) {
                            oldValue = value;
                            Write(value);
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
