﻿namespace flame.runtime.emit
{
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.IO;
    using System.Linq;
    using System.Security;
    using System.Security.Cryptography;
    using System.Text;
    using Newtonsoft.Json;

    public class FlameAssembly
    {
        public string Name { get; protected set; }
        public List<(string key, string value)> Metadata { get; protected set; }
        protected byte[] ILCode { get; set; }

        public AssemblyTag Tag { get; protected set; } = new AssemblyTag(AssemblyTag.SignType.UnSecurity, AssemblyTag.ArchType.Any, 1);

        /// <exception cref="BadImageFormatException"/>
        public static FlameAssembly Load(byte[] bytes)
        {
            using var mem = new MemoryStream(bytes);

            var raw = Encoding.ASCII.GetString(mem.ReadBytes(10));
            if (raw[0] != 'E' && raw[1] != 'F')
                throw new BadImageFormatException();
            if(!AssemblyTag.IsTag(raw))
                throw new BadImageFormatException();
            var tag = AssemblyTag.Parse(raw);
            mem.ReadBytes(1); // read '\n'
            mem.ReadBytes(1); // read '\n'
            var headerLen = BitConverter.ToInt64(mem.ReadBytes(sizeof(long)), 0);
            using var outMemory = new MemoryStream();
            if (tag.Sign == AssemblyTag.SignType.Signed)
            {
                using var sig = new AssemblySigner(SymmetricAlgorithm.Create("Rijndael"), HashAlgorithm.Create("MD5"));
                using var inMemory = new MemoryStream(mem.ReadBytes((int) headerLen));
                
                var pass = new SecureString();
                Array.ForEach("flame-asm".ToArray(), pass.AppendChar);
                pass.MakeReadOnly();
                sig.SetPassword(pass);
                sig.DecryptStream(inMemory, outMemory, default);
            }
            else
                outMemory.Write(mem.ReadBytes((int) headerLen), 0, (int)headerLen);

            (string key, string value)[] Metadata = { ("", "") };
            dynamic header = JsonConvert.DeserializeObject(Encoding.UTF32.GetString(outMemory.ToArray()), new{Name = "", Metadata}.GetType());
           
            mem.ReadBytes(1); // read '\n'

            var bodyLen = BitConverter.ToInt64(mem.ReadBytes(sizeof(long)), 0);
            var body = mem.ReadBytes((int) bodyLen);

            var asm = new FlameAssembly
            {
                ILCode = body,
                Metadata = new List<(string key, string value)>(header.Metadata), 
                Name = header.Name,
                Tag = tag
            };


            return asm;
        }
        /// <exception cref="BadImageFormatException"/>
        public static FlameAssembly LoadFrom(string filename) // todo
            => Load(File.ReadAllBytes(filename));

        public virtual byte[] GetILCode() => ILCode;
    }
}