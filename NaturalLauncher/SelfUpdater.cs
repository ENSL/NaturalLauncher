/*
 * Natural Launcher

Copyright (C) 2018  Mael "Khelben" Vignaux

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the

GNU General Public License for more details.
You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
	
You can contact me with any question at the mail : mael.vignaux@elseware-experience.com
*/

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace NaturalLauncher
{
    class SelfUpdater
    {
        public static bool isSelfUpdating = true;

        public static string LaucherLocalVNumber;
        public static string LaucherRemoteVNumber;
        public static string curDir = Directory.GetCurrentDirectory();

        public static MainWindow MainWindowReference;

        public static string LauncherFolderURL = Properties.Settings.Default.LauncherUrl;
        public static string VersionFileName = "launcher.version";
        public static string PublicKeyPath = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "NslKey.cer";
        public static string manifestUrl = Properties.Settings.Default.IndexURL + "CryptoManifest.txt";

        public static LauncherManifest CryptoManifest = new LauncherManifest();

        public static int NumberOfFiles = 0;

        public bool allowMessage=false;

        public BackgroundWorker backgroundWorker;

        public void SetMainWindowRef(MainWindow MainWindowRef)
        {
            MainWindowReference = MainWindowRef;
        }

        public void UpdateLauncher()
        {
            if(isSelfUpdating)
            {
                CheckUpdaterVersion();

                DeleteOldFiles(); //delete all the updatelog and .bak files

                if (LaucherLocalVNumber != LaucherRemoteVNumber)
                {
                    UpdateCryptoManifest();

                    // Move the files in the launcher directory to a name with .bak
                    MoveLauncherFiles(); //Attention, non recursive !!!

                    // falsify all the buttons
                    MainWindowReference.StartButton.IsEnabled = false;
                    MainWindowReference.VerifyButton.IsEnabled = false;
                    MainWindowReference.SettingButton.IsEnabled = false;

                    // We gotta update the launcher !
                    MessageBoxResult AlertBox = System.Windows.MessageBox.Show("The Launcher Will be self updating !" + Environment.NewLine + "It will restart itself once done..."); //not true anymore
                    ConsoleManager.Show();

                    // background worker download the files and switch the launcher when over
                    backgroundWorker = new System.ComponentModel.BackgroundWorker();
                    backgroundWorker.WorkerSupportsCancellation = true;
                    backgroundWorker.DoWork += UpdateLauncher_Worker;
                    backgroundWorker.RunWorkerCompleted += UpdateLauncher_RunWorkerCompleted;
                    backgroundWorker.RunWorkerAsync();
                }
                else
                {
                    if (allowMessage) //inform the player we don't need update (only when using context menu so far)
                    {
                        MessageBoxResult AlertBox = System.Windows.MessageBox.Show("No Update Required !");
                    }
                }
            }

        }

        private static void UpdateCryptoManifest()
        {
            using (WebClient webClient = new WebClient())
            {
                Console.WriteLine("Downloading the CryptoManifest");
                string dasManifest = webClient.DownloadString(new Uri(manifestUrl));
                CryptoManifest = JsonConvert.DeserializeObject<LauncherManifest>(dasManifest);
                NumberOfFiles = CryptoManifest.Files.Count;
            }
        }

        public static void CheckUpdaterVersion()
        {
            LaucherLocalVNumber = Util.LocalVersion(curDir + Path.DirectorySeparatorChar + VersionFileName);
            Console.WriteLine("Checking launcher's Version ...");
            Console.WriteLine("Local Version of the updater is :" + LaucherLocalVNumber);
            if (LaucherLocalVNumber != null)
            {
                LaucherRemoteVNumber = Util.RemoteVersion(Launcher.MainPageURL + VersionFileName);
                Console.WriteLine("Got the remote version with success :" + LaucherRemoteVNumber);
            }
            else //if it doesn exist, we need to inform the user
            {
                LaucherLocalVNumber = "0.0.0.0";
                Util.PlaySoundFX("error");
                LaucherRemoteVNumber = Util.RemoteVersion(Launcher.MainPageURL + VersionFileName);
                Console.WriteLine("Couldnt find remote version number");
            }
        }

        public static void MoveLauncherFiles()
        {
            foreach (KeyValuePair<string, string> kv in CryptoManifest.Files)
            {
                string fullpath = curDir + Path.DirectorySeparatorChar + kv.Key.Remove(0,1);
                if (File.Exists(fullpath))
                {
                    Console.WriteLine(fullpath + " : Changing to .bak extension ...");
                    System.IO.File.Move(fullpath, fullpath + ".bak");
                }
            }
        }

        /*public static void AddNewFilesToChangeList()
        {
            // Check the new files
            List<string> NewFiles = Util.GetDirectoryFilesFromUrl(LauncherFolderURL);

            // we add the new files into the changedlist
            foreach (string file in NewFiles)
            {
                if (!ChangedFiles.Contains(file))
                    ChangedFiles.Add(file);
            }

            NumberOfFiles = ChangedFiles.Count;
        }*/

        public void UpdateLauncher_Worker(object sender, DoWorkEventArgs e)
        {
            WebClient webClient = new WebClient();
            webClient.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
            webClient.DownloadFileCompleted += webClient_DownloadFileCompleted;

            // Download the changed files
            foreach (KeyValuePair<string, string> kv in CryptoManifest.Files)
            {
                string smtg = kv.Key.Remove(0, 1);
                DownloadLauncherFiles(kv.Key.Remove(0,1), webClient);
            }

        }

        public void UpdateLauncher_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!backgroundWorker.IsBusy && NumberOfFiles<=0) //double check but somehow it executed twice oO
            {
                CheckSignatureAndLaunch();
            }
        }

        public void DownloadLauncherFiles(string file, WebClient webClient)
        {
            Console.WriteLine(file + " : Downloading ...");
            webClient.DownloadFileAsync(new Uri(LauncherFolderURL+ file), curDir + Path.DirectorySeparatorChar + file);

            while (webClient.IsBusy)
            {
                Thread.Sleep(1);
            }
        }

        void webClient_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                System.Windows.MessageBox.Show(e.Error.ToString());
            }
            else
            {
                try
                {
                    NumberOfFiles--;
                    Console.WriteLine("Downloaded !" + NumberOfFiles.ToString() + " more files to go !");
                }
                catch
                { }
            }
        }

        public static void DeleteOldFiles()
        {
            Console.WriteLine("Cleaning old and update files ...");
            foreach (string file in Directory.GetFiles(curDir))
            {
                // Cleaning BAK files
                if(Path.GetExtension(file)==".bak")
                {
                    File.Delete(file);
                }
                string localPath = Util.ToLocalPath(curDir, file);

                // Cleaning UpdateLogs files
                if (localPath.StartsWith("/UpdateLog"))
                {
                    File.Delete(file);
                }
            }
        }

        public static void CheckSignatureAndLaunch()
        {
            Console.WriteLine("Starting signature check");
            foreach (KeyValuePair<string, string> kv in CryptoManifest.Files)
            {
                string FullPath = curDir + Path.DirectorySeparatorChar + kv.Key.Remove(0, 1);
                var stream = File.ReadAllBytes(FullPath);
                string signatureHexaString = "";
                CryptoManifest.Files.TryGetValue(kv.Key, out signatureHexaString);

                if (!Verify(stream, PublicKeyPath, signatureHexaString))
                {
                    Util.PlaySoundFX("error");
                    System.Windows.MessageBox.Show(kv.Key + " unvalidated. Your version is corrupted, please redownload the launcher from a trusted source (ensl.org)");
                    Console.WriteLine(kv.Key + " : UNVALIDATED and DELETED...");
                    File.Delete(FullPath);
                    Console.Read();
                    Environment.Exit(0);
                }
                else
                    Console.WriteLine(kv.Key + " : validated ...");
            }

            Task.Delay(10);
            Console.WriteLine("Refreshing version Number");
            Util.CreateLocalVersionFile(curDir, VersionFileName, LaucherRemoteVNumber);

            Util.LaunchAgain(curDir + Path.DirectorySeparatorChar + "CleanAndLaunch.exe");

            Environment.Exit(0);
        }

        public static bool Verify(byte[] data, string PublicKeyPath, string SignatureHexaString)
        {
            X509Certificate2 publicKey = LoadPublicKey(PublicKeyPath);
            byte[] signature = StringToByte(SignatureHexaString);

            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            if (publicKey == null)
            {
                MessageBoxResult AlertBox = MessageBox.Show("Your launcher is missing the public crypto key", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                throw new ArgumentNullException("MissingKeyFile");
            }

            if (signature == null)
            {
                throw new ArgumentNullException("signature");
            }

            var provider = (RSACryptoServiceProvider)publicKey.PublicKey.Key;

            return provider.VerifyData(data, new SHA1CryptoServiceProvider(), signature);

        }

        public static X509Certificate2 LoadPublicKey(string PublicKeyPath)
        {
            if(File.Exists(PublicKeyPath))
                return new X509Certificate2(PublicKeyPath);
            else
                throw new ArgumentNullException("MissingKeyFile");
        }

        public static byte[] StringToByte(string str)
        {
            String[] arr = str.Split('-');
            byte[] array = new byte[arr.Length];
            for (int i = 0; i < arr.Length; i++) array[i] = Convert.ToByte(arr[i], 16);
            return array;
        }

        public static bool CheckOneFileSignature(string fileName)
        {
            UpdateCryptoManifest(); //first let's update the crypto manifest with latest values

            foreach (KeyValuePair<string, string> kv in CryptoManifest.Files)
            {
                if (kv.Key.Remove(0, 1) == fileName) // we found the filename in the manifest
                {
                    string FullPath = curDir + Path.DirectorySeparatorChar + kv.Key.Remove(0, 1);
                    var stream = File.ReadAllBytes(FullPath);
                    string signatureHexaString = "";
                    CryptoManifest.Files.TryGetValue(kv.Key, out signatureHexaString);
                    return Verify(stream, PublicKeyPath, signatureHexaString);
                }
            }
            return false;
        }
    }
}
