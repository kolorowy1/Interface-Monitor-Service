using System;
using System.ServiceProcess;
using System.Threading;

namespace InterfaceMonitor
{
    public class InterfaceMonitorService : ServiceBase
    {
        private Thread td;
        private System.ComponentModel.Container components = null;

        public InterfaceMonitorService()
        {
            InitializeComponent();
        }

        static void Main()
        {
#if DEBUG
            //Start interface monitor service
            new InterfaceMonitor();
#else
            System.ServiceProcess.ServiceBase[] ServicesToRun;
            ServicesToRun = new System.ServiceProcess.ServiceBase[] { new InterfaceMonitorService() };
            System.ServiceProcess.ServiceBase.Run(ServicesToRun);
#endif
        }

        private void InitializeComponent()
        {
            this.ServiceName = "IMSInterfaceMonitor";
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                td = new Thread(this.InterfaceThread);
                td.Start();
            }
            catch (Exception e)
            {
                String ss = String.Format("Interface Monitor : OnStart : Thread Start Exception : {0}", e);
            }
            base.OnStart(args);
        }

        protected override void OnStop()
        {
            td.Abort();
        }

        private void InterfaceThread()
        {
            new InterfaceMonitor();
        }
    }
}
