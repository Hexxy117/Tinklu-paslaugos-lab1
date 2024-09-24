namespace Servers;

using System.Net;

using NLog;

using SimpleRpc.Transports;
using SimpleRpc.Transports.Http.Server;
using SimpleRpc.Serialization.Hyperion;

using Services;

/// <summary>
/// Class for server configuration.
/// </summary>
public class Server
{
	/// <summary>
	/// Logger for this class.
	/// </summary>
	Logger log = LogManager.GetCurrentClassLogger();

	/// <summary>
	/// Configure loggin subsystem.
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
	/// Program entry point.
	/// </summary>
	/// <param name="args">Command line arguments.</param>
	public static void Main(string[] args)
	{
		var self = new Server();
		self.Run(args);
	}

	/// <summary>
	/// Program body.
	/// </summary>
	/// <param name="args">Command line arguments.</param>
	private void Run(string[] args)
	{
		//configure logging
		ConfigureLogging();

		//indicate server is about to start
		log.Info("Server is about to start");

		//start the server
		StartServer(args);
	}

	/// <summary>
	/// Starts integrated server.
	/// </summary>
	/// <param name="args">Command line arguments.</param>
	private void StartServer(string[] args)
	{
		///create web app builder
		var builder = WebApplication.CreateBuilder(args);

		//configure integrated server
		builder.WebHost.ConfigureKestrel(opts =>
		{
			opts.Listen(IPAddress.Loopback, 5000);
		});

		//add SimpleRPC services
		builder.Services
			.AddSimpleRpcServer(new HttpServerTransportOptions { Path = "/simplerpc" })
			.AddSimpleRpcHyperionSerializer();

		//add our custom services
		builder.Services
			// .AddScoped<ITrafficLightService, TrafficLightService>();  //instance-per-request, AddTransient would result in the same
			.AddSingleton<IOvenService>(new OvenService());   //singleton

		//build the server
		var app = builder.Build();

		//add SimpleRPC middleware
		app.UseSimpleRpcServer();

		//run the server
		app.Run();
		// app.RunAsync(); //use this if you need to implement background processing in the main thread
	}
}