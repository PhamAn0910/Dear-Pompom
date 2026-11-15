using System;
using NUnit.Framework;

namespace DearPompom.Core.Tests
{
    public class EventBusTests
    {
        [Test]
        public void Publish_ReachesSubscribersOfThatTypeOnly()
        {
            var bus = new EventBus();
            int chapters = 0, activities = 0;
            bus.Subscribe<ChapterUnlocked>(e => chapters += e.Chapter);
            bus.Subscribe<ActivityRecorded>(e => activities++);
            bus.Publish(new ChapterUnlocked(2));
            Assert.AreEqual(2, chapters);
            Assert.AreEqual(0, activities);
        }

        [Test]
        public void Unsubscribe_StopsDelivery_AndDoubleSubscribeIsIgnored()
        {
            var bus = new EventBus();
            int n = 0;
            Action<ChapterUnlocked> handler = e => n++;
            bus.Subscribe(handler);
            bus.Subscribe(handler);
            bus.Publish(new ChapterUnlocked(2));
            Assert.AreEqual(1, n);
            bus.Unsubscribe(handler);
            bus.Publish(new ChapterUnlocked(3));
            Assert.AreEqual(1, n);
        }

        [Test]
        public void AThrowingHandler_DoesNotBlockTheOthers()
        {
            var bus = new EventBus();
            Exception reported = null;
            bool reached = false;
            bus.OnHandlerError = e => reported = e;
            bus.Subscribe<ChapterUnlocked>(e => { throw new InvalidOperationException("boom"); });
            bus.Subscribe<ChapterUnlocked>(e => reached = true);
            bus.Publish(new ChapterUnlocked(2));
            Assert.IsTrue(reached);
            Assert.IsNotNull(reported);
        }

        [Test]
        public void HandlersMayUnsubscribeWhileBeingCalled()
        {
            var bus = new EventBus();
            int n = 0;
            Action<ChapterUnlocked> once = null;
            once = e => { n++; bus.Unsubscribe(once); };
            bus.Subscribe(once);
            bus.Publish(new ChapterUnlocked(2));
            bus.Publish(new ChapterUnlocked(3));
            Assert.AreEqual(1, n);
        }
    }
}
