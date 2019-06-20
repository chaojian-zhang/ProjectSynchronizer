/* To-Do:
 * * Add button to create missing folders
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using YamlDotNet.Serialization;

namespace ProjectSynchronizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region Constructor
        public MainWindow()
        {
            // Load configuration
            if (!File.Exists(ConfigurationFileName))
                File.WriteAllText(ConfigurationFileName, new Serializer().Serialize(
                    new Configurations()
                    {
                        FolderNameList = new string[] { },
                        SourcePath = string.Empty,
                        TargetPath = string.Empty,
                        SummaryText = string.Empty,
                        StatusText = "Ready."
                    }));

            string yaml = File.ReadAllText(ConfigurationFileName);
            Configurations = new Deserializer().Deserialize<Configurations>(yaml);

            // Initialize view
            InitializeComponent();
        }
        private const string ConfigurationFileName = "Configurations.yaml";
        #endregion

        #region View Properties
        private Configurations Configurations;
        public string FolderNameList
        {
            get => string.Join(Environment.NewLine, Configurations.FolderNameList);
            set => SetField(ref Configurations.FolderNameList, value.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
        }
        public string SourcePath
        {
            get => Configurations.SourcePath;
            set => SetField(ref Configurations.SourcePath, value);
        }
        public string TargetPath
        {
            get => Configurations.TargetPath;
            set => SetField(ref Configurations.TargetPath, value);
        }
        public string SummaryText
        {
            get => Configurations.SummaryText;
            set => SetField(ref Configurations.SummaryText, value);
        }
        public string StatusText
        {
            get => Configurations.StatusText;
            set => SetField(ref Configurations.StatusText, value);
        }
        #endregion

        #region Window Events
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
            => DragMove();

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            string yaml = new Serializer().Serialize(Configurations);
            File.WriteAllText(ConfigurationFileName, yaml);
        }
        #endregion

        #region Commands
        private void ExitCommand_Executed(object sender, ExecutedRoutedEventArgs e)
            => Close();
        private void ExitCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
            => e.CanExecute = true;
        private void SwapCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            string temp = SourcePath;
            SourcePath = TargetPath;
            TargetPath = temp;

            // Automatically check
            bool valid = Directory.Exists(SourcePath)
                && Directory.Exists(TargetPath)
                && Configurations.FolderNameList.Where(f => !Directory.Exists(
                    System.IO.Path.Combine(SourcePath, f))).Count() == 0
                && !string.IsNullOrWhiteSpace(FolderNameList);
            if(valid)
                CheckCommand_Executed(null, null);
            else
                // Clear symmary
                UpdateSummaryText(string.Empty);
        }
        private void SwapCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
            => e.CanExecute = true;
        private void SyncCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            // Update
            UpdateSummaryText("Start Syncing...");

            // Calculate change and do sync
            int count = CopyChangedFiles();

            // Update
            UpdateSummaryText($"Finished. {count} files moved from Source folders to Target folders.");
        }
        private void CheckCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            // Summary Update
            UpdateSummaryText(DetectChangeLog());

            // Label Update
            UpdateStatusText("Click Sync to sync changed files from Source to Target; Use F2 to swap Source/Target.");
        }
        private void SyncCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            // Can execute state
            e.CanExecute = Directory.Exists(SourcePath)
                && Directory.Exists(TargetPath)
                && Configurations.FolderNameList.Where(f => !Directory.Exists(
                    System.IO.Path.Combine(SourcePath, f))).Count() == 0
                && !string.IsNullOrWhiteSpace(FolderNameList);

            // Label update
            if (!e.CanExecute)
            {
                if (!Directory.Exists(SourcePath))
                    UpdateStatusText($"Source Path '{SourcePath}' doesn't exist.");
                else if (!Directory.Exists(TargetPath))
                    UpdateStatusText($"Target Path '{TargetPath}' doesn't exist.");
                else
                {
                    IEnumerable<string> nonExisting = Configurations.FolderNameList
                        .Where(f => !Directory.Exists(
                            System.IO.Path.Combine(SourcePath, f)));
                    UpdateStatusText($"{string.Join(", ", nonExisting)} doesn't exist in source.");
                }                
            }
            else
                UpdateStatusText("Click Check to detect changes.");
        }
        #endregion

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

        #region Sub-Routines
        /// <summary>
        /// Detect potential changes between folders
        /// </summary>
        private string DetectChangeLog()
        {
            FolderComparison comparer = new FolderComparison();
            StringBuilder builder = new StringBuilder();
            foreach (string folderName in Configurations.FolderNameList)
            {
                string fullPath1 = System.IO.Path.Combine(SourcePath, folderName);
                string fullPath2 = System.IO.Path.Combine(TargetPath, folderName);

                // Compare existence
                if(!Directory.Exists(fullPath1))
                    builder.Append($"<<< Source path '{fullPath1}' doesn't exist." + Environment.NewLine);
                else if (!Directory.Exists(fullPath2))
                    builder.Append($">>> Target path '{fullPath2}' doesn't exist." + Environment.NewLine);
                else
                {
                    // Compare contents
                    FolderComparisonResult result = comparer.Compare(fullPath1, fullPath2);
                    if (result.IdenticalFileSequence)
                        builder.Append($"* {folderName}: No change." + Environment.NewLine);
                    else
                        builder.Append($"* {folderName}" +
                            $"\n\t({result.CommonFiles.Length} common files)" +
                            $"{(result.SourceExtra.Length > 0 ? $"\n\t{result.SourceExtra.Length} new: {string.Join(", ", result.SourceExtra)}" : string.Empty)}" +
                            $"{(result.TargetExtra.Length > 0 ? $"\n\t{result.TargetExtra.Length} unexpected: {string.Join(", ", result.TargetExtra)}" : string.Empty)}" + Environment.NewLine);
                }
            }
            return builder.ToString();
        }
        /// <summary>
        /// Copy changed files from source path to target path; Return file count;
        /// Notice only changes from source is updated; For update changes from target, Swap first.
        /// 
        /// Notice file comparison is by content not by modification date;
        /// Existing files are overwritten.
        /// </summary>
        private int CopyChangedFiles()
        {
            FolderComparison comparer = new FolderComparison();
            int count = 0;
            foreach (string folderName in Configurations.FolderNameList)
            {
                string fullPath1 = System.IO.Path.Combine(SourcePath, folderName);
                string fullPath2 = System.IO.Path.Combine(TargetPath, folderName);

                // Skip non-existence
                if (!Directory.Exists(fullPath1))
                    continue;
                else if (!Directory.Exists(fullPath2))
                    continue;
                else
                {
                    // Compare contents
                    FolderComparisonResult result = comparer.Compare(fullPath1, fullPath2);
                    if (result.IdenticalFileSequence)
                        continue;
                    else
                    {
                        foreach (string item in result.SourceExtra)
                        {
                            File.Copy(System.IO.Path.Combine(fullPath1, item), System.IO.Path.Combine(fullPath2, item), true);
                            count++;
                        }
                    }
                }
            }
            return count;
        }
        private void UpdateStatusText(string text)
            => StatusText = text;
        private void UpdateSummaryText(string text)
            => SummaryText = text;
        #endregion
    }
}
