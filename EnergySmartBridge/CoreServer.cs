using EnergySmartBridge.Modules;
using log4net;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace EnergySmartBridge
{
    public class CoreServer
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly List<IModule> modules = new List<IModule>();
        private readonly List<Task> tasks = new List<Task>();

        public CoreServer()
        {
            Thread handler = new Thread(Server);
            handler.Start();
        }

        private void Server()
        {
            Global.running = true;

            log.Debug("Starting up server " +
                Assembly.GetExecutingAssembly().GetName().Version.ToString());

            // Initialize modules
            WebServerModule webServer = new WebServerModule();
            modules.Add(webServer);

            modules.Add(new MQTTModule(webServer));
       
            // Startup modules
            foreach (IModule module in modules)
            {
                tasks.Add(Task.Factory.StartNew(() =>
                {
                    module.Startup();
                }));
            }

            // Wait for all threads to stop
            Task.WaitAll(tasks.ToArray());
        }

        public void Shutdown()
        {
            Global.running = false;

            // Shutdown modules
            foreach (IModule module in modules)
                module.Shutdown();

            // Wait for all threads to stop
            if (tasks != null)
                Task.WaitAll(tasks.ToArray());

            log.Debug("Shutdown completed");
        }
    }
}