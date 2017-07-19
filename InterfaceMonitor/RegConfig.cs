using System;
using System.Collections.Generic;
using Microsoft.Win32;

namespace InterfaceMonitor
{
    /// <summary>
    /// Interface monitor service registry settings and configurations
    /// </summary>
    class RegConfig
    {
        private const string iMonitorReg = @"SOFTWARE\IMS\InterfaceMonitor";

        //default moniotr registry
        /// <summary>
        /// Default monitor settings
        /// </summary>
        /// <param name="staticIP">string IP addres (format 172.16.2.1)</param>
        /// <param name="gateway">string Gateway (format 172.16.2.256)</param>
        /// <param name="description">String array Part of the device hardware description</param>
        /// <param name="hardwareID">String array Part of the device hardware ID</param>
        /// <param name="osMajor">int Major version number of Windows OS</param>
        public void SetMonitorDefaultRegistry(string staticIP, string gateway, string[] description, string[] hardwareID, int osMajor)
        {
            try
            {
                //set registry
                RegistryKey rk = Registry.LocalMachine.CreateSubKey(iMonitorReg);
                rk.SetValue("StaticIP", staticIP);
                rk.SetValue("SubnetMask", gateway);
                rk.SetValue("CurrentLanName", "");
                rk.SetValue("DeviceDescription", description);
                rk.SetValue("CurrentDeviceID", "");
                rk.SetValue("NOTE1", "If UseHardwareID=true, monitor will check for device Hardware ID, else monitor will check for Device Description");

                if (osMajor == 5) //windows xp, server 2003
                {
                    rk.SetValue("UseHardwareID", "False");
                    rk.SetValue("NOTE", "Set UseHardwareID MUST be false for Windows XP/Server 2003");
                }
                else if (osMajor > 5) //windows vista and up
                {
                    rk.SetValue("CurrentInterfaceIndex", "");
                    rk.SetValue("HardwareID", hardwareID);
                    rk.SetValue("UseHardwareID", "True");
                }
                rk.Close();
            }
            catch (Exception ex)
            {
                //Failed
            }
        }

        /// <summary>
        /// Previous verson of interface monitor setting needs to updated registry
        /// </summary>
        public void UpdateLegacyRegistry()
        {
            try
            {
                RegistryKey rk = Registry.LocalMachine.OpenSubKey(iMonitorReg, true);
                rk.DeleteValue("LanName");
                rk.DeleteValue("InterfaceIndex");
                rk.DeleteValue("DeviceID");

                if (rk.GetValueKind("HardwareID") != RegistryValueKind.MultiString)
                {
                    ChangeRegistryType(rk, "HardwareID");
                }
                if (rk.GetValueKind("DeviceDescription") != RegistryValueKind.MultiString)
                {
                    ChangeRegistryType(rk, "DeviceDescription");
                }
                rk.Close();
            }
            catch { } //silent fail
        }

        /// <summary>
        /// Change registry from single string value to multi string value for hardware ID and device description
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        private void ChangeRegistryType(RegistryKey key, string value)
        {
            try
            {
                //get registry value first
                string temp = (string)key.GetValue(value);
                key.DeleteValue(value);
                key.SetValue(value, new string[] { temp });
            }
            catch (Exception ex)
            {
                //Failed
            }
        }

        /// <summary>
        /// Get configuration from registry if hadrware id will be used or not
        /// </summary>
        /// <returns>True or False</returns>
        public bool GetRegUseHardwareID()
        {
            bool useHardware = true; //since this is default setting
            try
            {
                RegistryKey rk = Registry.LocalMachine.OpenSubKey(iMonitorReg, true);
                if (rk != null)
                {
                    useHardware = Convert.ToBoolean(rk.GetValue("UseHardwareID"));
                }
                rk.Close();
            }
            catch (Exception ex)
            {
                //Failed
            }
            return useHardware;
        }

