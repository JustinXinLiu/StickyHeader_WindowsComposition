using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;

namespace StickyHeader_WindowsComposition
{
    [Flags]
    public enum VisualPropertyType
    {
        None = 0,
        Opacity = 1 << 0,
        Offset = 1 << 1,
        Scale = 1 << 2,
        Size = 1 << 3,
        RotationAngleInDegrees = 1 << 4,
        All = ~0
    }

    public static class Extensions
    {
        public static IEnumerable<T> GetValues<T>() => Enum.GetValues(typeof(T)).Cast<T>();

        public static void EnableImplicitAnimation(this Visual visual, VisualPropertyType typeToAnimate,
            double duration = 800, double delay = 0, CompositionEasingFunction easing = null)
        {
            var compositor = visual.Compositor;

            var animationCollection = compositor.CreateImplicitAnimationCollection();

            foreach (var type in GetValues<VisualPropertyType>())
            {
                if (!typeToAnimate.HasFlag(type)) continue;

                var animation = CreateAnimationByType(compositor, type, duration, delay, easing);

                if (animation != null)
                {
                    animationCollection[type.ToString()] = animation;
                }
            }

            visual.ImplicitAnimations = animationCollection;
        }

        private static KeyFrameAnimation CreateAnimationByType(Compositor compositor, VisualPropertyType type,
            double duration = 800, double delay = 0, CompositionEasingFunction easing = null)
        {
            KeyFrameAnimation animation;

            switch (type)
            {
                case VisualPropertyType.Offset:
                case VisualPropertyType.Scale:
                    animation = compositor.CreateVector3KeyFrameAnimation();
                    break;
                case VisualPropertyType.Size:
                    animation = compositor.CreateVector2KeyFrameAnimation();
                    break;
                case VisualPropertyType.Opacity:
                case VisualPropertyType.RotationAngleInDegrees:
                    animation = compositor.CreateScalarKeyFrameAnimation();
                    break;
                default:
                    return null;
            }

            animation.InsertExpressionKeyFrame(1.0f, "this.FinalValue", easing);
            animation.Duration = TimeSpan.FromMilliseconds(duration);
            animation.DelayTime = TimeSpan.FromMilliseconds(delay);
            animation.Target = type.ToString();

            return animation;
        }
    }

    public sealed partial class MainPage : Page
    {
        private readonly Compositor _compositor;
        private float _offsetY;

        public MainPage()
        {
            InitializeComponent();

            _compositor = Window.Current.Compositor;

            var stickyHeaderBgVisual = _compositor.CreateSpriteVisual();
            stickyHeaderBgVisual.Brush = _compositor.CreateColorBrush(Colors.Orange);
            //stickyHeaderBgVisual.RelativeSizeAdjustment = Vector2.One;
            ElementCompositionPreview.SetElementChildVisual(StickyGridBackground, stickyHeaderBgVisual);

            StickyGridBackground.SizeChanged += (s, e) =>
            {
                stickyHeaderBgVisual.Size = e.NewSize.ToVector2();
            };

            Loaded += (s, e) =>
            {
                // Let's first get the offset Y for the main ScrollViewer relatively to the sticky Grid.
                var transform = ((UIElement)MainScroll.Content).TransformToVisual(StickyGrid);
                _offsetY = (float)transform.TransformPoint(new Point(0, 0)).Y;

                // Bring the element to the very front.
                Canvas.SetZIndex(StickyGrid, 1);

                MakeSticky();

                stickyHeaderBgVisual.EnableImplicitAnimation(VisualPropertyType.Size, 400);
            };

            MainScroll.ViewChanged += (s, e) =>
            {

                if (MainScroll.VerticalOffset > -_offsetY * 3)
                {
                    Grid.SetRowSpan(StickyGridBackground, 1);
                    Grid.SetColumnSpan(StickyGridBackground, 1);
                }
                else
                {
                    Grid.SetRowSpan(StickyGridBackground, 2);
                    Grid.SetColumnSpan(StickyGridBackground, 2);
                }

                //Debug.WriteLine(MainScroll.VerticalOffset);
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
