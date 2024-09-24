namespace Clients;

using Microsoft.Extensions.DependencyInjection;

using SimpleRpc.Serialization.Hyperion;
using SimpleRpc.Transports;
using SimpleRpc.Transports.Http.Client;

using NLog;

using Services;


/// <summary>
/// Client example.
/// </summary>
class Client
{

	/// <summary>
	/// Logger for this class.
	/// </summary>
	Logger mLog = LogManager.GetCurrentClassLogger();

	/// <summary>
	/// Configures logging subsystem.
	/// </summary>
	private void ConfigureLogging()
	{
		var config = new NLog.Config.LoggingConfiguration();

		var console =
			new NLog.Targets.ConsoleTarget("console")
			{
				Layout = @"${date:format=HH\:mm\:ss}|${level}| ${message} ${exception}"
			};
		config.AddTarget(console);
		config.AddRuleForAllLevels(console);

		LogManager.Configuration = config;
	}

	/// <summary>
	/// Program body. Sends loaves of bread to be loaded into the oven. Logs actions.
	/// </summary>
	private void Run()
	{
		//configure logging
		ConfigureLogging();

		//initialize random number generator
		var rnd = new Random();

		//run everything in a loop to recover from connection errors
		while (true)
		{
			try
			{
				//connect to the server, get service client proxy
				var sc = new ServiceCollection();
				sc
					.AddSimpleRpcClient(
						"ovenService",
						new HttpClientTransportOptions
						{
							Url = "http://127.0.0.1:5000/simplerpc",
							Serializer = "HyperionMessageSerializer"
						}
					)
					.AddSimpleRpcHyperionSerializer();

				sc.AddSimpleRpcProxy<IOvenService>("ovenService");

				var sp = sc.BuildServiceProvider();

				var oven = sp.GetService<IOvenService>();

				// Did not use class because the IDs don't really matter?
				int load;

				//log component data
				mLog.Info($"I am loading component.");
				Console.Title = $"I am loading component.";

				//do the bread stuff
				//lets get this bread
				//making the dough B)
				while (true)
				{
					// generate random bread amount
					load = rnd.Next(1, 10);
					// wait a bit
					Thread.Sleep(500 + rnd.Next(1500));
					mLog.Info("I try to load bread into the oven");
					// get oven state
					var ovenState = oven.GetOvenState();
					// wait a bit more
					Thread.Sleep(rnd.Next(500));
					// based on state, either load bread or wait a bit if the oven is heating
					if (ovenState == State.Loading)
					{
						mLog.Info($"Oven is LOADING state, I will load {load} bread.");
						oven.Load(load);
					}
					else
					{
						mLog.Info("Oven is HEATING state, I will wait and try again.");
						Thread.Sleep(4000);
					}

				}

			}
			catch (Exception e)
			{
				//log whatever exception to console
				mLog.Warn(e, "Unhandled exception caught. Will restart main loop.");

				//prevent console spamming
				Thread.Sleep(2000);
			}
		}
	}

	/// <summary>
	/// Program entry point.
	/// </summary>
	/// <param name="args">Command line arguments.</param>
	static void Main(string[] args)
	{
		var self = new Client();
		self.Run();
	}
}
