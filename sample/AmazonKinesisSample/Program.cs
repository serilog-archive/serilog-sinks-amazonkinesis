using System;
using System.Diagnostics;
using System.Threading;
using Amazon.Kinesis;
using Serilog;
using Serilog.Debugging;
using Serilog.Sinks.Amazon.Kinesis;
using Serilog.Sinks.Amazon.Kinesis.Stream;
using Serilog.Sinks.Amazon.Kinesis.Stream.Sinks;

namespace AmazonKinesisSample
{
    public class Position
    {
        public double Lat { get; set; }
        public double Long { get; set; }
    }

    public class Program
    {
        const string streamName = "firehose";
        const int shardCount = 1;

        public static void Main()
        {
            SelfLog.Out = Console.Out;

            var client = new AmazonKinesisClient();

            var streamOk = KinesisApi.CreateAndWaitForStreamToBecomeAvailable(
                kinesisClient: client,
                streamName: streamName,
                shardCount: shardCount
            );

            var loggerConfig = new LoggerConfiguration()
                .WriteTo.ColoredConsole()
                .MinimumLevel.Debug();

            if (streamOk)
            {
                loggerConfig.WriteTo.AmazonKinesis(
                    kinesisClient: client,
                    streamName: streamName,
                    shardCount: shardCount,
                    period: TimeSpan.FromSeconds(2),
                    bufferBaseFilename: "./logs/kinesis-buffer",
                    onLogSendError: OnLogSendError
                );
            }

            Log.Logger = loggerConfig.CreateLogger();

#if false

            for (var i = 0; i < 50; i++)
            {
                for (int j = 0; j < 500; j++)
                {
                    Thread.Sleep(1);
                    Log.Debug("Count: {i} {j}", i, j);
                }

                Console.Write(".");
            }

#endif

            LogStuff();

            Log.Fatal("That's all folks - and all done using {WorkingSet} bytes of RAM", Environment.WorkingSet);
            Console.ReadKey();
        }

        static void OnLogSendError(object sender, LogSendErrorEventArgs logSendErrorEventArgs) {
            Console.WriteLine("Error: {0}", logSendErrorEventArgs.Message);
        }

        private static void LogStuff()
        {
            Log.Verbose("This app, {ExeName}, demonstrates the basics of using Serilog", "Demo.exe");

            ProcessInput(new Position { Lat = 24.7, Long = 132.2 });
            ProcessInput(new Position { Lat = 24.71, Long = 132.15 });
            ProcessInput(new Position { Lat = 24.72, Long = 132.2 });


            Log.Information("Just biting {Fruit} number {Count}", "Apple", 12);
            Log.ForContext<Program>().Information("Just biting {Fruit} number {Count:0000}", "Apple", 12);

            // ReSharper disable CoVariantArrayConversion
            Log.Information("I've eaten {Dinner}", new[] { "potatoes", "peas" });
            // ReSharper restore CoVariantArrayConversion

            Log.Information("I sat at {@Chair}", new { Back = "straight", Legs = new[] { 1, 2, 3, 4 } });
            Log.Information("I sat at {Chair}", new { Back = "straight", Legs = new[] { 1, 2, 3, 4 } });

            const int failureCount = 3;
            Log.Warning("Exception coming up because of {FailureCount} failures...", failureCount);

            try
            {
                DoBad();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "There's those {FailureCount} failures", failureCount);
            }

            Log.Verbose("This app, {ExeName}, demonstrates the basics of using Serilog", "Demo.exe");

            try
            {
                DoBad();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "We did some bad work here.");
            }

            var result = 0;
            var divideBy = 0;
            try
            {
                result = 10 / divideBy;
            }
            catch (Exception e)
            {
                Log.Error(e, "Unable to divide by {divideBy}", divideBy);
            }
        }

        static void DoBad()
        {
            throw new InvalidOperationException("Everything's broken!");
        }

        static readonly Random Rng = new Random();

        static void ProcessInput(Position sensorInput)
        {
            var sw = new Stopwatch();
            sw.Start();
            Log.Debug("Processing some input on {MachineName}...", Environment.MachineName);
            Thread.Sleep(Rng.Next(0, 100));
            sw.Stop();

            Log.Information("Processed {@SensorInput} in {Time:000} ms", sensorInput, sw.ElapsedMilliseconds);
        }
    }
}
