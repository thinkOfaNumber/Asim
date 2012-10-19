using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExcelReader.Interface
{
    public class MyWorksheet
    {
        public string Name { get; set; }
        public object[,] Data { get; set; }
    }
}
