using AutoHotInterception.Helpers;
using NUnit.Framework;
using System.Collections.Generic;

namespace UnitTests
{
    [TestFixture]
    class TranslateAhkCodeTests
    {
        [Test, TestCaseSource("TestKeyProvider")]
        public void PressRelease(string name, int code, List<ManagedWrapper.Stroke> pressResult, List<ManagedWrapper.Stroke> releaseResult)
        {
            var actualResult = ScanCodeHelper.TranslateAhkCode((ushort)code, 1);
            AssertResults(pressResult, actualResult);
            
            actualResult = ScanCodeHelper.TranslateAhkCode((ushort)code, 0);
            AssertResults(releaseResult, actualResult);
        }

        private void AssertResults(List<ManagedWrapper.Stroke> expectedResult, List<ManagedWrapper.Stroke> actualResult)
        {
            Assert.That(actualResult.Count == expectedResult.Count, $"Expecting {expectedResult.Count} strokes, but got {actualResult.Count}");
            for (int i = 0; i < expectedResult.Count; i++)
            {

                Assert.That(actualResult[i].key.code, Is.EqualTo(expectedResult[i].key.code),
                    $"Code should be {expectedResult[i].key.code}, got {actualResult[i].key.code}");
                Assert.That(actualResult[i].key.state, Is.EqualTo(expectedResult[i].key.state),
                    $"Code should be {expectedResult[i].key.state}, got {actualResult[i].key.state}");
            }
        }

        private static List<ManagedWrapper.Stroke> Result(ushort code1, ushort state1, ushort? code2 = null, ushort? state2 = null)
        {
            var strokes = new List<ManagedWrapper.Stroke>();
            strokes.Add(new ManagedWrapper.Stroke() { key = { code = code1, state = state1 } });
            if (code2 != null)
            {
                strokes.Add(new ManagedWrapper.Stroke() { key = { code = (ushort)code2, state = (ushort)state2 } });
            }
            return strokes;
        }

        private static IEnumerable<TestCaseData> TestKeyProvider()
        {
            yield return new TestCaseData("One", 2, Result(2, 0), Result(2, 1));
            yield return new TestCaseData("Scroll Lock", 70, Result(70, 0), Result(70, 1));

            yield return new TestCaseData("Numpad Enter", 284, Result(28, 0), Result(28, 1));
            yield return new TestCaseData("Right Control", 285, Result(29, 2), Result(29, 3));
            yield return new TestCaseData("Numpad Div", 309, Result(53, 2), Result(53, 3));
            yield return new TestCaseData("Right Shift", 310, Result(54, 0), Result(54, 1));
            yield return new TestCaseData("Print Screen", 311, Result(42, 2, 55, 2), Result(55, 3, 42, 3));
            yield return new TestCaseData("Right Alt", 312, Result(56, 2), Result(56, 3));
            yield return new TestCaseData("Numlock", 325, Result(69, 0), Result(69, 1));
            yield return new TestCaseData("Pause", 69, Result(29, 4, 69, 4), Result(29, 5, 69, 5));
            yield return new TestCaseData("Home", 327, Result(42, 2, 71, 2), Result(71, 3, 42, 3));
            yield return new TestCaseData("Up", 328, Result(42, 2, 72, 2), Result(72, 3, 42, 3));
            yield return new TestCaseData("PgUp", 329, Result(42, 2, 73, 2), Result(73, 3, 42, 3));
            yield return new TestCaseData("Left", 331, Result(42, 2, 75, 2), Result(75, 3, 42, 3));
            yield return new TestCaseData("Right", 333, Result(42, 2, 77, 2), Result(77, 3, 42, 3));
            yield return new TestCaseData("End", 335, Result(42, 2, 79, 2), Result(79, 3, 42, 3));
            yield return new TestCaseData("Down", 336, Result(42, 2, 80, 2), Result(80, 3, 42, 3));
            yield return new TestCaseData("PgDn", 337, Result(42, 2, 81, 2), Result(81, 3, 42, 3));
            yield return new TestCaseData("PgDn", 338, Result(42, 2, 82, 2), Result(82, 3, 42, 3));
            yield return new TestCaseData("Delete", 339, Result(42, 2, 83, 2), Result(83, 3, 42, 3));
            yield return new TestCaseData("Left Win", 347, Result(91, 2), Result(91, 3));
            yield return new TestCaseData("Right Win", 348, Result(92, 2), Result(92, 3));
            yield return new TestCaseData("Apps", 349, Result(93, 2), Result(93, 3));
        }
    }

}