        /// <summary>
        /// Get all device hardware IDs from registry
        /// </summary>
        /// <returns>Return list as string with all device hardware IDs</returns>
        public List<string> GetRegHardwareIDs()
        {
            List<string> hardwareIDs = null;
            try
            {
                //Reading Interface Monitor registry setting - HardwareID
                RegistryKey rk = Registry.LocalMachine.OpenSubKey(iMonitorReg, true);
                if (rk != null)
                {
                    string[] valueNames = (string[])rk.GetValue("HardwareID");
                    hardwareIDs = new List<string>();
                    foreach (string name in valueNames)
                    {
                        hardwareIDs.Add(name.ToString());
                    }
                }
                rk.Close();
            }
            catch (Exception ex) 
            {
                //Failed
            }
            
            return hardwareIDs;
        }

        //returns Device Description for registry
        /// <summary>
        /// Get all device descriptions from registry
        /// </summary>
        /// <returns>Return list as string with all device description</returns>
        public List<string> GetRegDeviceDescriptions()
        {
            List<string> descriptions = null;
            try
            {
                //Reading Interface Monitor registry setting - DeviceDescription
                RegistryKey rk = Registry.LocalMachine.OpenSubKey(iMonitorReg, true);
                if (rk != null)
                {
                    descriptions = new List<string>();
                    string[] valueNames = (string[])rk.GetValue("DeviceDescription");
                    foreach(string name in valueNames)
                    {
                        descriptions.Add(name.ToString());
                    }
                }
                rk.Close();
            }
            catch (Exception ex)
            {
                //Failed
            }
            return descriptions;
        }

        /// <summary>
        /// Set registry setting for current device ID currently monitoring
        /// </summary>
        /// <param name="deviceID">String device ID</param>
        public void UpdateRegDeviceID(string deviceID)
        {
            try
            {
                //Updating Interface Monitor registry setting - CurrentDeviceID
                RegistryKey rk = Registry.LocalMachine.OpenSubKey(iMonitorReg, true);
                if (rk != null)
                {
                    rk.SetValue("CurrentDeviceID", deviceID);
                }
                rk.Close();
            }
            catch (Exception ex)
            {
                //Failed
            }
        }

        /// <summary>
        /// Set registry setting for network interface index currently monitoring
        /// </summary>
        /// <param name="interfaceIndex">String interface index</param>
        public void UpdateRegInterfaceIndex(string interfaceIndex)
        {
            try
            {
                RegistryKey rk = Registry.LocalMachine.OpenSubKey(iMonitorReg, true);
                if (rk != null)
                {
                    rk.SetValue("CurrentInterfaceIndex", interfaceIndex);
                    //UpdateRegInterfaceIndex - CurrentInterfaceIndex
                }
                rk.Close();
            }
            catch (Exception ex)
            {
                //Failed
            }
        }

        //returns IP set in registry
        /// <summary>
        /// Get configuration static IP address from registry
        /// </summary>
        /// <returns>IP address string (format 172.16.2.1)</returns>
        public string GetRegIP()
        {
            string ip = null;
            try
            {
                //Reading Interface Monitor registry setting - StaticIP
                RegistryKey rk = Registry.LocalMachine.OpenSubKey(iMonitorReg, true);
                if (rk != null)
                {
                    ip = (string)rk.GetValue("StaticIP");
                }
                rk.Close();
            }
            catch (Exception ex)
            {
                //Failed
            }
            return ip;
        }
        
        /// <summary>
        /// Get configuration subnet mask from registry
        /// </summary>
        /// <returns>Subnet mastk string (format 255.255.0.0)</returns>
        public string GetRegSubnet()
        {
            string sub = null;
            try
            {
                //Reading Interface Monitor registry setting - SubnetMask
                RegistryKey rk = Registry.LocalMachine.OpenSubKey(iMonitorReg, true);
                if (rk != null)
                {
                    sub = (string)rk.GetValue("SubnetMask");
                }
                rk.Close();
            }
            catch (Exception ex)
            {
                //Failed
            }
            return sub;
        }

        /// <summary>
        /// Set registry setting for network interface LAN name currently monitoring
        /// </summary>
        /// <param name="lanName"></param>
        public void UpdateRegLanName(string lanName)
        {
            try
            {
                //Updating Interface Monitor registry setting - CurrentLanName
                RegistryKey rk = Registry.LocalMachine.OpenSubKey(iMonitorReg, true);
                if (rk != null)
                {
                    rk.SetValue("CurrentLanName", lanName);
                }
                rk.Close();
            }
            catch (Exception ex)
            {
                //Failed
            }
        }
    }
}
