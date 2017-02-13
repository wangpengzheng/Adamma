using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace Adamma
{
    public class BugsNavigator
    {
        public static Boolean NavigateToPSBug(String _PSID, Boolean _isAX6 = false)
        {
            try
            {
                string path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                path += @"\Documents\Product Studio Files";
                string filePath = path + @"\" + _PSID + ".psf";
                
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }


                if (!File.Exists(filePath))
                {
                    FileStream fs = File.Create(filePath);
                    fs.Close();

                    StreamWriter sw = new StreamWriter(filePath, true, Encoding.UTF8);
                    sw.Write("<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                            "<Data Application=\"Product Studio\" Type=\"Form\" Version=\"2.1\">" +
                            "<Form Product=\"" + (_isAX6 ? "AX6" : "AXSE") + "\" FormType=\"Bug\" FormID=\"" + _PSID + "\"/></Data>");
                    sw.Close();
                }

                Process.Start(filePath);
            }
            catch
            {
                return false;
            }
            return true;
        }

        public static void NavigateToTFSBug(String _TFSID)
        {
            string webServiceURL = "http://vstfmbs:8080/tfs/MBS/DAXSE/_workitems#_a=edit&id=" + _TFSID;
            System.Diagnostics.Process.Start(webServiceURL);
        }
    }
}
