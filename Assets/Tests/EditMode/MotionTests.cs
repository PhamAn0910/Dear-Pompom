using System;
using NUnit.Framework;

namespace DearPompom.Core.Tests
{
    public class EaseTests
    {
        [Test]
        public void EveryCurve_StartsAtZero_EndsAtOne_AndClampsInput()
        {
            foreach (EaseType type in Enum.GetValues(typeof(EaseType)))
            {
                Assert.AreEqual(0f, Ease.Evaluate(type, 0f), type.ToString());
                Assert.AreEqual(1f, Ease.Evaluate(type, 1f), type.ToString());
                Assert.AreEqual(0f, Ease.Evaluate(type, -3f), type.ToString());
                Assert.AreEqual(1f, Ease.Evaluate(type, 7f), type.ToString());
                Assert.AreEqual(0f, Ease.Evaluate(type, float.NaN), type.ToString());
            }
        }

        [Test]
        public void OutCurves_AreAheadOfLinear_InCurvesAreBehind()
        {
            Assert.Greater(Ease.Evaluate(EaseType.OutCubic, 0.3f), 0.3f);
            Assert.Greater(Ease.Evaluate(EaseType.OutQuad, 0.3f), 0.3f);
            Assert.Less(Ease.Evaluate(EaseType.InCubic, 0.3f), 0.3f);
            Assert.AreEqual(0.5f, Ease.Evaluate(EaseType.InOutSine, 0.5f), 1e-5f);
            Assert.AreEqual(0.5f, Ease.Evaluate(EaseType.InOutCubic, 0.5f), 1e-5f);
        }

        [Test]
        public void MonotonicCurves_NeverGoBackwards()
        {
            EaseType[] monotonic =
            {
                EaseType.Linear, EaseType.InSine, EaseType.OutSine, EaseType.InOutSine, EaseType.InQuad,
                EaseType.OutQuad, EaseType.InOutQuad, EaseType.InCubic, EaseType.OutCubic,
                EaseType.InOutCubic, EaseType.OutQuart,
            };
            foreach (EaseType type in monotonic)
            {
                float previous = 0f;
                for (int i = 1; i <= 100; i++)
                {
                    float value = Ease.Evaluate(type, i / 100f);
                    Assert.GreaterOrEqual(value, previous - 1e-6f, type + " at " + i);
                    previous = value;
                }
            }
        }

        [Test]
        public void OutBack_OvershootsByAboutTenPercent()
        {
            float peak = 0f;
            for (int i = 0; i <= 100; i++) peak = Math.Max(peak, Ease.Evaluate(EaseType.OutBack, i / 100f));
            Assert.Greater(peak, 1.05f);
            Assert.Less(peak, 1.15f);
            Assert.AreEqual(12f, Ease.Lerp(10f, 20f, 0.2f), 1e-5f);
            Assert.AreEqual(21f, Ease.Lerp(10f, 20f, 1.1f), 1e-5f); // unclamped on purpose
        }
    }

    public class SpringTests
    {
        private static Spring Run(float seconds, int steps, float damping)
        {
            var spring = new Spring(0f);
            for (int i = 0; i < steps; i++) spring.Step(1f, 3f, damping, seconds / steps);
            return spring;
        }

        [Test]
        public void Result_DoesNotDependOnFrameRate()
        {
            foreach (float damping in new[] { 1f, 0.5f })
            {
                Spring one = Run(0.4f, 1, damping), sixty = Run(0.4f, 24, damping), slow = Run(0.4f, 4, damping);
                Assert.AreEqual(one.Value, sixty.Value, 1e-3f);
                Assert.AreEqual(one.Value, slow.Value, 1e-3f);
                Assert.AreEqual(one.Velocity, sixty.Velocity, 1e-2f);
            }
        }

