using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace EnergySmartBridge
{
    partial class Service : ServiceBase
    {
        static CoreServer server;

        public Service()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            server = new CoreServer();
        }

        protected override void OnStop()
        {
            server.Shutdown();
        }

        protected override void OnShutdown()
        {
            server.Shutdown();
        }
    }
}
