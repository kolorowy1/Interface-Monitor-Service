# Interface-Monitor0Service
Windows service monitoring PNP (plug and play) changes and set static IP address on specific netowrk interface

# Summary

Windows service runs in background monitoring PNP (plug and play) devices. This service is utilizing WMI (Windows Management Instrumentation) to find network adapter and set static IP address when USB device is connected. 

Service monitors USB connect/disconnect events, when event is triggered, interface monitor will try to find device connected to the system using WMI and device hardware ID or description. 

Configuration for service can be set/changed in registry. Add hardware IDs or device description, multiple devices is allowed. Set needed static IP address and subnet mask. When device is found, interface is verified if available, if so, set static IP address. 

If next devices is connected from the list and previous devices is still connected, IP address will not be changed on new devices as the previous device is still using static IP address. Disconnect previous devices will trigger IP address change on next found device. 

# Configuration

When services starts default settings are created in registry HKEY_LOCAL_MACHINE\SOFTWARE\IMS\InterfaceMonitor
Registry below allow you to make changes to static IP address, subnetmask, device hardware ID, device description.

StaticIP - 172.16.2.1 (set desired IP address)

SubnetMask - 255.255.255.0 (set desired subnet mask)

UseHardwareID - True (set True or False if service needs to look for device base of hardware ID set true or description set to false)

HardwareID - USB\VID_0B95&PID_772 (partial name of network adapter hardware ID, this is multi string registry which allows you to check                                     for more than one network adapter)

DeviceDescription - Realtek USB FE Family Controller (partial name of network adapter description, this is multi string registry which                                      allows you to check for more than one network adapter)  

# TODO

Add configuration for default gateway and DNS IP 
