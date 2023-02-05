using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace GShadeVerCheck
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Franz's craptastic gshade checker. All rights reserved.");

            string gshaderegpath = @"SOFTWARE\GShade";

            // make sure we open the x64 registry
            var localMachineX64View = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            var gshadereg = localMachineX64View.OpenSubKey(gshaderegpath);

            // registry entry does not exist
            if (gshadereg == null)
            {
                Console.WriteLine("Couldn't find registry entry. This isn't going to work for manual or script installs.");
                
            }

            var gshadeinstallver = gshadereg.GetValue("instver") ?? "";

            Console.WriteLine($"gshadeinstallver: {gshadeinstallver}");

            // we should cache this
            string gshadevercheckurl = "https://api.github.com/repos/mortalitas/gshade/tags";

            

            List<GithubTagEntry> gshadetags;
            using var client = new HttpClient()
            {
                DefaultRequestHeaders =
                {
                    UserAgent = { new ProductInfoHeaderValue("GShadeScraper", "1.0")}
                }

            };
            client.BaseAddress = new Uri(gshadevercheckurl);
            var request = new HttpRequestMessage(HttpMethod.Get, gshadevercheckurl);

            var resp = client.SendAsync(request).Result;
            resp.EnsureSuccessStatusCode();

            gshadetags = JsonConvert.DeserializeObject<List<GithubTagEntry>>(resp.Content.ReadAsStringAsync().Result);

            var latestgshade = gshadetags.FirstOrDefault();

            Console.WriteLine($"Latest GShade tag: {latestgshade.name}");

            string ghtagver = latestgshade.name;
            if ($"v{gshadeinstallver}" != ghtagver)
            {
                // version mismatch
                Console.WriteLine("Version mismatch! Prompt to run the GShade updater.");

                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.CreateNoWindow = false;
                startInfo.UseShellExecute = true;
                startInfo.FileName = Environment.ExpandEnvironmentVariables(
                    "%ProgramFiles%\\GShade\\GShade Control Panel.exe");
                startInfo.WindowStyle = ProcessWindowStyle.Normal;
                startInfo.Arguments = "/U";
                startInfo.Verb = "runas";

                try
                {
                    using (Process exeProcess = Process.Start(startInfo))
                    {
                        exeProcess.WaitForExit();
                    }
                }
                catch (Win32Exception ex)
                {
                    // log the error and continue anyways. This isn't fatal.
                    Console.WriteLine(ex.ToString());
                }
            }
            else
            {
                Console.WriteLine("You're on the latest version of GShade!");
                // we should cache our last check time as cooldown
            }

        }
    }

    internal class GithubTagEntry
    {
        internal class GHTCommit
        {
            string sha { get; set; }
            string url { get; set; }
        }
        public string name { get; set; }

        public string zipball_url { get; set; }

        public string tarball_url { get; set; }

        [JsonProperty("commit")]
        public GHTCommit commit { get; set; }

        public string node_id { get; set; }
    }
}
