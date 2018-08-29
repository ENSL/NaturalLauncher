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
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;

namespace NaturalLauncher
{
    /// <summary>
    /// Logique d'interaction pour Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {

        public static MainWindow MainWindowReference;
        public static bool windowfullyopen = false;

        public Settings()
        {
            InitializeComponent();

            ParamNameTxtbox.SpellCheck.IsEnabled = true;
            ParamNameTxtbox.SpellCheck.CustomDictionaries.Add(new Uri("pack://application:,,,/Ressources/Commande.lex")); //add the command custom dictonary

            //RefreshNLInstallButtonState(); //nl pack based

            Util.GetAValueInCfg("hud_style",out string HUDStyleparam);
            switch(HUDStyleparam)
            {
                case "0":
                    ClassicRadioButton.IsChecked = true;
                    break;
                case "1":
                    MinimalRadioButton.IsChecked = true;
                    break;
                case "2":
                    NLRadioButton.IsChecked = true;
                    break;
            }

            try
            {
                XmlBuilder.ReadConfigXml(out string uno, out bool dos, out string discordStatus, out bool keepAlive);
                Launcher.keepLauncherAlive = keepAlive;
                DiscordTxtbox.Text = discordStatus;
                KeepAliveChecker.IsChecked = keepAlive;
                windowfullyopen = true;
            }
            catch(Exception exception)
            {
                App.SendReport(exception, "Could Not Read ConfigXml");
            }            

        }

        public void SetMainWindowRef(MainWindow MainWindowRef)
        {
            MainWindowReference = MainWindowRef;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            MainWindow.sw = null;
            windowfullyopen = false;
        }

        private void FindParameter_Click(object sender, RoutedEventArgs e)
        {
                try
                {
                    Util.GetAValueInCfg(ParamNameTxtbox.Text,out string ParamValue);
                    ParamValueTxtbox.Text = ParamValue;
                }
                catch
                {
                    ParamValueTxtbox.Text = "not found";
                }
        }

