using System;
using System.Windows.Controls;
using System.Windows;

namespace Mosey.Models.Controls
{
    #region Custom DependencyProperties
    public class Padding
    {
        public static readonly DependencyProperty LeftProperty = DependencyProperty.RegisterAttached(
            "Left",
            typeof(double),
            typeof(Padding),
            new PropertyMetadata(0.0, LeftChanged));

        private static void LeftChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var contentControl = d as ContentControl;
            if (contentControl != null)
            {
                Thickness currentPadding = contentControl.Padding;
                contentControl.Padding = new Thickness((double)e.NewValue, currentPadding.Top, currentPadding.Right, currentPadding.Bottom);
            }
        }

        public static void SetLeft(UIElement element, double value)
        {
            element.SetValue(LeftProperty, value);
        }

        public static double GetLeft(UIElement element)
        {
            return 0;
        }

        public static readonly DependencyProperty TopProperty = DependencyProperty.RegisterAttached(
            "Top",
            typeof(double),
            typeof(Padding),
            new PropertyMetadata(0.0, TopChanged));

        private static void TopChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var contentControl = d as ContentControl;
            if (contentControl != null)
            {
                Thickness currentPadding = contentControl.Padding;
                contentControl.Padding = new Thickness(currentPadding.Left, (double)e.NewValue, currentPadding.Right, currentPadding.Bottom);
            }
        }

        public static void SetTop(UIElement element, double value)
        {
            element.SetValue(TopProperty, value);
        }

        public static double GetTop(UIElement element)
        {
            return 0;
        }

        public static readonly DependencyProperty RightProperty = DependencyProperty.RegisterAttached(
            "Right",
            typeof(double),
            typeof(Padding),
            new PropertyMetadata(0.0, RightChanged));

        private static void RightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var contentControl = d as ContentControl;
            if (contentControl != null)
            {
                Thickness currentPadding = contentControl.Padding;
                contentControl.Padding = new Thickness(currentPadding.Left, currentPadding.Top, (double)e.NewValue, currentPadding.Bottom);
            }
        }

        public static void SetRight(UIElement element, double value)
        {
            element.SetValue(RightProperty, value);
        }

        public static double GetRight(UIElement element)
        {
            return 0;
        }

        public static readonly DependencyProperty BottomProperty = DependencyProperty.RegisterAttached(
            "Bottom",
            typeof(double),
            typeof(Padding),
            new PropertyMetadata(0.0, BottomChanged));

        private static void BottomChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var contentControl = d as ContentControl;
            if (contentControl != null)
            {
                Thickness currentPadding = contentControl.Padding;
                contentControl.Padding = new Thickness(currentPadding.Left, currentPadding.Top, currentPadding.Right, (double)e.NewValue);
            }
        }

        public static void SetBottom(UIElement element, double value)
        {
            element.SetValue(BottomProperty, value);
        }

        public static double GetBottom(UIElement element)
        {
            return 0;
        }
    }

    public class Margin
    {
        public static readonly DependencyProperty LeftProperty = DependencyProperty.RegisterAttached(
            "Left",
            typeof(double),
            typeof(Margin),
            new PropertyMetadata(0.0, LeftChanged));

        private static void LeftChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var frameworkElement = d as FrameworkElement;
            if (frameworkElement != null)
            {
                Thickness currentMargin = frameworkElement.Margin;
                frameworkElement.Margin = new Thickness((double)e.NewValue, currentMargin.Top, currentMargin.Right, currentMargin.Bottom);
            }
        }

        public static void SetLeft(UIElement element, double value)
        {
            element.SetValue(LeftProperty, value);
        }

        public static double GetLeft(UIElement element)
        {
            return 0;
        }

        public static readonly DependencyProperty TopProperty = DependencyProperty.RegisterAttached(
            "Top",
            typeof(double),
            typeof(Margin),
            new PropertyMetadata(0.0, TopChanged));

        private static void TopChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var frameworkElement = d as FrameworkElement;
            if (frameworkElement != null)
            {
                Thickness currentMargin = frameworkElement.Margin;
                frameworkElement.Margin = new Thickness(currentMargin.Left, (double)e.NewValue, currentMargin.Right, currentMargin.Bottom);
            }
        }

        public static void SetTop(UIElement element, double value)
        {
            element.SetValue(TopProperty, value);
        }

        public static double GetTop(UIElement element)
        {
            return 0;
        }

        public static readonly DependencyProperty RightProperty = DependencyProperty.RegisterAttached(
            "Right",
            typeof(double),
            typeof(Margin),
            new PropertyMetadata(0.0, RightChanged));

        private static void RightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var frameworkElement = d as FrameworkElement;
            if (frameworkElement != null)
            {
                Thickness currentMargin = frameworkElement.Margin;
                frameworkElement.Margin = new Thickness(currentMargin.Left, currentMargin.Top, (double)e.NewValue, currentMargin.Bottom);
            }
        }

        public static void SetRight(UIElement element, double value)
        {
            element.SetValue(RightProperty, value);
        }

        public static double GetRight(UIElement element)
        {
            return 0;
        }

        public static readonly DependencyProperty BottomProperty = DependencyProperty.RegisterAttached(
            "Bottom",
            typeof(double),
            typeof(Margin),
            new PropertyMetadata(0.0, BottomChanged));

        private static void BottomChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var frameworkElement = d as FrameworkElement;
            if (frameworkElement != null)
            {
                Thickness currentMargin = frameworkElement.Margin;
                frameworkElement.Margin = new Thickness(currentMargin.Left, currentMargin.Top, currentMargin.Right, (double)e.NewValue);
            }
        }

        public static void SetBottom(UIElement element, double value)
        {
            element.SetValue(BottomProperty, value);
        }

        public static double GetBottom(UIElement element)
        {
            return 0;
        }
    }
    #endregion Custom DependencyProperties

    #region LabelValueGrid
    /// <summary>
    /// Provides an automatic two column <c>Grid</c> without having to manually specify column definitions
    /// A list of controls will be alternated to the two columns
    /// </summary>
    public class LabelValueGrid : Grid
    {
        public LabelValueGrid()
            : base()
        {
            ColumnDefinitions.Add(new ColumnDefinition());
            ColumnDefinitions.Add(new ColumnDefinition());
            ColumnDefinitions[0].Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Auto);
            ColumnDefinitions[1].Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star);
        }

        protected override void OnVisualChildrenChanged(System.Windows.DependencyObject visualAdded, System.Windows.DependencyObject visualRemoved)
        {
            base.OnVisualChildrenChanged(visualAdded, visualRemoved);

            int curRow = -1;
            int curCol = 1;

            RowDefinitions.Clear();

            if (Children != null)
                foreach (System.Windows.UIElement curChild in Children)
                {
                    if (curCol == 0)
                        curCol = 1;
                    else
                    {
                        curCol = 0;
                        curRow++;
                        RowDefinitions.Add(new RowDefinition() { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Auto) });
                    }

                    Grid.SetRow(curChild, curRow);
                    Grid.SetColumn(curChild, curCol);
                }

            RowDefinitions.Add(new RowDefinition() { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        }
    }
    #endregion LabelValueGrid

    #region FlexPanel
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
            var desiredSize = new Size();
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

            if (double.IsPositiveInfinity(availableSize.Height) || double.IsPositiveInfinity(availableSize.Width)) return desiredSize;
            else return availableSize;
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
                    if (GetFlex(child)) flexChildrenWeightParts += GetFlexWeight(child);
                    else totalLength += child.DesiredSize.Height;
                }

                var flexSize = Math.Max(0, (finalSize.Height - totalLength) / flexChildrenWeightParts);

                foreach (UIElement child in Children)
                {
                    var arrangeRect = new Rect();
                    if (GetFlex(child)) arrangeRect = new Rect(0, currentLength, finalSize.Width, flexSize * GetFlexWeight(child));
                    else arrangeRect = new Rect(0, currentLength, finalSize.Width, child.DesiredSize.Height);

                    child.Arrange(arrangeRect);
                    currentLength += arrangeRect.Height;
                }
            }
            else
            {
                foreach (UIElement child in Children)
                {
                    if (GetFlex(child)) flexChildrenWeightParts += GetFlexWeight(child);
                    else totalLength += child.DesiredSize.Width;
                }

                var flexSize = Math.Max(0, (finalSize.Width - totalLength) / flexChildrenWeightParts);

                foreach (UIElement child in Children)
                {
                    var arrangeRect = new Rect();
                    if (GetFlex(child)) arrangeRect = new Rect(currentLength, 0, flexSize * GetFlexWeight(child), finalSize.Height);
                    else arrangeRect = new Rect(currentLength, 0, child.DesiredSize.Width, finalSize.Height);

                    child.Arrange(arrangeRect);
                    currentLength += arrangeRect.Width;
                }
            }

            return finalSize;
        }
    }
    #endregion FlexPanel
}