        [Test]
        public void CriticalDamping_NeverOvershoots_UnderDampingDoes()
        {
            var critical = new Spring(0f);
            var bouncy = new Spring(0f);
            float criticalPeak = 0f, bouncyPeak = 0f;
            for (int i = 0; i < 300; i++)
            {
                critical.Step(1f, 3f, 1f, 1f / 60f);
                bouncy.Step(1f, 3f, 0.4f, 1f / 60f);
                criticalPeak = Math.Max(criticalPeak, critical.Value);
                bouncyPeak = Math.Max(bouncyPeak, bouncy.Value);
            }
            Assert.LessOrEqual(criticalPeak, 1.0001f);
            Assert.Greater(bouncyPeak, 1.1f);
            Assert.IsTrue(critical.IsSettled(1f));
            Assert.IsTrue(bouncy.IsSettled(1f, 0.01f));
        }

        [Test]
        public void AHugeFrame_LandsOnTheTarget_InsteadOfExploding()
        {
            var spring = new Spring(0f);
            spring.Velocity = 50f;
            spring.Step(1f, 4f, 0.5f, 30f); // tab was in the background for 30 seconds
            Assert.AreEqual(1f, spring.Value, 1e-3f);
            Assert.AreEqual(0f, spring.Velocity, 1e-3f);
        }

        [Test]
        public void ZeroOrNegativeDelta_ChangesNothing()
        {
            var spring = new Spring(0.25f);
            spring.Step(1f, 3f, 1f, 0f);
            spring.Step(1f, 3f, 1f, -1f);
            spring.Step(1f, 3f, 1f, float.NaN);
            Assert.AreEqual(0.25f, spring.Value);
        }

        [Test]
        public void Damp_HalvesTheDistanceEveryHalfLife_AtAnyFrameRate()
        {
            Assert.AreEqual(5f, Smooth.Damp(0f, 10f, 0.2f, 0.2f), 1e-4f);
            float stepped = 0f;
            for (int i = 0; i < 12; i++) stepped = Smooth.Damp(stepped, 10f, 0.2f, 0.2f / 12f);
            Assert.AreEqual(5f, stepped, 1e-3f);
            Assert.AreEqual(10f, Smooth.Damp(0f, 10f, 0f, 0.016f)); // no half-life: snap
            Assert.AreEqual(3f, Smooth.Damp(3f, 10f, 0.2f, 0f));
        }
    }

    public class TweenStateTests
    {
        [Test]
        public void WaitsForTheDelay_ThenRuns_ThenCompletesExactlyAtTheEnd()
        {
            var tween = new TweenState(1f, EaseType.Linear, delay: 0.5f);
            Assert.AreEqual(0f, tween.Advance(0.25f));
            Assert.AreEqual(0f, tween.Advance(0.25f));
            Assert.AreEqual(0.5f, tween.Advance(0.5f), 1e-5f);
            Assert.IsFalse(tween.IsComplete);
            Assert.AreEqual(1f, tween.Advance(5f));
            Assert.IsTrue(tween.IsComplete);
            Assert.AreEqual(1f, tween.Advance(1f)); // further calls are harmless
        }

        [Test]
        public void ZeroDuration_CompletesOnTheFirstAdvance()
        {
            var tween = new TweenState(0f, EaseType.OutBack);
            Assert.AreEqual(1f, tween.Advance(0.016f));
            Assert.IsTrue(tween.IsComplete);
        }

        [Test]
        public void PingPongOnce_GoesThereAndBack()
        {
            var tween = new TweenState(1f, EaseType.Linear, repeats: 1, pingPong: true);
            Assert.AreEqual(0.75f, tween.Advance(0.75f), 1e-5f);
            Assert.AreEqual(0.75f, tween.Advance(0.5f), 1e-5f); // 0.25 s into the way back
            Assert.IsFalse(tween.IsComplete);
            Assert.AreEqual(0f, tween.Advance(1f), 1e-5f);
            Assert.IsTrue(tween.IsComplete);
        }