        private void ChangeParameter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if(ParamValueTxtbox.Text  != "not found")
                {
                    Util.ChangeAValueInCfg(ParamNameTxtbox.Text, ParamValueTxtbox.Text);
                }
            }
            catch
            {
                
            }
        }
        
        private void RefreshNLInstallButtonState()
        {
            if (Launcher.IsNLPack())
            {
                NLInstallButton.IsEnabled = false;
                NLUnInstallButton.IsEnabled = true;
            }
            else
            {
                NLInstallButton.IsEnabled = true;
                NLUnInstallButton.IsEnabled = false;
            }
        }

        private void NLInstallButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO Install THE NL PACK, USING NL FOLDERS URL
            Launcher.SetIsNLPack(true);
            RefreshNLInstallButtonState();
            MainWindowReference.CallUpdateGame();
            MainWindowReference.SettingButton.IsEnabled = false;
            this.Hide();
        }

        private void NLUnInstallButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO REMOVE THE NL PACK FILES USING NL FOLDERS URL
            // THEN REPAIR INSTALLATION
            Launcher.SetIsNLPack(false);
            RefreshNLInstallButtonState();
            MainWindowReference.CallUpdateGame();
            MainWindowReference.SettingButton.IsEnabled = false;
            this.Hide();
        }

        private void AdvSettingsInstallButton_Click(object sender, RoutedEventArgs e)
        {
            // GET THE CONFIG FILES INSIDE THE GAME URL. SHOULDNT NEED A MANIFEST, JUST GET CFGs
            try
            {
                if (System.Windows.Forms.MessageBox.Show("You are about to download a set of configuration files "
                + Environment.NewLine + "These files will override your current and existing configuration files if they exists."
                + Environment.NewLine + "Please chose if you want to continue.", "", MessageBoxButtons.YesNo, MessageBoxIcon.Asterisk, MessageBoxDefaultButton.Button1) == System.Windows.Forms.DialogResult.Yes)
                {
                    using (WebClient webClient = new WebClient()) // yeah I know it's brutal :D
                    {
                        webClient.DownloadFile(new Uri(Properties.Settings.Default.GameUrl + "/config.cfg"), Launcher.NSFolder + System.IO.Path.DirectorySeparatorChar + "config.cfg");
                        webClient.DownloadFile(new Uri(Properties.Settings.Default.GameUrl + "/userconfig.cfg"), Launcher.NSFolder + System.IO.Path.DirectorySeparatorChar + "userconfig.cfg");
                        webClient.DownloadFile(new Uri(Properties.Settings.Default.GameUrl + "/alien_.cfg"), Launcher.NSFolder + System.IO.Path.DirectorySeparatorChar + "alien_.cfg");
                        webClient.DownloadFile(new Uri(Properties.Settings.Default.GameUrl + "/pistol_.cfg"), Launcher.NSFolder + System.IO.Path.DirectorySeparatorChar + "pistol_.cfg");
                        webClient.DownloadFile(new Uri(Properties.Settings.Default.GameUrl + "/reset_.cfg"), Launcher.NSFolder + System.IO.Path.DirectorySeparatorChar + "reset_.cfg");
                        webClient.DownloadFile(new Uri(Properties.Settings.Default.GameUrl + "/rine_.cfg"), Launcher.NSFolder + System.IO.Path.DirectorySeparatorChar + "rine_.cfg");
                    }

                    MessageBoxResult AlertBox = System.Windows.MessageBox.Show("Advanced settings Installed with success");
                }
            }
            catch
            {
                MessageBoxResult AlertBox = System.Windows.MessageBox.Show("Advanced settings failed to install");
            }
        }

        private void AdvSettingsUnInstallButton_Click(object sender, RoutedEventArgs e)
        {
            // REMOVE THE CFG FILES. ENDWITH(config.cfg) AND ENDWITH(_.cfg). GAME SHOULD RECREATE THE CONFIG FILE
            try
            {
                foreach (string file in Directory.GetFiles(Launcher.NSFolder))
                {
                    if (file.EndsWith("_.cfg") || file.EndsWith("config.cfg"))
                        File.Delete(file);
                }
                MessageBoxResult AlertBox = System.Windows.MessageBox.Show("Advanced settings Removed with success");
            }
            catch
            {
                MessageBoxResult AlertBox = System.Windows.MessageBox.Show("Failed to remove settings");
            }
        }

        private void BrowseHLFolderButton_Click(object sender, RoutedEventArgs e)
        {
            // BROWSE AND CHANGE THE FOLDER FILE. DONT FORGET TO UPDATE THE GLOBAL VARIABLE
                bool IsItNLPack = Launcher.IsNLPack();
                string folderPath = Util.AskForHLFolder();

                if (folderPath!=null)
                {
                    XmlBuilder.CreateConfigXml();
                    Launcher.HLFolder = folderPath;
                    Launcher.NSFolder = folderPath + System.IO.Path.DirectorySeparatorChar + "ns";
                }
                else
                {
                    // throw new FileNotFoundException("Could not find HL folder"); // no need to crash the launcher for this
                }

                MainWindowReference.CallUpdateGame();
        }

        private void ClassicRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if(windowfullyopen)
            {
                ClassicRadioButton.IsChecked = true;
                MinimalRadioButton.IsChecked = false;
                NLRadioButton.IsChecked = false;
                Util.ChangeAValueInCfg("hud_style", "0", true);
            }
        }

        private void MinimalRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (windowfullyopen)
            {
                ClassicRadioButton.IsChecked = false;
                MinimalRadioButton.IsChecked = true;
                NLRadioButton.IsChecked = false;
                Util.ChangeAValueInCfg("hud_style", "1", true);
            }
        }

        private void NLRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (windowfullyopen)
            {
                ClassicRadioButton.IsChecked = false;
                MinimalRadioButton.IsChecked = false;
                NLRadioButton.IsChecked = true;
                Util.ChangeAValueInCfg("hud_style", "2", true);
            }
        }

        private void AddToIgnoreButton_Click(object sender, RoutedEventArgs e)
        {
            Launcher.AddToIgnoreList();
        }

        private void StopAfterLaunch_Click(object sender, RoutedEventArgs e)
        {
            Launcher.keepLauncherAlive = KeepAliveChecker.IsChecked.Value;

            XmlBuilder.CreateConfigXml();
        }

        private void ChangeDiscord_Click(object sender, RoutedEventArgs e)
        {
            Launcher.discordCustomStatus = DiscordTxtbox.Text;
            Launcher.UpdateDiscord(false);
            XmlBuilder.CreateConfigXml();
        }
    }
}
