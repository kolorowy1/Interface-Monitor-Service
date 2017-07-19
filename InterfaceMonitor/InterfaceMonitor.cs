using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;
using System.Management;

namespace InterfaceMonitor
{
    class InterfaceMonitor
    {
        //Default settings
        private const string AX88x72A_Description = "ASIX AX88772 USB2.0 to Fast Ethernet Adapter";
        private static readonly string[] DEFAULT_HARDWAREID = new string[] { @"USB\VID_0B95&PID_772", @"USB\VID_0B95&PID_7E2",
            @"USB\VID_0DB0&PID_A877", @"USB\VID_0421&PID_772", @"USB\VID_125E&PID_180", @"USB\VID_0BDA&PID_8152", @"USB\VID_0BDA&PID_8050" ,@"USB\VID_0BDA&PID_8152",
            @"USB\VID_0BDA&PID_8153" };
        private const string RLT_Description = "Realtek USB FE Family Controller";

        //Realtek PNPID for CD ROM
        private const string RLTCDROM_HID = "Realtek_USB_CD-ROM______2";
        private readonly string RealTekCD_PNP = "USBSTOR\\CDROM&VEN_REALTEK&PROD_USB_CD-ROM";

        private List<string> DevicePnpIDList = new List<string>();

        private bool UseHardware { get; set; }
        private List<string> Descriptions { get; set; }
        private List<string> HardwareIDs { get; set; }
        private int OSMajor { get; set; }
        private int OSMinor { get; set; }
        private string CurrentDeviceID { get; set; }

        private bool pFlag = false;

        RegConfig _RegConfig = new RegConfig();
        WMIQuery _WMIQuery = new WMIQuery();
        NetMonitor _NetMonitor = new NetMonitor();

        /// <summary>
        /// Start interface monitor service
        /// </summary>
        public InterfaceMonitor()
        {
            //check what version of OS this is
            OSMajor = _NetMonitor.GetOSMajor();
            OSMinor = _NetMonitor.GetOSMinor();

            _NetMonitor.PropertyChange += new NetMonitor.PropertyChangeHandler(_NetMonitor.PropertyHasChanged);

            try
            {
                if (Registry.LocalMachine.OpenSubKey(@"SOFTWARE\IMS\InterfaceMonitor", true) == null)
                {
                    string[] des = new string[] { AX88x72A_Description, RLT_Description };
                    string[] hID = DEFAULT_HARDWAREID;
                    _RegConfig.SetMonitorDefaultRegistry("172.16.2.1", "255.255.255.0", des, hID, OSMajor); //set default settings
                }
                else //legacy registry
                {
                    _RegConfig.UpdateLegacyRegistry();
                }

                //InterfaceMonitor Starting

                GetCurrentSetting();
                StartDeviceWatcher();
            }
            catch (Exception ex)
            {
                //Main startup failed
                this.Shutdown();
            }
        }

        /// <summary>
        /// Get current configuration settings from registry
        /// </summary>
        private void GetCurrentSetting()
        {
            UseHardware = _RegConfig.GetRegUseHardwareID();
            //check exisitng devices
            List<string> pnpIDList = new List<string>();
            if (UseHardware)
            {
                HardwareIDs = _RegConfig.GetRegHardwareIDs();
                //add Realtek USB CD-ROM as we must monitor for this as well
                HardwareIDs.Add(RLTCDROM_HID);
                foreach (var hID in HardwareIDs)
                {
                    //Checking for HardwareID
                    List<string> tempList = new List<string>(_WMIQuery.GetPNDeviceID(hID, true));
                    foreach (var item in tempList)
                    {
                        pnpIDList.Add(item);
                    }
                }
            }
            else
            {
                Descriptions = _RegConfig.GetRegDeviceDescriptions();
                foreach (var info in Descriptions)
                {
                    //checking for Description
                    List<string> tempList = new List<string>(_WMIQuery.GetPNDeviceID(info));
                    foreach (var item in tempList)
                    {
                        pnpIDList.Add(item);
                    }
                }
            }

            //check if list is not empty
            bool isEmpty = ListHelper.IsNullOrEmpty(pnpIDList);
            if (!isEmpty)
            {
                //get first item in the list only
                List<string> distinctPNPID = pnpIDList.Distinct().ToList();
                DevicePnpIDList = distinctPNPID;
                //GetCurrentSetting: Found current device ID
                CurrentDeviceID = DevicePnpIDList.FirstOrDefault();

                if (CurrentDeviceID.Contains(RealTekCD_PNP))
                {
                    //Found Realtek Flush Drive
                    //Install current driver from RealTek Flush Drive as the network adapter has change to be CD-ROM
                    InstallFromCD drive = new InstallFromCD();
                    drive.GetRealtekCDROM();
                    _NetMonitor.ApplyPatch = true;
                }

                _NetMonitor.PNPID = CurrentDeviceID;
            }
            else
            { 
                //nNo device(s) found 
            }
        }

