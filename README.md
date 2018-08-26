# NaturalLauncher

*Natural selection launcher and updater. Allows for quick update of the natural selection folder.


# TO BUILD FROM SOURCES
*If you are building from sources, I imagine you don't want the .exe file to permanantly check for online consistency (public key required) nor self update.
*In order to do so, simply go in SelfUpdater.cs and set the variable "isSelfUpdating" to false.
*You are basicaly good to go !

# CHANGE THE ONLINE FILES THE LAUNCHER REFERS TO
*If you want you own files online, simply change, in the configuration setting of the natural launcher projet in VS, the IndexURL to your custom URL
*as well as the GameUrl to the game URL (sounds logical enough ?)

# TO COMMIT A NEW NS VERSION / PATCH

## FROM THE LAUNCHER:
1. Launch the launcher (tricky sentence)
2. Verify it has the right half life folder target
3. Right click in the launcher
4. Build Manifest

## FROM THE MANIFESTBUILDER.EXE
1. Place the manifestbuilder.exe app in the correct folder
2. Execute it
3. Enjoy
4. Really do it

## THEN
5. Upload files in the "Game" Folder of the ftp
6. Upload the manifest in the root folder.
7. Update the app.version with the new number in root folder