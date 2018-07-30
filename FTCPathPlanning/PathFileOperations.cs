using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

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

        public static IEnumerable<Path> Read(string filename)
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
    }
}
