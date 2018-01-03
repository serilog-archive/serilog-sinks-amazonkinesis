using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Serilog.Sinks.Amazon.Kinesis")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyProduct("Serilog.Sinks.Amazon.Kinesis")]
[assembly: AssemblyCopyright("Copyright © Serilog Contributors 2016")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("9cddc147-93bb-47dc-899c-b41384d7ae23")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

[assembly: InternalsVisibleTo("Serilog.Sinks.Amazon.Kinesis.Tests, PublicKey="+
                              "0024000004800000940000000602000000240000525341310004000001000100fb8d13fd344a1c" +
                              "6fe0fe83ef33c1080bf30690765bc6eb0df26ebfdf8f21670c64265b30db09f73a0dea5b3db4c9" +
                              "d18dbf6d5a25af5ce9016f281014d79dc3b4201ac646c451830fc7e61a2dfd633d34c39f87b818" +
                              "94191652df5ac63cc40c77f3542f702bda692e6e8a9158353df189007a49da0f3cfd55eb250066" +
                              "b19485ec")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2, " +
                              "PublicKey=" +
                              "0024000004800000940000000602000000240000525341310004000001000100" +
                              "c547cac37abd99c8db225ef2f6c8a3602f3b3606cc9891605d02baa56104f4cf" +
                              "c0734aa39b93bf7852f7d9266654753cc297e7d2edfe0bac1cdcf9f717241550" +
                              "e0a7b191195b7667bb4f64bcb8e2121380fd1d9d46ad2d92d2d15605093924cc" +
                              "eaf74c4861eff62abf69b9291ed0a340e113be11e6a7d3113e92484cf7045cc7")] // Required to do AutoFixture magic on internal interfaces