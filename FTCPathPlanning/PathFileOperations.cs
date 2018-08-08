using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Windows;

namespace FTCPathPlanning
{
    public static class PathFileOperations
    {
        public static void Write(string filename, IEnumerable<Path> paths)
        {
            XmlSerializer ser = new XmlSerializer(typeof(List<Path>), new Type[] { typeof(Path), typeof(LinearPath), typeof(QuadraticPath) });
            using (StreamWriter file = File.CreateText(filename))
            {
                using (XmlWriter json = XmlWriter.Create(file))
                {
                    ser.Serialize(json, paths.ToList());
                }
            }
        }

        public static List<Path> Read(string filename)
        {
            List<Path> ret = null;
            XmlSerializer ser = new XmlSerializer(typeof(List<Path>), new Type[] { typeof(Path), typeof(LinearPath), typeof(QuadraticPath) });
            using (StreamReader file = File.OpenText(filename))
            {
                using (XmlReader json = XmlReader.Create(file))
                {
                    ret = (List<Path>)ser.Deserialize(json);
                }
            }
            return ret;
        }

        public static void Export(string filename, IEnumerable<Path> paths, double granularity)
        {
            using (StreamWriter file = File.CreateText(filename))
            {
                file.WriteLine("distance,wheelangle");
                foreach(Path p in paths)
                {
                    file.WriteLine("#####{0}#####", p.Name);
                    //todo get motion direction, then do this evaluation per granularity increment
                    int direction = Math.Sign(p.EndX - p.StartX);
                    CubicFunction func = p.GetFunction();
                    CubicFunction deriv = func.Differentiate();
                    for (double x = p.StartX; direction > 0 ? x < p.EndX : x > p.EndX; x += granularity * direction)
                    {
                        WriteDistancePoint(x, p.StartX, direction, func, deriv, file);
                    }
                    WriteDistancePoint(p.EndX, p.StartX, direction, func, deriv, file);
                }
            }
        }

        private static void WriteDistancePoint(double x, double start, int direction, CubicFunction func, CubicFunction deriv, StreamWriter file)
        {
            double dist = Math.Abs(func.IntegrateLength(start, x));
            double angle = Math.Atan(deriv.Evaluate(x)) * 180 / Math.PI - 90;
            //if we're moving to the left, that means we just flip the wheels around
            if (direction < 0)
            {
                angle += 180;
            }
            file.WriteLine("{0},{1}", dist, angle);
        }
    }
}
