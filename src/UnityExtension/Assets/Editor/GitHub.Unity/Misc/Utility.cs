using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace GitHub.Unity
{
    class Utility : ScriptableObject
    {
        public static Texture2D GetIcon(string filename, string filename2x = "")
        {
            if (EditorGUIUtility.pixelsPerPoint > 1f && !string.IsNullOrEmpty(filename2x))
            {
                filename = filename2x;
            }

            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("GitHub.Unity.IconsAndLogos." + filename);
            if (stream != null)
                return stream.ToTexture2D();

            var iconPath = EntryPoint.Environment.ExtensionInstallPath.Combine("IconsAndLogos", filename).ToString(SlashMode.Forward);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
        }
    }

    static class StreamExtensions
    {
        private static MethodInfo loadImage;
        private static Func<Texture2D, MemoryStream, Texture2D> invokeLoadImage;

        static StreamExtensions()
        {
            // 5.6
            // looking for Texture2D.LoadImage(byte[] data)
            loadImage = typeof(Texture2D).GetMethods().FirstOrDefault(x => x.Name == "LoadImage" && x.GetParameters().Length == 1);
            if (loadImage != null)
            {
                invokeLoadImage = (tex, ms) =>
                {
                    loadImage.Invoke(tex, new object[] { ms.ToArray() });
                    return tex;
                };
            }
            else
            {
                // 2017.1
                var t = typeof(Texture2D).Assembly.GetType("UnityEngine.ImageConversion", false, false);
                if (t == null)
                {
                    // 2017.2 and above
                    t = Assembly.Load("UnityEngine.ImageConversionModule").GetType("UnityEngine.ImageConversion", false, false);
                }

                if (t != null)
                {
                    // looking for ImageConversion.LoadImage(this Texture2D tex, byte[] data)
                    loadImage = t.GetMethods().FirstOrDefault(x => x.Name == "LoadImage" && x.GetParameters().Length == 2);
                    invokeLoadImage = (tex, ms) =>
                    {
                        loadImage.Invoke(null, new object[] { tex, ms.ToArray() });
                        return tex;
                    };
                }
            }

            if (loadImage == null)
            {
                Logging.Error("Could not find ImageConversion.LoadImage method");
            }
        }

        public static Texture2D ToTexture2D(this Stream input)
        {
            var buffer = new byte[16 * 1024];
            using (var ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }

                var tex = new Texture2D(1, 1);
                tex = invokeLoadImage(tex, ms);
                return tex;
            }
        }
    }
}
