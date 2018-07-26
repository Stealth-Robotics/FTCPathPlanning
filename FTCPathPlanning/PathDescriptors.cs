using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using Matrices;
using System.Windows.Shapes;

namespace FTCPathPlanning
{
    [CategoryOrder("Start Position", 0)]
    [CategoryOrder("End Position", 100)]
    public abstract class Path : BindableBase
    {
        public readonly List<RelativePoint> OwnedPoints = new List<RelativePoint>();
        public Polyline OwnedPolyline;

        protected List<string> newProps = new List<string>();
        private void updateNewProps()
        {
            foreach (string s in newProps)
                OnPropertyChanged(s);
        }

        private string name;
        [Browsable(false)]
        public string Name
        {
            get
            {
                return name;
            }
            internal set
            {
                SetProperty(ref name, value);
            }
        }
        public Path(string name)
        {
            Name = name;
        }

        double startX;
        [Category("Start Position")]
        [DisplayName("X")]
        [ReadOnly(true)]
        [Editor(typeof(RangedPositionEditor), typeof(RangedPositionEditor))]
        public double StartX
        {
            get
            {
                return startX;
            }
            set
            {
                SetProperty(ref startX, value);
                OnPropertyChanged("FullLength");
                updateNewProps();
            }
        }

        double startY;
        [Category("Start Position")]
        [DisplayName("Y")]
        [ReadOnly(true)]
        [Editor(typeof(RangedPositionEditor), typeof(RangedPositionEditor))]
        public double StartY
        {
            get
            {
                return startY;
            }
            set
            {
                SetProperty(ref startY, value);
                OnPropertyChanged("FullLength");
                updateNewProps();
            }
        }

        double endX;
        [Category("End Position")]
        [DisplayName("X")]
        [Editor(typeof(RangedPositionEditor), typeof(RangedPositionEditor))]
        public double EndX
        {
            get
            {
                return endX;
            }
            set
            {
                SetProperty(ref endX, value);
                OnPropertyChanged("FullLength");
                updateNewProps();
            }
        }

        double endY;
        [Category("End Position")]
        [DisplayName("Y")]
        [Editor(typeof(RangedPositionEditor), typeof(RangedPositionEditor))]
        public double EndY
        {
            get
            {
                return endY;
            }
            set
            {
                SetProperty(ref endY, value);
                OnPropertyChanged("FullLength");
                updateNewProps();
            }
        }

        [Category(null)]
        [DisplayName("Path Length")]
        [PropertyOrder(100)]
        [Editor(typeof(RangedPositionEditor), typeof(RangedPositionEditor))]
        public double FullLength
        {
            get
            {
                if (StartX != EndX)
                {
                    return GetFunction().IntegrateLength(StartX, EndX);
                }
                else
                {
                    //we have an invalid function, so let's get a euclidean distance between endpoints instead
                    return Math.Sqrt(Math.Pow(EndX - StartX, 2) + Math.Pow(EndY - StartY, 2));
                }
            }
        }

        public List<Point> GeneratePath(double interval)
        {
            List<Point> path = new List<Point>();
            int direction = Math.Sign(EndX - StartX);
            CubicFunction func = GetFunction();
            for(double x = StartX; direction == 1 ? x < EndX : x > EndX; x += direction * interval)
            {
                path.Add(new Point(x, func.Evaluate(x)));
            }
            path.Add(new Point(EndX, func.Evaluate(EndX)));
            return path;
        }

        public abstract CubicFunction GetFunction();

        public void SetStartPoint(double x, double y)
        {
            StartX = x;
            StartY = y;
        }
    }

    [DisplayName("Linear Path")]
    public class LinearPath : Path
    {
        public LinearPath(string name) : base(name) { }

        public override CubicFunction GetFunction()
        {
            Matrix m = new Matrix(2, 3);
            m[0, 0] = StartX;
            m[0, 1] = 1;
            m[0, 2] = StartY;
            m[1, 0] = EndX;
            m[1, 1] = 1;
            m[1, 2] = EndY;
            m = m.RREF();
            return new CubicFunction(0, 0, m[0, 2], m[1, 2]);
        }
    }

    [DisplayName("Quadratic Path")]
    [CategoryOrder("Guide Point", 2)]
    public class QuadraticPath : Path
    {
        public QuadraticPath(string name) : base(name)
        {
            newProps.Add("Seg1Length");
            newProps.Add("Seg2Length");
        }

        double gx;
        [Category("Guide Point")]
        [DisplayName("X")]
        [Editor(typeof(RangedPositionEditor), typeof(RangedPositionEditor))]
        public double GuidePointX
        {
            get
            {
                return gx;
            }
            set
            {
                SetProperty(ref gx, value);
                OnPropertyChanged("Seg1Length");
                OnPropertyChanged("Seg2Length");
                OnPropertyChanged("FullLength");
            }
        }

        double gy;
        [Category("Guide Point")]
        [DisplayName("Y")]
        [Editor(typeof(RangedPositionEditor), typeof(RangedPositionEditor))]
        public double GuidePointY
        {
            get
            {
                return gy;
            }
            set
            {
                SetProperty(ref gy, value);
                OnPropertyChanged("Seg1Length");
                OnPropertyChanged("Seg2Length");
                OnPropertyChanged("FullLength");
            }
        }

        [Category(null)]
        [DisplayName("First Segment Length")]
        [PropertyOrder(1)]
        [Editor(typeof(RangedPositionEditor), typeof(RangedPositionEditor))]
        public double Seg1Length
        {
            get
            {
                return GetFunction().IntegrateLength(StartX, GuidePointX);
            }
        }

        [Category(null)]
        [DisplayName("Second Segment Length")]
        [PropertyOrder(2)]
        [Editor(typeof(RangedPositionEditor), typeof(RangedPositionEditor))]
        public double Seg2Length
        {
            get
            {
                return GetFunction().IntegrateLength(GuidePointX, EndX);
            }
        }

        public override CubicFunction GetFunction()
        {
            Matrix m = new Matrix(3, 4);
            for(int degree = 2; degree >= 0; degree--)
            {
                m[0, 2 - degree] = Math.Pow(StartX, degree);
                m[1, 2 - degree] = Math.Pow(GuidePointX, degree);
                m[2, 2 - degree] = Math.Pow(EndX, degree);
            }
            m[0, 3] = StartY;
            m[1, 3] = GuidePointY;
            m[2, 3] = EndY;
            m = m.RREF().ToPrecision(10);
            Matrix[] matrices = Matrix.Split(m, 3);
            if (matrices[0] == Matrix.I(3))
            {
                return new CubicFunction(0, m[0, 3], m[1, 3], m[2, 3]);
            }
            else return new CubicFunction(0, 0, 0, 0);
        }
    }
}
