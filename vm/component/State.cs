﻿namespace vm.component
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using static System.Console;
    public unsafe class State
    {
        private readonly Bus _bus;

        public State(Bus bus) => _bus = bus;

        public ulong pc { get; set; } = 0;

        /// <summary>
        /// base register cell
        /// </summary>
        public ulong r1, r2, r3;
        /// <summary>
        /// value register cell
        /// </summary>
        public ulong u1, u2;
        /// <summary>
        /// magic cell
        /// </summary>
        public ulong x1;
        /// <summary>
        /// id
        /// </summary>
        public ulong instructionID;
        /// <summary>
        /// last <see cref="r1"/>
        /// </summary>
        public ulong prev;

        public ulong[] regs = new ulong[16];
        public sbyte halt { get; set; } = 0;

        public List<uint> program { get; set; }

        public void Load(uint[] prog) => program = prog.ToList();

        public uint Fetch() => program.ElementAt((int)pc++);

        public string pX = "";
        public void Eval()
        {
            switch (instructionID)
            {
                case 0xA:
                    WriteLine($"r r r u u x");
                    WriteLine($"1 2 3 1 2 1");
                    WriteLine($"{r1:X} {r2:X} {r3:X} {u1:X} {u2:X} {x1:X}");
                    return;
                case 0x1:
                    WriteLine($"loadi");
                    break;
                case 0x2:
                    WriteLine($"add");
                    break;
                case 0x0:
                case 0xD when r1 == 0xE && r2 == 0xA && r3 == 0xD:
                    WriteLine("HALT");
                    break;
                case 0xF when x1 == 0xC && r3 == 0xE:
                    WriteLine($"push");
                    break;
                case 0xF when x1 == 0xF && r3 == 0xE:
                    WriteLine($"pop, dump");
                    break;
                case 0x3:
                    WriteLine($"swap");
                    break;
                case 0xF when x1 == 0x1:
                    WriteLine($"dump_l");
                    break;
                case 0xF when x1 == 0x2:
                    WriteLine($"dump_l");
                    break;
                case 0xF when x1 == 0x3:
                    WriteLine($"dump_l");
                    break;
                case 0xE when x1 == 0x1:
                    WriteLine($"dump_p");
                    break;
                case 0xE when x1 == 0x2:
                    WriteLine($"dump_p");
                    break;
                case 0xE when x1 == 0x3:
                    WriteLine($"dump_p");
                    break;
                case 0x8 when u2 == 0xF:
                    WriteLine("jump_t");
                    break;
                case 0x8 when u2 == 0xC:
                    WriteLine("ref_t");
                    break;
            }
            //WriteLine($"r r r u u x");
            //WriteLine($"1 2 3 1 2 1");
            switch (instructionID)
            {
                case 0x1:
                    regs[r1] = u1;
                    break;
                case 0x2:
                    regs[r1] = regs[r2] + regs[r3];
                    break;
                case 0x3:
                    regs[r1] ^= regs[r2];
                    regs[r2] = regs[r1] ^ regs[r2];
                    regs[r1] ^= regs[r2];
                    break;
                case 0x4:
                    regs[r1] = regs[r2] - regs[r3];
                    break;
                case 0x5:
                    regs[r1] = regs[r2] * regs[r3];
                    break;
                case 0x6:
                    regs[r1] = regs[r2] / regs[r3];
                    break;
                case 0x7:
                    regs[r1] = (uint)Math.Pow(regs[r2], regs[r3]);
                    break;
                case 0x8 when u2 == 0xF: // 0x8F000F0
                    pc = regs[r1];
                    break;
                case 0x8 when u2 == 0xC: // 0x8F000C0
                    regs[r1] = pc;
                    break;
                case 0x0:
                case 0xD when r1 == 0xE && r2 == 0xA && r3 == 0xD:
                    halt = 1;
                    break;
                case 0xA: break;

                case 0xF when x1 == 0xC && r3 == 0xE: //0xF16E{s}C
                    _bus.Find((int)r1).write((short)r2, (int)(u1 << 4 | u2));
                    break;
                case 0xF when x1 == 0xF && r3 == 0xE: //0xF17E{s}F
                    _bus.Find((int)r1).write((short)r2, (int)(u1 << 4 | u2));
                    break;
                case 0xF when x1 == 0x1:
                case 0xF when x1 == 0x2:
                case 0xF when x1 == 0x3:
                case 0xE when x1 == 0x1:
                case 0xE when x1 == 0x2:
                case 0xE when x1 == 0x3:
                    break;
            }
            prev = r1;
        }

        public void Print()
        {
            //WriteLine($"*0x{instructionID:X4} :0x{r1:X4} :0x{r2:X4} :0x{r3:X4} &0x{u1:X4} &0x{u2:X4} %0x{x1:X4}");
        }

        public void WriteLine(string s) => Title += s;

        public void Accept(ulong mem)
        {
            Title = $"fetch page {mem:x8} - ";
            instructionID = (mem & 0xF000000) >> 24;
            r1 = (mem & 0xF00000) >> 20;
            r2 = (mem & 0xF0000) >> 16;
            r3 = (mem & 0xF000) >> 12;
            u1 = (mem & 0xF00) >> 8;
            u2 = (mem & 0xF0) >> 4;
            x1 = (mem & 0xF);
        }

        public ulong Degrade()
        {
            return (instructionID << 24) | (r1 << 20) | (r2 << 16) | (r3 << 12) | (u1 << 8) | (u2 << 4) | x1;
        }
    }
}