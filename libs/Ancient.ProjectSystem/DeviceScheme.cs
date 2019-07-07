﻿namespace Ancient.ProjectSystem
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Newtonsoft.Json;

    public class DeviceScheme
    {
        public Dictionary<string, string> scheme = new Dictionary<string, string>();

        public void Validate(out List<(string code, string msg)> errors)
        {
            errors = new List<(string code, string msg)>();

            foreach (var device in scheme.GroupBy(x => x.Key).Where(x => x.Count() > 1).Select(x => x.Key))
                errors.Add(("ASH0501", $"dublicate device '{device}' in scheme '#[SCHEME]#'."));

            foreach (var offset in scheme.GroupBy(x => x.Value).Where(x => x.Count() > 1).Select(x => x.Key))
                errors.Add(("ASH0505", $"dublicate device-offset '{offset}' in scheme '#[SCHEME]#'."));

        }

        public static DeviceScheme Open(FileInfo file) 
            => JsonConvert.DeserializeObject<DeviceScheme>(File.ReadAllText(file.FullName));
        public static DeviceScheme Null => new DeviceScheme();
        public static DeviceScheme Default
        {
            get
            {
                var f = new FileInfo($"./device.scheme");
                if(!f.Exists)
                    File.WriteAllText($"./device.scheme",JsonConvert.SerializeObject(new DeviceScheme()));
                return Open(f);
            }
        }

        public short getOffsetByDevice(string id, short @default)
        {
            if (scheme.ContainsKey(id))
                return short.Parse(scheme[id]);
            return @default;
        }
    }
}