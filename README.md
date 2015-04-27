# Serilog.Sinks.AmazonKinesis [![Build status](https://ci.appveyor.com/api/projects/status/mxoloqnptg3b9j47/branch/master?svg=true)](https://ci.appveyor.com/project/serilog/serilog-sinks-amazonkinesis/branch/master)

A Serilog sink that writes events as documents to [Amazon Kinesis](http://aws.amazon.com/kinesis/).

## Getting started

To get started install the _Serilog.Sinks.AmazonKinesis_ package from Visual Studio's _NuGet_ console:

```powershell
PM> Install-Package Serilog.Sinks.AmazonKinesis
```

Point the logger to Kinesis:

```csharp
const string streamName = "firehose";
const int shardCount = 2;

SelfLog.Out = Console.Out;

var client = AWSClientFactory.CreateAmazonKinesisClient();
            
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
        bufferLogShippingInterval: TimeSpan.FromSeconds(5),
        bufferBaseFilename: "./logs/kinesis-buffer"
    );
}

Log.Logger = loggerConfig.CreateLogger();
    
```

And use the Serilog logging methods to associate named properties with log events:

```csharp
Log.Error("Failed to log on user {ContactId}", contactId);
```
