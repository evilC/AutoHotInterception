using System;
using AutoHotInterception;

namespace TestApp
{
    internal class TestApp
    {
        private static void Main()
        {
            var mmt = new MouseMoveTester(TestDevices.LogitechWheelMouse);
            //var mbt = new MouseButtonsTester(TestDevices.LogitechWheelMouse);
            //var kt = new KeyboardTester(TestDevices.WyseKeyboard);
            //var kkt = new KeyboardKeyTester(TestDevices.WyseKeyboard, AhkKeys.Obj("1"));
            //var tt = new TabletTester();
            //var sct = new ScanCodeTester();
            //var sst = new SetStateTester(TestDevices.WyseKeyboard, AhkKeys.Obj("1"));
            Console.ReadLine();
        }
    }
}