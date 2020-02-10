using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace AzureCompiler.Core
{
    public static class Extensions
    {
        public static bool CaseInsensitiveContains(this string text, string value, StringComparison stringComparison = StringComparison.CurrentCultureIgnoreCase)
        {
            return text.IndexOf(value, stringComparison) >= 0;
        }

        [Obsolete]
        public static XmlAttribute CreateAttributeIfNotExists(this XmlNode node, String name, String valueToAssign = null)
        {
            XmlAttribute atr = node.Attributes[name];
            if (atr == null)
            {
                atr = node.OwnerDocument.CreateAttribute(name);
                node.Attributes.Append(atr);
            }
            if (null != valueToAssign)
                atr.Value = valueToAssign;
            return atr;
        }

    }
}
