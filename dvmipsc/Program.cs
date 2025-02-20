﻿// SPDX-License-Identifier: AGPL-3.0-only
/**
* Digital Voice Modem - IPSC
* AGPLv3 Open Source. Use is subject to license terms.
* DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
*
* @package DVM / IP Site Connect
* @license AGPLv3 License (https://opensource.org/licenses/AGPL-3.0)
*
*   Copyright (C) 2022-2024 Bryan Biedenkapp, N2PLL
*   Copyright (C) 2025 Caleb, K4PHP
*
*/

using FneLogLevel = fnecore.LogLevel;
using fnecore.Utility;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;
using Serilog.Events;

namespace dvmipsc
{
    /// <summary>
    /// 
    /// </summary>
    public enum ERRNO : int
    {
        /// <summary>
        /// No error
        /// </summary>
        ENOERR = 0,
        /// <summary>
        /// Bad commandline options
        /// </summary>
        EBADOPTIONS = 1,
        /// <summary>
        /// Missing configuration file
        /// </summary>
        ENOCONFIG = 2
    }

    internal class Program
    {
        private static ConfigurationObject config;

        /// <summary>
        /// Gets the instance of the <see cref="ConfigurationObject"/>.
        /// </summary>
        public static ConfigurationObject Configuration => config;

        /// <summary>
        /// Gets the <see cref="fnecore.LogLevel"/>.
        /// </summary>
        public static FneLogLevel FneLogLevel
        {
            get;
            private set;
        } = FneLogLevel.INFO;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddLogging(config =>
                    {
                        config.ClearProviders();
                        config.AddProvider(new SerilogLoggerProvider(Log.Logger));
                    });
                    services.AddHostedService<Service>();
                });

        /// <summary>
        /// Internal helper to prints the program usage.
        /// </summary>
        private static void Usage(OptionSet p)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string fileName = Path.GetFileName(assembly.Location);
            Console.WriteLine();

            Console.WriteLine(string.Format("usage: {0} [-h | --help][-c | --config <path to configuration file>][-l | --log-on-console]",
            Path.GetFileNameWithoutExtension(fileName)));
            Console.WriteLine();
            Console.WriteLine("Options:");
            p.WriteOptionDescriptions(Console.Out);
        }

        public static void Main(string[] args)
        {
            const string defaultConfigFile = "config.yml";
            bool showHelp = false, showLogOnConsole = true;
            string configFile = string.Empty;

            // command line parameters
            OptionSet options = new OptionSet()
            {
                { "h|help", "show this message and exit", v => showHelp = v != null },
                { "c=|config=", "sets the path to the configuration file", v => configFile = v },
                { "l|log-on-console", "shows log on console", v => showLogOnConsole = v != null },
            };

            // attempt to parse the commandline
            try
            {
                options.Parse(args);
            }
            catch (OptionException)
            {
                Console.WriteLine("error: invalid arguments");
                Usage(options);
                Environment.Exit((int)ERRNO.EBADOPTIONS);
            }

            if (showHelp)
            {
                Usage(options);
                Environment.Exit((int)ERRNO.ENOERR);
            }

            Assembly assembly = Assembly.GetExecutingAssembly();
            string executingPath = Path.GetDirectoryName(assembly.Location);

            // do we some how have a "null" config file?
            if (configFile == null)
            {
                if (File.Exists(Path.Combine(new string[] { executingPath, defaultConfigFile })))
                    configFile = Path.Combine(new string[] { executingPath, defaultConfigFile });
                else
                {
                    Console.WriteLine("error: cannot read the configuration file");
                    Environment.Exit((int)ERRNO.ENOCONFIG);
                }
            }


            // do we some how have a empty config file?
            if (configFile == string.Empty)
            {
                if (File.Exists(Path.Combine(new string[] { executingPath, defaultConfigFile })))
                    configFile = Path.Combine(new string[] { executingPath, defaultConfigFile });
                else
                {
                    Console.WriteLine("error: cannot read the configuration file");
                    Environment.Exit((int)ERRNO.ENOCONFIG);
                }
            }

            try
            {
                using (FileStream stream = new FileStream(configFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (TextReader reader = new StreamReader(stream))
                    {
                        string yml = reader.ReadToEnd();

                        // setup the YAML deseralizer for the configuration
                        IDeserializer ymlDeserializer = new DeserializerBuilder()
                            .WithNamingConvention(CamelCaseNamingConvention.Instance)
                            .Build();

                        config = ymlDeserializer.Deserialize<ConfigurationObject>(yml);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"error: cannot read the configuration file, {configFile}\n{e.Message}");
                Environment.Exit((int)ERRNO.ENOCONFIG);
            }


            // setup logging configuration
            LoggerConfiguration logConfig = new LoggerConfiguration();
            logConfig.MinimumLevel.Debug();
            const string logTemplate = "{Level:u1}: {Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Message}{NewLine}{Exception}";

            // setup file logging
            LogEventLevel fileLevel = LogEventLevel.Information;
            switch (config.Log.FileLevel)
            {
                case 1:
                    fileLevel = LogEventLevel.Debug;
                    FneLogLevel = FneLogLevel.DEBUG;
                    break;
                case 2:
                case 3:
                default:
                    fileLevel = LogEventLevel.Information;
                    FneLogLevel = FneLogLevel.INFO;
                    break;
                case 4:
                    fileLevel = LogEventLevel.Warning;
                    FneLogLevel = FneLogLevel.WARNING;
                    break;
                case 5:
                    fileLevel = LogEventLevel.Error;
                    FneLogLevel = FneLogLevel.ERROR;
                    break;
                case 6:
                    fileLevel = LogEventLevel.Fatal;
                    FneLogLevel = FneLogLevel.FATAL;
                    break;
            }

            logConfig.WriteTo.File(Path.Combine(new string[] { config.Log.FilePath, config.Log.FileRoot + "-.log" }), fileLevel, logTemplate, rollingInterval: RollingInterval.Day);

            // setup console logging
            if (showLogOnConsole)
            {
                LogEventLevel dispLevel = LogEventLevel.Information;
                switch (config.Log.DisplayLevel)
                {
                    case 1:
                        dispLevel = LogEventLevel.Debug;
                        FneLogLevel = FneLogLevel.DEBUG;
                        break;
                    case 2:
                    case 3:
                    default:
                        dispLevel = LogEventLevel.Information;
                        FneLogLevel = FneLogLevel.INFO;
                        break;
                    case 4:
                        dispLevel = LogEventLevel.Warning;
                        FneLogLevel = FneLogLevel.WARNING;
                        break;
                    case 5:
                        dispLevel = LogEventLevel.Error;
                        FneLogLevel = FneLogLevel.ERROR;
                        break;
                    case 6:
                        dispLevel = LogEventLevel.Fatal;
                        FneLogLevel = FneLogLevel.FATAL;
                        break;
                }

                logConfig.WriteTo.Console(dispLevel, logTemplate);
            }

            // initialize logger
            Log.Logger = logConfig.CreateLogger();

            Log.Logger.Information("Copyright (c) 2025 Caleb, K4PHP and DVMProject Authors, All Rights Reserved.");

            try
            {
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception e)
            {
                Log.Logger.Error(e, "An unhandled exception occurred"); // TODO: make this less terse
            }
        }

    }
}
