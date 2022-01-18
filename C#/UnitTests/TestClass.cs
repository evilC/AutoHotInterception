// NUnit 3 tests
// See documentation : https://github.com/nunit/docs/wiki/NUnit-Documentation
using System;
using System.Collections;
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
        public List<KeyStroke> PressStrokes { get; }
        public List<KeyStroke> ReleaseStrokes { get; }
        public List<ExpectedResult> PressResults { get; }
        public List<ExpectedResult> ReleaseResults { get; }

        public TestKey(string name, List<KeyStroke> pressStrokes, List<KeyStroke> releaseStrokes, 
            List<ExpectedResult> pressResults, List<ExpectedResult> releaseResults)
        {
            Name = name;
            PressStrokes = pressStrokes;
            ReleaseStrokes = releaseStrokes;
            PressResults = pressResults;
            ReleaseResults = releaseResults;
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
    public class ScanCodeHelperTests
    {
        ScanCodeHelper sch;
        private static List<TestKey> _testKeys = new List<TestKey>()
        {
            new TestKey("Numpad Enter", Stroke(28, 0), Stroke(28, 1), Result(284, 1), Result(284, 0)),
            new TestKey("Right Control", Stroke(29, 2), Stroke(29, 3), Result(285, 1), Result(285, 0)),
            new TestKey("Numpad Div", Stroke(53, 2), Stroke(53, 3), Result(309, 1), Result(309, 0)),
            new TestKey("Right Shift", Stroke(54, 0), Stroke(54, 1), Result(310, 1), Result(310, 0)),
            new TestKey("Right Alt", Stroke(56, 2), Stroke(56, 3), Result(312, 1), Result(312, 0)),
            new TestKey("Numlock", Stroke(69, 0), Stroke(69, 1), Result(325, 1), Result(325, 0)),
            //new TestKey("Pause", Stroke(69, 0), Stroke(69, 1), Result(325, 1), Result(325, 0)),
            new TestKey("Home", Stroke(42, 2, 71, 2), Stroke(71, 3, 42, 3), Result(null, null, 327, 1), Result(null, null, 327, 0)),
        };

        [SetUp]
        public void SetUpBeforeEachTest()
        {
            sch = new ScanCodeHelper();
        }

        private static List<KeyStroke> Stroke (ushort code1, ushort state1, ushort code2 = 0, ushort state2 = 0)
        {
            var strokes = new List<KeyStroke>();
            strokes.Add(new KeyStroke() { code = code1, state = state1 });
            if (code2 != 0)
            {
                strokes.Add(new KeyStroke() { code = code2, state = state2 });
            }
            return strokes;
        }

        private static List<ExpectedResult> Result(ushort? code1, ushort? state1, ushort? code2 = null, ushort? state2 = null)
        {
            var results = new List<ExpectedResult>();
            if (code1 == null) results.Add(null);
            else results.Add(new ExpectedResult((ushort)code1, (ushort)state1));
            if (code2 == null) results.Add(null);
            else results.Add(new ExpectedResult((ushort)code2, (ushort)state2));
            return results;
        }

        [Test]
        public void PressReleaseTests()
        {
            //DoTest(_testKeys[6]);
            foreach (var testKey in _testKeys)
            {
                DoTest(testKey);
            }
        }

        private void DoTest(TestKey testKey)
        {
            Debug.WriteLine($"\nTesting key {testKey.Name}...");
            Debug.WriteLine("Testing Press");
            for (int i = 0; i < testKey.PressStrokes.Count; i++)
            {
                var stroke = testKey.PressStrokes[i];
                Debug.WriteLine($"Sending stroke #{i+1} with code {stroke.code}, state {stroke.state}");
                var expectedResult = testKey.PressResults[i];
                var actualResult = sch.TranslateScanCode(stroke);
                AssertResult(actualResult, expectedResult);
            }

            Debug.WriteLine("Testing Release");
            for (int i = 0; i < testKey.ReleaseStrokes.Count; i++)
            {
                var stroke = testKey.ReleaseStrokes[i];
                Debug.WriteLine($"Sending stroke #{i+1} with code {stroke.code}, state {stroke.state}");
                var expectedResult = testKey.ReleaseResults[i];
                var actualResult = sch.TranslateScanCode(stroke);
                AssertResult(actualResult, expectedResult);
            }
            Debug.WriteLine("OK!");
        }

        void AssertResult(TranslatedKey actualResult, ExpectedResult expectedResult)
        {
            if (expectedResult == null)
            {
                Debug.WriteLine($"Expecting result of null");
                Assert.That(actualResult == null, "Result should be null");
            }
            else
            {
                Debug.WriteLine($"Expecting code of {expectedResult.Code}, state of {expectedResult.State}");
                Assert.That(actualResult != null, "Result should not be null");
                Assert.That(actualResult.AhkCode, Is.EqualTo(expectedResult.Code), $"Code should be {expectedResult.Code}, got {actualResult.AhkCode}");
                Assert.That(actualResult.State, Is.EqualTo(expectedResult.State), $"State should be {expectedResult.State}, got {actualResult.State}");
            }
        }
    }
}
