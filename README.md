# Log4NetToSeqLogger
Adds the possibility to do structured logging from log4net to Seq events logging server.

So if you want to do structured logging with an event logging server like Seq but do not want to change your dependancy on log4net (unstructured logging), then this repo is for you.

<h2>Instructions</h2>

Start by installing Seq (STRUCTURED LOGS FOR .NET APPS)
https://getseq.net/

Copy the one class SeqToLog4NetLogManager.cs into your project (or run the demo project)

Configure the url to the Seq server. (This is done once during startup)
        SeqToLog4NetLogManager.Configure(seqServerUrl: "http://localhost:5341/");

Instead of using log4net's GetLogger(assembly/name), you call SeqToLog4NetLogManager.GetLogger(assembly/name).

Change this:
        ILog log4netLogger = log4net.LogManager.GetLogger("TestLogger");
to
        IStructuredLogger logger = SeqToLog4NetLogManager.GetLogger("TestLogger");

The difference is the interface backing the logger. So instead of using ILog from log4net, you use IStructuredLogger that implements ILog. All your normal calls to Warn/Error/Debug/Info/Fatal will continue to work.

Now you can start logging structured data using the additional optional parameter object structure.

        IStructuredLogger logger = SeqToLog4NetLogManager.GetLogger("TestLogger");

        // Anonymous type of data that you wish to log
        var SensorInput = new { Latitude = 25, Longitude = 134 };

        // Log a fatal error using the anonymous type and no exception
        logger.Fatal("The sensor is reporting some incorrect values", SensorInput);


You can of course continue to use log4net like normal. This just extends log4net by adding 2 new methods to each logging type (Info/Warn/Debug/Error/Fatal)


Complete test code, MVC action, if you have copied the SeqToLog4NetLogManager.cs file to your project:

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
        
<h2>JSON parsing, i do not use Newtonsoft.json</h2>
The parsing of your data is done by Newtonsoft.json. If you wish to use a different parser, then you will have to update one line of code inside SeqToLog4NetLogManager.cs.

Navigate or search for the following method LogDataToServer(LoggingEvent log, object structure) and update the following code,

        // Change this line if you want to use a different JSON serializer
        var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(structure);
to a Json serializer of your choice.

<h2>Codebase</h2>
Parts of the codebase is taken from this project, https://github.com/continuousit/seq-client and has been slightly modified to allow sending of log4net events with custom data.
