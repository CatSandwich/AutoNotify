using System;

namespace AutoNotify
{
    [AttributeUsage(AttributeTargets.Field)]
    public class AutoNotifyPropertyAttribute : Attribute
    {
        public string Name;
        public AutoNotifyPropertyAttribute( string name)
        {
            Name = name;
        }
    }
}
