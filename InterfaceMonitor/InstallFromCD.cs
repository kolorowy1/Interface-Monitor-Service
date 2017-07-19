using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

/// <summary>
///Use only for Realtek USB to Ethernet network card. Drivers for this device are embedded on internal USB flash drive.
/// </summary>
namespace InterfaceMonitor
{
    class InstallFromCD
    {
        private const string realtekEXE = "RTK_NIC_DRIVER_INSTALLER.sfx.exe";

        /// <summary>
        /// Find Realtek USB drive and install drivers
        /// </summary>
        public void GetRealtekCDROM()
        {
            DriveInfo[] allDrives = DriveInfo.GetDrives();
            foreach (DriveInfo d in allDrives)
            {
                if (d.DriveType == DriveType.CDRom)
                {
                    var path = Path.Combine(d.RootDirectory.ToString(), realtekEXE);
                    if (File.Exists(path))
                    {
                        CDRomInswtall(path);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Install drivers from flash drive.
        /// </summary>
        /// <param name="path">Location of .exe file of Realtek USB drivers</param>
        private void CDRomInswtall(string path)
        {
            try
            {
                //Install Realtek drivers from flash drive
                Process p = new Process();
                p.StartInfo.FileName = path;
                p.StartInfo.UseShellExecute = false;
                p.Start();
                p.WaitForExit();
            }
            catch (Exception ex)
            {
                //Failed 
            }
        }

        /// <summary>
        /// Install patch for Realtek USB network adapter. Patch will delete virtual CD drive and device will be used only as network adapter. 
        /// </summary>
        public void InstallRealtekPatch()
        {
            try
            {
                //run Realtek patch once the network adapter has been installed this will eliminate any futre flush drives on reboot from this device
                string path = System.Reflection.Assembly.GetExecutingAssembly().Location;
                path = Path.GetDirectoryName(path);
                if (Environment.Is64BitOperatingSystem)
                {
                    path = Path.Combine(path, "RTL_Patch", "Patch64.exe");
                }
                else
                {
                    path = Path.Combine(path, "RTL_Patch", "Patch32.exe");
                }
                Process p = new Process();
                p.StartInfo.FileName = path;
                p.StartInfo.UseShellExecute = false;
                p.Start();
                p.WaitForExit();
            }
            catch (Exception ex)
            {
                //Failed
            }
        }
    }
}