        [Test]
        public void EndlessTween_SurvivesAVeryLongFrame()
        {
            var tween = new TweenState(2f, EaseType.Linear, repeats: -1, pingPong: true);
            float value = tween.Advance(3600f * 5 + 0.5f); // 5 hours in a background tab
            Assert.IsFalse(tween.IsComplete);
            Assert.AreEqual(0.25f, value, 1e-2f); // an even number of plays passed, 0.5 s into the next
        }

        [Test]
        public void Finish_JumpsToTheRestingValue()
        {
            var plain = new TweenState(1f, EaseType.OutCubic);
            plain.Advance(0.1f);
            plain.Finish();
            Assert.AreEqual(1f, plain.Progress);

            var punch = new TweenState(1f, EaseType.OutQuad, repeats: 1, pingPong: true);
            punch.Advance(0.3f);
            punch.Finish();
            Assert.AreEqual(0f, punch.Progress); // a there-and-back tween rests where it started
            Assert.IsTrue(punch.IsComplete);
        }
    }

    public class FlipbookTests
    {
        [Test]
        public void LoopingClip_Wraps()
        {
            Assert.AreEqual(0, Flipbook.FrameAt(0, 12f, 24, true));
            Assert.AreEqual(1, Flipbook.FrameAt(1 / 12.0, 12f, 24, true));
            Assert.AreEqual(23, Flipbook.FrameAt(23.5 / 12.0, 12f, 24, true));
            Assert.AreEqual(0, Flipbook.FrameAt(2.0, 12f, 24, true));
            Assert.AreEqual(6, Flipbook.FrameAt(3600 * 3 + 0.5, 12f, 24, true));
            Assert.IsFalse(Flipbook.IsFinished(99999, 12f, 24, true));
        }

        [Test]
        public void OneShotClip_HoldsTheLastFrame()
        {
            Assert.AreEqual(10, Flipbook.FrameAt(10.2 / 12.0, 12f, 11, false));
            Assert.AreEqual(10, Flipbook.FrameAt(500, 12f, 11, false));
            Assert.IsFalse(Flipbook.IsFinished(10.5 / 12.0, 12f, 11, false));
            Assert.IsTrue(Flipbook.IsFinished(11 / 12.0, 12f, 11, false));
        }

        [Test]
        public void BadInput_ReturnsFrameZero()
        {
            Assert.AreEqual(0, Flipbook.FrameAt(5, 0f, 10, true));
            Assert.AreEqual(0, Flipbook.FrameAt(-1, 12f, 10, true));
            Assert.AreEqual(0, Flipbook.FrameAt(5, 12f, 0, true));
            Assert.AreEqual(0, Flipbook.FrameAt(double.NaN, 12f, 10, false));
        }
    }

    public class CountdownTests
    {
        [Test]
        public void CountsAgainstTheClock_NotAgainstFrames()
        {
            var clock = new FakeClock(2026, 11, 16);
            var timer = new Countdown(clock);
            Assert.AreEqual("00:00", timer.Format());
            timer.Start(TimeSpan.FromMinutes(25));
            Assert.AreEqual("25:00", timer.Format());
            clock.Advance(TimeSpan.FromSeconds(0.4));
            Assert.AreEqual("25:00", timer.Format()); // rounds up: 24:59.6 still reads 25:00
            clock.Advance(TimeSpan.FromSeconds(0.7));
            Assert.AreEqual("24:59", timer.Format());
            clock.AdvanceMinutes(10); // e.g. the tab was hidden: no frames, the clock still moved
            Assert.AreEqual(10.0, timer.Elapsed.TotalMinutes, 0.05);
            Assert.AreEqual(0.4f, timer.Fraction, 0.01f);
            Assert.IsFalse(timer.IsDue);
            clock.AdvanceMinutes(60);
            Assert.IsTrue(timer.IsDue);
            Assert.AreEqual(TimeSpan.Zero, timer.Remaining);
            Assert.AreEqual(25.0, timer.Elapsed.TotalMinutes, 1e-6); // never more than the target
            Assert.AreEqual("00:00", timer.Format());
        }

