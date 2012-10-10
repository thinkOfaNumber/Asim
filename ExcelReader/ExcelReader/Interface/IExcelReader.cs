using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExcelReader.Interface
{
    public interface IExcelReader
    {
        void ProcessConfigSheet(bool attachToRunningProcess);
        void ProcessAllWorksheets();
        void GenerateGraphs(string template, string output);
    }
}
