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
using System.IO;
using System.Diagnostics;
using System.Net;
using System.Windows;
using DiscordRPC;
using Newtonsoft.Json;
using System.Windows.Forms;

namespace NaturalLauncher
{
    class Launcher
    {
        public static string curDir = Directory.GetCurrentDirectory();
        public static Uri MainPageURL = new Uri(Properties.Settings.Default.IndexURL);
        public static Uri CreditPageURL = new Uri(Properties.Settings.Default.CreditURL);

        public static string HLFolder = "";
        public static string NSFolder;

        public static string VersionFileName = "app.version";
        public static string ManifestName = "Manifest.txt";
        public static string NLManifestName = "NLManifest.txt";
        public static string configName = "Launcher.xml";

        public static bool keepLauncherAlive = true;
        public static bool restartSteam = false; // used at the end of the verify process

        public static DateTime LAUNCHTIME = System.DateTime.UtcNow;
        public static string discordCustomStatus = "Currently In The Launcher";

        public static void PlayGame()
        {
            LAUNCHTIME = System.DateTime.UtcNow; //new time is launch time
            UpdateDiscord(true); //doesnt work cause the timer set it back to false. need static var TODO
            Process.Start("steam://rungameid/17558255459196993606");
            //Environment.Exit(0); // not stopping the process to let discord rpc live
            if (!keepLauncherAlive)
                Environment.Exit(0);
        }

        internal static void UpdateDiscord(bool InGame)
        {
            int gatherPlayers = Util.ReadGathererCount();
            UpdatePubServ(out int pubPlayers, out int maxPlayers);

            int displayPlayers = pubPlayers; //by default
            string discordState = "Players in public";

            if (gatherPlayers > 6)
            {
                displayPlayers = gatherPlayers;
                discordState = "Gather forming";
            }

            if (!InGame)
            {
                MainWindow.Discordclient.SetPresence(new RichPresence()
                {
                    Details = discordCustomStatus,
                    State = discordState,
                    /*Secrets = new Secrets()
                    {
                        JoinSecret = "MTI4NzM0OjFpMmhuZToxMjMxMjM",
                    },*/
                    Party = new Party()
                    {
                        ID = "ae488379 - 351d - 4a4f - ad32 - 2b9b01c91657",
                        Size = displayPlayers,
                        Max = 12,
                    },
                    Timestamps = new Timestamps()
                    {
                        Start = LAUNCHTIME,
                    },
                    Assets = new Assets()
                    {
                        LargeImageKey = "portrait_png",
                        LargeImageText = "Natural Selection Enhanced Launcher",
                        SmallImageKey = "skulku_png",
                    },
                });
            }
            if (InGame)
            {
                MainWindow.Discordclient.SetPresence(new RichPresence()
                {
                    Details = "In Game",
                    State = "Next gather",
                    Party = new Party()
                    {
                        ID = "ae488379 - 351d - 4a4f - ad32 - 2b9b01c91657",
                        Size = Util.ReadGathererCount(),
                        Max = 12,
                    },
                    Timestamps = new Timestamps()
                    {
                        Start = LAUNCHTIME,
                    },
                    Assets = new Assets()
                    {
                        LargeImageKey = "portrait_png",
                        LargeImageText = "Natural Selection Enhanced Launcher",
                        SmallImageKey = "skulku_png",
                    },
                });
            }
        }

        public static void LaunchWebsite(string Url)
        {
            Process.Start(Url);
        }

        public static void RestartSteam()
        {
            string strCmdText;
            strCmdText = "/c taskkill /f /IM \"steam.exe\" ";
            System.Diagnostics.Process.Start("CMD.exe", strCmdText); // kill steam
            System.Threading.Thread.Sleep(1000); // wait one sec
            strCmdText = "/c start steam:";
            System.Diagnostics.Process.Start("CMD.exe", strCmdText); // start steam
            
        }

        public static void RefreshInternetPageAsync(string whichPage)
        {
            using (var webClient = new WebClient())
            {
                if(whichPage == "Main")
                {
                    string FileName = "index.html";
                    webClient.DownloadFileAsync(MainPageURL, FileName);
                }

                if (whichPage == "Credit")
                {
                    string CreditFileName = "credit.html";
                    webClient.DownloadFileAsync(CreditPageURL, CreditFileName);
                }
            }
        }

