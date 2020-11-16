using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;
using Serilog.Sinks.MSSqlServer.Sinks.MSSqlServer.Options;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace SerilogWithDb
{
    public class Program
    {
        private const string ConnectionString =
            "";
        private const string SchemaName = "T";
        private const string TableName = "EventLog";

        public static void Main(string[] args)
        {
            try
            {
                try
                {
                    var options = BuildColumnOptions();

                    Log.Logger = new LoggerConfiguration()
                        .MinimumLevel.ControlledBy(GetLoggingLevel())
                        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                        .MinimumLevel.Override("System", LogEventLevel.Warning)
                        .Enrich.WithMachineName()
                        .Enrich.FromLogContext()
                        //.Enrich.WithCaller()
                        .Enrich.WithProperty("Application", "Serilog+DB_EXAMPLE")
                        .WriteTo.Console()
                        .WriteTo.MSSqlServer(ConnectionString,
                            sinkOptions: new SinkOptions
                            {
                                TableName = TableName,
                                SchemaName = SchemaName
                            },
                            appConfiguration: null,
                            //restrictedToMinimumLevel: LogEventLevel.Verbose,
                            formatProvider: null,
                            columnOptions: options,
                            columnOptionsSection: null)
                        .CreateLogger();

                    Serilog.Debugging.SelfLog.Enable(msg =>
                    {
                        Debug.Print(msg);
                        Debugger.Break();
                    });
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                Log.Debug("Getting started");

                Log.Information("Hello {Name} from thread {ThreadId}", Environment.GetEnvironmentVariable("USERNAME"),
                    Thread.CurrentThread.ManagedThreadId);

                Log.Warning("No coins remain at position {@Position}", new {Lat = 25, Long = 134});

                CreateHostBuilder(args).Build().Run();

                Log.Debug("Application started.");
            }
            catch (DivideByZeroException e)
            {
                Log.Error(e, "Division by zero");
            }
            finally
            {

                Log.CloseAndFlush();

            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureLogging(config => { config.ClearProviders(); })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>()
                        .UseSerilog(dispose: true);
                });
        }

        private static LoggingLevelSwitch GetLoggingLevel()
        {
            var hostBuilder = new WebHostBuilder();
            var environment = hostBuilder.GetSetting("environment");

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var loggingLevel = new LoggingLevelSwitch();
            bool res = Enum.TryParse<LogEventLevel>(configuration["DefaultLoggingLevel"], true, out var defaultLevel);
            loggingLevel.MinimumLevel = res ? defaultLevel : LogEventLevel.Information;

            return loggingLevel;
        }

        private static ColumnOptions BuildColumnOptions()
        {
            var columnOptions = new ColumnOptions
            {
                TimeStamp = { ConvertToUtc = true},

                AdditionalColumns = new Collection<SqlColumn>
                {
                    new SqlColumn {DataType = SqlDbType.NVarChar, ColumnName = "Application"},
                    new SqlColumn {DataType = SqlDbType.NVarChar, ColumnName = "MachineName"},
                    new SqlColumn {DataType = SqlDbType.NVarChar, ColumnName = "CallerName"}
                }
            };

            columnOptions.Store.Remove(StandardColumn.MessageTemplate);
            //columnOptions.Store.Remove(StandardColumn.TimeStamp);
            //columnOptions.Store.Remove(StandardColumn.Properties);
            //columnOptions.Store.Remove(StandardColumn.LogEvent);

            return columnOptions;
        }


        //TimeStamp =
        //{
        //    ColumnName = "Logged",
        //    ConvertToUtc = true,
        //    DataType = SqlDbType.DateTime
        //},

        /*
        CREATE TABLE [Log] (
           [Id] int IDENTITY(1,1) NOT NULL,
           [Message] nvarchar(max) NULL,
           [MessageTemplate] nvarchar(max) NULL,
           [Level] nvarchar(128) NULL,
           [TimeStamp] datetimeoffset(7) NOT NULL,
           [Exception] nvarchar(max) NULL,
           [Properties] xml NULL,
           [LogEvent] nvarchar(max) NULL

           CONSTRAINT [PK_Log]
             PRIMARY KEY CLUSTERED ([Id] ASC)
        )
         */

    }
}
