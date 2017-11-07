using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;

namespace WindowsFormsApp1
{
    class Class1
    {
        public static string getWinFeature = " /online /get-featureinfo /featurename:";
        public static string enableWinFeature = " /online /enable-feature /featurename:";
        public static string hyperV = "Microsoft-Hyper-V";
        public static string wincontainerFeature = "Containers";
        public static bool _completed;


        public static bool CheckdockerInstallation()
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
                    return true;
                }
            }

            return false;
        }
        public static bool CheckWinFeatureStatus(string featureName, string utilityProcess)
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

            processInfo.UseShellExecute = false;
            Process x = Process.Start(processInfo);
            while (!x.StandardOutput.EndOfStream)
            {
                string line = x.StandardOutput.ReadLine();
                if (String.Equals("State : Disabled", line))
                {
                    x.Close();
                    return false;
                }
            }

            x.WaitForExit();
            return true;
        }

        private void Completed(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Cancelled == true)
            {
                Console.WriteLine("Download has been canceled.");
            }
            else
            {
                Console.WriteLine("Download completed!");
            }

            _completed = true;
        }
        public bool InstallDockerforWindows()
        {
            string downloadPath = @"c:\temp\Docker4WindowsInstaller.exe";

            DownloadFile(downloadPath);
            //Thread.Sleep(10000);
            while (!_completed) ;
            if (_completed)
            {
                Process dockerInstallProc = Process.Start(@"c:\temp\Docker4WindowsInstaller.exe");
                int id = dockerInstallProc.Id;
                Process tempProc = Process.GetProcessById(id);
                tempProc.WaitForExit();
            }
                
            return true;
        }

        private void DownloadFile(string path)
        {
            _completed = false;
            //using (WebClient webClient = new WebClient())
            //{
            //    webClient.DownloadFile(new Uri("https://aka.ms/download-d4w-dockertools"), path);
            //    webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadFileCallback2);
            //    webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadProgressCallBack);
            //    //webClient.DownloadFile(new Uri("https://aka.ms/download-d4w-dockertools"), path);
            //    //_completed = true;
            //}

            using (var wc = new WebClient())
            {
                wc.DownloadProgressChanged += HandleDownloadProgress;
                wc.DownloadFileCompleted += HandleDownloadComplete;

                var syncObj = new Object();
                lock (syncObj)
                {
                    wc.DownloadFileAsync(new Uri("https://aka.ms/download-d4w-dockertools"), path, syncObj);
                    //This would block the thread until download completes
                    Monitor.Wait(syncObj);
                }
            }
            _completed = true;

        }

        public void HandleDownloadComplete(object sender, AsyncCompletedEventArgs e)
        {
            lock (e.UserState)
            {
                //releases blocked thread
                Monitor.Pulse(e.UserState);
            }
        }


        public void HandleDownloadProgress(object sender, DownloadProgressChangedEventArgs args)
        {
            //Process progress updates here
        }

    }
}