        public static void CheckInstallDirectory()
        {
            bool NeedDirectory = false;

            if (File.Exists(curDir + Path.DirectorySeparatorChar + configName))
            {
                string IndicatedFolder = "";
                bool IsNLPack = false;
                XmlBuilder.ReadConfigXml(out IndicatedFolder, out IsNLPack, out string discordStatus, out bool keepAlive);

                if (IndicatedFolder.Length >0)
                {
                    HLFolder = IndicatedFolder;
                    discordCustomStatus = discordStatus;
                    keepLauncherAlive = keepAlive;
                }
                else
                    NeedDirectory = true;

                if (!Directory.Exists(IndicatedFolder))
                    NeedDirectory = true;
            }
            else
                NeedDirectory = true;

            if (NeedDirectory)
            {
                string folderPath = Util.AskForHLFolder();

                if (folderPath!=null)
                {
                    HLFolder = folderPath;
                    NSFolder = HLFolder + Path.DirectorySeparatorChar + "ns";
                    XmlBuilder.CreateConfigXml();

                    if (Directory.Exists(NSFolder))
                    {
                        if(System.Windows.Forms.MessageBox.Show("Warning, you are updating an existing NS installation: "
                        + Environment.NewLine + "A backup of your old ns folder can be made under the name ns_saved.zip"
                        + Environment.NewLine + "Please choose if you want to backup your current ns folder.", "", MessageBoxButtons.YesNo, MessageBoxIcon.Asterisk, MessageBoxDefaultButton.Button1)== DialogResult.Yes)
                        { Util.ZipDirectory(NSFolder); }
                    }
                    else
                    {
                        Directory.CreateDirectory(NSFolder);
                    }

                }
                else
                {
                    Environment.Exit(0); //stop the process
                }
            }
            
            NSFolder = HLFolder + Path.DirectorySeparatorChar + "ns";

        }

        public static bool IsNLPack()
        {
            bool Result = false; //by default
            if (File.Exists(curDir + Path.DirectorySeparatorChar + configName))
            {
                string IndicatedFolder = "";
                XmlBuilder.ReadConfigXml(out IndicatedFolder, out Result, out string plop, out bool noob);
            }
            else
            {
                Util.PlaySoundFX("error");
                MessageBoxResult AlertBox = System.Windows.MessageBox.Show("Launcher Config file not found");
                
                CheckInstallDirectory();
                return false;
            }

            return Result;
        }

        public static void SetIsNLPack(bool IsNlPack)
        {
            XmlBuilder.CreateConfigXml();
        }

        public static void AddToIgnoreList()
        {
            Util.GetIgnoreManifest(true); //let's verify we have a ignore list from the start

            string IgnorePath = curDir + Path.DirectorySeparatorChar + "customignore.list";
            string IgnoreString = File.ReadAllText(IgnorePath);
            LauncherManifest IgnoreManifest = JsonConvert.DeserializeObject<LauncherManifest>(IgnoreString);// then get it

            var dialog = new System.Windows.Forms.OpenFileDialog();

            dialog.InitialDirectory = NSFolder;
            dialog.Multiselect = true;

            dialog.Title = "Please indicate what file(s) to add in the ignore list !";

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                foreach(string file in dialog.FileNames)
                {
                    string localPath = Util.ToLocalPath(NSFolder, file);
                    IgnoreManifest.Files[localPath] = "1"; //we using the launchermanifest but set "1" to ignore verify and update ("0" is ignore only update)
                }

                File.WriteAllText(IgnorePath, JsonConvert.SerializeObject(IgnoreManifest, Formatting.Indented));
            }
        }

        internal static void UpdatePubServ(out int publicPlayers, out int maxPlayers)
        {
            // TO BE CONTINUED but so far we can retrieve pub serv info... 
            string serverIP = "104.156.251.121"; //public us
            int port = 27015;
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse(serverIP), port);

            var ServInfo = new ServerChecker.A2S_INFO(remoteEP);

            publicPlayers = ServInfo.Players; //out
            maxPlayers = ServInfo.MaxPlayers; //out
            // Check for the launcher to self update
        }
    }
}
