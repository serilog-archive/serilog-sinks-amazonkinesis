# Serilog.Sinks.AmazonKinesis [![Build status](https://ci.appveyor.com/api/projects/status/0ry328alcvvykk8m/branch/master?svg=true)](https://ci.appveyor.com/project/superlogical/serilog-sinks-amazonkinesis/branch/master)

A Serilog sink that writes events as documents to [Amazon Kinesis](http://aws.amazon.com/kinesis/).

## Getting started

To get started install the _Serilog.Sinks.AmazonKinesis_ package from Visual Studio's _NuGet_ console:

```powershell
PM> Install-Package Serilog.Sinks.AmazonKinesis
```

Point the logger to Kinesis:

```csharp
SelfLog.Out = Console.Out;

const string streamName = "firehose";
const int shardCount = 2;

var client = AWSClientFactory.CreateAmazonKinesisClient();
var streamOk = KinesisApi.CreateAndWaitForStreamToBecomeAvailable(client, streamName, shardCount);

var loggerConfig = new LoggerConfiguration();

if (streamOk)
{
    loggerConfig.WriteTo.AmazonKinesis(
        kinesisClient: client, 
        streamName: streamName, 
        shardCount: shardCount,
        bufferLogShippingInterval: TimeSpan.FromSeconds(5),
        bufferBaseFilename: "./logs/kinesis-buffer",
        period: TimeSpan.FromSeconds(2)
    );
}

Log.Logger = loggerConfig
    .MinimumLevel.Debug()
    .CreateLogger();
    
```

And use the Serilog logging methods to associate named properties with log events:

```csharp
Log.Error("Failed to log on user {ContactId}", contactId);
```
