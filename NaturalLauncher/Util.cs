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
using HtmlAgilityPack;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.IO.Compression;
using System.Windows.Resources;

namespace NaturalLauncher
{
    class Util
    {
        public static string ComputeMD5(string file)
        {
            MD5 md5 = MD5.Create();

            string hash = "";
            using (var stream = File.OpenRead(file))
            {
                hash = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "");
            }

            return hash;
        }

        public static string LocalVersion(string path)
        {
            string lv = null;

            if (string.IsNullOrEmpty(path)
                || System.IO.Path.GetInvalidPathChars().Intersect(
                                  path.ToCharArray()).Count() != 0
                || !new System.IO.FileInfo(path).Exists)
            {
                lv = null;
            }
            else if (new System.IO.FileInfo(path).Exists)
            {
                string s = System.IO.File.ReadAllText(path);
                if (ValidateFile(s)) { 
                    lv = s;
                }
                else { 
                    lv = null;
                }
            }

            return lv;
        }

        public static string CreateLocalVersionFile(string folderPath,
                     string fileName, string version)
        {
            if (!new System.IO.DirectoryInfo(folderPath).Exists)
            {
                System.IO.Directory.CreateDirectory(folderPath);
            }

            string path = folderPath + Path.DirectorySeparatorChar + fileName;

            if (new System.IO.FileInfo(path).Exists)
            {
                new System.IO.FileInfo(path).Delete();
            }

            if (!new System.IO.FileInfo(path).Exists)
            {
                System.IO.File.WriteAllText(path, version);
            }
            return path;
        }

        // method used to check internet connectivity
        public static bool CheckForInternetConnection(string UrlToCheck)
        {
            try
            {
                using (var client = new WebClient())
                using (client.OpenRead(UrlToCheck)) // or http://clients3.google.com/generate_204
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public static bool ValidateFile(string contents)
        {
            bool val = false;
            if (!string.IsNullOrEmpty(contents))
            {
                string pattern = "^([0-9]*\\.){3}[0-9]*$";
                System.Text.RegularExpressions.Regex re =
                            new System.Text.RegularExpressions.Regex(pattern);
                val = re.IsMatch(contents);
            }
            return val;
        }

        public static string RemoteVersion(string url)
        {
            string rv = "";

            try
            {
                System.Net.HttpWebRequest req = (System.Net.HttpWebRequest)
                System.Net.WebRequest.Create(url);
                System.Net.HttpWebResponse response =
                  (System.Net.HttpWebResponse)req.GetResponse();
                System.IO.Stream receiveStream = response.GetResponseStream();
                System.IO.StreamReader readStream =
                  new System.IO.StreamReader(receiveStream, Encoding.UTF8);
                string s = readStream.ReadToEnd();
                response.Close();
                if (ValidateFile(s))
                {
                    rv = s;
                }
            }
            catch (Exception exception)
            {
                // Anything could have happened here but 
                // we don't want to stop the user
                // from using the application.
                rv = null;
                App.SendReport(exception, "Couldnt Find the Remote Version of NS");
            }
            return rv;
        }

        public static string Size(long bytes)
        {
            if (bytes > 1000000000)
            {
                return ((float)bytes / 1000000000f).ToString("f") + " GB";
            }

            if (bytes > 1000000)
            {
                return ((float)bytes / 1000000f).ToString("f") + " MB";
            }
            if (bytes > 1000)
            {
                return ((float)bytes / 1000f).ToString("f") + " KB";
            }
            return ((float)bytes).ToString("f") + " B";
        }

        public static string ToLocalPath(string root, string dir)
        {
            return dir.Replace(root, "").Replace("\\", "/");
        }

        public static string GetLongTimeString()
        {
            var src = DateTime.Now;
            string StringToReturn = src.Month + "_" + src.Day + "_" + src.Hour + "_" + src.Minute + "_" + src.Second;
            return StringToReturn;
        }

        public static string GetShortTimeString()
        {
            var src = DateTime.Now;
            string StringToReturn = "[ " + src.Hour + " : " + src.Minute + " : " + src.Second + " ] : ";
            return StringToReturn;
        }

        public static string GetSteamFolder(bool WantCommon) //just testing if ever needed
        {
            string path = "";
            // HKEY_CURRENT_USER/Software/Valve/Steam
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\Valve\\Steam"))
                {
                    if (key != null)
                    {
                        Object o = key.GetValue("SteamPath");
                        if (o != null)
                        {
                            path = o as String;

                            if(WantCommon)
                                path = path + "/steamapps/common";

                            path = Path.GetFullPath(path);

                            Console.WriteLine(path);

                            // NEEDS A NUGGET PACKAGE GAMELOOP.VDF TO READ
                            // To get full list of game folders : steamapps\libraryfolders.vdf 
                            /*string LibraryFoldersPath = path + Path.DirectorySeparatorChar + "steamapps" + Path.DirectorySeparatorChar + "libraryfolders.vdf";

                            // we need a function to extract folder directory
                            dynamic FolderLibrary = VdfConvert.Deserialize(File.ReadAllText(LibraryFoldersPath));
                            var FolderLibraryValues = FolderLibrary.Value;
                            var importantJsonObject = FolderLibrary.ToJson();*/
                        }
                    }
                }
            }
            catch
            {
                MessageBoxResult AlertBox = MessageBox.Show("Steam Folder not found");
                path = "";
            }

            return path;
            
        }

        public static string GetHLFolder() //just testing if ever needed
        {
            string path = "";
            // HKEY_CURRENT_USER/Software/Valve/Steam
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\Valve\\Steam"))
                {
                    if (key != null)
                    {
                        Object o = key.GetValue("ModInstallPath");
                        if (o != null)
                        {
                            path = o as String;

                            path = Path.GetFullPath(path);

                            Console.WriteLine(path);
                        }
                    }
                }
            }
            catch(Exception exception)
            {
                MessageBoxResult AlertBox = MessageBox.Show("HL Folder not found","Alert", MessageBoxButton.OK , MessageBoxImage.Error);
                App.SendReport(exception, "Couldnt Find the Remote Version of NS");
                path = "";
            }

            return path;

        }

