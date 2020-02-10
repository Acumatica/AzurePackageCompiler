using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace AzureCompiler.Core
{
    [Obsolete("This class transfered from PX.WebConfig")]
    public static class XMLHelper
    {
        private const string location = "HC:location[@inheritInChildApplications=\"false\"]";

        public static XmlNode GetNode(this XmlDocument xdoc, String path, Boolean create)
        {
            if (xdoc == null || path == null) throw new ArgumentNullException();

            String[] parts = SplitPath(path);

            Dictionary<String, String> attributes = new Dictionary<String, String>();
            XmlNode locationNode = null;
            XmlNode node = xdoc.DocumentElement;
            for (int i = 0; i < parts.Length; i++)
            {
                String searcher = parts[i].Trim('/');

                XmlNode alternative = null;
                XmlNode result = node.SelectChildNode(searcher);
                if (locationNode == null) locationNode = node.SelectChildNode(location);
                if (locationNode != null) alternative = locationNode.SelectChildNode(searcher);

                //searching locations
                if (result == null && alternative != null) result = alternative;
                if (result == null && locationNode != null) node = locationNode;

                #region Create node if it isn't exist
                if (result == null && create)
                {
                    String name = searcher;
                    if (name.IndexOf(':') >= 0) name = name.Substring(name.IndexOf(':') + 1);
                    if (name.IndexOf('[') >= 0)
                    {
                        String conditions = name.Substring(name.IndexOf('['));
                        conditions = conditions.Trim('[', ']');
                        foreach (String condition in conditions.Split(new String[] { " or ", " and " }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            String[] definition = condition.Split(new String[] { "=" }, StringSplitOptions.RemoveEmptyEntries);
                            if (definition.Length != 2) continue;

                            String pkey = definition[0].Trim('@');
                            String pvalue = definition[1].Trim('\"');
                            attributes.Add(pkey, pvalue);
                        }

                        name = name.Substring(0, name.IndexOf('['));
                    }

                    result = xdoc.CreateElement(node.Prefix, name, node.NamespaceURI);
                    //result = xdoc.CreateNode(XmlNodeType.Element, name, node.NamespaceURI);
                    node.AppendChild(result);
                }

                if (result == null) return result;

                node = result;
                locationNode = alternative;
            }
            #endregion

            if (create)
            {
                foreach (KeyValuePair<String, String> pair in attributes)
                {
                    XmlAttribute atr = node.Attributes[pair.Key];
                    if (atr == null)
                    {
                        atr = node.EnsureAttribute(pair.Key);
                        atr.Value = pair.Value;
                    }
                }
            }
            return node;
        }

        public static void SetValue(this XmlDocument xdoc, String path, String property, String value)
        {
            if (xdoc == null || path == null || property == null) throw new ArgumentNullException();
            if (value == null) return;

            XmlNode node = GetNode(xdoc, path, true);
            XmlAttribute atr = node.EnsureAttribute(property);
            atr.Value = value;
        }    
     
        public static void SetXml(this XmlDocument xdoc, String path, String value, Boolean append)
        {
            if (value == null) return;
            XmlNode node = xdoc.GetNode(path, true);
            XmlNode parent = node.ParentNode;
            parent.RemoveChild(node);
            parent.InnerXml = append ? parent.InnerXml + value : value + parent.InnerXml;
        }

        public static void DeleteNode(this XmlDocument xdoc, String path)
        {
            XmlNode node = GetNode(xdoc, path, false);
            if (node == null) return;

            node.ParentNode.RemoveChild(node);
        }
        public static void DeleteAttribute(this XmlDocument xdoc, String path, String property)
        {
            XmlNode node = GetNode(xdoc, path, false);
            if (node == null) return;

            XmlAttribute atr = node.Attributes[property];
            if (atr == null) return;

            node.Attributes.Remove(atr);
        }       

        public static XmlNode SelectChildNode(this XmlNode node, String name)
        {
            if (node == null || name == null) throw new ArgumentException();

            XmlNamespaceManager nm = node.GetNamespaceManager();
            String prefix = node.GetNamespacePrefix(nm);

            String fullname = name.Trim('/');
            fullname = fullname.IndexOf(":") >= 0 ? fullname.Substring(fullname.IndexOf(':') + 1) : fullname;
            if (!String.IsNullOrEmpty(prefix)) fullname = String.Concat(prefix, ":", fullname);

            return node.SelectSingleNode(fullname, nm);
        }
        public static XmlNamespaceManager GetNamespaceManager(this XmlNode node)
        {
            if (node == null) throw new ArgumentException();

            XmlDocument xdoc = node.OwnerDocument;
            XmlNamespaceManager nm = new XmlNamespaceManager(xdoc.NameTable);

            if (!String.IsNullOrEmpty(node.NamespaceURI))
                nm.AddNamespace(RandomString(3).ToUpperInvariant(), node.NamespaceURI);
            else if (!String.IsNullOrEmpty(xdoc.DocumentElement.NamespaceURI))
                nm.AddNamespace(RandomString(3).ToUpperInvariant(), xdoc.DocumentElement.NamespaceURI);

            return nm;
        }
     
        public static String GetNamespacePrefix(this XmlNode node, XmlNamespaceManager nm)
        {
            String namespaceUri = null;
            if (!String.IsNullOrEmpty(node.NamespaceURI)) namespaceUri = node.NamespaceURI;
            else if (!String.IsNullOrEmpty(node.OwnerDocument.DocumentElement.NamespaceURI)) namespaceUri = node.OwnerDocument.DocumentElement.NamespaceURI;

            return namespaceUri == null ? null : nm.LookupPrefix(namespaceUri);
        }
        public static XmlAttribute EnsureAttribute(this XmlNode node, String name)
        {
            XmlAttribute atr = node.Attributes[name];
            if (atr == null)
            {
                atr = node.OwnerDocument.CreateAttribute(name);
                node.Attributes.Append(atr);
            }
            return atr;
        }
   
        public static String TryGetAttributeValue(this XmlNode node, String name)
        {
            XmlAttribute attr = node?.Attributes[name];
            return attr?.Value;
        }      
      
        private static String RandomString(int size)
        {
            StringBuilder builder = new StringBuilder();
            Random random = new Random();
            for (int i = 0; i < size; i++)
            {
                char ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
                builder.Append(ch);
            }
            return builder.ToString();
        }

        private static String[] SplitPath(String str)
        {
            List<String> list = new List<String>();

            Int32 last = 0;
            Boolean Quote = false;
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == '\"') Quote = !Quote;
                if ((str[i] == '/') && (!Quote))
                {
                    list.Add(str.Substring(last, i - last));
                    last = i + 1;
                }
            }
            if (last < str.Length) list.Add(str.Substring(last));

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (String.IsNullOrEmpty(list[i])) list.RemoveAt(i);
            }

            return list.ToArray();
        }
    }
}
