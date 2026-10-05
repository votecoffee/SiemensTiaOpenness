using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Win32;

namespace SiemensTiaOpenness
{
    internal static class TiaPortalAssemblyResolver
    {
        private const string OpennessRegistryPath = @"SOFTWARE\Siemens\Automation\Openness";
        private const string EngineeringAssemblyName = "Siemens.Engineering";

        private static string _selectedEngineeringVersion;

        public static string SelectedEngineeringVersion
        {
            get { return _selectedEngineeringVersion; }
        }

        public static string ReferencedApiVersion
        {
            get
            {
                AssemblyName reference = Assembly.GetExecutingAssembly()
                    .GetReferencedAssemblies()
                    .FirstOrDefault(a => string.Equals(a.Name, EngineeringAssemblyName, StringComparison.OrdinalIgnoreCase));

                return reference == null || reference.Version == null
                    ? null
                    : reference.Version.ToString();
            }
        }

        public static IList<string> GetCompatibleEngineeringVersions()
        {
            string apiVersion = ReferencedApiVersion;
            if (string.IsNullOrEmpty(apiVersion))
                return new List<string>();

            return GetInstalledEngineeringVersions()
                .Where(version => HasApiVersion(version, apiVersion))
                .OrderByDescending(ParseEngineeringVersion)
                .ToList();
        }

        public static void Initialize(string engineeringVersion)
        {
            if (string.IsNullOrWhiteSpace(engineeringVersion))
                throw new ArgumentException("A TIA Portal engineering version must be selected.", "engineeringVersion");

            _selectedEngineeringVersion = engineeringVersion;
            AppDomain.CurrentDomain.AssemblyResolve += ResolveSiemensEngineeringAssembly;
        }

        private static Assembly ResolveSiemensEngineeringAssembly(object sender, ResolveEventArgs args)
        {
            var requested = new AssemblyName(args.Name);
            if (requested.Name == null ||
                !requested.Name.StartsWith(EngineeringAssemblyName, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            string requestedVersion = requested.Version == null ? null : requested.Version.ToString();
            if (string.IsNullOrEmpty(requestedVersion))
                return null;

            string path = GetAssemblyPath(_selectedEngineeringVersion, requestedVersion, requested.Name);
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return null;

            return Assembly.LoadFrom(path);
        }

        private static IList<string> GetInstalledEngineeringVersions()
        {
            var versions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (RegistryView view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                try
                {
                    using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view))
                    using (RegistryKey openness = baseKey.OpenSubKey(OpennessRegistryPath))
                    {
                        if (openness == null)
                            continue;

                        foreach (string name in openness.GetSubKeyNames())
                            versions.Add(name);
                    }
                }
                catch
                {
                    // A missing registry view should not prevent discovery from the other view.
                }
            }

            return versions.ToList();
        }

        private static bool HasApiVersion(string engineeringVersion, string apiVersion)
        {
            return !string.IsNullOrEmpty(GetAssemblyPath(engineeringVersion, apiVersion, EngineeringAssemblyName));
        }

        private static string GetAssemblyPath(string engineeringVersion, string apiVersion, string assemblyName)
        {
            foreach (RegistryView view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                try
                {
                    using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view))
                    using (RegistryKey apiKey = baseKey.OpenSubKey(
                        OpennessRegistryPath + @"\" + engineeringVersion + @"\PublicAPI\" + apiVersion))
                    {
                        if (apiKey == null)
                            continue;

                        string path = apiKey.GetValue(assemblyName) as string;
                        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                            return path;
                    }
                }
                catch
                {
                    // Continue with the other registry view.
                }
            }

            return null;
        }

        private static Version ParseEngineeringVersion(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return new Version(0, 0);

            string normalized = value.Trim();
            if (normalized.StartsWith("V", StringComparison.OrdinalIgnoreCase))
                normalized = normalized.Substring(1);

            Version parsed;
            return Version.TryParse(normalized, out parsed) ? parsed : new Version(0, 0);
        }
    }
}
