using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExcelReader.Logic
{
    public interface IExcelReader
    {
        void ProcessConfigSheet(bool attachToRunningProcess);
        void ProcessAllWorksheets();
    }
}
