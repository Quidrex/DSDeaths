using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace DSDeaths {
    class Program {
        const int PROCESS_WM_READ = 0x0010;

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CloseHandle(IntPtr hObject);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool ReadProcessMemory(
            IntPtr hProcess, int lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);

        static byte[] buffer = new byte[4];

        static bool GetInt(IntPtr hProcess, int address, ref int value) {
            int temp = 0;

            if (ReadProcessMemory(hProcess, address, buffer, buffer.Length, ref temp)) {
                value = BitConverter.ToInt32(buffer, 0);
                return true;
            }

            return false;
        }

        static bool Write(string filename, string prefix, int value) {
            Console.Write("Value ");
            Console.Write(value);
            Console.Write("... ");

            try {
                Directory.CreateDirectory(Directory.GetParent(filename).FullName);
                using (StreamWriter file = new StreamWriter(File.Open(filename, FileMode.Create))) {
                    file.Write(prefix);
                    file.Write(value);
                }
                Console.WriteLine("written");
                return true;
            } catch (IOException) {
                Console.WriteLine("failed");
                return false;
            }
        }

        static void Main(string[] args) {
            if (args.Length != 2) {
                Console.WriteLine("Usage: DSDeaths.exe \"Prefix\" \"Filename\"");
                Console.WriteLine("  e.g. DSDeaths.exe \"Deaths: \" \"%USERPROFILE%\\ds.txt\"");
                Console.ReadKey();
                return;
            }

            string prefix = args[0];
            string filename = Environment.ExpandEnvironmentVariables(args[1]);

            Process[] processes;

            for (;;) {
                int oldValue = 0;
                int value = 0;

                Console.WriteLine("Waiting for DARKSOULS to start...");
                Write(filename, prefix, value);
                while ((processes = Process.GetProcessesByName("DARKSOULS")).Length == 0) {
                    Thread.Sleep(500);
                }
                IntPtr handle = OpenProcess(PROCESS_WM_READ, false, processes[0].Id);

                Console.WriteLine("DARKSOULS started");
                for (;;) {
                    if (GetInt(handle, 0x01378700, ref value)) {
                        if (value != 0) {
                            if (GetInt(handle, value + 0x5c, ref value)) {
                                if (value != oldValue) {
                                    if (Write(filename, prefix, value)) {
                                        oldValue = value;
                                    }
                                }
                            } else {
                                break;
                            }
                        }
                    } else {
                        break;
                    }

                    Thread.Sleep(500);
                }

                Console.WriteLine("DARKSOULS exited");
                CloseHandle(handle);
            }
        }
    }
}
