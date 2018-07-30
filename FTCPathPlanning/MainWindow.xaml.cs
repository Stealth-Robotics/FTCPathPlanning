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

        private void Paths_ItemAdded(object item)
        {
            Path p = item as Path;
            if (Paths.Items.Count == 1)
            {
                //this is the only path
                p.SetStartPoint(originX.Value ?? 0, originY.Value ?? 0);
            }
            else
            {
                Path prev = Paths.Items[Paths.Items.Count - 2] as Path;
                p.SetStartPoint(prev.EndX, prev.EndY);
                NonDependencyBinding.Create(prev, "EndX", p, "StartX");
                NonDependencyBinding.Create(prev, "EndY", p, "StartY");
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
            RenderPath(p);
            //everything is now set. attach an event handler to re-render paths
            p.PropertyChanged += P_PropertyChanged;

        }

        private void P_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "Name")
            {
                RenderPath(sender as Path);
            }
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
            foreach(Path p in Paths.Items)
            {
                RenderPath(p);
            }
        }

        private void Paths_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0) return;
            Path selection = e.AddedItems[0] as Path;
            if (selection != null)
            {
                Binding nameBinding = new Binding("Name");
                nameBinding.Source = selection;
                Props.SetBinding(PropertyGrid.SelectedObjectNameProperty, nameBinding);
                //Props.SelectedObjectName = selection.Name;
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

        private void RenderPath(Path path)
        {
            if (path.OwnedPolyline != null)
            {
                Plotter.Children.Remove(path.OwnedPolyline);
            }
            Polyline l = new Polyline();
            l.Stroke = darkOrange;
            l.StrokeThickness = 2;
            Panel.SetZIndex(l, 2);
            List<Point> untransformed = path.GeneratePath(0.1);
            if(untransformed.Count == 1)
            {
                //it's an invalid (straight vertical) path
                untransformed.Clear();
                untransformed.Add(new Point(path.StartX, path.StartY));
                untransformed.Add(new Point(path.EndX, path.EndY));
            }
            foreach (Point p in untransformed)
            {
                double x = ftToPx(p.X, false);
                double y = ftToPx(p.Y, true);
                l.Points.Add(new Point(x, y));
            }
            Plotter.Children.Add(l);
            path.OwnedPolyline = l;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog save = new SaveFileDialog();
            save.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            save.Filter = "Path files (*.path)|*.path";
            bool success = save.ShowDialog() ?? false;
            if(success)
            {
                PathFileOperations.Write(save.FileName, Paths.Items.Cast<Path>());
                PathFileOperations.Read(save.FileName);
            }
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            string name = string.IsNullOrWhiteSpace(NewName.Text) ? (string)PathType.SelectedValue : NewName.Text;
            NewName.Text = "";
            Path item = null;
            switch((string)PathType.SelectedValue)
            {
                case "Linear Path":
                    item = new LinearPath(name);
                    break;
                case "Quadratic Path":
                    item = new QuadraticPath(name);
                    break;
                default:
                    return;
            }
            Paths.Items.Add(item);
            Paths_ItemAdded(item);
        }

        private void Rename_Click(object sender, RoutedEventArgs e)
        {
            Path selected = Paths.SelectedItem as Path;
            if (selected != null)
            {
                string name = string.IsNullOrWhiteSpace(NewName.Text) ? selected.Name : NewName.Text;
                NewName.Text = "";
                selected.Name = name;
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            Path selected = Paths.SelectedItem as Path;
            if(selected != null)
            {
                const string msgFormat = "This will delete the path '{0}' and all subsequent paths. Are you sure you want to continue?";
                MessageBoxResult result = System.Windows.MessageBox.Show(string.Format(msgFormat, selected.Name),
                    "Continue?", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
                if(result == MessageBoxResult.Yes)
                {
                    int index = Paths.SelectedIndex;
                    while(Paths.Items.Count > index)
                    {
                        Path item = Paths.Items[index] as Path;
                        item.PropertyChanged -= P_PropertyChanged;
                        foreach(RelativePoint p in item.OwnedPoints)
                        {
                            Plotter.Children.Remove(p);
                        }
                        Plotter.Children.Remove(item.OwnedPolyline);
                        NonDependencyBinding.CleanupBindingSource(item);
                        Paths.Items.RemoveAt(index);
                    }
                    Paths.SelectedIndex = -1;
                    Paths.SelectedItem = null;
                }
            }
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog open = new OpenFileDialog();
            open.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            open.Filter = "Path files (*.path)|*.path";
            bool success = open.ShowDialog() ?? false;
            if (success)
            {
                IEnumerable<Path> paths = PathFileOperations.Read(open.FileName);
                //clear out everything we have. move the robot origin. Use Paths_ItemAdded
            }
        }
    }
}
