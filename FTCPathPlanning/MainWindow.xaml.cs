using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Xceed.Wpf.Toolkit;
using Microsoft.Win32;
using Xceed.Wpf.Toolkit.PropertyGrid;

namespace FTCPathPlanning
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //on right click move the robot origin. Left click will handle placement of points
            if (e.RightButton == MouseButtonState.Pressed)
            {
                Point pos = e.GetPosition(Plotter);
                Canvas.SetLeft(robot, pos.X);
                Canvas.SetTop(robot, pos.Y);

                originX.Value = pxToFt(pos.X, false);
                originY.Value = pxToFt(pos.Y, true);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RangedPositionEditor.Style = Resources["positionalSpinner"] as Style;
            //Canvas.SetLeft(robot, Plotter.ActualWidth / 2);
            //Canvas.SetTop(robot, Plotter.ActualHeight / 2);
            pointBindToCanvas(robot, Plotter);

            Binding xMin = new Binding("Minimum");
            xMin.Source = originX;
            robot.SetBinding(RelativePoint.XMinProperty, xMin);

            Binding xMax = new Binding("Maximum");
            xMax.Source = originX;
            robot.SetBinding(RelativePoint.XMaxProperty, xMax);

            Binding xCoord = new Binding("Value");
            xCoord.Source = originX;
            robot.SetBinding(RelativePoint.XPositionProperty, xCoord);

            Binding yMin = new Binding("Minimum");
            yMin.Source = originY;
            robot.SetBinding(RelativePoint.YMinProperty, yMin);

            Binding yMax = new Binding("Maximum");
            yMax.Source = originY;
            robot.SetBinding(RelativePoint.YMaxProperty, yMax);

            Binding yCoord = new Binding("Value");
            yCoord.Source = originY;
            robot.SetBinding(RelativePoint.YPositionProperty, yCoord);

            //center the heading arrow
            Canvas.SetLeft(originHeading, Plotter.ActualWidth / 2);
            Canvas.SetTop(originHeading, Plotter.ActualHeight / 2);

            List<Type> pathTypes = new List<Type>();
            pathTypes.Add(typeof(LinearPath));
            pathTypes.Add(typeof(QuadraticPath));
            Paths.NewItemTypes = pathTypes;

            //test area
        }

        private double pxToFt(double px, bool isY)
        {
            double ftPos = px / (isY ? Plotter.ActualHeight : Plotter.ActualWidth) * 12 - 6;
            return ftPos * (isY ? -1 : 1);
        }

        private double ftToPx(double ft, bool isY)
        {
            double pxPos = (isY ? -1 : 1) * ft + 6;
            return pxPos / 12 * (isY ? Plotter.ActualHeight : Plotter.ActualWidth);
        }

        private void pointBindToCanvas(RelativePoint p, Canvas c)
        {
            Binding canvasWidth = new Binding("ActualWidth");
            canvasWidth.Source = c;
            p.SetBinding(RelativePoint.CanvasWidthProperty, canvasWidth);

            Binding canvasHeight = new Binding("ActualHeight");
            canvasHeight.Source = c;
            p.SetBinding(RelativePoint.CanvasHeightProperty, canvasHeight);
        }

        private void FieldSwap_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Image Files|*.png|*.jpg|*.bmp";
            dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            bool? result = dlg.ShowDialog();
            if(result == true)
            {
                Field.Source = new BitmapImage(new Uri(dlg.FileName));
            }
        }

        private void RotateCCW_Click(object sender, RoutedEventArgs e)
        {
            RotateTransform rt = Field.RenderTransform as RotateTransform ?? new RotateTransform();
            rt.Angle -= 90;
            Field.RenderTransform = rt;
        }

        private void RotateCW_Click(object sender, RoutedEventArgs e)
        {
            RotateTransform rt = Field.RenderTransform as RotateTransform ?? new RotateTransform();
            rt.Angle += 90;
            Field.RenderTransform = rt;
        }

        private void robot_PositionChanged(object sender, PositionChangedEventArgs e)
        {
            Canvas.SetLeft(originHeading, ftToPx(robot.XPosition, false));
            Canvas.SetTop(originHeading, ftToPx(robot.YPosition, true));
            //set the first path's start position
            if(Paths.Items.Count > 0)
            {
                (Paths.Items[0] as Path).SetStartPoint(originX.Value ?? 0, originY.Value ?? 0);
            }
        }

        private void originAngle_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            originAngle.Value %= 360;
            if (originAngle.Value < 0)
                originAngle.Value += 360;
        }

        private void Paths_ItemAdded(object sender, ItemEventArgs e)
        {
            Path p = (e.Item as Path);
            if (Paths.Items.Count == 1)
            {
                //this is the only path
                p.SetStartPoint(originX.Value ?? 0, originY.Value ?? 0);
            }
            else
            {
                Path prev = Paths.Items[Paths.Items.Count - 2] as Path;
                p.SetStartPoint(prev.EndX, prev.EndY);
            }

            RelativePoint start = makeGuidePoint();
            RelativePoint end = makeGuidePoint();

            Binding startX = new Binding("StartX");
            startX.Source = p;
            start.SetBinding(RelativePoint.XPositionProperty, startX);

            Binding startY = new Binding("StartY");
            startY.Source = p;
            start.SetBinding(RelativePoint.YPositionProperty, startY);

            Binding endX = new Binding("EndX");
            endX.Source = p;
            end.SetBinding(RelativePoint.XPositionProperty, endX);

            Binding endY = new Binding("EndY");
            endY.Source = p;
            end.SetBinding(RelativePoint.YPositionProperty, endY);

            Plotter.Children.Add(start);
            Plotter.Children.Add(end);
            p.OwnedPoints.Add(start);
            p.OwnedPoints.Add(end);

            if(p is QuadraticPath)
            {
                RelativePoint mid = makeGuidePoint();

                Binding gx = new Binding("GuidePointX");
                gx.Source = p;
                mid.SetBinding(RelativePoint.XPositionProperty, gx);

                Binding gy = new Binding("GuidePointY");
                gy.Source = p;
                mid.SetBinding(RelativePoint.YPositionProperty, gy);

                Plotter.Children.Add(mid);
                p.OwnedPoints.Add(mid);
            }
            //todo add polyline
        }

        SolidColorBrush darkOrange = new SolidColorBrush(Colors.DarkOrange);
        private RelativePoint makeGuidePoint()
        {
            RelativePoint p = new RelativePoint() { Stroke = darkOrange, Diameter = 8 };
            Panel.SetZIndex(p, 2);
            p.XMin = p.YMin = -6;
            p.XMax = p.YMax = 6;
            pointBindToCanvas(p, Plotter);
            return p;
        }

        private void Plotter_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //todo re-render the path plots
        }

        private void Paths_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Path selection = e.AddedItems[0] as Path;
            if (selection != null)
            {
                foreach (Path path in Paths.Items)
                {
                    foreach (RelativePoint rp in path.OwnedPoints)
                    {
                        rp.Visibility = Visibility.Collapsed;
                    }
                }
                foreach(RelativePoint rp in selection.OwnedPoints)
                {
                    rp.Visibility = Visibility.Visible;
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //this will become the basis of RenderPath function.
            Polyline l = new Polyline();
            l.Stroke = darkOrange;
            l.StrokeThickness = 2;
            Panel.SetZIndex(l, 2);
            List<Point> untransformed = (Paths.SelectedItem as Path).GeneratePath(0.1);
            foreach(Point p in untransformed)
            {
                double x = ftToPx(p.X, false);
                double y = ftToPx(p.Y, true);//don't need to invert Y because the polyline wants to maintain coordinate system
                l.Points.Add(new Point(x, y));
            }
            Plotter.Children.Add(l);
        }

        private void Paths_ItemDeleted(object sender, ItemEventArgs e)
        {
            foreach(RelativePoint rp in (e.Item as Path).OwnedPoints)
            {
                Plotter.Children.Remove(rp);
            }
            //todo remove polyline
        }
    }
}
