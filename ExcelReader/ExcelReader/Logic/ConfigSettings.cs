using System.Collections.Generic;

namespace ExcelReader.Logic
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
        public List<string> InputFiles { get; set; }
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

        public List<TemplateInformation> TemplateFiles { get; set; }

        public ConfigSettings()
        {
            InputFiles = new List<string>();
            OutputFiles = new List<OutputInformation>();
            TemplateFiles = new List<TemplateInformation>();
            RunSimulator = true;
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

    public class TemplateInformation
    {
        public string TemplateName { get; set; }
        public string OutputName { get; set; }
    }
}
