using System;
using System.Threading;
using System.Net.NetworkInformation;
using System.Diagnostics;

namespace InterfaceMonitor
{
    /// <summary>
    /// Update IP settings for monitored network interface
    /// </summary>
    class NetMonitor
    {
        public int GetOSMajor() { return Environment.OSVersion.Version.Major; }
        public int GetOSMinor() { return Environment.OSVersion.Version.Minor; }
        public bool ApplyPatch { get; set; }

        private string _pnpDeviceID;
        private Thread threadObj;
        private bool _requestStop;
        private string _interfaceIndex;
        private string _lanName;
        //private string _settingID;
        //private bool _connected;

        WMIQuery _WMIQuery = new WMIQuery();
        RegConfig _RegConfig = new RegConfig();

        private bool IsInstalled { get; set; }

        public string PNPID
        {
            get { return this._pnpDeviceID; }
            set
            {
                if (this.PNPID != value)
                {
                    Object old = this._pnpDeviceID;
                    this._pnpDeviceID = value;
                    //PNPID: Device ID has changed
                    OnPropertyChange(this, new PropertyChangeEventArgs("PNPID", old, value));
                }
            }
        }

        // Delegate
        public delegate void PropertyChangeHandler(object sender, PropertyChangeEventArgs data);
        // The event
        public event PropertyChangeHandler PropertyChange;
        // The method which fires the Event
        protected void OnPropertyChange(object sender, PropertyChangeEventArgs data)
        {
            if (PropertyChange != null) // Check if there are any Subscribers
            {
                PropertyChange(this, data); // Call the Event
            }
        }

        /*
            Property has changed for PNPID start new thread to set netwrok adapter settings
        */
        public void PropertyHasChanged(object sender, PropertyChangeEventArgs data)
        {
            if (data.PropertyName == "PNPID")
            {
                try
                {
                    if (threadObj == null || threadObj.IsAlive == false)
                    {
                        _requestStop = false;
                        threadObj = new Thread(StartCheckingIfInstalled);
                        threadObj.IsBackground = true;
                        threadObj.Start();
                    }
                }
                catch (Exception ex)
                {
                    //Failed
                }
            }
        }

        /// <summary>
        /// Wait for windows to install drivers for new device and when device become available. 
        /// </summary>
        private void StartCheckingIfInstalled()
        {
            while (_requestStop == false)
            {
                IsInstalled = _WMIQuery.GetStatusLevel(PNPID);
                if (IsInstalled || PNPID == null)
                {
                    RequestStop();
                }
                else
                {
                    //Waiting for device to be installed
                }
                Thread.Sleep(1000);
            }

            if (IsInstalled == true)
            {
                //Device has been installed
                UpdateSetting();
                if(ApplyPatch)
                {
                    //apply patch to Realtek device, we do not care if it exists or not at this point
                    InstallFromCD rtl = new InstallFromCD();
                    rtl.InstallRealtekPatch();
                    ApplyPatch = false;
                }
            }
        }

        /// <summary>
        /// Terminate installation thread
        /// </summary>
        public void RequestStop()
        {
            _requestStop = true;
            //RequestStop: Stopping installation checking
        }

        /// <summary>
        /// Update registry settings with current device informaiton
        /// </summary>
        private void UpdateSetting()
        {
            _lanName = _WMIQuery.GetNetConnectionName(PNPID, GetOSMajor(), GetOSMinor());
            if (_lanName != null)
            {
                _RegConfig.UpdateRegLanName(_lanName);
                _RegConfig.UpdateRegDeviceID(PNPID);
                UpdateIP();
            }
        }

        /// <summary>
        /// Set IP addres for new netwrok adapter
        /// </summary>
        private void UpdateIP()
        {
            var ip = _RegConfig.GetRegIP();
            var sub = _RegConfig.GetRegSubnet();
            string arg = "";
            if (GetOSMajor() > 5)
            {
                _interfaceIndex = _WMIQuery.GetInterfaceIndex(PNPID, GetOSMajor());
                arg = ("interface ip set address " + _interfaceIndex + " static " + ip + " " + sub + " none");
                _RegConfig.UpdateRegInterfaceIndex(_interfaceIndex);
            }
            else
            {
                arg = ("interface ip set address \"" + _lanName + "\" static " + ip + " " + sub + " none");
                _RegConfig.UpdateRegLanName(_lanName);
            }
            //Set IP Address
            Process p = new Process();
            p.StartInfo.FileName = "netsh.exe";
            p.StartInfo.Arguments = arg;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.Start();
            p.WaitForExit();

            string output = p.StandardOutput.ReadToEnd();
            
        }
    }

    public class PropertyChangeEventArgs : EventArgs
    {
        public string PropertyName { get; internal set; }
        public object OldValue { get; internal set; }
        public object NewValue { get; internal set; }
        public PropertyChangeEventArgs(string propertyName, object oldValue, object newValue)
        {
            this.PropertyName = propertyName;
            this.OldValue = oldValue;
            this.NewValue = newValue;
        }
    }
}
