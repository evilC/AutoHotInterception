using System;
using AutoHotInterception;

namespace TestApp
{
    internal class TestApp
    {
        private static void Main()
        {
            //var mmt = new MouseMoveTester(TestDevices.LogitechWheelMouse);
            //var mbt = new MouseButtonsTester(TestDevices.LogitechWheelMouse);
            var kt = new KeyboardTester(TestDevices.WyseKeyboard, true);
            //var kkt = new KeyboardKeyTester(TestDevices.WyseKeyboard, AhkKeys.Obj("1"), true);
            //var tt = new TabletTester(TestDevices.ParbloIslandA609);
            //var sct = new ScanCodeTester(TestDevices.WyseKeyboard, true);
            //var sst = new SetStateTester(TestDevices.WyseKeyboard, AhkKeys.Obj("1"));
            Console.ReadLine();
        }
    }
}