using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExcelReader.Interface
{
    public interface IExcelReader
    {
        // Get settings and spawn processes
        void ProcessConfigSheet(bool attachToRunningProcess);
        void ProcessAllWorksheets();
        void Finalise();

        // Action property for showing output from sim
        Action<string> ShowSimOutput { get; set; }

        // use templates to show analysis
        void ShowAnalyst();
    }
}
