using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectSynchronizer
{
    public class FolderComparisonResult
    {
        /// <summary>
        /// Whether the folder contain identical file sequence
        /// </summary>
        public bool IdenticalFileSequence { get; set; }
        /// <summary>
        /// List of common files
        /// </summary>
        public string[] CommonFiles { get; set; }
        /// <summary>
        /// Extra/Changed files from source
        /// </summary>
        public string[] SourceExtra { get; set; }
        /// <summary>
        /// Extra/Changed files from target
        /// </summary>
        public string[] TargetExtra { get; set; }
    }

    public class FolderComparison
    {
        public FolderComparisonResult Compare(string folder1, string folder2, bool ignoreGit = true)
        {
            DirectoryInfo dir1 = new DirectoryInfo(folder1);
            DirectoryInfo dir2 = new DirectoryInfo(folder2);
            FolderComparisonResult result = new FolderComparisonResult();

            // Take a snapshot of the file system.  
            IEnumerable<FileInfo> list1 = dir1.GetFiles("*.*", SearchOption.AllDirectories);
            IEnumerable<FileInfo> list2 = dir2.GetFiles("*.*", SearchOption.AllDirectories);
            // Ignore .git etc.
            if (ignoreGit)
            {
                list1 = list1.Where(f => !f.FullName.Contains(Path.Combine(folder1, ".git")));
                list2 = list2.Where(f => !f.FullName.Contains(Path.Combine(folder2, ".git")));
            }

            // Initialize custom file comparer
            FileCompare myFileCompare = new FileCompare();

            // This query determines whether the two folders contain  
            // identical file lists, as evaluated by custom file comparer  
            // The query executes immediately because it returns a bool.  
            result.IdenticalFileSequence = list1.SequenceEqual(list2, myFileCompare);

            // Find the common files. Intersect() produces a sequence and doesn't   
            // execute until the foreach statement.  
            IEnumerable<FileInfo> queryCommonFiles = list1.Intersect(list2, myFileCompare);
            result.CommonFiles = queryCommonFiles.Count() > 0
                ? queryCommonFiles.Select(f => f.FullName
                    .Replace(dir1.FullName + Path.DirectorySeparatorChar, string.Empty)
                    .Replace(dir2.FullName + Path.DirectorySeparatorChar, string.Empty))
                    .Distinct()
                    .ToArray()  // Get only distinct children file path/name
                : new string[] { };

            // Find the set difference between the two folders
            // First check from folder1 to folder2, then check from folder2 to folder1
            IEnumerable<FileInfo> queryList1Only = (from file in list1
                                                    select file).Except(list2, myFileCompare);
            IEnumerable<FileInfo> queryList2Only = (from file in list2
                                                    select file).Except(list1, myFileCompare);
            // Set, and trimp to children file path/name
            result.SourceExtra = queryList1Only.Select(f => f.FullName.Replace(dir1.FullName + Path.DirectorySeparatorChar, string.Empty)).ToArray();
            result.TargetExtra = queryList2Only.Select(f => f.FullName.Replace(dir2.FullName + Path.DirectorySeparatorChar, string.Empty)).ToArray();

            // Return
            return result;
        }
    }

    // This implementation defines a very simple comparison  
    // between two FileInfo objects. It only compares the name  
    // of the files being compared and their length in bytes.  
    public class FileCompare : IEqualityComparer<FileInfo>
    {
        public FileCompare() { }

        public bool Equals(FileInfo f1, FileInfo f2)
        {
            return (f1.Name == f2.Name &&
                    f1.Length == f2.Length);
        }

        // Return a hash that reflects the comparison criteria. According to the   
        // rules for IEqualityComparer<T>, if Equals is true, then the hash codes must  
        // also be equal. Because equality as defined here is a simple value equality, not  
        // reference identity, it is possible that two or more objects will produce the same  
        // hash code.  
        public int GetHashCode(FileInfo fi)
        {
            string s = $"{fi.Name}{fi.Length}";
            return s.GetHashCode();
        }
    }
}