        /// <summary>
        /// Start management event watcher for PNP (Plug and Paly) devices
        /// </summary>
        private void StartDeviceWatcher()
        {
            ManagementScope mScope = new ManagementScope("root\\CIMV2");
            mScope.Options.EnablePrivileges = true;
            try
            {
                WqlEventQuery eventQuery = new WqlEventQuery();
                eventQuery.EventClassName = "__InstanceOperationEvent";
                eventQuery.WithinInterval = new TimeSpan(0, 0, 1);
                eventQuery.Condition = @"TargetInstance ISA 'Win32_USBControllerdevice'";

                ManagementEventWatcher eventWatcher = new ManagementEventWatcher(mScope, eventQuery);
                eventWatcher.EventArrived += new EventArrivedEventHandler(eventWatcher_EventArrived);
                eventWatcher.Start();
                //InterfaceMonitor Started
                    
            }
            catch (Exception ex)
            {
                //InterfaceMonitor failed to start
                this.Shutdown();
            }
        }

        /// <summary>
        /// This method is called to shutdown all the DeviceFolderSearch class instances.
        /// </summary>
        public void Shutdown()
        {
            pFlag = true;
            _NetMonitor.RequestStop();
        }

        //Event watcher messages arrived
        void eventWatcher_EventArrived(object sender, EventArrivedEventArgs e)
        {
            if (UseHardware)
            {
                ManagementObjectSearcher search = new ManagementObjectSearcher("root\\CIMV2", "SELECT HardwareID, PNPDeviceID FROM Win32_PnPEntity");
                EventArrived(search, UseHardware);
            }
            else
            {
                ManagementObjectSearcher search = new ManagementObjectSearcher("root\\CIMV2", "SELECT Description, PNPDeviceID FROM Win32_PnPEntity");
                EventArrived(search);
            }

            bool USBConnected = true;
            ManagementBaseObject baseObject = (ManagementBaseObject)e.NewEvent;
            if (baseObject.ClassPath.ClassName.Equals("__InstanceCreationEvent"))
            {
                //Console.WriteLine("Connected");
                //Device Connected
                USBConnected = true;
                List<string> distinctItem = DevicePnpIDList.Distinct().ToList();
                DevicePnpIDList = distinctItem;
                if (DevicePnpIDList.Count > 0)
                {
                    for (int i = 0; i < 1; i++)
                    {
                        CurrentDeviceID = DevicePnpIDList[i];
                    }
                }
            }
            else if (baseObject.ClassPath.ClassName.Equals("__InstanceDeletionEvent"))
            {
                //Device Removed
                for (int i = DevicePnpIDList.Count - 1; i >= 0; i--)
                {
                    var connected = _WMIQuery.IsConnected(DevicePnpIDList[i]);
                    if (!connected)
                    {
                        DevicePnpIDList.RemoveAt(i);
                    }
                }
                if (DevicePnpIDList.Count > 0)
                {
                    for (int i = 0; i < 1; i++)
                    {
                        CurrentDeviceID = DevicePnpIDList[i];
                    }
                }
                else
                {
                    CurrentDeviceID = null;
                    USBConnected = false;
                }
            }
            
            if (CurrentDeviceID.Contains(RealTekCD_PNP))
            {
                //Found Realtek Flush Drive
                //Install current driver from RealTek Flush Drive as the network adapter has change to be CD-ROM
                InstallFromCD drive = new InstallFromCD();
                drive.GetRealtekCDROM();
                _NetMonitor.ApplyPatch = true;
            }
            _NetMonitor.PNPID = CurrentDeviceID;
        }

        private void EventArrived(ManagementObjectSearcher searcher, bool useHardware)
        {
            foreach (ManagementObject item in searcher.Get())
            {
                if (item["HardwareID"] != null)
                {
                    string[] sValues = (string[])item["HardwareID"];
                    foreach (string sValue in sValues)
                    {
                        foreach (var hID in HardwareIDs)
                        {
                            if (sValue == hID)
                                DevicePnpIDList.Add((item["PNPDeviceID"] ?? string.Empty).ToString());
                        }
                    }
                }
            }
        }

        private void EventArrived(ManagementObjectSearcher searcher)
        {
            foreach (ManagementObject item in searcher.Get())
            {
                if (item["Description"] != null)
                {
                    string[] sValues = (string[])item["Description"];
                    foreach (string sValue in sValues)
                    {
                        foreach (var desc in Descriptions)
                        {
                            if (sValue == desc)
                                DevicePnpIDList.Add((item["PNPDeviceID"] ?? string.Empty).ToString());
                        }
                    }
                }
            }
        }
    }

    static class ListHelper
    {
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable)
        {
            if (enumerable == null)
            {
                return true;
            }
            return !enumerable.Any();
        }
    }
}
