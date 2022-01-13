using System;
using AutoHotInterception;

namespace TestApp
{
    internal class TestApp
    {
        private static void Main()
        {
            //var mt = new MouseTester();
            //var mbt = new MouseButtonsTester();
            //var kt = new KeyboardTester();
            var kkt = new KeyboardKeyTester(TestDevices.WyseKeyboard, AhkKeys.Obj("1"));
            //var tt = new TabletTester();
            //var sct = new ScanCodeTester();
            //var sst = new SetStateTester(TestDevices.WyseKeyboard, AhkKeys.Obj("1"));
            Console.ReadLine();
        }
    }
}