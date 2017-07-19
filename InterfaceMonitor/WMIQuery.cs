using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management;

namespace InterfaceMonitor
{
    /// <summary>
    /// WMI Query (Windows)
    /// </summary>
    class WMIQuery
    {
        /// <summary>
        /// Query WMI for PNP (Plug and Play) Device ID using hardware ID and return list of all PNP Device IDs using hardware ID 
        /// </summary>
        /// <param name="HardwareID">String hardware ID</param>
        /// <param name="useHardware">Not necessary but set to true when hardaer ID is used</param>
        /// <returns>List of all PNP Device IDs</returns>
        public List<string> GetPNDeviceID(string HardwareID, bool useHardware)
        {
            List<string> name = new List<string>();
            try
            {
                //Retriving PNPDevicID using HardwareID
                ManagementObjectSearcher searchQuery = new ManagementObjectSearcher("root\\CIMV2", "SELECT HardwareID, PNPDeviceID FROM Win32_PnPEntity");
                ManagementObjectCollection queryCollection = searchQuery.Get();
                foreach (ManagementObject item in queryCollection)
                {
                    if (item["HardwareID"] != null)
                    {
                        string[] sValues = (string[])item["HardwareID"];
                        foreach (string sValue in sValues)
                        {
                            if (sValue.Contains(HardwareID))
                            {
                                name.Add((item["PNPDeviceID"] ?? string.Empty).ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //Failed to find PNP Device ID
            }
            return name;
        }

        /// <summary>
        /// Query WMI for PNP (Plug and Play) Device ID using description and return list of all PNP Device IDs using hardware description 
        /// </summary>
        /// <param name="description">String part of the hardware desctription</param>
        /// <returns>List of all PNP Device IDs</returns>
        public List<string> GetPNDeviceID(string description)
        {
            List<string> name = new List<string>();
            try
            {
                //nRetrieving PNPDeviceID using Description
                ManagementObjectSearcher searchQuery = new ManagementObjectSearcher("root\\CIMV2", "SELECT Description, PNPDeviceID FROM Win32_PnPEntity WHERE Description='" + description + "'");
                ManagementObjectCollection queryCollection = searchQuery.Get();
                foreach (ManagementObject item in queryCollection)
                {
                    if (item["Description"] != null)
                    {
                        name.Add((item["PNPDeviceID"] ?? string.Empty).ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                //Failed to find PNP Device ID
            }
            return name;
        }

        /// <summary>
        /// Query WMI for netwrok interface index and return string. This command will work only for Windows Vista and up
        /// </summary>
        /// <param name="PNPDeviceID">WMI PNP (Plug and Play) Device ID</param>
        /// <param name="osMajor">Windows OS major version number</param>
        /// <returns>Index for netwrok device as string</returns>
        public string GetInterfaceIndex(string PNPDeviceID, int osMajor)
        {
            string index = null;
            try
            {
                if (osMajor > 5)
                {
                    ManagementObjectSearcher searchQuery = new ManagementObjectSearcher("root\\CIMV2", "SELECT PNPDeviceID, InterfaceIndex FROM Win32_NetworkAdapter WHERE PNPDeviceID <> NULL");
                    ManagementObjectCollection queryCollection = searchQuery.Get();
                    foreach (ManagementObject item in queryCollection)
                    {
                        if (item["PNPDeviceID"].ToString() == PNPDeviceID)
                        {
                            index = item["InterfaceIndex"].ToString();
                        }
                    }
                }
                else
                {
                    //GetInterfaceIndex is not valid for this operating system
                }
            }
            catch (Exception ex)
            {
                //Failed
            }
            return index;
        }

        /// <summary>
        /// Query WMI for network connection name
        /// </summary>
        /// <param name="PNPDeviceID">PNP (Plug and Play) Device ID</param>
        /// <param name="osMajor">Windows OS major version number</param>
        /// <param name="osMinor">Windows OS minor version number</param>
        /// <returns>Name of the network connection as string</returns>
        public string GetNetConnectionName(string PNPDeviceID, int osMajor, int osMinor)
        {
            string id = null;
            try
            {
                //Retrieving network connection name
                ManagementObjectSearcher searchQuery = new ManagementObjectSearcher("root\\CIMV2", "SELECT PNPDeviceID, NetConnectionID FROM Win32_NetworkAdapter WHERE PNPDeviceID <> NULL");
                ManagementObjectCollection queryCollection = searchQuery.Get();
                foreach (ManagementObject item in queryCollection)
                {
                    if (item["PNPDeviceID"].ToString() == PNPDeviceID)
                    {
                        id = (item["NetConnectionID"] ?? string.Empty).ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                //Failed
            }
            return id;
        }

        /// <summary>
        /// Query WMI to check if network device is available
        /// </summary>
        /// <param name="pnpDeviceID">PNP (Plug and Play) Device ID</param>
        /// <returns>True or False</returns>
        public bool GetStatusLevel(string pnpDeviceID)
        {
            bool status = false;
            try
            {
                //Retrieving interface status
                ManagementObjectSearcher searchQuery = new ManagementObjectSearcher("root\\CIMV2", "SELECT PNPDeviceID, Status FROM Win32_PnPEntity");
                ManagementObjectCollection queryCollection = searchQuery.Get();
                foreach (ManagementObject item in queryCollection)
                {
                    if (item["PNPDeviceID"] != null && pnpDeviceID == item["PNPDeviceID"].ToString())
                    {
                        // Console.WriteLine("PNPDevice: " + item["PNPDeviceID"].ToString());
                        if (item["Status"] != null)
                        {
                            var statusLevel = item["Status"];
                            string OK = "OK";
                            bool result = (statusLevel.ToString()).Equals(OK, StringComparison.CurrentCultureIgnoreCase);
                            if (result)
                            {
                                status = true;
                            }
                            //Console.WriteLine("ClassGuid: " + item["ClassGuid"]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //Failed
            }
            return status;
        }
        
        /// <summary>
        /// Check if network interface is connected to the system
        /// </summary>
        /// <param name="pnpDeviceID">PNP (Plug and Play) Device ID</param>
        /// <returns>True or False</returns>
        public bool IsConnected(string pnpDeviceID)
        {
            bool connected = false;
            try
            {
                //Retrieving if USB device is connected
                ManagementObjectSearcher searchQuery = new ManagementObjectSearcher("root\\CIMV2", "SELECT PNPDeviceID FROM Win32_PnPEntity");
                ManagementObjectCollection queryCollection = searchQuery.Get();

                foreach (ManagementObject item in queryCollection)
                {
                    if (item["PNPDeviceID"] != null && pnpDeviceID == item["PNPDeviceID"].ToString())
                    {
                        connected = true;
                    }
                }
            }
            catch (Exception ex)
            {
                //Failed
            }
            return connected;
        }
    }
}
