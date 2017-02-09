using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography;
using System.Text;
using Mono.Security.X509;

namespace GitHub.Unity
{
    static class MozRoots
    {
        // this URL is recommended by https://bugzilla.mozilla.org/show_bug.cgi?id=1279952#c8 and is also used as basis for curl's https://curl.haxx.se/ca/cacert.pem bundle
        private const string url = "https://hg.mozilla.org/releases/mozilla-release/raw-file/default/security/nss/lib/ckfw/builtins/certdata.txt";
        private static string inputFile = null; // if we want to load from a file instead in the future, we can use this

        public static void Run()
        {
            Process();
        }

        private static byte[] DecodeOctalString(string s)
        {
            var pieces = s.Split('\\');
            var data = new byte[pieces.Length - 1];
            for (var i = 1; i < pieces.Length; i++)
            {
                data[i - 1] = (byte)((pieces[i][0] - '0' << 6) + (pieces[i][1] - '0' << 3) + (pieces[i][2] - '0'));
            }

            return data;
        }

        private static X509Certificate DecodeCertificate(string s)
        {
            var rawdata = DecodeOctalString(s);
            return new X509Certificate(rawdata);
        }

        private static Stream GetFile()
        {
            try
            {
                if (inputFile != null)
                {
                    return File.OpenRead(inputFile);
                }
                else
                {
                    var req = (HttpWebRequest)WebRequest.Create(url);
                    req.Timeout = 10000;
                    return req.GetResponse().GetResponseStream();
                }
            }
            catch
            {
                return null;
            }
        }

        private static X509CertificateCollection DecodeCollection()
        {
            var roots = new X509CertificateCollection();
            var sb = new StringBuilder();
            var processing = false;

            using (var s = GetFile())
            {
                if (s == null)
                {
                    return null;
                }

                var sr = new StreamReader(s);
                while (true)
                {
                    var line = sr.ReadLine();
                    if (line == null)
                        break;

                    if (processing)
                    {
                        if (line.StartsWith("END"))
                        {
                            processing = false;
                            var root = DecodeCertificate(sb.ToString());
                            roots.Add(root);

                            sb = new StringBuilder();
                            continue;
                        }

                        sb.Append(line);
                    }
                    else
                    {
                        processing = line.StartsWith("CKA_VALUE MULTILINE_OCTAL");
                    }
                }

                return roots;
            }
        }

        private static int Process()
        {
            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => {
                if (sslPolicyErrors != SslPolicyErrors.None)
                {
                    //Console.WriteLine("WARNING: Downloading the trusted certificate list couldn't be done securely (error: {0}), continuing anyway. If you're using mozroots to bootstrap Mono's trust store on a clean system this might be OK, otherwise it could indicate a network intrusion. Please ensure you're using a trusted network or move to cert-sync.", sslPolicyErrors);
                }

                // this is very bad, but on a clean system without an existing trust store we don't really have a better option
                return true;
            };

            var roots = DecodeCollection();
            if (roots == null)
            {
                return 1;
            }
            else if (roots.Count == 0)
            {
                return 0;
            }

            var stores = X509StoreManager.CurrentUser;
            var trusted = stores.TrustedRoot.Certificates;
            var additions = 0;
            foreach (var root in roots)
            {
                if (!trusted.Contains(root))
                {
                    stores.TrustedRoot.Import(root);
                    additions++;
                }
            }

            if (additions > 0)
            {
                //WriteLine("{0} new root certificates were added to your trust store.", additions);
            }

            var removed = new X509CertificateCollection();
            foreach (var trust in trusted)
            {
                if (!roots.Contains(trust))
                {
                    removed.Add(trust);
                }
            }

            if (removed.Count > 0)
            {
                //WriteLine("{0} previously trusted certificates were removed.", removed.Count);

                foreach (var old in removed)
                {
                    stores.TrustedRoot.Remove(old);
                }
            }

            //WriteLine("Import process completed.{0}", Environment.NewLine);
            return 0;
        }
    }
}
