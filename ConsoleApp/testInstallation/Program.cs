using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace testInstallation
{
    class Program
    {
        private static string getWinFeature = " /online /get-featureinfo /featurename:";
        private static string enableWinFeature = " /online /enable-feature /featurename:";
        private static string hyperV = "Microsoft-Hyper-V";
        private static string wincontainerFeature = "Containers";
        private static bool isHyperVEnabled = true;
        private static bool isContainerEnabled = true;
        private static bool isDockerInstalled = true;

        public static void CheckdockerInstallation()
        {
            string value32 = string.Empty;
            RegistryKey localKey;
            if (Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess)
            {
                localKey = RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, RegistryView.Registry32);
            }
            else
            {
                localKey = RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, RegistryView.Registry64);
            }
                
            localKey = localKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
            if (localKey != null)
            {
                if (localKey.GetSubKeyNames().Contains("Docker for Windows"))
                {
                    isDockerInstalled = true;
                    return;
                }
            }

            isDockerInstalled = false;
        }
        public static void CheckWinFeatureStatus(string featureName, string utilityProcess)
        {
            System.Diagnostics.ProcessStartInfo processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = Path.Combine(Environment.ExpandEnvironmentVariables("%windir%"), "system32", utilityProcess)
            };

            if (Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess)
            {
                processInfo.FileName = Path.Combine(Environment.ExpandEnvironmentVariables("%windir%"), "sysnative", utilityProcess);
            }


            processInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
            processInfo.Arguments = featureName;
            processInfo.Verb = "runas";
            processInfo.RedirectStandardOutput = true;

            //process.Arguments = "/K c:\\temp\\Docker4WindowsInstaller.exe";

            processInfo.UseShellExecute = false;
            Process x = Process.Start(processInfo);
            while (!x.StandardOutput.EndOfStream)
            {
                string line = x.StandardOutput.ReadLine();
                if (String.Equals("State : Disabled", line))
                {
                    if (featureName.Contains(hyperV))
                    {
                        isHyperVEnabled = false;
                    }
                    if (featureName.Contains(wincontainerFeature))
                    {
                        isContainerEnabled = false;
                    }
                    break;
                }
            }

            x.WaitForExit();
        }

        public static void Main(string[] args)
        {
            CheckdockerInstallation();
            CheckWinFeatureStatus(String.Concat(getWinFeature, hyperV), "dism.exe");
            CheckWinFeatureStatus(String.Concat(getWinFeature, wincontainerFeature), "dism.exe");

            if (!isDockerInstalled)
            {
                Console.Write("Do you want to download and install the latest d4w? (y/n): ");
                var ans = Console.Read();
                if (ans == 'y' || ans == 'Y')
                {
                    string downloadPath = @"c:\temp\Docker4WindowsInstaller.exe";
                    using (WebClient webClient = new WebClient())
                    {
                        webClient.DownloadFile("https://aka.ms/download-d4w-dockertools", downloadPath);
                    }

                    Thread.Sleep(10000);
                    Process dockerInstallProc = Process.Start(@"c:\temp\Docker4WindowsInstaller.exe");
                    int id = dockerInstallProc.Id;
                    Process tempProc = Process.GetProcessById(id);
                    tempProc.WaitForExit();
                }

            }

            if (!isHyperVEnabled)
            {
                Console.Write("Do you want to enable hyperV? (y/n): ");
                var ans = Console.Read();
                if (ans == 'y' || ans == 'Y')
                    CheckWinFeatureStatus(String.Concat(enableWinFeature, hyperV, " /all /NoRestart"), "dism.exe");
            }

            if (!isContainerEnabled)
            {
                Console.Write("Do you want to enable containers? (y/n): ");
                var ans = Console.Read();
                if (ans == 'y' || ans == 'Y')
                    CheckWinFeatureStatus(String.Concat(enableWinFeature, wincontainerFeature, " /all /NoRestart"), "dism.exe");
            }


            System.Diagnostics.Process.Start("shutdown", "/r /t 30");




        }
    }
}
