﻿namespace vm
{
    using System;
    using System.Drawing;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using ancient.runtime;
    using ancient.runtime.context;
    using component;
    using ancient.runtime.emit;
    using ancient.runtime.hardware;
    using ancient.runtime.tools;
    using MoreLinq;
    using Pastel;

    internal class Program
    {
        public static void InitializeProcess()
        {
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Console.Title = "cpu_host";
            IntToCharConverter.Register<char>();
            
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Console.OutputEncoding = Encoding.Unicode;
        }

        public static void InitializeFlags(Bus bus)
        {
            /* @0x11 */
            bus.State.tc = AppFlag.GetVariable("VM_TRACE");
            /* @0x12 */
            bus.State.ec = AppFlag.GetVariable("VM_ERROR", true);
            /* @0x13 */
            bus.State.km = AppFlag.GetVariable("VM_KEEP_MEMORY");
            /* @0x14 */
            bus.State.fw = AppFlag.GetVariable("VM_MEM_FAST_WRITE");
        }

        private static void HandleDebugger()
        {
            Console.WriteLine("Waiting for debugger to attach".Pastel(Color.Red));
            while (!System.Diagnostics.Debugger.IsAttached)
                Thread.Sleep(100);
            Console.WriteLine("Debugger attached".Pastel(Color.Green));
        }

        public static void InitializeMemory(Bus bus, params string[] args)
        {
            if (bus.State.halt != 0) 
                return;
            if (!args.Any())
                bus.State.Load("<chip>", 0xB00B500000);
            else
            {
                var nameFile = Path.Combine(Path.GetDirectoryName(args.First()),Path.GetFileNameWithoutExtension(args.First()));
                var file = new FileInfo($"{nameFile}.dlx");
                var bios = new FileInfo($"{nameFile}.bios");
                var pdb  = new FileInfo($"{nameFile}.pdb");

                if (AppFlag.GetVariable("VM_ATTACH_DEBUGGER") && pdb.Exists)
                    bus.AttachDebugger(new Debugger(DebugSymbols.Open(File.ReadAllBytes(pdb.FullName))));
                if (bios.Exists)
                {
                    var asm = AncientAssembly.LoadFrom(bios.FullName);
                    var bytes = asm.GetILCode();
                    var meta = asm.GetMetaILCode();
                    bus.State.Load("<bios>",CastFromBytes(bytes));
                    bus.State.LoadMeta(meta);
                }
                if (file.Exists)
                {
                    var asm = AncientAssembly.LoadFrom(file.FullName);
                    var bytes = asm.GetILCode();
                    var meta = asm.GetMetaILCode();
                    bus.State.Load("<exec>",CastFromBytes(bytes));
                    bus.State.LoadMeta(meta);
                }
                else
                    bus.State.Load("<chip>",0xB00B5000);
            }
        }

        public static async Task Main(string[] args)
        {
            if (AppFlag.GetVariable("MANAGED_DEBUGGER_WAIT"))
                HandleDebugger();

            InitializeProcess();
            var bus = new Bus();

            

            InitializeFlags(bus);

            if(AppFlag.GetVariable("VM_TRACE"))
                DeviceLoader.OnTrace += Console.WriteLine;

            DeviceLoader.AutoGrub(bus.Add);

            if (AppFlag.GetVariable("REPL"))
            {
                Console.WriteLine("@ Ancient VM Interactive @".Pastel(Color.Green));
                InteractiveConstruction(bus);
            }
            else
                InitializeMemory(bus, args);

            while (bus.State.halt == 0)
            {
                bus.cpu.Step();
                await Task.Delay(1).ConfigureAwait(false);
            }

            bus.Unload();
        }

        public static ulong[] CastFromBytes(byte[] bytes)
        {
            if (bytes.Length % sizeof(ulong) == 0)
                return bytes.Batch(sizeof(ulong)).Select(x => BitConverter.ToUInt64(x.Reverse().ToArray())).Reverse().ToArray();
            if (bytes.Length % sizeof(uint) == 0)
                return bytes.Batch(sizeof(uint)).Select(x => BitConverter.ToUInt64(x.ToArray())).Reverse().ToArray();
            throw new Exception("invalid offset file.");
        }


        public static void InteractiveConstruction(Bus bus)
        {
            bus.State.km = true;
            bus.State.fw = true;
            while (true)
            {
                Console.Write("> ".Pastel(Color.Gray));
                var input = Console.ReadLine();
                if(input is null)
                    continue;

                if (!input.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase))
                {
                    Console.WriteLine("Invalid operation;".Pastel(Color.Red));
                    continue;
                }

                // shit))0
                if (input.Length < "0x000000000".Length)
                {
                    var need = "0x0000000000".Length;
                    var current = input.Length;
                    var diff = (need - current);
                    input = $"{input}{new string('0', diff)}";
                }

                var decoded = ulong.Parse(input.Remove(0, 2), NumberStyles.HexNumber);
                bus.State.halt = 0;
                bus.State.Append(decoded);
                bus.cpu.Step();
            }
            
        }
    }
}