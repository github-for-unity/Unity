using System;
using System.Reflection;

namespace GitHub.Unity
{
    enum ResourceType
    {
        Icon,
        Platform,
        Generic
    }

    class AssemblyResources
    {
        public static NPath ToFile(ResourceType resourceType, string resource, NPath destinationPath)
        {
            var os = "";
            if (resourceType == ResourceType.Platform)
            {
                os = DefaultEnvironment.OnWindows ? "windows"
                    : DefaultEnvironment.OnLinux ? "linux"
                        : "mac";
            }
            var type = resourceType == ResourceType.Icon ? "Icons"
                : resourceType == ResourceType.Platform ? "PlatformResources"
                : "Resources";
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(
                                     String.Format("GitHub.Unity.{0}{1}.{2}", type, os != "" ? "." + os : os, resource));
            if (stream != null)
                return destinationPath.Combine(resource).WriteAllBytes(stream.ToByteArray());

            return new NPath(type).Combine(os, resource);
        }
    }
}