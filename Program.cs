using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;


namespace DSDeaths {
    class Game {
        public readonly string name;
        public readonly int[] offsets;
        public readonly bool longType;

        public Game(string name, int[] offset, bool longType) {
            this.name = name;
            this.offsets = offset;
            this.longType = longType;
        }
    }

    class Program {
        const int PROCESS_WM_READ = 0x0010;

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CloseHandle(IntPtr hObject);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool ReadProcessMemory(
            IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);

        static readonly Game[] games =
        {
            new Game("DARKSOULS", new int[] {0xF78700, 0x5c}, false),
            new Game("DarkSoulsII", new int[] {0x160B8D0, 0xD0, 0x490, 0x104}, true),
            new Game("DarkSoulsIII", new int[] {0x4740178, 0x98}, true),
            new Game("DarkSoulsRemastered", new int[] {0x1D278F0, 0x98}, true),
            new Game("Sekiro", new int[] {0x3D7A1E0, 0x88, 0x2000, 0xDC}, true)
        };

        static bool Write(int value) {
            try {
                using (StreamWriter file = new StreamWriter(File.Open("DSDeaths.txt", FileMode.Create))) {
                    file.Write(value);
                }
                return true;
            } catch (IOException) {
                return false;
            }
        }

        static bool PeekMemory(IntPtr handle, IntPtr baseAddress, in Game game, ref int value) {
            long address = baseAddress.ToInt64();
            byte[] buffer = new byte[8];
            int discard = 0;

            foreach (int offset in game.offsets) {
                address += offset;

                if (!ReadProcessMemory(handle, (IntPtr)address, buffer, 8, ref discard)) {
                    return false;
                }

                address = BitConverter.ToInt64(buffer, 0);
                if (!game.longType) {
                    address = (int)address;
                }

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

            while (true) {
                Write(0);

                Console.WriteLine("Looking for Dark Souls process...");

                Process proc = null;
                Game game = null;

                while (true) {
                    if (ScanProcesses(ref proc, ref game)) {
                        break;
                    }
                    Thread.Sleep(500);
                }

                IntPtr handle = OpenProcess(PROCESS_WM_READ, false, proc.Id);
                IntPtr baseAddress = proc.MainModule.BaseAddress;
                int oldValue = 0, value = 0;

                while (true) {
                    if (PeekMemory(handle, baseAddress, game, ref value)) {
                        if (value != oldValue) {
                            oldValue = value;
                            Write(value);
                        }
                    } else if (proc.HasExited) {
                        Thread.Sleep(2000);
                        break;
                    }
                    Thread.Sleep(500);
                }

                Console.WriteLine("Process has exited.");
            }
        }
    }
}
