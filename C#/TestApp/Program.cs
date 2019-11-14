using System;
using AutoHotInterception;

namespace TestApp
{
    internal class TestApp
    {
        private static void Main()
        {
            //var mt = new MouseTester();
            var kt = new KeyboardTester();
            //var kt = new KeyboardKeyTester();
            //var tt = new TabletTester();
            //var mon = new MonitorTester();
            Console.ReadLine();
        }
    }
}