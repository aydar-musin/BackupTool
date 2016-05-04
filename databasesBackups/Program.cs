using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Data.SqlClient;
using System.IO.Compression;
using System.IO;

namespace databasesBackups
{
    class Program
    {
        static int counter = 0;

        static string[] databaseNames = ConfigurationSettings.AppSettings["dbNames"].Split(new char[]{'*'}, StringSplitOptions.RemoveEmptyEntries);
        static string backupDst = ConfigurationSettings.AppSettings["backupDst"];
        static string[] backupSrc = ConfigurationSettings.AppSettings["backupSrc"].Split(new char[] { '*' }, StringSplitOptions.RemoveEmptyEntries);

        static string backupId;
        static string currentDir;

        static void Main(string[] args)
        {
            try
            {
                backupId = Guid.NewGuid().ToString().Replace("-", ""); // prepare temp dir
                currentDir = Environment.CurrentDirectory+"/temp/"+backupId+"/";
                Directory.CreateDirectory(currentDir);

                BackupDirs();
                BackupDB();
                ZipAndSave();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine("Press any key to terminate...");
                Console.ReadKey();
            }
        }
        private static void BackupDirs()
        {
            foreach (var dir in backupSrc)
            {
                CopyDir(dir);
            }
        }
        private static void CopyDir(string dir)
        {
            DirectoryInfo dSource = new DirectoryInfo(dir);
            DirectoryInfo dDest =Directory.CreateDirectory(currentDir + dSource.Name);

            CopyAll(dSource, dDest);
        }
        public static void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            Directory.CreateDirectory(target.FullName);

            // Copy each file into the new directory.
            foreach (FileInfo fi in source.GetFiles())
            {
                Console.WriteLine(@"Copying {0}\{1}", target.FullName, fi.Name);
                fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir =
                    target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir, nextTargetSubDir);
            }
        }

        private static void BackupDB()
        {
            if (databaseNames.Length == 0)
                return;

            using (SqlConnection sqlConnection = new SqlConnection(ConfigurationSettings.AppSettings["ConnectionString"]))
            {
                sqlConnection.Open();

                foreach (var db in databaseNames)
                {
                    SqlCommand cmd = new SqlCommand();
                    cmd.CommandTimeout = 1000000000;

                    cmd.Connection = sqlConnection;
                    cmd.CommandText = string.Format(
    @"USE master;
BACKUP DATABASE [{0}]
TO DISK = '{1}'
", db, string.Format("{0}\\{1}.bak",currentDir, db));

                    cmd.ExecuteNonQuery();
                }
            }
        }

        private static void ZipAndSave()
        {
            System.IO.Compression.ZipFile.CreateFromDirectory(currentDir, backupDst + backupId + ".zip", CompressionLevel.Optimal,false,Encoding.UTF8);
            Directory.Delete(currentDir,true);
        }
    }
}