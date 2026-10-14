using UnityEngine.UIElements;
using UnityEngine;

namespace Farbod.DeveloperConsole
{
    public static class ScrollViewExtensions
    {
        public static bool IsScrolledToBottom(this ScrollView scrollView)
        {
            //// Get the vertical scroller
            //var verticalScroller = scrollView.verticalScroller;
            //if (verticalScroller == null)
            //    return true; // If no scroller, we're at bottom by default

            //// Check if we're at or very near the bottom (with small tolerance for floating point)
            //float tolerance = 1f;
            //return verticalScroller.value >= verticalScroller.highValue - tolerance;
            return false;
        }

        public static void ScrollToBottom(this ScrollView scrollView)
        {
            scrollView.schedule.Execute(() =>
            {
                // Force geometry recalculation
                var fakeOldRect = Rect.zero;
                var fakeNewRect = scrollView.layout;
                using var evt = GeometryChangedEvent.GetPooled(fakeOldRect, fakeNewRect);
                evt.target = scrollView.contentContainer;
                scrollView.contentContainer.SendEvent(evt);

                // Now the scroller should have the correct highValue
                float maxScroll = scrollView.verticalScroller.highValue;
                if (maxScroll < 0) maxScroll = 0;

                scrollView.scrollOffset = new Vector2(scrollView.scrollOffset.x, maxScroll);
            }).StartingIn(10);
        }

    }
}

