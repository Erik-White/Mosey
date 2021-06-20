using System;
using System.Windows;
using System.Windows.Controls;

namespace Mosey.GUI.Views.Controls
{
    /// <summary>
    /// A control based on a CSS FlexBox <see cref="https://www.w3schools.com/css/css3_flexbox.asp" />
    /// </summary>
    public class FlexPanel : Panel
    {
        public static bool GetFlex(DependencyObject obj) => (bool)obj.GetValue(FlexProperty);
        public static void SetFlex(DependencyObject obj, bool value) => obj.SetValue(FlexProperty, value);
        public static readonly DependencyProperty FlexProperty = DependencyProperty.RegisterAttached("Flex", typeof(bool), typeof(FlexPanel), new PropertyMetadata(false));

        public static int GetFlexWeight(DependencyObject obj) => (int)obj.GetValue(FlexWeightProperty);
        public static void SetFlexWeight(DependencyObject obj, int value) => obj.SetValue(FlexWeightProperty, value);
        public static readonly DependencyProperty FlexWeightProperty = DependencyProperty.RegisterAttached("FlexWeight", typeof(int), typeof(FlexPanel), new PropertyMetadata(1));

        public Orientation Orientation
        {
            get => (Orientation)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }
        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(nameof(Orientation), typeof(Orientation), typeof(FlexPanel), new PropertyMetadata(Orientation.Vertical));

        protected override Size MeasureOverride(Size availableSize)
        {
            Size desiredSize = new Size();
            foreach (UIElement child in Children)
            {
                child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

                if (Orientation == Orientation.Vertical)
                {
                    desiredSize.Height += child.DesiredSize.Height;
                    desiredSize.Width = Math.Max(desiredSize.Width, child.DesiredSize.Width);
                }
                else
                {
                    desiredSize.Width += child.DesiredSize.Width;
                    desiredSize.Height = Math.Max(desiredSize.Height, child.DesiredSize.Height);
                }
            }

            if (double.IsPositiveInfinity(availableSize.Height) || double.IsPositiveInfinity(availableSize.Width))
            {
                return desiredSize;
            }
            else
            {
                return availableSize;
            }
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var currentLength = 0d;
            var totalLength = 0d;
            var flexChildrenWeightParts = 0;

            if (Orientation == Orientation.Vertical)
            {
                foreach (UIElement child in Children)
                {
                    if (GetFlex(child))
                    {
                        flexChildrenWeightParts += GetFlexWeight(child);
                    }
                    else
                    {
                        totalLength += child.DesiredSize.Height;
                    }
                }

                var flexSize = Math.Max(0, (finalSize.Height - totalLength) / flexChildrenWeightParts);

                foreach (UIElement child in Children)
                {
                    Rect arrangeRect = new Rect();
                    if (GetFlex(child))
                    {
                        arrangeRect = new Rect(0, currentLength, finalSize.Width, flexSize * GetFlexWeight(child));
                    }
                    else
                    {
                        arrangeRect = new Rect(0, currentLength, finalSize.Width, child.DesiredSize.Height);
                    }

                    child.Arrange(arrangeRect);
                    currentLength += arrangeRect.Height;
                }
            }
            else
            {
                foreach (UIElement child in Children)
                {
                    if (GetFlex(child))
                    {
                        flexChildrenWeightParts += GetFlexWeight(child);
                    }
                    else
                    {
                        totalLength += child.DesiredSize.Width;
                    }
                }

                var flexSize = Math.Max(0, (finalSize.Width - totalLength) / flexChildrenWeightParts);

                foreach (UIElement child in Children)
                {
                    Rect arrangeRect = new Rect();
                    if (GetFlex(child))
                    {
                        arrangeRect = new Rect(currentLength, 0, flexSize * GetFlexWeight(child), finalSize.Height);
                    }
                    else
                    {
                        arrangeRect = new Rect(currentLength, 0, child.DesiredSize.Width, finalSize.Height);
                    }

                    child.Arrange(arrangeRect);
                    currentLength += arrangeRect.Width;
                }
            }

            return finalSize;
        }
    }
}
