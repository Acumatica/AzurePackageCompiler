using System;

namespace AzureCompiler.Core
{
    [Obsolete("This class transfered from PX.WebConfig")]
    public class ModuleInfo
    {
        public String Name { get; set; }
        public String Type { get; set; }

        public ModuleInfo(String name, String type)
        {
            Name = name;
            Type = type;
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() != typeof(ModuleInfo)) return false;
            ModuleInfo ext = (ModuleInfo)obj;
            return String.Equals(ext.Name, this.Name);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

}
