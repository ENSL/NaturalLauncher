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
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace NaturalLauncher
{
    class XmlBuilder
    {

        public static bool BuildXmlDocument(string ManifestRootPath)
        { 
            DirectoryInfo dir = new DirectoryInfo(ManifestRootPath);
            var doc = new XDocument(new XDeclaration("1.0", "UTF-8", "yes"),
            StartCreateXML(dir));

            try
            {
                File.WriteAllText(ManifestRootPath + Path.DirectorySeparatorChar + "Manifest.xml", doc.ToString()); //write the new voidy ignore manifest
                return true;
            }
            catch
            {
                return false;
            }
            
        }

        public static XElement StartCreateXML(DirectoryInfo dir) // create a full directory xml
        {
            var xmlInfo = new XElement("MainDirectory");
            //get all the files first
            foreach (var file in dir.GetFiles())
            {
                xmlInfo.Add(new XElement("file", new XAttribute("name", file.Name)));
            }
            //get subdirectories
            var subdirectories = dir.GetDirectories().ToList().OrderBy(d => d.Name);
            foreach (var subDir in subdirectories)
            {
                xmlInfo.Add(CreateSubdirectoryXML(subDir));
            }
            return xmlInfo;
        }

        public static XElement CreateSubdirectoryXML(DirectoryInfo dir)
        {
            //get directories
            var xmlInfo = new XElement("folder", new XAttribute("name", dir.Name));
            //get all the files first
            foreach (var file in dir.GetFiles())
            {
                xmlInfo.Add(new XElement("file", new XAttribute("name", file.Name)));
            }
            //get subdirectories
            var subdirectories = dir.GetDirectories().ToList().OrderBy(d => d.Name);
            foreach (var subDir in subdirectories)
            {
                xmlInfo.Add(CreateSubdirectoryXML(subDir));
            }
            return xmlInfo;
        }

        public static bool ReadConfigXml(out string HLFolder, out bool IsNlPack, out string customDiscordStatus, out bool keepLauncherAlive)
        {
            XmlDocument doc = new XmlDocument();

            try
            {
                doc.Load(Launcher.curDir + Path.DirectorySeparatorChar + Launcher.configName);
                XmlNodeList nodelist = doc.SelectNodes("/LauncherConfiguration");
                HLFolder = doc.SelectSingleNode("//HLFolder").InnerText;
                IsNlPack = doc.SelectSingleNode("//NLPack").InnerText == "True";
                customDiscordStatus = doc.SelectSingleNode("//DiscordStatus").InnerText;
                keepLauncherAlive = doc.SelectSingleNode("//keeplauncherAlive").InnerText == "True";
                return true;
            }
            catch
            {
                HLFolder = "";
                IsNlPack = false;
                customDiscordStatus = "Gather forming";
                keepLauncherAlive = true;
                return false;
            }

        }

        public static bool CreateConfigXml()
        {
            var xmlInfo = new XElement("LauncherConfiguration");
            xmlInfo.Add(new XElement("HLFolder", Launcher.HLFolder));
            xmlInfo.Add(new XElement("NLPack", "False"/*Launcher.IsNLPack().ToString()*/)); //doesnt matter anymore, also, can't use this function cause it asks for the config file...
            xmlInfo.Add(new XElement("DiscordStatus", Launcher.discordCustomStatus));
            xmlInfo.Add(new XElement("keeplauncherAlive", Launcher.keepLauncherAlive.ToString()));

            var doc = new XDocument(new XDeclaration("1.0", "UTF-8", "yes"), xmlInfo);

            try
            {
                File.WriteAllText(Launcher.curDir + Path.DirectorySeparatorChar + Launcher.configName, doc.ToString()); //write the new voidy ignore manifest
                return true;
            }
            catch
            {
                return false;
            }
        }

    }
}
