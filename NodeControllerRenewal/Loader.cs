using ModsCommon;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace NodeController
{
    public abstract class Loader : ModsCommon.Utilities.Loader
    {
        private static string RecoveryDirectory => Path.Combine(Directory.GetCurrentDirectory(), "NodeControllerRenewal");
        public static string DataRecovery => nameof(DataRecovery);
        public static string DataName => $"{DataRecovery}.{GetSaveName()}";
        private static Regex DataRegex { get; } = new Regex(@$"{DataRecovery}\.(?<name>.+)\.(?<date>\d+)");

        public static Dictionary<string, string> GetDataRestoreList()
        {
            var files = GetRestoreList($"{DataRecovery}*.xml");
            var result = new Dictionary<string, string>();
            foreach (var file in files)
            {
                var match = DataRegex.Match(file);
                if (!match.Success)
                    continue;

                var date = new DateTime(long.Parse(match.Groups["date"].Value));
                result[file] = $"{match.Groups["name"].Value} {date.ToString(SingletonMod<Mod>.Instance.Culture)}";
            }
            return result;
        }
        private static string[] GetRestoreList(string pattern)
        {
            var files = Directory.GetFiles(RecoveryDirectory, pattern);
            return files;
        }

        public static bool DumpData(out string path)
        {
            SingletonMod<Mod>.Logger.Debug($"Dump data");

            try
            {
                var data = GetString(SingletonManager<Manager>.Instance.ToXml());
                return SaveToFile(DataName, data, out path);
            }
            catch (Exception error)
            {
                SingletonMod<Mod>.Logger.Error("Save dump failed", error);

                path = string.Empty;
                return false;
            }
        }

        public static bool SaveToFile(string name, string xml, out string file)
        {
            SingletonMod<Mod>.Logger.Debug($"Save to file");
            try
            {
                if (!Directory.Exists(RecoveryDirectory))
                    Directory.CreateDirectory(RecoveryDirectory);

                file = Path.Combine(RecoveryDirectory, $"{name}.{DateTime.Now.Ticks}.xml");
                using (var fileStream = File.Create(file))
                using (var writer = new StreamWriter(fileStream))
                {
                    writer.Write(xml);
                }
                SingletonMod<Mod>.Logger.Debug($"Dump saved {file}");
                return true;
            }
            catch (Exception error)
            {
                SingletonMod<Mod>.Logger.Error("Save dump failed", error);

                file = string.Empty;
                return false;
            }
        }

        public static bool ImportData(string file)
        {
            SingletonMod<Mod>.Logger.Debug($"Import data");

            try
            {
                using var fileStream = File.OpenRead(file);
                using var reader = new StreamReader(fileStream);
                var xml = reader.ReadToEnd();
                var config = XmlExtension.Parse(xml);

                SingletonManager<Manager>.Instance.Import(config);

                SingletonMod<Mod>.Logger.Debug($"Data was imported");

                return true;
            }
            catch (Exception error)
            {
                SingletonMod<Mod>.Logger.Error("Could not import data", error);
                return false;
            }
        }
    }
}
