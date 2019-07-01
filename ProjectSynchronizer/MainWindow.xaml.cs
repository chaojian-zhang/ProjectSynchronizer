/* To-Do:
 * * Add button to create missing folders
 * * Project management side panel is not updating text automatically though it's strange because why not? An notification with same name is called, unless it is class specific
 * * Check seems not working
 * * We need better ways to manage project names and file saving and laoding
 * * We need to be able to create new project yamls
 * * [Bug] During synchronization source and target folder are wrong! (The same effect can be observed during swapping)
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
            // Load projects
            Projects = new ObservableCollection<Configurations>();
            foreach (string file in Directory.EnumerateFiles(AppDomain.CurrentDomain.BaseDirectory))
            {
                if (System.IO.Path.GetExtension(file).ToLower() != ".yaml")
                    continue;
                string yaml = File.ReadAllText(file);
                Configurations project = new Deserializer().Deserialize<Configurations>(yaml);
                if (project.Status == ConfigurationStatus.Current)
                    CurrentProject = project;
                Projects.Add(project);
                // Don't delete the file (Dangerous! Especially during debug we might just STOP the execution)
            }
            if (CurrentProject == null)
            {
                CurrentProject = new Configurations()
                {
                    OldFileName = "Default.yaml",
                    ProjectName = "Default",
                    FolderNameList = new string[] { },
                    SourcePath = string.Empty,
                    TargetPath = string.Empty,
                    SummaryText = string.Empty,
                    StatusText = "Ready."
                };
                File.WriteAllText("Default.yaml", new Serializer().Serialize(CurrentProject));
                Projects.Add(CurrentProject);
            }

            // Initialize view
            InitializeComponent();
        }
        #endregion

        #region View Properties
        private Configurations _CurrentProject;
        private ObservableCollection<Configurations> _Projects;
        public Configurations CurrentProject
        {
            get => _CurrentProject;
            set
            {
                SetField(ref _CurrentProject, value);
                NotifyPropertyChanged(nameof(FolderNameList));
                NotifyPropertyChanged(nameof(SourcePath));
                NotifyPropertyChanged(nameof(TargetPath));
                NotifyPropertyChanged(nameof(SummaryText));
                NotifyPropertyChanged(nameof(StatusText));
                NotifyPropertyChanged(nameof(ProjectName));
            }
        }
        public string ProjectName
        {
            get => CurrentProject.ProjectName;
            set => SetField(ref CurrentProject.ProjectName, value);
        }
        public string FolderNameList
        {
            get => string.Join(Environment.NewLine, CurrentProject.FolderNameList);
            set => SetField(ref CurrentProject.FolderNameList, value.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
        }
        public string SourcePath
        {
            get => CurrentProject.SourcePath;
            set => SetField(ref CurrentProject.SourcePath, value);
        }
        public string TargetPath
        {
            get => CurrentProject.TargetPath;
            set => SetField(ref CurrentProject.TargetPath, value);
        }
        public string SummaryText
        {
            get => CurrentProject.SummaryText;
            set => SetField(ref CurrentProject.SummaryText, value);
        }
        public string StatusText
        {
            get => CurrentProject.StatusText;
            set => SetField(ref CurrentProject.StatusText, value);
        }
        public ObservableCollection<Configurations> Projects
        {
            get => _Projects;
            set => SetField(ref _Projects, value);
        }
        #endregion

        #region Window Events
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
            => DragMove();
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            foreach (Configurations project in Projects)
            {
                if (project == CurrentProject)
                    project.Status = ConfigurationStatus.Current;
                else
                    project.Status = ConfigurationStatus.None;

                // Delete old file
                File.Delete(project.OldFileName);
                // Save new file
                string newFileName = project.ProjectName.EscapeFilename() + ".yaml";
                // Filename conflict resolution
                if (File.Exists(newFileName))
                    newFileName += project.ProjectName.EscapeFilename() 
                        + DateTime.Now.ToLongTimeString().Replace(':', '_')
                        + ".yaml";
                project.OldFileName = newFileName;
                string yaml = new Serializer().Serialize(project);
                File.WriteAllText(newFileName, yaml);
            }
        }
        private void GridSplitter_MouseDown(object sender, MouseButtonEventArgs e)
            => ProjectPanel.Visibility = ProjectPanel.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;
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
                && CurrentProject.FolderNameList.Where(f => !Directory.Exists(
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
            string change = DetectChangeLog();
            UpdateSummaryText(change);

            // Label Update
            UpdateStatusText("Click Sync to sync changed files from Source to Target; Use F2 to swap Source/Target.");
        }
        private void SyncCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            // Can execute state
            e.CanExecute = Directory.Exists(SourcePath)
                && Directory.Exists(TargetPath)
                && CurrentProject.FolderNameList.Where(f => !Directory.Exists(
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
                    IEnumerable<string> nonExisting = CurrentProject.FolderNameList
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
            StringBuilder builder = new StringBuilder($"Source: {SourcePath};\nTarget: {TargetPath}.\n");
            foreach (string folderName in CurrentProject.FolderNameList)
            {
                string fullPath1 = System.IO.Path.Combine(SourcePath, folderName);
                string fullPath2 = System.IO.Path.Combine(TargetPath, folderName);

                // Compare existence
                if(!Directory.Exists(fullPath1))
                    builder.Append($"<<< Source path '{fullPath1}' doesn't exist.\n");
                else if (!Directory.Exists(fullPath2))
                    builder.Append($">>> Target path '{fullPath2}' doesn't exist.\n");
                else
                {
                    // Compare contents
                    FolderComparisonResult result = comparer.Compare(fullPath1, fullPath2);
                    if (result.IdenticalFileSequence)
                        builder.Append($"* {folderName}: No change.\n");
                    else
                        builder.Append($"* {folderName}" +
                            $"\n\t({result.CommonFiles.Length} common files)" +
                            $"{(result.SourceExtra.Length > 0 ? $"\n\t{result.SourceExtra.Length} new: {string.Join(", ", result.SourceExtra)}" : string.Empty)}" +
                            $"{(result.TargetExtra.Length > 0 ? $"\n\t{result.TargetExtra.Length} unexpected: {string.Join(", ", result.TargetExtra)}" : string.Empty)}\n");
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
            foreach (string folderName in CurrentProject.FolderNameList)
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
