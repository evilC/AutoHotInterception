using System;
using TestApp.Helpers;

namespace TestApp
{
    internal class TestApp
    {
        private static void Main()
        {
            //var mmt = new MouseMoveTester(TestDevices.LogitechWheelMouse);
            //var mbt = new MouseButtonTester(TestDevices.LogitechWheelMouse, MouseButtons.Left, true);
            //var ambt = new MouseButtonsTester(TestDevices.LogitechWheelMouse, true);
            //var kt = new KeyboardTester(TestDevices.WyseKeyboard, true);
            //var kmt = new KeyboardAndMouseTester(TestDevices.WyseKeyboard, true).AddDevice(TestDevices.LogitechWheelMouse, true);
            //var kkt = new KeyboardKeyTester(TestDevices.WyseKeyboard, AhkKeys.Obj("1"), true);
            //var tt = new TabletTester(TestDevices.ParbloIslandA609);
            var sct = new ScanCodeTester(TestDevices.WyseKeyboard, true);
            //var sst = new SetStateTester(TestDevices.WyseKeyboard, AhkKeys.Obj("1"));
            Console.ReadLine();
        }
    }
}