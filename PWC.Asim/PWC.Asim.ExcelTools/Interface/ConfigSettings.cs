// Copyright (C) 2012, 2013  Power Water Corporation
//
// This file is part of "Asim" - A Renewable Energy Power Station
// Control System Simulator
//
// Asim is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;

namespace PWC.Asim.ExcelTools.Logic
{
    public class ConfigSettings
    {
        /// <summary>
        /// The location of the simulator application.
        /// </summary>
        public string Simulator { get; set; }
        /// <summary>
        /// Whether or not the simulator is to be run.
        /// </summary>
        public bool RunSimulator { get; set; }
        public string Iterations { get; set; }
        /// <summary>
        /// The list of files that are to be used in the simulator.
        /// </summary>
        public List<InputInformation> InputFiles { get; set; }
        /// <summary>
        /// The list of output files that are generated in the simulator.
        /// </summary>
        public List<OutputInformation> OutputFiles { get; set; }
        /// <summary>
        /// The directory where the input and output will be read for the simulator.
        /// </summary>
        public string Directory { get; set; }
        /// <summary>
        /// The prefix that is added to the files that are split up from the sheets in the excel document
        /// </summary>
        public string SplitFilePrefix { get; set; }
        /// <summary>
        /// The community name will be prefixed to all output files.
        /// </summary>
        public string CommunityName { get; set; }
        /// <summary>
        /// Don't prefix the date to the output files - useful for testing.
        /// </summary>
        public bool NoDate { get; set; }

        public List<TemplateInformation> TemplateFiles { get; set; }

        public LogFileInformation LogInformation { get; set; }

        private DateTime _dateSimulatorRun;
        public DateTime DateSimulatorRun { get { return _dateSimulatorRun; } }

        /// <summary>
        /// The "start date" to start counting relative times.  Absolute times in input files 
        /// before this date are ignored.
        /// </summary>
        public DateTime? StartDate { get; set; }

        public string WatchFile { get; set; }
        public List<string> WatchGlobs { get; set; }

        public List<List<string>> ExtraArgList { get; set; }

        public ConfigSettings()
        {
            InputFiles = new List<InputInformation>();
            OutputFiles = new List<OutputInformation>();
            TemplateFiles = new List<TemplateInformation>();
            ExtraArgList = new List<List<string>>();
            RunSimulator = true;
            _dateSimulatorRun = DateTime.Now;
            Simulator = Environment.GetEnvironmentVariable("ProgramFiles") + @"\Power Water Corporation\Asim\Asim.exe";
        }
    }

    public class OutputInformation
    {
        public string Filename { get; set; }
        public string Period { get; set; }
        public List<string> Variables { get; set; }

        public OutputInformation()
        {
            Variables = new List<string>();
        }
    }

    public class InputInformation
    {
        public string Filename { get; set; }
        public bool Recycle { get; set; }
    }

    public class TemplateInformation
    {
        public string TemplateName { get; set; }
        public string OutputName { get; set; }
    }

    public class LogFileInformation
    {
        public string LogFile { get; set; }
        public List<string> Globs { get; set; }

        public LogFileInformation()
        {
            Globs = new List<string>();
        }
    }
    
}
