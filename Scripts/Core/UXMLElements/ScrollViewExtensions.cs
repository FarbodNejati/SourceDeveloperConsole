using UnityEngine.UIElements;

namespace Farbod.DeveloperConsole
{
    public static class ScrollViewExtensions
    {
        public static bool IsScrolledToBottom(this ScrollView scrollView)
        {
            // Get the vertical scroller
            var verticalScroller = scrollView.verticalScroller;
            if (verticalScroller == null)
                return true; // If no scroller, we're at bottom by default

            // Check if we're at or very near the bottom (with small tolerance for floating point)
            float tolerance = 1f;
            return verticalScroller.value >= verticalScroller.highValue - tolerance;
        }

        public static void ScrollToBottom(this ScrollView scrollView)
        {
            scrollView.schedule.Execute(() =>
            {
                var scroller = scrollView.verticalScroller;
                if (scroller != null)
                {
                    scroller.value = scroller.highValue > 0 ? scroller.highValue : 0;
                }
            }).StartingIn(0);
        }

    }
}

