using System.Collections.Generic;
using System.Diagnostics;
using AutoHotInterception.Helpers;
using NUnit.Framework;
using static AutoHotInterception.Helpers.ManagedWrapper;

namespace UnitTests
{
    public class TestKey
    {
        public string Name { get; }
        public List<Stroke> PressStrokes { get; }
        public List<Stroke> ReleaseStrokes { get; }
        public ExpectedResult PressResult { get; }
        public ExpectedResult ReleaseResult { get; }

        public TestKey(string name, List<Stroke> pressStrokes, List<Stroke> releaseStrokes,
            ExpectedResult pressResult, ExpectedResult releaseResult)
        {
            Name = name;
            PressStrokes = pressStrokes;
            ReleaseStrokes = releaseStrokes;
            PressResult = pressResult;
            ReleaseResult = releaseResult;
        }
    }

    public class ExpectedResult
    {
        public ushort Code { get; }
        public ushort State { get; }

        public ExpectedResult(ushort code, ushort state)
        {
            Code = code;
            State = state;
        }
    }

    [TestFixture]
    class ScanCodeHelperTests
    {
        ScanCodeHelper sch = new ScanCodeHelper();

        private static List<Stroke> Stroke(ushort code1, ushort state1, ushort code2 = 0, ushort state2 = 0)
        {
            var strokes = new List<Stroke>();
            strokes.Add(new Stroke() { key = { code = code1, state = state1 } });
            if (code2 != 0)
            {
                strokes.Add(new Stroke() { key = { code = code2, state = state2 } });
            }
            return strokes;
        }

        private static ExpectedResult Result(ushort code, ushort state)
        {
            var results = new ExpectedResult(code, state);
            return results;
        }

        [Test, TestCaseSource("TestKeyProvider")]
        public void PressRelease(string name, List<Stroke> pressStrokes, List<Stroke> releaseStrokes, ExpectedResult pressResult, ExpectedResult releaseResult )
        {
            Debug.WriteLine($"\nTesting key {name}...");
            Debug.WriteLine("Testing Press");
            var expectedResult = pressResult;
            var actualResult = sch.TranslateScanCodes(pressStrokes);
            AssertResult(actualResult, expectedResult);

            Debug.WriteLine("Testing Release");
            expectedResult = releaseResult;
            actualResult = sch.TranslateScanCodes(releaseStrokes);
            AssertResult(actualResult, expectedResult);

            Debug.WriteLine("OK!");
        }

        private static IEnumerable<TestCaseData> TestKeyProvider()
        {
            yield return new TestCaseData("Numpad Enter", Stroke(28, 0), Stroke(28, 1), Result(284, 1), Result(284, 0));
            yield return new TestCaseData("Right Control", Stroke(29, 2), Stroke(29, 3), Result(285, 1), Result(285, 0));
            yield return new TestCaseData("Numpad Div", Stroke(53, 2), Stroke(53, 3), Result(309, 1), Result(309, 0));
            yield return new TestCaseData("Right Shift", Stroke(54, 0), Stroke(54, 1), Result(310, 1), Result(310, 0));
            yield return new TestCaseData("Right Alt", Stroke(56, 2), Stroke(56, 3), Result(312, 1), Result(312, 0));
            yield return new TestCaseData("Numlock", Stroke(69, 0), Stroke(69, 1), Result(325, 1), Result(325, 0));
            yield return new TestCaseData("Pause", Stroke(29, 4, 69, 0), Stroke(29, 5, 69, 1), Result(69, 1), Result(69, 0));
            yield return new TestCaseData("Home", Stroke(42, 2, 71, 2), Stroke(71, 3, 42, 3), Result(327, 1), Result(327, 0));
            yield return new TestCaseData("Up", Stroke(42, 2, 72, 2), Stroke(72, 3, 42, 3), Result(328, 1), Result(328, 0));
            yield return new TestCaseData("PgUp", Stroke(42, 2, 73, 2), Stroke(73, 3, 42, 3), Result(329, 1), Result(329, 0));
            yield return new TestCaseData("Left", Stroke(42, 2, 75, 2), Stroke(75, 3, 42, 3), Result(331, 1), Result(331, 0));
            yield return new TestCaseData("Right", Stroke(42, 2, 77, 2), Stroke(77, 3, 42, 3), Result(333, 1), Result(333, 0));
            yield return new TestCaseData("End", Stroke(42, 2, 79, 2), Stroke(79, 3, 42, 3), Result(335, 1), Result(335, 0));
            yield return new TestCaseData("Down", Stroke(42, 2, 80, 2), Stroke(80, 3, 42, 3), Result(336, 1), Result(336, 0));
            yield return new TestCaseData("PgDn", Stroke(42, 2, 81, 2), Stroke(81, 3, 42, 3), Result(337, 1), Result(337, 0));
            yield return new TestCaseData("Insert", Stroke(42, 2, 82, 2), Stroke(82, 3, 42, 3), Result(338, 1), Result(338, 0));
            yield return new TestCaseData("Delete", Stroke(42, 2, 83, 2), Stroke(83, 3, 42, 3), Result(339, 1), Result(339, 0));
            yield return new TestCaseData("Left Windows", Stroke(91, 2), Stroke(91, 3), Result(347, 1), Result(347, 0));
            yield return new TestCaseData("Right Windows", Stroke(92, 2), Stroke(92, 3), Result(348, 1), Result(348, 0));
            yield return new TestCaseData("Apps", Stroke(93, 2), Stroke(93, 3), Result(349, 1), Result(349, 0));

            // Test Home block in E0 mode (Numlock on)
            yield return new TestCaseData("HomeE0", Stroke(71, 2), Stroke(71, 3), Result(327, 1), Result(327, 0));
            yield return new TestCaseData("UpE0", Stroke(72, 2), Stroke(72, 3), Result(328, 1), Result(328, 0));
            yield return new TestCaseData("PgUpE0", Stroke(73, 2), Stroke(73, 3), Result(329, 1), Result(329, 0));
            yield return new TestCaseData("LeftE0", Stroke(75, 2), Stroke(75, 3), Result(331, 1), Result(331, 0));
            yield return new TestCaseData("RightE0", Stroke(77, 2), Stroke(77, 3), Result(333, 1), Result(333, 0));
            yield return new TestCaseData("EndE0", Stroke(79, 2), Stroke(79, 3), Result(335, 1), Result(335, 0));
            yield return new TestCaseData("DownE0", Stroke(80, 2), Stroke(80, 3), Result(336, 1), Result(336, 0));
            yield return new TestCaseData("PgDnE0", Stroke(81, 2), Stroke(81, 3), Result(337, 1), Result(337, 0));
            yield return new TestCaseData("InsertE0", Stroke(82, 2), Stroke(82, 3), Result(338, 1), Result(338, 0));
            yield return new TestCaseData("DeleteE0", Stroke(83, 2), Stroke(83, 3), Result(339, 1), Result(339, 0));

        }

        void AssertResult(TranslatedKey actualResult, ExpectedResult expectedResult)
        {
            Debug.WriteLine($"Expecting code of {expectedResult.Code}, state of {expectedResult.State}");
            Assert.That(actualResult.AhkCode, Is.EqualTo(expectedResult.Code), $"Code should be {expectedResult.Code}, got {actualResult.AhkCode}");
            Assert.That(actualResult.State, Is.EqualTo(expectedResult.State), $"State should be {expectedResult.State}, got {actualResult.State}");
        }
    }
}
