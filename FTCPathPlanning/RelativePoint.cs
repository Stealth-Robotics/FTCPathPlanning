using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows;
using System.Windows.Data;
using System.Windows.Controls;

namespace FTCPathPlanning
{
    public class PositionChangedEventArgs : EventArgs
    {
        public double NewX;
        public double NewY;
    }
    public delegate void PositionChangedEventHandler(object sender, PositionChangedEventArgs e);

    public class RelativePoint : Shape
    {
        EllipseGeometry source;

        public static readonly DependencyProperty RadiusProperty =
            DependencyProperty.Register("Diameter", typeof(double), typeof(RelativePoint));

        public double Diameter
        {
            get { return (double)GetValue(RadiusProperty); }
            set { SetValue(RadiusProperty, value); }
        }

        // Using a DependencyProperty as the backing store for XMin.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty XMinProperty =
            DependencyProperty.Register("XMin", typeof(double), typeof(RelativePoint), new PropertyMetadata(-10.0, LayoutChanged));

        public double XMin
        {
            get { return (double)GetValue(XMinProperty); }
            set { SetValue(XMinProperty, value); }
        }

        // Using a DependencyProperty as the backing store for XMax.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty XMaxProperty =
            DependencyProperty.Register("XMax", typeof(double), typeof(RelativePoint), new PropertyMetadata(10.0, LayoutChanged));

        public double XMax
        {
            get { return (double)GetValue(XMaxProperty); }
            set { SetValue(XMaxProperty, value); }
        }

        // Using a DependencyProperty as the backing store for YMin.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty YMinProperty =
            DependencyProperty.Register("YMin", typeof(double), typeof(RelativePoint), new PropertyMetadata(-10.0, LayoutChanged));

        public double YMin
        {
            get { return (double)GetValue(YMinProperty); }
            set { SetValue(YMinProperty, value); }
        }

        // Using a DependencyProperty as the backing store for YMax.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty YMaxProperty =
            DependencyProperty.Register("YMax", typeof(double), typeof(RelativePoint), new PropertyMetadata(10.0, LayoutChanged));

        public double YMax
        {
            get { return (double)GetValue(YMaxProperty); }
            set { SetValue(YMaxProperty, value); }
        }

        // Using a DependencyProperty as the backing store for XPosition.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty XPositionProperty =
            DependencyProperty.Register("XPosition", typeof(double), typeof(RelativePoint), new PropertyMetadata(0.0, LayoutChanged));

        public double XPosition
        {
            get { return (double)GetValue(XPositionProperty); }
            set
            {
                if(XMin <= value && value <= XMax)
                    SetValue(XPositionProperty, value);
            }
        }

        // Using a DependencyProperty as the backing store for YPosition.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty YPositionProperty =
            DependencyProperty.Register("YPosition", typeof(double), typeof(RelativePoint), new PropertyMetadata(0.0, LayoutChanged));

        public double YPosition
        {
            get { return (double)GetValue(YPositionProperty); }
            set
            {
                if(YMin <= value && value <= YMax)
                    SetValue(YPositionProperty, value);
            }
        }

        // Using a DependencyProperty as the backing store for CanvasWidth.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CanvasWidthProperty =
            DependencyProperty.Register("CanvasWidth", typeof(double), typeof(RelativePoint), new PropertyMetadata(1.0, LayoutChanged));

        public double CanvasWidth
        {
            get { return (double)GetValue(CanvasWidthProperty); }
            set { SetValue(CanvasWidthProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CanvasHeight.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CanvasHeightProperty =
            DependencyProperty.Register("CanvasHeight", typeof(double), typeof(RelativePoint), new PropertyMetadata(1.0, LayoutChanged));

        public double CanvasHeight
        {
            get { return (double)GetValue(CanvasHeightProperty); }
            set { SetValue(CanvasHeightProperty, value); }
        }
        
        private Point getPixelLocation()
        {
            double xPos = (XPosition - XMin) / (XMax - XMin) * CanvasWidth;
            double yPos = (-YPosition - YMin) / (YMax - YMin) * CanvasHeight;
            return new Point(xPos, yPos);
        }

        private static void LayoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RelativePoint me = d as RelativePoint;
            Point location = me.getPixelLocation();
            Canvas.SetLeft(me, location.X);
            Canvas.SetTop(me, location.Y);

            me.PositionChanged?.Invoke(me, new PositionChangedEventArgs() { NewX = me.XPosition, NewY = me.YPosition });
        }


        public event PositionChangedEventHandler PositionChanged;

        public RelativePoint()
        {
            source = new EllipseGeometry();

            Binding heightBinder = new Binding("Diameter");
            heightBinder.Mode = BindingMode.TwoWay;
            heightBinder.Source = this;
            SetBinding(HeightProperty, heightBinder);

            Binding widthBinder = new Binding("Diameter");
            widthBinder.Mode = BindingMode.TwoWay;
            widthBinder.Source = this;
            SetBinding(WidthProperty, widthBinder);

            Binding colorBinder = new Binding("Fill");
            colorBinder.Mode = BindingMode.TwoWay;
            colorBinder.Source = this;
            SetBinding(StrokeProperty, colorBinder);
        }

        protected override Geometry DefiningGeometry
        {
            get
            {
                source.RadiusX = ActualWidth / 2;
                source.RadiusY = ActualHeight / 2;
                return source;
            }
        }
    }
}
