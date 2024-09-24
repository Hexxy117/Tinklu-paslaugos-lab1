namespace Clients;

using Microsoft.Extensions.DependencyInjection;

using SimpleRpc.Serialization.Hyperion;
using SimpleRpc.Transports;
using SimpleRpc.Transports.Http.Client;

using NLog;

using Services;


/// <summary>
/// Heating component client.
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
	/// Program body. Generates heat values and sends them to the server. Logs actions taken.
	/// </summary>
	private void Run()
	{
		// configure logging
		ConfigureLogging();

		// initialize random number generator
		var rnd = new Random();

		// Run everythin in a loop to recover from connection errors
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

				int heat;

				//log component data
				mLog.Info($"I am heating component.");
				Console.Title = $"I am heating component.";

				//do the heating stuff
				while (true)
				{
					// generate temperature value
					heat = rnd.Next(-3, 5);
					// wait a bit (why not?)
					Thread.Sleep(500 + rnd.Next(1500));
					mLog.Info("Heating component attempts to head the oven.");
					// retrieve oven state
					var ovenState = oven.GetOvenState();
					// wait a bit more
					Thread.Sleep(rnd.Next(500));
					// Check the oven state, if heating - send heat, if loading - wait and try again.
					if (ovenState == State.Heating)
					{
						mLog.Info($"Oven is in HEATING state, adjusting temperature by {heat} C.");
						oven.Heat(heat);
					}
					else
					{
						mLog.Info("Oven is in LOADING state, I will wait and try again.");
						// wait more or less the time in which the state might change
						Thread.Sleep(3000);
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
