using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ProjectSynchronizer
{
    public enum ConfigurationStatus
    {
        Current,
        None
    }
    public class Configurations: INotifyPropertyChanged
    {
        public string FileName;
        private string _ProjectName;
        public string ProjectName { get => _ProjectName; set => SetField(ref _ProjectName, value); }
        public ConfigurationStatus Status;
        public string[] FolderNameList;
        public string SourcePath;
        public string TargetPath;
        public string SummaryText;
        public string StatusText;

        #region Data Binding Interface
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged([CallerMemberName]string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        public bool SetField<type>(ref type field, type value, [CallerMemberName]string propertyName = null)
        {
            if (EqualityComparer<type>.Default.Equals(field, value)) return false;
            field = value;
            NotifyPropertyChanged(propertyName);
            return true;
        }
        #endregion
    }
}