        public static int ReadGathererCount()
        {
            var url = Properties.Settings.Default.GatherURL;
            var web = new HtmlWeb();
            var doc = web.Load(url);
            string htmlstring = doc.Text;
            int Count = 0;
            // need to retrieve this : <ul id="gatherers"> 
            var nodal = doc.DocumentNode.Descendants("ul");

            for(int i = 0; i < nodal.Count(); i++)
            {
                try
                {
                    if (nodal.ElementAt(i).Id == "gatherers")
                    {
                        Count = nodal.ElementAt(i).SelectNodes("li").Count(); //and count the <li> inside
                    }
                }
                catch
                {
                    Count = 0;
                }
                    
            }

            return Count;
        }

        // This function is here to compare the general "standard" ns install with the advanced "NlPack" install version
        // If the user is using the NLPack then the manifest has to be updated consequently.
        public static LauncherManifest CleanManifestWithOptions(LauncherManifest GeneralManifest, LauncherManifest NLManifest)
        {
            //string value = "";
                foreach (KeyValuePair<string, string> kv in NLManifest.Files)
                {
                    if (GeneralManifest.Files.ContainsKey(kv.Key))
                    {
                        // we should delete, not to verify this. Then a second period of the updater check changed with the nl folder and its own manifest.
                        // the following code is in case you want to change the value (commented)

                        /*NLManifest.Files.TryGetValue(kv.Key, out value);
                        GeneralManifest.Files[kv.Key] = value; */

                        GeneralManifest.Files.Remove(kv.Key);
                    }
                }

            return GeneralManifest;
        }

        public static bool RemoteFileExists(string url)
        {
            try
            {
                //Creating the HttpWebRequest
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                //Setting the Request method HEAD, you can also use GET too.
                request.Method = "HEAD";
                //Getting the Web Response.
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                //Returns TRUE if the Status code == 200
                response.Close();
                return (response.StatusCode == HttpStatusCode.OK);
            }
            catch
            {
                //Any exception will returns false.
                return false;
            }
        }

