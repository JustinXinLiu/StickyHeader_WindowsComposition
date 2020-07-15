using System.Diagnostics;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;

namespace StickyHeader_WindowsComposition
{
    public sealed partial class MainPage : Page
    {
        private float _offsetY;

        public MainPage()
        {
            InitializeComponent();

            Loaded += (s, e) =>
            {
                // Let's first get the offset Y for the main ScrollViewer relatively to the sticky Grid.
                var transform = ((UIElement)MainScroll.Content).TransformToVisual(StickyGrid);
                _offsetY = (float)transform.TransformPoint(new Point(0, 0)).Y;

                // Bring the element to the very front.
                Canvas.SetZIndex(StickyGrid, 1);

                MakeSticky();
            };

            MainScroll.ViewChanged += (s, e) =>
            {

                if (MainScroll.VerticalOffset > -_offsetY)
                {
                    StickyGrid.Opacity = 0.5;
                }
                else
                {
                    StickyGrid.Opacity = 1;
                }

                Debug.WriteLine(MainScroll.VerticalOffset);
            };
        }

        private void MakeSticky()
        {
            // Get Composition variables.
            var scrollProperties = ElementCompositionPreview.GetScrollViewerManipulationPropertySet(MainScroll);
            var stickyGridVisual = ElementCompositionPreview.GetElementVisual(StickyGrid);
            var compositor = scrollProperties.Compositor;

            // Basically, what the expression 
            // "ScrollingProperties.Translation.Y > OffsetY ? 0 : OffsetY - ScrollingProperties.Translation.Y"
            // means is that -
            // When ScrollingProperties.Translation.Y > OffsetY, it means the scroller has yet to scroll to the sticky Grid, so
            // at this time we don't want to do anything, hence the return of 0;
            // when the expression becomes false, we need to offset the the sticky Grid on Y Axis by adding a negative value
            // of ScrollingProperties.Translation.Y. This means the result will forever be just OffsetY after hitting the top.
            var scrollingAnimation = compositor.CreateExpressionAnimation("ScrollingProperties.Translation.Y > OffsetY ? 0 : OffsetY - ScrollingProperties.Translation.Y");
            scrollingAnimation.SetReferenceParameter("ScrollingProperties", scrollProperties);
            scrollingAnimation.SetScalarParameter("OffsetY", _offsetY);

            // Kick off the expression animation.
            ElementCompositionPreview.SetIsTranslationEnabled(StickyGrid, true);
            stickyGridVisual.StartAnimation("Translation.Y", scrollingAnimation);
        }
    }
}
