using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace AzureCompiler.Core
{
    public class AssemblyAzurePackage
    {
        public enum SiteTypes
        {
            RegularSite,
            WebSite,
        }

        private readonly string _pathToCsPack ;
        private readonly string _outPath ;
        private readonly string _sourceDir; 
        private readonly string _framework;
        private readonly string _config = "Standard.csdef";
        private readonly string _vmSize;
        private readonly bool _useStandard = true;
        private readonly string _outFileName = "AzurePackage.cspkg";

        private readonly string _currentLocation;
        private string[] _tempDataDirs = new String[] { @"Files\App_Data\Database", @"Files\App_Data\Database\Data", @"Files\App_Data\Database\Data\System" };
        private List<string> _filesToRemove = new List<string>();

        private const string PACKAGE_PROPS = "properties.txt";
        private const string PACKAGE_INSTALL_FILES = "files.txt";
        /// <summary>
        /// Init
        /// </summary>
        /// <param name="pathToCsPack">path to cspack.exe. required for compilation azure package</param>
        /// <param name="sourceDir">Path to root folder unpacked ErpPackage</param>
        /// <param name="outPath">Result package path</param>
        /// <param name="framework">version of framework</param>
        public AssemblyAzurePackage(string pathToCsPack, string sourceDir, string outPath, string framework, string vmSize, string customConfig = "")
        {
            _pathToCsPack = pathToCsPack;
            _outPath = outPath;
            _framework = framework;
            _sourceDir = sourceDir;
            _vmSize = vmSize;
           _currentLocation = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            if (!string.IsNullOrEmpty(customConfig))
            {
                _config = customConfig;
                _useStandard = false;
            }
            else
            {
               _outFileName = $@"Standard_{_vmSize}.cspkg";
            }
        }

        public int Build()
        {
            int rVal = 0;
            try
            {               
                PrepareFiles(_sourceDir);

                if(_useStandard)
                    UpdateConfigFile();
                Log.Information("Run package compilation");
                RunCommand(_currentLocation, _pathToCsPack, _config,
                        $"/out:\"{Path.Combine(_outPath, _outFileName)}\"",
                        $"/roleFiles:Web;{PACKAGE_INSTALL_FILES}",
                        $"/rolePropertiesFile:Web;{PACKAGE_PROPS}",
                        $"/sitePhysicalDirectories:Web;Main;\"{Path.Combine(_sourceDir, @"Files")}\""
                    );
            }
            catch(Exception ex)
            {
                Log.Error(ex, "Build error");
                rVal = 1;
            }
            finally
            {
                CleanUp(_sourceDir);
                Log.Information("Done");
            }

            return rVal;
        }

        private void UpdateConfigFile()
        {
            Log.Information("Update vm size in config file");
            string newValue = string.Empty;
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(_config);
            string elementNamespace = xmlDoc.DocumentElement.GetNamespaceOfPrefix("xmlns");
            XmlNamespaceManager ns = new XmlNamespaceManager(xmlDoc.NameTable);
            ns.AddNamespace("msbld", "http://schemas.microsoft.com/ServiceHosting/2008/10/ServiceDefinition");
            XmlNode node = xmlDoc.SelectSingleNode("//msbld:WebRole", ns);
            node.Attributes["vmsize"].Value = _vmSize;
            xmlDoc.Save(_config);      
        }

        private void PrepareFiles(string sourseFolder)
        {
            Log.Information("Prepare files and folders");
            string siteRoot = Path.Combine(sourseFolder, "Files");

            Log.Debug("Copy framework");
            File.Copy($@"framework\{_framework}-web.exe", $@"DummySite\{_framework}-web.exe", true);

            Log.Debug("Update install.bat");
            UpdateInstallBat($@"DummySite\install.cmd");

            Log.Debug("Generate files.txt for DummySite");
            GenerateFilesFile("files.txt", "DummySite");
            _filesToRemove.Add($@"DummySite\{_framework}-web.exe");

           
          
            String s = Path.Combine(siteRoot, "App_Data", "CustomizationStatus.xml");
            if (File.Exists(s))
            {
                File.Copy(s, "CustomizationStatus.xml.bak", true);
                _filesToRemove.Add("CustomizationStatus.xml.bak");
                File.Delete(s);
            }

            Log.Debug("Create temp data folders");
            foreach (String path in _tempDataDirs)
            {
                s = Path.Combine(sourseFolder, path);
                if (!Directory.Exists(s))
                    Directory.CreateDirectory(s);
            }

            Log.Debug("Copy files in temp data folders");
            foreach (var item in Directory.GetFiles(Path.Combine(sourseFolder, "Database"), "*.sql", SearchOption.TopDirectoryOnly))
            {
                var newFile = Path.Combine(siteRoot, "App_Data", "Database", Path.GetFileName(item));
                _filesToRemove.Add(newFile);
                File.Copy(item, newFile, true);
            }
            foreach (var item in Directory.GetFiles(Path.Combine(sourseFolder, "Database"), "*.xml", SearchOption.TopDirectoryOnly))
            {
                var newFile = Path.Combine(siteRoot, "App_Data", "Database", Path.GetFileName(item));
                _filesToRemove.Add(newFile);
                File.Copy(item, newFile, true);
            }
            File.Copy(Path.Combine(sourseFolder, "Database", "ERPDatabaseSetup.adc"), Path.Combine(siteRoot, "App_Data", "Database", "ERPDatabaseSetup.adc"), true);
            _filesToRemove.Add(Path.Combine(siteRoot, "App_Data", "Database", "ERPDatabaseSetup.adc"));
            foreach (var item in Directory.GetFiles(Path.Combine(sourseFolder, "Database", "Data", "System"), "*.xml", SearchOption.TopDirectoryOnly))
            {
                var newFile = Path.Combine(siteRoot, "App_Data", "Database", "Data", "System", Path.GetFileName(item));
                _filesToRemove.Add(newFile);
                File.Copy(item, newFile, true);
            }

            if(File.Exists(Path.Combine(siteRoot, "files.list")))
            {
                _filesToRemove.Add("files.list.bak");
                File.Copy(Path.Combine(siteRoot, "files.list"), "files.list.bak", true);
            }

            Log.Debug("Update files.list");
            CreateFilesList(siteRoot);

            Log.Debug("Update Web.config");
            if (File.Exists(Path.Combine(siteRoot, "Web.Config")))
            {
                File.Copy(Path.Combine(siteRoot, "Web.Config"), "Web.Config.bak", true);
                WebConfig webConfig = new WebConfig();
                webConfig.Run(Path.Combine(siteRoot, "Web.Config"));
                _filesToRemove.Add("Web.Config.bak");
            }

            // File.Copy(Path.Combine(ScriptsPath, @"Azure\ServiceConfiguration.cscfg"), Path.Combine(DeployPath, "Azure\\Files\\ServiceConfiguration.cscfg"), true);
            // PrepareAzureConfig(Path.Combine(DeployPath, "Azure\\Files\\ServiceConfiguration.cscfg"));
        }

        private void UpdateInstallBat(string file)
        {
            string[] lines = File.ReadAllLines(file);
            Encoding encod = new UTF8Encoding(false);            

            using (StreamWriter writer = new StreamWriter(file, false, encod))
            {
                for (int currentLine = 1; currentLine <= lines.Length; ++currentLine)
                {
                    string line = lines[currentLine - 1];
                    if (line.CaseInsensitiveContains("set netfx=\"NDP"))
                    {                      
                       writer.Write($"set netfx=\"{_framework.ToUpper()}\"\n");
                    }
                    else
                    {
                        writer.WriteLine(line);
                    }
                }
            }
        }

        private void CleanUp(string sourseFolder)
        {
            Log.Debug("Remove bak files");
            string siteRoot = Path.Combine(sourseFolder, "Files");

            try
            {
                if (File.Exists("Web.Config.bak"))
                    File.Copy("Web.Config.bak", Path.Combine(siteRoot, "Web.Config"), true);

                if (File.Exists("files.list.bak"))
                    File.Copy("files.list.bak", Path.Combine(siteRoot, "files.list"), true);

                if (File.Exists("CustomizationStatus.xml.bak"))
                    File.Copy("CustomizationStatus.xml.bak", Path.Combine(siteRoot, "App_Data", "CustomizationStatus.xml"), true);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "CleanUp bak error");
            }

            Log.Debug("Remove temp files");
            try
            {
                foreach (var item in _filesToRemove)
                {
                    if (File.Exists(item))
                        File.Delete(item);
                }
            }
            catch (Exception ex) {
                Log.Error(ex, "CleanUp files error");
            }

            Log.Debug("Remove temp folders");
            try
            {
                foreach (String path in _tempDataDirs.Reverse())
                {
                    String s = Path.Combine(sourseFolder, path);
                    if (Directory.Exists(s))
                        Directory.Delete(s, true);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "CleanUp folders error");
            }
        }

        private void PrepareAzureConfig(String file)
        {
            /*if (File.Exists(file))
            {
                using (AzureConfiguration acm = new AzureConfiguration(file, false))
                {
                   // acm.Version = BuildVersion;
                }
            }*/
        }

        private void CreateFilesList(String folder)
        {
            if (folder[folder.Length - 1] != Path.DirectorySeparatorChar) folder += Path.DirectorySeparatorChar;

            List<String> files = new List<String>();
            foreach (String file in Directory.GetFiles(folder, "*", SearchOption.AllDirectories))
            {
                if(Path.GetFileName(file) != "files.list")
                files.Add(file.Substring(folder.Length));
            }

            using (StreamWriter writer = new StreamWriter(Path.Combine(folder, "files.list")))
            {
                foreach (String s in files)
                    writer.WriteLine(s);
            }
        }

        private void GenerateFilesFile(String file, String folder)
        {
            if (folder[folder.Length - 1] != Path.DirectorySeparatorChar) folder += Path.DirectorySeparatorChar;

            using (StreamWriter sw = new StreamWriter(new FileStream(file, FileMode.Create, FileAccess.ReadWrite, FileShare.None)))
            {
                foreach (String fullPath in Directory.GetFiles(folder, "*", SearchOption.AllDirectories))
                {
                    String relativePath = fullPath.Substring(folder.Length);
                    sw.WriteLine(String.Format("{0};{1}", fullPath, relativePath));
                }
            }
        } 

        private int RunCommand(string workingDir, string command, params string[] param)
        {
            string agrs = string.Join(" ", param);
            Log.Information($"Call {command} {agrs}");
            using (Process process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = agrs,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = workingDir
                };

                process.OutputDataReceived += new DataReceivedEventHandler(OutputHandler);
                process.ErrorDataReceived += new DataReceivedEventHandler(OutputHandler);
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
                return process.ExitCode;
            }
        }

        private void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (!string.IsNullOrEmpty(outLine.Data))
               Log.Information(outLine.Data);
        }

    }
}
