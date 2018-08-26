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
using System.IO;
using System.Security.Cryptography;

namespace NaturalLauncher
{
    class ManifestBuilder
    {

        private static MD5 md5;

        public static void BuildManifest(string directory)
        {
            LauncherManifest manifest = new LauncherManifest();

            md5 = MD5.Create();

            RecursiveBuildManifest(directory, "", manifest);

            /*SaveFileDialog dialog = new SaveFileDialog(); // commented but needed when you want to save a manifest somewhere else (could be usefull for a manifest builder console app
            dialog.InitialDirectory = Environment.CurrentDirectory;
            dialog.FileName = "Manifest.txt";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText(dialog.FileName, JsonConvert.SerializeObject(manifest, Formatting.Indented));
            }*/

            string ManifestPath = Launcher.curDir + Path.DirectorySeparatorChar + Launcher.ManifestName;
            File.WriteAllText(ManifestPath, JsonConvert.SerializeObject(manifest, Formatting.Indented));
        }

        private static void RecursiveBuildManifest(string projectRoot, string dir, LauncherManifest manifest)
        {
            string path = projectRoot + dir;

            foreach (string file in Directory.GetFiles(path))
            {
                string localPath = Util.ToLocalPath(projectRoot, file);
                string hash = Util.ComputeMD5(file);

                if (!localPath.EndsWith("_.cfg") && localPath != "/Manifest.txt" && localPath != "/ManifestBuilder.exe"
                    && localPath != "/Newtonsoft.Json.dll") //we don't want  cfg files to get updated here cept config.cfg which is in ignore.list
                    manifest.Files[localPath] = hash;
            }

            foreach (string nextDir in Directory.GetDirectories(path))
            {
                RecursiveBuildManifest(projectRoot, Util.ToLocalPath(projectRoot, nextDir), manifest);
            }
        }


    }
}