using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectSynchronizer
{
    public enum ConfigurationStatus
    {
        Current,
        None
    }
    public class Configurations
    {
        public string ProjectName { get; set; }
        public ConfigurationStatus Status { get; set; }
        public string[] FolderNameList { get; set; }
        public string SourcePath { get; set; }
        public string TargetPath { get; set; }
        public string SummaryText { get; set; }
        public string StatusText { get; set; }
    }
}
