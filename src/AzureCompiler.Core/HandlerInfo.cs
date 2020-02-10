using System;

namespace AzureCompiler.Core
{
    [Obsolete("This class transfered from PX.WebConfig")]
    public class HandlerInfo
    {
        public const String VerbAll = "*";

        public String Name { get; set; }
        public String Path { get; set; }
        public String Verb { get; set; }
        public String Type { get; set; }
        public String RequireAccess { get; set; }

        public HandlerInfo(String name, String path)
            : this(name, path, VerbAll)
        { }
        public HandlerInfo(String name, String path, String verb)
            : this(name, path, verb, null)
        { }
        public HandlerInfo(String name, String path, String verb, String type)
        {
            Name = name;
            Path = path;
            Verb = verb;
            Type = type;
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() != typeof(HandlerInfo)) return false;
            HandlerInfo extHandler = (HandlerInfo)obj;
            return String.Equals(extHandler.Name, this.Name);
        }
        public override int GetHashCode()
        {
            return Name == null ? 0 : Name.GetHashCode();
        }
    }

}
