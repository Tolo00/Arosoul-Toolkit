using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace ProjectWizard
{
    /// <summary>Points NuGetForUnity's repositoryPath (where DLLs are installed) at the configured folder.</summary>
    internal static class NuGetConfigFile
    {
        private static string FilePath => Path.Combine(Application.dataPath, "NuGet.config");

        /// <summary>repositoryPath is relative to Assets, e.g. "./Plugins/NuGet".</summary>
        private static string DesiredValue
        {
            get
            {
                var folder = WizardConfig.NuGetInstallFolder.Replace('\\', '/').Trim('/');
                return folder.StartsWith("Assets/") ? "./" + folder.Substring("Assets/".Length) : null;
            }
        }

        /// <summary>Creates or edits NuGet.config. Returns true if the file was changed.</summary>
        public static bool EnsureRepositoryPath()
        {
            var desired = DesiredValue;
            if (desired == null)
            {
                WizardLog.Warn($"NuGetInstallFolder '{WizardConfig.NuGetInstallFolder}' must be inside Assets. Using NuGetForUnity's default.");
                return false;
            }

            if (!File.Exists(FilePath))
            {
                File.WriteAllText(FilePath,
$@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <add key=""nuget.org"" value=""https://api.nuget.org/v3/index.json"" />
  </packageSources>
  <activePackageSource>
    <add key=""All"" value=""(Aggregate source)"" />
  </activePackageSource>
  <config>
    <add key=""repositoryPath"" value=""{desired}"" />
  </config>
</configuration>
");
                return true;
            }

            var doc = XDocument.Load(FilePath);
            var config = doc.Root.Element("config");
            if (config == null)
            {
                config = new XElement("config");
                doc.Root.Add(config);
            }

            var entry = config.Elements("add").FirstOrDefault(e => (string)e.Attribute("key") == "repositoryPath");
            if (entry == null)
            {
                entry = new XElement("add");
                entry.SetAttributeValue("key", "repositoryPath");
                config.Add(entry);
            }
            else if ((string)entry.Attribute("value") == desired)
            {
                return false;
            }

            entry.SetAttributeValue("value", desired);
            doc.Save(FilePath);
            return true;
        }
    }
}
