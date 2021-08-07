using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace DSDeaths
{
    class Program
    {
        const int PROCESS_WM_READ = 0x0010;

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CloseHandle(IntPtr hObject);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool ReadProcessMemory(
            IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);

        static bool Write(int value)
        {
            try
            {
                /*string folder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string path = Path.Combine(folder, "DSDeaths.txt");*/

                using (StreamWriter file = new StreamWriter(File.Open("DSDeaths.txt", FileMode.Create)))
                {
                    file.Write(value);
                }
                return true;
            }
            catch (IOException)
            {
                return false;
            }
        }

        static byte[] buffer = new byte[8];

        static bool GetInt(IntPtr hProcess, IntPtr address, ref int value)
        {
            int temp = 0;

            if (ReadProcessMemory(hProcess, address, buffer, 4, ref temp))
            {
                value = BitConverter.ToInt32(buffer, 0);
                return true;
            }

            return false;
        }

        static bool GetLong(IntPtr hProcess, IntPtr address, ref long value)
        {
            int temp = 0;

            if (ReadProcessMemory(hProcess, address, buffer, 8, ref temp))
            {
                value = BitConverter.ToInt64(buffer, 0);
                return true;
            }

            return false;
        }

        delegate bool Peek(IntPtr handle, IntPtr baseAddress, ref int value);

        static bool Peek1(IntPtr handle, IntPtr baseAddress, ref int value)
        {
            int address = 0;
            if (GetInt(handle, (IntPtr)(baseAddress.ToInt64() + 0xF78700), ref address) && address != 0)
                if (GetInt(handle, (IntPtr)(address + 0x5c), ref value))
                    return true;
            return false;
        }

        static bool Peek2(IntPtr handle, IntPtr baseAddress, ref int value)
        {
            long address = 0;
            if (GetLong(handle, (IntPtr)(baseAddress.ToInt64() + 0x160B8D0), ref address) && address != 0)
                if (GetLong(handle, (IntPtr)(address + 0xD0), ref address) && address != 0)
                    if (GetLong(handle, (IntPtr)(address + 0x490), ref address) && address != 0)
                        if (GetInt(handle, (IntPtr)(address + 0x104), ref value))
                            return true;
            return false;
        }

        static bool Peek3(IntPtr handle, IntPtr baseAddress, ref int value)
        {
            long address = 0;
            if (GetLong(handle, (IntPtr)(baseAddress.ToInt64() + 0x4740178), ref address) && address != 0)
                if (GetInt(handle, (IntPtr)(address + 0x98), ref value))
                    return true;
            return false;
        }

        static bool PeekRemastered(IntPtr handle, IntPtr baseAddress, ref int value)
        {
            long address = 0;
            if (GetLong(handle, (IntPtr)(baseAddress.ToInt64() + 0x1D278F0), ref address) && address != 0)
                if (GetInt(handle, (IntPtr)(address + 0x98), ref value))
                    return true;
            return false;
        }

        static bool PeekSekiro(IntPtr handle, IntPtr baseAddress, ref int value)
        {
            long address = 0;
            if (GetLong(handle, (IntPtr)(baseAddress.ToInt64() + 0x3D5AAC0), ref address) && address != 0)
                        if (GetInt(handle, (IntPtr)(address + 0x90), ref value))
                            return true;
            return false;
        }

        static void Main(string[] args)
        {
            Process[] proc1, proc2, proc3, procRemastered, procSekiro;

            Console.CancelKeyPress += delegate {
                Write(0);
            };

            for (; ; )
            {
                Write(0);

                Console.WriteLine("Looking for Dark Souls process...");

                for (; ; )
                {
                    proc1 = Process.GetProcessesByName("DARKSOULS");
                    proc2 = Process.GetProcessesByName("DarkSoulsII");
                    proc3 = Process.GetProcessesByName("DarkSoulsIII");
                    procRemastered = Process.GetProcessesByName("DarkSoulsRemastered");
                    procSekiro = Process.GetProcessesByName("Sekiro");
                    if (proc1.Length != 0 || proc2.Length != 0 || proc3.Length != 0 || procRemastered.Length != 0 || procSekiro.Length != 0)
                        break;
                    Thread.Sleep(500);
                }

                Process proc;
                Peek peek;

                if (proc1.Length != 0)
                {
                    Console.WriteLine("Found Dark Souls 1!");
                    proc = proc1[0];
                    peek = Peek1;
                }
                else if (proc2.Length != 0)
                {
                    Console.WriteLine("Found Dark Souls 2!");
                    proc = proc2[0];
                    peek = Peek2;
                }
                else if (proc3.Length != 0)
                {
                    Console.WriteLine("Found Dark Souls 3!");
                    proc = proc3[0];
                    peek = Peek3;
                }
                else if (procRemastered.Length != 0)
                {
                    Console.WriteLine("Found Dark Souls Remastered!");
                    proc = procRemastered[0];
                    peek = PeekRemastered;
                }
                else
                {
                    Console.WriteLine("Found Sekiro!");
                    proc = procSekiro[0];
                    peek = PeekSekiro;
                }

                IntPtr handle = OpenProcess(PROCESS_WM_READ, false, proc.Id);
                IntPtr baseAddress = proc.MainModule.BaseAddress;
                int oldValue = 0, value = 0;

                for (; ; )
                {
                    if (peek(handle, baseAddress, ref value))
                    {
                        if (value != oldValue)
                        {
                            oldValue = value;
                            Write(value);
                        }
                    }
                    else if (proc.HasExited)
                    {
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