        [Test]
        public void Pause_FreezesTheRemainingTime()
        {
            var clock = new FakeClock(2026, 11, 16);
            var timer = new Countdown(clock);
            timer.Start(TimeSpan.FromMinutes(5));
            clock.AdvanceMinutes(2);
            timer.Pause();
            clock.AdvanceMinutes(30);
            Assert.AreEqual(3.0, timer.Remaining.TotalMinutes, 1e-6);
            Assert.IsFalse(timer.IsDue);
            timer.Resume();
            clock.AdvanceMinutes(1);
            Assert.AreEqual(2.0, timer.Remaining.TotalMinutes, 1e-6);
            Assert.AreEqual(3.0, timer.Elapsed.TotalMinutes, 1e-6);
            timer.Stop();
            Assert.IsFalse(timer.IsRunning);
            Assert.AreEqual(TimeSpan.Zero, timer.Elapsed);
        }
    }
}

namespace DearPompom.Core.Tests
{
    public class PatchClipDataTests
    {
        private static PatchClipData TwoFrameClip()
        {
            var clip = new PatchClipData { name = "t", mode = "patch", width = 1920, height = 1080, background = "t_base" };
            clip.sheets.Add("t_sheet0");
            clip.regions.Add(new PatchRect { x = 0, y = 0, w = 64, h = 64 });        // top-left tile
            clip.regions.Add(new PatchRect { x = 1856, y = 1016, w = 64, h = 64 });  // bottom-right tile
            clip.patches.Add(new PatchSource { sheet = 0, x = 2, y = 2, w = 64, h = 64 });
            clip.patches.Add(new PatchSource { sheet = 0, x = 70, y = 2, w = 64, h = 64 });
            var first = new PatchFrame();
            first.draw.Add(new PatchDraw { region = 1, patch = 0 });
            var second = new PatchFrame();
            second.draw.Add(new PatchDraw { region = 0, patch = 1 });
            second.draw.Add(new PatchDraw { region = 1, patch = 1 });
            clip.frames.Add(first);
            clip.frames.Add(second);
            return clip;
        }

        [Test]
        public void FrameTable_HasMinusOneWhereTheBackgroundShows()
        {
            PatchClipData clip = TwoFrameClip();
            Assert.IsNull(clip.Validate());
            int[][] table = clip.BuildFrameTable();
            Assert.AreEqual(new[] { -1, 0 }, table[0]);
            Assert.AreEqual(new[] { 1, 1 }, table[1]);
        }

        [Test]
        public void Coordinates_FlipYBetweenImageAndUnity()
        {
            PatchClipData clip = TwoFrameClip();
            float x, y, w, h;
            clip.RegionCentre(0, 100f, out x, out y);
            Assert.AreEqual(-9.28f, x, 1e-4f); // left of centre
            Assert.AreEqual(5.08f, y, 1e-4f);  // top of the image is +y in Unity
            clip.RegionCentre(1, 100f, out x, out y);
            Assert.AreEqual(9.28f, x, 1e-4f);
            Assert.AreEqual(-5.08f, y, 1e-4f);
            clip.PatchTextureRect(0, 2048, out x, out y, out w, out h);
            Assert.AreEqual(2f, x);
            Assert.AreEqual(2048 - 2 - 64, y); // 2 px from the top of the sheet = 1982 px from the bottom
            Assert.AreEqual(64f, w);
            Assert.AreEqual(64f, h);
        }

        [Test]
        public void Validate_NamesTheProblem()
        {
            PatchClipData clip = TwoFrameClip();
            clip.frames[1].draw[0].patch = 9;
            StringAssert.Contains("frame 1", clip.Validate());
            clip = TwoFrameClip();
            clip.background = "";
            StringAssert.Contains("background", clip.Validate());
            clip = TwoFrameClip();
            clip.patches[0].sheet = 3;
            StringAssert.Contains("sheet", clip.Validate());
            Assert.IsNotNull(new PatchClipData().Validate());
        }
    }
}
