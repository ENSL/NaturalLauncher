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

using System;
using System.ComponentModel;
using System.Windows;
using System.Security.Cryptography;
using System.IO;
using System.Net;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Threading;
using DiscordRPC;
using System.Timers;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Resources;


namespace NaturalLauncher
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        // TODO We need to know if ns files are being updated to lock the verify/update functions if so (with a file on the main serv).

        public static bool IsDebugMode = false; //no use anymore, but stay here just in case I need him
        public static bool IsVerification = true; //true by default since it's more commun to verify than to update

        public static Settings sw;

        public static string versionNumber;
        public static string remoteVersionNumber;

        public static bool downloadCancelled = false;

        public BackgroundWorker backgroundWorker;
        public static DiscordRpcClient Discordclient;

        Uri LocalIndexURL = new Uri(String.Format("file:///{0}/index.html", Launcher.curDir));

        private static string LogReportPath = Launcher.curDir;
        TextWriter UpdateLog;

        public void MainWindow_Closed(object sender, EventArgs e)
        {
            if (sw != null)
            {
                sw.Close();
            }
        }

        public MainWindow()
        {
            InitializeComponent();

#if DEBUG
            IsDebugMode = true;
#endif

            // Self Updater
            SelfUpdater.DeleteOldFiles(); // not really needed since CleanAndLaunch.exe

            SelfUpdater TheSelfUpdater = new SelfUpdater();
            TheSelfUpdater.SetMainWindowRef(this);
            TheSelfUpdater.UpdateLauncher();

            if (SelfUpdater.LaucherRemoteVNumber != SelfUpdater.LaucherLocalVNumber && SelfUpdater.isSelfUpdating)
            {
                Hide();
            }
            else
            {
                // Set the title of the window with the correct launcher version
                if (SelfUpdater.isSelfUpdating)
                    Launcher_Window.Title = "Natural Launcher v " + SelfUpdater.LaucherLocalVNumber;
                else
                    Launcher_Window.Title = "Natural Launcher";

                // CHECK THE HL AND NS INSTALL DIR AND WRITE IT IN THE LAUNCHER XML
                Launcher.CheckInstallDirectory();

                // set the experimental label
                if (Launcher.isExperimental)
                {
                    Experimental_label.Visibility = Visibility.Visible;
                }
                else
                {
                    Experimental_label.Visibility = Visibility.Hidden;
                }

                // WE SET DISCORD PRESENCE
                string ApplicationID = "474144509048127503";
                Discordclient = new DiscordRpcClient(ApplicationID);

                Discordclient.Initialize();

                OnlineStatusAtLaunch();

                System.Timers.Timer PresenceTimer = new System.Timers.Timer();
                PresenceTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
                PresenceTimer.Enabled = true;
                PresenceTimer.Interval = 5000;

                // *** Get the main index.html if connected *** \\
                if (Util.CheckForInternetConnection(Launcher.MainPageURL.AbsoluteUri)) //if we are connected
                {
                    Launcher.RefreshInternetPageAsync("Main"); // we refresh the page
                    MainWebBrowser.Source = Launcher.MainPageURL; // and display it from source
                }
                else // if not simply show the saved file
                {
                    MainWebBrowser.Navigate(LocalIndexURL.AbsolutePath);
                }

                // CHECK APP.VERSION NUMBER
                Check_Version();

                if (versionNumber != remoteVersionNumber && versionNumber != "0.0.0.0")
                {
                    MessageBoxResult AlertBox = System.Windows.MessageBox.Show("Update Needed !", "Update", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    StartButton.Content = "Please Update";
                    IsVerification = false;
                }
                if (versionNumber == "0.0.0.0")
                {
                    StartButton.Content = "Verify";
                    IsVerification = false;

                }
                if (versionNumber == remoteVersionNumber && versionNumber != "0.0.0.0")
                {
                    StartButton.Content = "Play"; // TODO The algorithm to verify btween local and remote manifest
                }
            }
        }


        private void ReadyLauncherForUpdate(string FirstLogLine)
        {
            StartButton.IsEnabled = false;
            VerifyButton.IsEnabled = false;
            SettingButton.IsEnabled = false;
            UpdateLog = new StreamWriter(LogReportPath + Path.DirectorySeparatorChar + "UpdateLog_" + Util.GetLongTimeString() + ".txt", true); // dt forget to close()
            UpdateLog.WriteLine(Util.GetShortTimeString() + FirstLogLine);
        }

        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            Launcher.UpdateDiscord(false);
            
            Delegate myDelegate = (Action)Update_gatherButton;
            Delegate secondDelegate = (Action)Update_ServLabel;
            WebsiteButton.Dispatcher.Invoke(myDelegate);
            ServInfoLabel.Dispatcher.Invoke(secondDelegate);
        }

        private void OnlineStatusAtLaunch()
        {
            Launcher.UpdateDiscord(false);

            Delegate myDelegate = (Action)Update_gatherButton;
            Delegate secondDelegate = (Action)Update_ServLabel;
            WebsiteButton.Dispatcher.Invoke(myDelegate);
            ServInfoLabel.Dispatcher.Invoke(secondDelegate);
        }

        private void Update_gatherButton()
        {
            if(Util.ReadGathererCount()>0)
                WebsiteButton.Content = "Join Gather ( " + Util.ReadGathererCount() + " / 12 )";
            else
                WebsiteButton.Content = "Join Gather";
        }

        private void Update_ServLabel()
        {
            Launcher.UpdatePubServ(out int pubPlayers, out int maxPlayers);
            ServInfoLabel.Content = "Public Server : " + pubPlayers + " / " + maxPlayers;

        }

        private void Check_Version()
        {
            // *** Get the version number *** \\
            versionNumber = Util.LocalVersion(Launcher.curDir + Path.DirectorySeparatorChar + Launcher.VersionFileName);
            remoteVersionNumber = Util.RemoteVersion(Properties.Settings.Default.IndexURL + Launcher.VersionFileName);

            if (versionNumber != null)
            {
                VersionLabel.Content = versionNumber;
            }
            else //if it doesn exist, we need to inform the user
            {
                versionNumber = "0.0.0.0";
                VersionLabel.Content = "Verify";
                StartButton.Content = "Verify";
                MessageBoxResult AlertBox = System.Windows.MessageBox.Show("Please verify the installation" + Environment.NewLine
                    + "This message will show up if this is the first time you launch this software"
                    , "Alert", MessageBoxButton.OK, MessageBoxImage.Exclamation); //not true anymore
                Launcher.restartSteam = true;
                SettingButton.IsEnabled = false;
                IsVerification = false;
            }
        }

        #region Clicks
        private void Start_Click(object sender, RoutedEventArgs e)
        {
            if (SelfUpdater.CheckOneFileSignature("NaturalLauncher.exe") || !SelfUpdater.isSelfUpdating)
            {
                if (versionNumber != remoteVersionNumber || versionNumber == "0.0.0.0")
                {
                    ReadyLauncherForUpdate("Start Update from play button");
                    StartButton.Content = "Verifying...";
                    IsVerification = false;
                    UpdateGame(); //verify the game with the right click context menu
                }
                else
                {
                    Launcher.PlayGame();
                    Util.PlaySoundFX("finish");
                }
            }
            else
            {
                Util.PlaySoundFX("error");
                MessageBoxResult AlertBox = System.Windows.MessageBox.Show("Your launcher is corrupted or out of date, please relaunch" + Environment.NewLine
                    + "if the problem persist, please redownload the launcher from an official source (ensl.org)");
                File.Delete(Launcher.curDir + Path.DirectorySeparatorChar + "launcher.version");

                Environment.Exit(0);
            }
        }

        private void Website_Click(object sender, RoutedEventArgs e)
        {
            Launcher.LaunchWebsite(Properties.Settings.Default.GatherURL);
        }

        private void Setting_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sw == null)
                {
                    sw = new Settings();
                }
                sw.SetMainWindowRef(this);
                sw.Show();
            }
            catch(Exception exception)
            {
                App.SendReport(exception, "Could Not Open Setting window");
            }
        }

        private void Verify_Click(object sender, RoutedEventArgs e)
        {
            ReadyLauncherForUpdate("Verification started from launcher");

            if (versionNumber != remoteVersionNumber || versionNumber == "0.0.0.0")
            {
                IsVerification = false;
                UpdateLog.WriteLine(Util.GetShortTimeString() + "but it's an update");
            }
            else
            {
                IsVerification = true;
            }

            UpdateGame(); //verify the game with the right click context menu
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            var brush = new ImageBrush();
            try
            {
                if (!downloadCancelled)
                {
                    downloadCancelled = true;
                    backgroundWorker.CancelAsync();

                    Uri resourceUri = new Uri("Ressources/playimage.png", UriKind.Relative); //"pack://application:,,,/NaturalLauncher;component/Images/Resource_Image.png"
                    StreamResourceInfo streamInfo = Application.GetResourceStream(resourceUri);

                    BitmapFrame temp = BitmapFrame.Create(streamInfo.Stream);
                    brush.ImageSource = temp;

                    PauseButton.Background = brush;

                }
                else
                {
                    ReadyLauncherForUpdate("Verification resumed from launcher");
                    UpdateGame(); //verify the game with the right click context menu

                    Uri resourceUri = new Uri("Ressources/pauseimage.png", UriKind.Relative);
                    StreamResourceInfo streamInfo = Application.GetResourceStream(resourceUri);

                    BitmapFrame temp = BitmapFrame.Create(streamInfo.Stream);
                    brush.ImageSource = temp;

                    PauseButton.Background = brush;
                }
            }
            catch
            {
                downloadCancelled = false;
                Uri resourceUri = new Uri("Ressources/pauseimage.png", UriKind.Relative);
                StreamResourceInfo streamInfo = Application.GetResourceStream(resourceUri);

                BitmapFrame temp = BitmapFrame.Create(streamInfo.Stream);
                brush.ImageSource = temp;

                PauseButton.Background = brush;
            } //I don't give a f if it doesnt catch shit

        }
        #endregion

        #region ContextMenu
        private void BuildManifest(object sender, RoutedEventArgs e)
        {
            ManifestBuilder.BuildManifest(Launcher.NSFolder); // build the manifest of the file to update
        }
        private void VerifyTheGame(object sender, RoutedEventArgs e)
        {
            /*UpdateLog = new StreamWriter(LogReportPath + Path.DirectorySeparatorChar + "UpdateLog_" + Util.GetLongTimeString() + ".txt", true); // dt forget to close()
            UpdateLog.WriteLine(Util.GetShortTimeString() + "Verification started from context menu");
            UpdateGame(); //verify the game with the right click context menu*/ //not used anymore
        }
        private void CheckLauncherUpdate(object sender, RoutedEventArgs e)
        {
            SelfUpdater TheSelfUpdater = new SelfUpdater();
            TheSelfUpdater.allowMessage = true;
            TheSelfUpdater.UpdateLauncher();
        }
        private void JoinDiscord(object sender, RoutedEventArgs e)
        {
            Process.Start("https://discord.gg/ZUSSBUA");
        }
        private void SeeCredit(object sender, RoutedEventArgs e)
        {
            Process.Start("https://discord.gg/ZUSSBUA");
        }
        private void JoinPublicServer(object sender, RoutedEventArgs e)
        {
            Process.Start("steam://connect/104.156.251.121:27015");
        }
        private void AddToIgnore(object sender, RoutedEventArgs e)
        {
            Launcher.AddToIgnoreList();
        }

        private void OpenNSFolder(object sender, RoutedEventArgs e)
        {
            Process.Start(Launcher.NSFolder);
        }
        #endregion  

        #region UpdateMaelAlgo

        public void UpdateGame()
        {
            Util.PlaySoundFX("start");
            TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
            downloadCancelled = false;
            backgroundWorker = new System.ComponentModel.BackgroundWorker();
            backgroundWorker.WorkerReportsProgress = true;
            backgroundWorker.WorkerSupportsCancellation = true;
            backgroundWorker.DoWork += UpdateGame_Worker;
            backgroundWorker.ProgressChanged += UpdateGame_ProgressChanged;
            backgroundWorker.RunWorkerCompleted += UpdateGame_RunWorkerCompleted;
            backgroundWorker.RunWorkerAsync();
        }


        public void CallUpdateGame() //same as updategame for now
        {
            ReadyLauncherForUpdate("update Called from a setting's change");
            UpdateGame();
        }


        public void UpdateGame_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                StartButton.Content = "Error";
                UpdateProgressBar.Value = 0;
                MessageBoxResult AlertBox = System.Windows.MessageBox.Show("Error while downloading, please try again");
                ProgressLabel.Content = "Error while downloading, please try again";
                TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Error;
                return;
            }

            if (downloadCancelled)
            {
                ProgressLabel.Content = "Paused !";
                UpdateLog.WriteLine(Util.GetShortTimeString() + "Stop requested by the user...");
                TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Paused; //yellow thing
            }
            else
            {
                ProgressLabel.Content = "Up to date !";
                if (Launcher.restartSteam)
                {
                    Launcher.RestartSteam();
                    UpdateLog.WriteLine(Util.GetShortTimeString() + "Restarting Steam");
                }
                StartButton.Content = "Play";
                UpdateLog.WriteLine(Util.GetShortTimeString() + "Completed the update or verify work");
                TaskbarItemInfo.ProgressValue = 0;
                TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
                Util.CreateLocalVersionFile(Launcher.curDir, Launcher.VersionFileName, remoteVersionNumber); //update the version with the new one

                if(Launcher.isExperimental)
                {
                    Experimental_label.Visibility = Visibility.Visible;
                }
                else
                {
                    Experimental_label.Visibility = Visibility.Hidden;
                }
                Check_Version(); // we should have a changed app.version file to check
            }

            UpdateLog.Close();
            backgroundWorker.Dispose();

            Util.PlaySoundFX("finish");

            StartButton.IsEnabled = true;
            SettingButton.IsEnabled = true;
            VerifyButton.IsEnabled = true;
        }

        public void UpdateGame_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ProgressLabel.Content = e.UserState as string;
            UpdateProgressBar.Value = e.ProgressPercentage;
            TaskbarItemInfo.ProgressValue = (double)((float)e.ProgressPercentage / 100f);
        }

        public void UpdateGame_Worker(object sender, DoWorkEventArgs e)
        {
            backgroundWorker.ReportProgress(0, "Downloading Manifest...");

            string ManifestURL = Launcher.MainPageURL.AbsoluteUri + Launcher.ManifestName;

            WebClient webClient = new WebClient();
            webClient.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
            webClient.DownloadProgressChanged += webClient_DownloadProgressChanged;
            webClient.DownloadFileCompleted += webClient_DownloadFileCompleted;

            string manifest = webClient.DownloadString(ManifestURL);
            LauncherManifest RemoteManifest = JsonConvert.DeserializeObject<LauncherManifest>(manifest);
            string UrlToDownloadGame = Properties.Settings.Default.GameUrl;

            if (Launcher.isExperimental)
            {
                try
                {
                    string XPManifestURL = Launcher.MainPageURL.AbsoluteUri + Launcher.XPManifestName;
                    string XPmanifest = webClient.DownloadString(XPManifestURL);
                    LauncherManifest XPManifest = JsonConvert.DeserializeObject<LauncherManifest>(XPmanifest);

                    //RemoteManifest = Util.CleanManifestWithOptions(RemoteManifest, XPManifest); //cleaning the manifest hash consequently
                    RemoteManifest = XPManifest;
                    UrlToDownloadGame = Properties.Settings.Default.NsXpURL;
                }
                catch
                {
                    MessageBoxResult AlertBox = System.Windows.MessageBox.Show("Couldn't find the experimental manifest online, downloading normal version instead"
                    , "Alert", MessageBoxButton.OK, MessageBoxImage.Exclamation); //not true anymore
                    RemoteManifest = JsonConvert.DeserializeObject<LauncherManifest>(manifest);
                    UrlToDownloadGame = Properties.Settings.Default.GameUrl;
                }
            }

            LauncherManifest IgnoreManifest = Util.GetIgnoreManifest(IsVerification); //attention, might not exist, if so it has to be downloaded, if not let's use it !

            string gameInstallDir = Launcher.NSFolder;
            UpdateLog.WriteLine(Util.GetShortTimeString() + "Install Directory located in : " + gameInstallDir);
            UpdateLog.WriteLine(Util.GetShortTimeString() + "Is Experimental : " + Launcher.isExperimental.ToString());
            UpdateLog.WriteLine(Util.GetShortTimeString() + "Distant Manifest located at : " + ManifestURL);

            var md5 = MD5.Create();
            int totalFiles = RemoteManifest.Files.Count;
            UpdateLog.WriteLine(Util.GetShortTimeString() + "Total file to update : " + totalFiles);

            int curFile = 0;

            // Update the standard files
            foreach (KeyValuePair<string, string> kv in RemoteManifest.Files)
            {
                bool ShouldDownload = false;
                string gameFilePath = gameInstallDir + kv.Key.Replace("/", Path.DirectorySeparatorChar.ToString());
                IgnoreManifest.Files.TryGetValue(kv.Key, out string IgnoreValue); //get the ignore file value in manifest ("0" ignore only verif, "1" ignore verif + update)

                bool Condition1 = File.Exists(gameFilePath) && IgnoreManifest.Files.ContainsKey(kv.Key) == false;//if the file exists and not in the ignore manifest, let's have it !
                bool Condition2 = File.Exists(gameFilePath) && IgnoreManifest.Files.ContainsKey(kv.Key) && IgnoreValue == "0" && !IsVerification; //if the file exists, is in the ignore manifest, but is updatable and... it's update time !

                if (Condition1 || Condition2) 
                {
                    int progress = (int)(((float)curFile / (float)totalFiles) * 100);
                    backgroundWorker.ReportProgress(progress, "(" + (curFile) + " / " + totalFiles + ") Checking " + kv.Key);

                    //Check its md5 hash
                    using (var stream = File.OpenRead(gameFilePath))
                    {
                        var hash = Util.ComputeMD5(gameFilePath);

                        if (hash != kv.Value)
                        {
                            UpdateLog.WriteLine(Util.GetShortTimeString() + gameFilePath + " needs to be updated");
                            ShouldDownload = true;
                        }
                    }
                }
                if (!File.Exists(gameFilePath))
                {
                    UpdateLog.WriteLine(Util.GetShortTimeString() + gameFilePath + " not existing, needs downloading");
                    ShouldDownload = true;
                }
                if (File.Exists(gameFilePath) && Launcher.keepCustomFiles && ( kv.Key.EndsWith(".wav") || kv.Key.EndsWith(".mp3") || kv.Key.EndsWith(".spr") || kv.Key.EndsWith(".tga") ) )
                {
                    UpdateLog.WriteLine(Util.GetShortTimeString() + gameFilePath + " Keeping custom files");
                    ShouldDownload = false;
                }
                if (File.Exists(gameFilePath) && IgnoreManifest.Files.ContainsKey(kv.Key) && IgnoreValue == "1") // ignore everything
                {
                    UpdateLog.WriteLine(Util.GetShortTimeString() + gameFilePath + " Is Ignored for update, not checking");
                    ShouldDownload = false;
                }
                if (File.Exists(gameFilePath) && IgnoreManifest.Files.ContainsKey(kv.Key) && IsVerification && IgnoreValue == "0") //if we need to ignore update and it's verification, we ignore !
                {
                    UpdateLog.WriteLine(Util.GetShortTimeString() + gameFilePath + " Is Ignored for verifications, not checking");
                    ShouldDownload = false;
                }


                if (ShouldDownload)
                {
                    DownloadFile(curFile, totalFiles, kv, RemoteManifest, gameFilePath, UrlToDownloadGame, webClient);

                    var hash = Util.ComputeMD5(gameFilePath);

                    if (hash != kv.Value)
                    {
                        //MessageBox.Show("Failed Validating " + kv.Key + " : Redownloading"); //problem with double validation, is it too soon in the chain of event ?
                        UpdateLog.WriteLine(Util.GetShortTimeString() + "failed to verify newly downloaded file, redownloading");
                        DownloadFile(curFile, totalFiles, kv, RemoteManifest, gameFilePath, UrlToDownloadGame, webClient);
                    }
                    UpdateLog.WriteLine(Util.GetShortTimeString() + "Download complete for file: " + kv.Key + " with new hash :" + hash + " for manifest hash : " + kv.Value);
                }
                if (backgroundWorker.CancellationPending)
                {
                    UpdateLog.WriteLine(Util.GetShortTimeString() + "Update cancelled");
                    return;
                }
                
                curFile++;
            }
                
            backgroundWorker.ReportProgress(100, "Writing Local Manifest");

            // if we keept the custom files, it's better to use them (thus the hud_style 0)
            if(Launcher.keepCustomFiles)
            {
                try
                {
                    Util.ChangeAValueInCfg("hud_style ", "0"); // change 
                }
                catch
                {
                    //MessageBoxResult AlertBox = System.Windows.MessageBox.Show("Could not write config file to change hud_style 0");
                    UpdateLog.WriteLine(Util.GetShortTimeString() + "Failed changing the hud_style value");
                }
            }


            /* string manifestDirectory = Launcher.curDir + Path.DirectorySeparatorChar + Launcher.ManifestName;

            File.WriteAllText(manifestDirectory, manifest); // replace all with function in util */
        }

        private void DownloadFile(int curFile, int totalFiles, KeyValuePair<string, string> kv, LauncherManifest RemoteManifest, string gameFilePath, string folderURL, WebClient webClient)
        {
            int progress = (int)(((float)curFile / (float)totalFiles) * 100);

            string status = "(" + (curFile) + " / " + totalFiles + ") Downloading: " + kv.Key;

            UpdateLog.WriteLine(Util.GetShortTimeString() + "Downloading file: " + kv.Key);

            backgroundWorker.ReportProgress(progress, status);

            string remoteFile = (folderURL + "/" + kv.Key.Substring(1));
            
            Directory.CreateDirectory(Path.GetDirectoryName(gameFilePath));
            if (File.Exists(gameFilePath))
            {
                UpdateLog.WriteLine(Util.GetShortTimeString() + "File exist, deleting old file");
                File.Delete(gameFilePath);
            }

            try
            {
                webClient.DownloadFileAsync(new Uri(remoteFile), gameFilePath, status);
            }
            catch (Exception e)
            {
                MessageBoxResult AlertBox = System.Windows.MessageBox.Show("new file not found");
                UpdateLog.WriteLine(Util.GetShortTimeString() + "Failed Downloading file: " + kv.Key + "with message : " + e.Message);
                backgroundWorker.CancelAsync();
            }

            while (webClient.IsBusy)
            {
                Thread.Sleep(1);
            }
        }

        void webClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            string status = e.UserState as string + " ( " + Util.Size(e.BytesReceived) + " / " + Util.Size(e.TotalBytesToReceive) + " )";
            backgroundWorker.ReportProgress(e.ProgressPercentage, status);
        }

        void webClient_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                System.Windows.MessageBox.Show(e.Error.ToString());
                backgroundWorker.CancelAsync();
            }

        }
        #endregion

    }

}
