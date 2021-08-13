using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using AutoNotify;

namespace Test
{
    public partial class MyObject
    {
        [AutoNotifyProperty("Health")]
        private int _health;
        [AutoNotifyProperty("Name")]
        private string _name;
        [AutoNotifyProperty("Exp")]
        private float  _exp;
    }

    class Program
    {
        static void Main(string[] args)
        {
            var so = new MyObject();
            so.PropertyChanged += (sender, ev) =>
            {
                Console.WriteLine(ev.PropertyName + " was modified!!!");
            };
            so.Health = 5;
            so.Exp = 1f;
            so.Name = "CatSandwich";
        }
    }
}
