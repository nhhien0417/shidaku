using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StorageUtils.Utils
{
    public static class FileUtils
    {
        public static void OpenInMacFileBrowser(string path)
        {
            bool openInsidesOfFolder = false;
            // try mac
            string macPath = path.Replace("\\", "/"); // mac finder doesn't like backward slashes
            if (System.IO.Directory.Exists(macPath)) // if path requested is a folder, automatically open insides of that folder
            {
                openInsidesOfFolder = true;
            }
            //Debug.Log("macPath: " + macPath);
            //Debug.Log("openInsidesOfFolder: " + openInsidesOfFolder);
            if (!macPath.StartsWith("\""))
            {
                macPath = "\"" + macPath;
            }
            if (!macPath.EndsWith("\""))
            {
                macPath = macPath + "\"";
            }
            string arguments = (openInsidesOfFolder ? "" : "-R ") + macPath;
            //Debug.Log("arguments: " + arguments);
            try
            {
                System.Diagnostics.Process.Start("open", arguments);
            }
            catch(System.ComponentModel.Win32Exception e)
            {
                // tried to open mac finder in windows
                // just silently skip error
                // we currently have no platform define for the current OS we are in, so we resort to this
                e.HelpLink = ""; // do anything with this variable to silence warning about not using it
            }
        }
    }
}