        public static List<string> TheDirectory = new List<string>();

        public static List<string> GetDirectoryFilesFromUrl(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    string html = reader.ReadToEnd();
                    Regex regex = new Regex(GetDirectoryListingRegexForUrl(url));
                    MatchCollection matches = regex.Matches(html);
                    if (matches.Count > 0)
                    {
                        foreach (Match match in matches)
                        {
                            if (match.Success)
                            {
                                TheDirectory.Add(match.Groups["name"].ToString());
                            }
                        }
                    }
                }
            }

            TheDirectory.Remove("Parent Directory");

            return TheDirectory;

        }

        public static string GetDirectoryListingRegexForUrl(string url)
        {
            if (url.Equals(url))
            {
                return "<a href=\".*\">(?<name>.*)</a>"; //problem with the .exe.config file
            }
            throw new NotSupportedException();
        }

        public static string AskForHLFolder()
        {
            string folderPath = "";
            var dialog = new System.Windows.Forms.OpenFileDialog();

            dialog.InitialDirectory = Util.GetHLFolder();
            dialog.Filter = "Half life Executable|hl.exe";

            dialog.Title = "Please indicate where is your Half Life steam executable !";

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                folderPath = dialog.FileName;
                folderPath = folderPath.Remove(folderPath.Length + 1 - 8);
                return folderPath;
            }
            else
            {
                Util.PlaySoundFX("error");
                MessageBoxResult AlertBox = System.Windows.MessageBox.Show("This launcher need your half life directory in order to function. Please relaunch.", "Alert", MessageBoxButton.OK, MessageBoxImage.Error);
                
                folderPath = null;
                return folderPath;
            }
        }

        public static LauncherManifest GetIgnoreManifest(bool isVerification)
        {
            // we first get the ignore.list used by dev
            string IgnorePath = Launcher.curDir + Path.DirectorySeparatorChar + "ignore.list";
            LauncherManifest NewManifest = new LauncherManifest();

            if (File.Exists(IgnorePath) && isVerification) //then we can use the one in the folder. That's ok
            {
                string IgnoreString = File.ReadAllText(IgnorePath);
                NewManifest = JsonConvert.DeserializeObject<LauncherManifest>(IgnoreString);
            }
            else
            {
                try
                {
                    using (WebClient webClient = new WebClient()) // yeah I know it's brutal :D
                    {
                        webClient.DownloadFile(new Uri(Properties.Settings.Default.IndexURL + "/ignore.list"), IgnorePath);
                    }

                    string IgnoreString = File.ReadAllText(IgnorePath);
                    NewManifest = JsonConvert.DeserializeObject<LauncherManifest>(IgnoreString);
                }
                catch(Exception exception)
                {
                    /*MessageBoxResult AlertBox = System.Windows.MessageBox.Show("Could not retrieve a new ignore manifest file, Creating a void one...");
                    NewManifest.Files["/config.cfg"] = "";*/
                    Util.PlaySoundFX("error");
                    MessageBoxResult AlertBox = System.Windows.MessageBox.Show("Launcher couldn't find a correct ignore.list from online source, please verify you internet connection..."
                        , "Alert", MessageBoxButton.OK, MessageBoxImage.Error);

                    App.SendReport(exception, "Launcher couldn't find a correct ignore.list from online source, please verify you internet connection...");
                }
            }
            // then we read the custom one on the disk
            string CustomIgnorePath = Launcher.curDir + Path.DirectorySeparatorChar + "customignore.list";
            LauncherManifest CustomignoreManifest = new LauncherManifest();

            if (File.Exists(CustomIgnorePath))
            {
                string IgnoreString = File.ReadAllText(CustomIgnorePath);
                CustomignoreManifest = JsonConvert.DeserializeObject<LauncherManifest>(IgnoreString);
            }
            else
            {
                CustomignoreManifest.Files["/exemple.xpl"] = "1";

                File.WriteAllText(CustomIgnorePath, JsonConvert.SerializeObject(CustomignoreManifest, Formatting.Indented)); //write the new voidy ignore manifest
            }

            // then we add both manifest to return the ignore list
            foreach(KeyValuePair<string, string> kv in CustomignoreManifest.Files)
            {
                NewManifest.Files.Add(kv.Key, kv.Value);
            }

            return NewManifest;
        }

        public static void CopyDirectory(string SourcePath, string DestinationPath) //not used anymore.
        {
            //Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(SourcePath, "*",
                SearchOption.AllDirectories))
                Directory.CreateDirectory(dirPath.Replace(SourcePath, DestinationPath));

            ConsoleManager.Show();
            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(SourcePath, "*.*",
                SearchOption.AllDirectories))
            {
                /*string newdirr = Path.GetDirectoryName(newPath);
                if (!newdirr.Contains("sound") || !newPath.Contains("maps") || !newPath.Contains("dlls") || !newPath.Contains("cl_dlls"))
                {*/
                    Console.WriteLine(Path.GetFileName(newPath) + " being copied...");
                    File.Copy(newPath, newPath.Replace(SourcePath, DestinationPath), true);
                //}
            }

            ConsoleManager.Hide();

        }

        public static void ZipDirectory(string SourceDirPath)
        {
            ConsoleManager.Show();
            Console.WriteLine("Ziping directory...");
            Console.WriteLine("This can take up to 2 minutes, please wait...");
            ZipFile.CreateFromDirectory(SourceDirPath, SourceDirPath + "_saved.zip", CompressionLevel.Optimal,false);
            Console.WriteLine("Ziping done !");
            ConsoleManager.Hide();
        }

        public static bool GetAValueInCfg(string ValueName, out string Value)
        {
            string[] CfgLines = File.ReadAllLines(Launcher.NSFolder + Path.DirectorySeparatorChar + "config.cfg");
            string[] UCfgLines = File.ReadAllLines(Launcher.NSFolder + Path.DirectorySeparatorChar + "userconfig.cfg");
            bool found = false;
            Value = "not found";

            // first we look in the config.cfg then we will look in userconfig and keep its value if it exists (as it's the last cfg executed)
            foreach (string Line in CfgLines)
            {
                if(Line.StartsWith(ValueName))
                {
                    Value = Line.Remove(0, ValueName.Length + 2); //on enleve tout le debut et le premier guillemet
                    Value = Value.Remove(Value.Length - 1, 1); // et le dernier
                    found = true;
                    //initialInt = Convert.ToInt32(initialValue);
                }
            }

            // if it's the userconfig, take that instead
            foreach (string Line in UCfgLines)
            {
                if (Line.StartsWith(ValueName))
                {
                    Value = Line.Remove(0, ValueName.Length + 2); //on enleve tout le debut et le premier guillemet
                    Value = Value.Remove(Value.Length - 1, 1); // et le dernier
                    found = true;
                    //initialInt = Convert.ToInt32(initialValue);
                }
            }

            return found;
        }

        public static void ChangeAValueInCfg(string ValueName,string Value,bool isStrictToConfig = false)
        {
            string[] CfgLines = File.ReadAllLines(Launcher.NSFolder + Path.DirectorySeparatorChar + "config.cfg");
            string[] UCfgLines = File.ReadAllLines(Launcher.NSFolder + Path.DirectorySeparatorChar + "userconfig.cfg");
            bool cfgchange = false;
            bool ucfgchange = false;
            bool found = false;
            int loopindex = 0;
            // first we look in the config.cfg then we will look in userconfig and keep its value if it exists (as it's the last cfg executed)
            foreach (string Line in CfgLines)
            {
                if (Line.StartsWith(ValueName))
                {
                    CfgLines[loopindex] = ValueName + " " + "\"" + Value + "\"";
                    found = true;
                    cfgchange = true;
                }
                loopindex++;
            }

            if (isStrictToConfig) //if should only be in config.cfg, let's verify it's not in usercfg
            {
                loopindex = 0;
                foreach (string Line in UCfgLines)
                {
                    if (Line.StartsWith(ValueName))
                    {
                        UCfgLines[loopindex] = ""; //we remove the doublon
                        ucfgchange = true;
                    }
                    loopindex++;
                }
            }
            if (!isStrictToConfig) //we can check usercfg
            {
                loopindex = 0;
                foreach (string Line in UCfgLines)
                {
                    if (Line.StartsWith(ValueName))
                    {
                        UCfgLines[loopindex] = ValueName + " " + "\"" + Value + "\"";
                        found = true;
                        ucfgchange = true;
                    }
                    loopindex++;
                }
            }
            if (!found && !isStrictToConfig) //we didnt find it, let's add it to usercfg!
            {
                Array.Resize(ref UCfgLines, UCfgLines.Length + 1);
                UCfgLines[UCfgLines.Length - 1] = ValueName + " " + "\"" + Value + "\"";
                ucfgchange = true;
            }

            if (!found && isStrictToConfig) //we didnt find it, let's add it to config cfg !
            {
                Array.Resize(ref CfgLines, CfgLines.Length + 1);
                CfgLines[CfgLines.Length - 1] = ValueName + " " + "\"" + Value + "\"";
                cfgchange = true;
            }

            // ecrire le deux fichiers à l'emplacement source !
            try
            {
                if(cfgchange)
                {
                    File.WriteAllLines(Launcher.NSFolder + Path.DirectorySeparatorChar + "config.cfg", CfgLines);
                }
            }
            catch(Exception exception)
            {
                System.Windows.MessageBox.Show("Could not write config.cfg, please verify the file is not read only !", "Read only", MessageBoxButton.OK, MessageBoxImage.Error);
                App.SendReport(exception, "Could not write config.cfg, please verify the file is not read only !");
            }
            try
            {
                if (ucfgchange)
                {
                    File.WriteAllLines(Launcher.NSFolder + Path.DirectorySeparatorChar + "userconfig.cfg", UCfgLines);
                }
            }
            catch (Exception exception)
            {
                System.Windows.MessageBox.Show("Could not write userconfig.cfg, please verify the file is not read only !" + Environment.NewLine +
                    "This problem may also be caused by a hud_style setting in your userconfig.cfg file !", "Read only", MessageBoxButton.OK, MessageBoxImage.Error);
                App.SendReport(exception, "Could not write userconfig.cfg, please verify the file is not read only !");
            }

        }

        public static void LaunchAgain(string PathToExe)
        {
            ProcessStartInfo processInfo = new ProcessStartInfo();
            processInfo.WorkingDirectory = Path.GetDirectoryName(PathToExe);
            processInfo.FileName = Path.GetFileName(PathToExe);
            processInfo.ErrorDialog = true;
            processInfo.UseShellExecute = false;
            processInfo.RedirectStandardOutput = true;
            processInfo.RedirectStandardError = true;
            Process proc = Process.Start(processInfo);
        }

        public static void PlaySoundFX(string Type) //now only play when error because whiners
        {
            Uri resourceUri = new Uri("Ressources/sdx_error.wav", UriKind.Relative);
            switch (Type)
            {
                case "error":
                    resourceUri = new Uri("Ressources/sdx_error.wav", UriKind.Relative);
                    StreamResourceInfo streamInfo = Application.GetResourceStream(resourceUri);

                    System.Media.SoundPlayer player = new System.Media.SoundPlayer(streamInfo.Stream);
                    player.Play();
                    break;
                case "start":
                    resourceUri = new Uri("Ressources/sdx_started.wav", UriKind.Relative);
                    break;
                case "finish":
                    resourceUri = new Uri("Ressources/sdx_finished.wav", UriKind.Relative);
                    break;
                default:
                    resourceUri = new Uri("Ressources/sdx_error.wav", UriKind.Relative);
                    break;
            }


        }
    }
}
