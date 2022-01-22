using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrackChangePropertyLib;

namespace ConsoleApp1
{
    [Tracking]
    public class Test
    {
        public string prop1 { get; set; }
        public int? prop2 { get; set; }

        public DateTime? prop3 { get; set; }
    }
    class Program
    {
        static void Main(string[] args)
        {
            var dd = new Test();
            dd.prop3 = DateTime.Now;

        }
    }
}
