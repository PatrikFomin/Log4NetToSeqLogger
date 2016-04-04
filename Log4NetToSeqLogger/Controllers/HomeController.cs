using log4net;
using SeqToLog4NetLogger.Models;
using System;
using System.Collections.Generic;
using System.Web.Hosting;
using System.Web.Mvc;


namespace SeqToLog4NetLogger.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            // Configure log4net (This is done using your normal initialization methods. I just put it here for demo usage)
            log4net.Config.XmlConfigurator.ConfigureAndWatch(new System.IO.FileInfo(System.IO.Path.Combine(HostingEnvironment.ApplicationPhysicalPath, "log4net.config")));
            // Configure SeqToLog4Net log manager
            SeqToLog4NetLogManager.Configure(seqServerUrl: "http://localhost:5341/");

            // Create a custom exception for testing
            var divideByZeroException = new DivideByZeroException("You may not do this!");

            // Anonymous type
            var SensorInput = new { Latitude = 25, Longitude = 134 };

            // Dictionary
            var UserInput = new Dictionary<string, string>();
            UserInput.Add("UserName", "Demo");
            UserInput.Add("Language", "English");
            
            // Do some logging using the SeqToLog4Net log manager that will both log to the Seq server and to log4net traditional files
            IStructuredLogger logger = SeqToLog4NetLogManager.GetLogger("TestLogger"); // IStructuredLogger inherits ILog meaning that you can continue logging as you would normally using log4net
            // Log an error using the dictionary with an exception
            logger.Error("This is a fatal user error", divideByZeroException, UserInput);
            // Log a fatal error using the anonymous type and no exception
            logger.Fatal("The sensor is reporting some incorrect values", SensorInput);
            // Log a warning with no additional data
            logger.Warn("Just a general warning with no additional data", null);

            // Do some logging using log4net. It will just end up in the log file and not be sent to the Seq server.
            ILog log4netLogger = log4net.LogManager.GetLogger("TestLogger");
            log4netLogger.Debug("A normal log4net call");
            // Log like you normally would using log4net
            logger.Info("The program has reached point X in this program");

            return View();
        }
    }
}