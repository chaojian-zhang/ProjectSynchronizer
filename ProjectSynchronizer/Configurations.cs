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
        public string OldFileName;
        public string ProjectName;
        public ConfigurationStatus Status;
        public string[] FolderNameList;
        public string SourcePath;
        public string TargetPath;
        public string SummaryText;
        public string StatusText;
    }
}
