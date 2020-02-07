using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace AzureCompiler.Core
{
    public partial class WebConfig
    {
        protected readonly XmlDocument xdoc = new XmlDocument();
        protected const String excludeRemoveTegs = ";httpModules;httpHandlers;multiAuth;";
        List<XmlNode> handlers = new List<XmlNode>();

        public void Run(string path)
        {
            xdoc.Load(path);
      
            List<HandlerInfo> handlers = new List<HandlerInfo>();
            FillWebHandlers(handlers);
            List<ModuleInfo> modules = new List<ModuleInfo>();
            FillWebModules(modules);

            xdoc.DeleteNode("//HC:system.webServer//HC:handlers");
            xdoc.DeleteNode("//HC:system.web//HC:httpHandlers");
            xdoc.DeleteNode("//HC:system.webServer//HC:modules");
            xdoc.DeleteNode("//HC:system.web//HC:httpModules");
            xdoc.DeleteNode("//HC:system.web//HC:securityPolicy");
            xdoc.DeleteNode("//HC:system.web//HC:trust");

            handlers.ForEach(CreateHandler);
            modules.ForEach(CreateModule);

            xdoc.SetValue("//HC:system.webServer//HC:modules", "runAllManagedModulesForAllRequests", "true");

            xdoc.SetValue(String.Format("//HC:appSettings//HC:add[@key=\"{0}\"]", "AutoUpdate"), "value", Boolean.TrueString);
            xdoc.SetValue(String.Format("//HC:appSettings//HC:add[@key=\"{0}\"]", "AutoRenewal"), "value", Boolean.TrueString);
            xdoc.SetValue(String.Format("//HC:appSettings//HC:add[@key=\"{0}\"]", "IsAzure"), "value", Boolean.TrueString);


            xdoc.SetValue("//HC:system.web//HC:machineKey", "validationKey", SecurityHelper.GenerateValidationKey());
            xdoc.SetValue("//HC:system.web//HC:machineKey", "decryptionKey", SecurityHelper.GenerateDecryptionKey());
            xdoc.SetValue("//HC:system.web//HC:machineKey", "validation", "SHA1");
            xdoc.SetValue("//HC:system.web//HC:pxdatabase/HC:providers/HC:add", "companyID", "1");
            xdoc.SetValue("//HC:system.web/HC:identity", "impersonate", Boolean.FalseString);
           
            xdoc.SetValue("//HC:system.webServer/HC:httpErrors", "errorMode", "Detailed");
            xdoc.SetValue("//HC:system.webServer/HC:asp", "scriptErrorSentToBrowser", Boolean.TrueString);
            CreateReference("Microsoft.WindowsAzure.ServiceRuntime", "2.3.*.*", "31BF3856AD364E35");
            xdoc.GetNode("//HC:system.webServer/HC:modules/HC:remove[@name=\"WebDAVModule\"]", true);

            #region Create Code Dom
            String codedom = "<system.codedom>" +
                "<compilers>" +
                    "<compiler language=\"c#;cs;csharp\" extension=\".cs\" type=\"Microsoft.CSharp.CSharpCodeProvider,System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089\" warningLevel=\"4\">" +
                        "<providerOption name=\"CompilerVersion\" value=\"v4.0\"/>" +
                        "<providerOption name=\"WarnAsError\" value=\"false\"/>" +
                    "</compiler>" +
                    "<compiler language=\"vb;vbs;visualbasic;vbscript\" extension=\".vb\" type=\"Microsoft.VisualBasic.VBCodeProvider, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089\" warningLevel=\"4\">" +
                        "<providerOption name=\"CompilerVersion\" value=\"v4.0\"/>" +
                        "<providerOption name=\"OptionInfer\" value=\"true\"/>" +
                        "<providerOption name=\"WarnAsError\" value=\"false\"/>" +
                    "</compiler>" +
                "</compilers>" +
            "</system.codedom>";
            xdoc.SetXml("//HC:system.codedom", codedom, true);
            #endregion

            CreateCompression();


            #region probably is obsolete
            xdoc.SetValue("//HC:system.web/HC:customErrors", "mode", "Off");
            //Upgrade OData
            string enableCompressionValue = xdoc.GetNode("//HC:system.web//HC:odata", false).TryGetAttributeValue("enableCompression");
            if (enableCompressionValue != null)
            {
                xdoc.SetValue(String.Format("//HC:appSettings//HC:add[@key=\"{0}\"]", "EnableCompression"), "value", enableCompressionValue);
                xdoc.DeleteAttribute("//HC:system.web//HC:odata", "enableCompression");
            }
            string compressionThresholdValue = xdoc.GetNode("//HC:system.web//HC:odata", false).TryGetAttributeValue("compressionThreshold");
            if (compressionThresholdValue != null)
            {
                xdoc.SetValue(String.Format("//HC:appSettings//HC:add[@key=\"{0}\"]", "CompressionThreshold"), "value", compressionThresholdValue);   
                xdoc.DeleteAttribute("//HC:system.web//HC:odata", "compressionThreshold");
            }
            #endregion

            AddRemoveNodes(xdoc.DocumentElement);
            xdoc.Save(path);
        }

        protected void AddRemoveNodes(XmlNode node)
        {
            if (excludeRemoveTegs.Contains(String.Format(";{0};", node.Name))) return;

            //load recursively
            foreach (XmlNode n in node.ChildNodes)
            {
                if (n.NodeType == XmlNodeType.Comment) continue;
                if (n.ChildNodes.Count > 0) AddRemoveNodes(n); 
            }

            List<String> tagNames = new List<String>();
            foreach (XmlNode n in node.ChildNodes)
            {
                if (n.NodeType == XmlNodeType.Comment) continue;
                if (n.Name != "add") continue;
                XmlAttribute nameAtr = n.Attributes["name"];
                if (nameAtr == null) continue;

                tagNames.Add(nameAtr.Value);
            }
            foreach (XmlNode n in node.ChildNodes)
            {
                if (n.NodeType == XmlNodeType.Comment) continue;
                if (n.Name != "remove") continue;
                XmlAttribute nameAtr = n.Attributes["name"];
                if (nameAtr == null) continue;

                if (tagNames.Contains(nameAtr.Value)) tagNames.Remove(nameAtr.Value);
            }

            foreach (String tag in tagNames)
            {
                XmlNode removeElement = xdoc.CreateElement(node.Prefix, "remove", node.NamespaceURI);
                removeElement.CreateAttributeIfNotExists("name", tag);
                node.PrependChild(removeElement);
            }
        }

        public void CreateHandler(HandlerInfo handler)
        {
            XmlNode anode = xdoc.GetNode(String.Format("//HC:system.webServer/HC:handlers/HC:add[@name=\"{0}\"]", handler.Name), true);
            anode.CreateAttributeIfNotExists("path", handler.Path);
            anode.CreateAttributeIfNotExists("verb", handler.Verb);
            if (!String.IsNullOrEmpty(handler.Type))
            {
                anode.CreateAttributeIfNotExists("type", handler.Type);
            }         
        }

        private void FillWebModules(ICollection<ModuleInfo> modules)
        {
            XmlNode node = xdoc.GetNode("//HC:system.webServer//HC:modules", false);
            if (node == null) return;

            foreach (XmlNode n in node.ChildNodes)
            {
                if (n.NodeType == XmlNodeType.Comment || !"add".Equals(n.Name, StringComparison.OrdinalIgnoreCase)) continue;

                XmlAttribute type = n.Attributes["type"];
                XmlAttribute name = n.Attributes["name"];

                ModuleInfo module = new ModuleInfo(name.Value, type.Value);
                if (!modules.Contains(module))
                    modules.Add(module);
            }
        }

        public void CreateModule(ModuleInfo module)
        {
            XmlNode anode = xdoc.GetNode(String.Format("//HC:system.webServer/HC:modules/HC:add[@name=\"{0}\"]", module.Name), true);
            anode.CreateAttributeIfNotExists("type", module.Type);
        }

        private void FillWebHandlers(ICollection<HandlerInfo> handlers)
        {
            XmlNode node = xdoc.GetNode("//HC:system.webServer//HC:handlers", false);
            if (node == null) return;

            foreach (XmlNode n in node.ChildNodes)
            {
                if (n.NodeType == XmlNodeType.Comment) continue;
                if (String.Compare(n.Name, "add", true) != 0) continue;

                XmlAttribute name = n.Attributes["name"];
                XmlAttribute type = n.Attributes["type"];
                XmlAttribute path = n.Attributes["path"];
                XmlAttribute verb = n.Attributes["verb"];

                HandlerInfo handler = new HandlerInfo(name.Value, path?.Value, verb?.Value, type?.Value);
                if (handler != null && !handlers.Contains(handler)) handlers.Add(handler);
            }
        } 
    
		private void CreateCompression()
		{
			#region xml
			const string xmlFragment = @"
  <httpCompression directory=""%SystemDrive%\inetpub\temp\IIS Temporary Compressed Files"">
             <scheme name=""gzip"" dll=""%Windir%\system32\inetsrv\gzip.dll"" dynamicCompressionLevel=""9"" />
             <dynamicTypes>
                 <add mimeType=""text/*"" enabled=""true"" />
                 <add mimeType=""message/*"" enabled=""true"" />
                 <add mimeType=""application/x-javascript"" enabled=""true"" />
                 <add mimeType=""application/javascript"" enabled=""true"" />
                 <add mimeType=""application/xslt+xml"" enabled=""true"" />
                 <add mimeType=""*/*"" enabled=""false"" />
             </dynamicTypes>
             <staticTypes>
                 <add mimeType=""text/*"" enabled=""true"" />
                 <add mimeType=""message/*"" enabled=""true"" />
                 <add mimeType=""application/javascript"" enabled=""true"" />
                 <add mimeType=""*/*"" enabled=""false"" />
             </staticTypes>
         </httpCompression>
			";
			#endregion

			XmlNode node = xdoc.GetNode("//HC:system.webServer/HC:httpCompression", false);
			XmlNode parent = xdoc.GetNode("//HC:system.webServer", true);
			if (node != null) parent.RemoveChild(node);

			parent.InnerXml = parent.InnerXml + xmlFragment;
		}       

        public void CreateReference(String type, String version, String key)
        {
            string res = String.Format("{0}, Version={1}, Culture=neutral, PublicKeyToken={2}", type, version, key);
            XmlNode node = xdoc.GetNode("//HC:system.web/HC:compilation/HC:assemblies", false);

            Boolean exist = false;
            foreach (XmlNode n in node.ChildNodes)
            {
                if (node.Attributes["assembly"] != null && node.Attributes["assembly"].Value == res)
                    exist = true;
            }

            if (!exist)
            {
                XmlNode newNode = xdoc.CreateElement(node.Prefix, "add", node.NamespaceURI);
                XmlAttribute atr = xdoc.CreateAttribute("assembly");
                atr.Value = res;
                newNode.Attributes.Append(atr);
                node.AppendChild(newNode);
            }
        }
    }
}