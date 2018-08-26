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
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Timers;

// The goal of this application is just to clean old files in the system and launch the new launcher... EZ RIGHT ? The app is called after the self updater included in the whole NaturalLauncher.
// This is not used anymore. Meh !
namespace CleanAndLaunch
{
    class Program
    {
        public static string curDir = Directory.GetCurrentDirectory();
        public static string ProcessName = "NaturalLauncher.exe";

        static void Main(string[] args)
        {
            Thread.Sleep(1000);
            DeleteOldFiles();

            System.Timers.Timer ReLaunchTimer = new System.Timers.Timer();
            ReLaunchTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            ReLaunchTimer.Interval = 100;
            ReLaunchTimer.Enabled = true;
            Console.Read();
        }

        public static void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            if (IsProcessOpen(ProcessName)==false)
            {
                Launch();
            }
        }

        static void Launch()
        {
            Console.WriteLine("Launching...");

            Process myprocess = new Process();
            myprocess.StartInfo.FileName = curDir + Path.DirectorySeparatorChar + ProcessName;
            try
            {
                ProcessStartInfo processInfo = new ProcessStartInfo();
                processInfo.WorkingDirectory = curDir;
                processInfo.FileName = ProcessName;
                processInfo.ErrorDialog = true;
                processInfo.UseShellExecute = false;
                processInfo.RedirectStandardOutput = true;
                processInfo.RedirectStandardError = true;
                Process proc = Process.Start(processInfo);
                Environment.Exit(0);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
                Console.ReadKey();
            }

        }

        public static bool IsProcessOpen(string name)
        {
            foreach (Process clsProcess in Process.GetProcesses())
            {
                if (clsProcess.ProcessName.Contains(name))
                {
                    return true;
                }
            }

            return false;
        }

        public static void DeleteOldFiles()
        {
            Console.WriteLine("Cleaning old and update files ...");
            foreach (string file in Directory.GetFiles(curDir))
            {
                // Cleaning BAK files
                if (Path.GetExtension(file) == ".bak")
                {
                    File.Delete(file);
                }
                string localPath = ToLocalPath(curDir, file);

                // Cleaning UpdateLogs files
                if (localPath.StartsWith("/UpdateLog"))
                {
                    File.Delete(file);
                }
            }
        }

        public static string ToLocalPath(string root, string dir)
        {
            return dir.Replace(root, "").Replace("\\", "/");
        }
    }
}
