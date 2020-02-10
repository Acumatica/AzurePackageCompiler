using AzureCompiler.Core;
using CommandLine;
using CommandLine.Text;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace AzureCompiler
{
    class Program
    {
        public enum FrameworkTypes
        {
            NDP48,
            NDP472
        }

        public class Options
        {
            [Option('c', "cspack", Required = true, HelpText = "Path to cspack.exe in Microsoft SDK folder")]
            public string PathToCsPack { get; set; }

            [Option('s', "sourceDir", Required = true, HelpText = "Path to source files of ErpPackage.zip")]
            public string SourceDir { get; set; }

            [Option('o', "outDir", Required = true, HelpText = "Output path to azure package")]
            public string OutDir { get; set; }

            [Option('f', "framework", Required = true, HelpText = "Framework version. NDP48 - for version 2019R2/2020R1, NDP482 - 2019R1")]
            public FrameworkTypes Framework { get; set; }

            [Option('v', "vmSize", Required = false, HelpText = "Vitrual machine size. Can be skipped if you use custom config.")]
            public string VmSize { get; set; }

            [Option('g', "config", Required = false, HelpText = "Path to your custom config file (*.csdef). Can be skipped if you use standard config.")]
            public string CustomConfig { get; set; }

            [Option('q', "quiet", Required = false, HelpText = "Closing window when program is finished.", Default=false)]
            public bool Quiet { get; set; }
        }

        private static string _logFile = "log.txt";

        static void Main(string[] args)
        {
            bool quiet = false;
            if (File.Exists(_logFile))
                File.Delete(_logFile);

            Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(_logFile)
            .CreateLogger();

            var parser = new Parser(with => with.HelpWriter = null);       
            var parserResult = parser.ParseArguments<Options>(args);
            parserResult
                   .WithParsed<Options>(o =>
                   {
                       quiet = o.Quiet;
                       if (string.IsNullOrEmpty(o.VmSize) && string.IsNullOrEmpty(o.CustomConfig))
                       {
                           Log.Error("Please set value for Vitrual machine size or path to custom config file");
                       }
                       else
                       {
                           AssemblyAzurePackage a = new AssemblyAzurePackage(o.PathToCsPack, o.SourceDir, o.OutDir, o.Framework.ToString(), o.VmSize, o.CustomConfig);
                           a.Build();
                       }
                  
                   })
                   .WithNotParsed(x =>
                   {
                       var helpText = HelpText.AutoBuild(parserResult, h =>
                       {
                           h.AutoHelp = false;     //hide --help
                           h.AutoVersion = false;  //hide --version
                           return HelpText.DefaultParsingErrorsHandler(parserResult, h);
                       }, e => e);
                       Console.WriteLine(helpText);
                   });        

            if (!quiet)
            {
                Console.WriteLine("For exit please press any key...");
                Console.ReadLine();
            }
        }
    }
}
