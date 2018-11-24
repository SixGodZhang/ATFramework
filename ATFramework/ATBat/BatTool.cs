using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace ATFramework.Bat
{
    public class BatTool
    {
        private static string infoLogPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/Logs/BatInfoLog.log";
        private static string errorLogPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/Logs/BatErrorLog.log";
        private static bool batExcuteResult = true;

        static BatTool()
        {
            if (!File.Exists(infoLogPath))
                File.Create(infoLogPath);

            if (!File.Exists(errorLogPath))
                File.Create(errorLogPath);
        }

        /// <summary>
        /// 调用批处理
        /// </summary>
        /// <param name="batPath">Bat路径</param>
        public static bool CallBat(string batPath)
        {
            //Initialize
            string outPutString = string.Empty;
            string errMsg = string.Empty;
            batExcuteResult = true;

            Thread t1 = new Thread(new ParameterizedThreadStart(ReadOutput));
            t1.IsBackground = true;
            Thread t2 = new Thread(new ParameterizedThreadStart(ReadError));
            t1.IsBackground = true;

            using (Process pro = new Process())
            {
                FileInfo file = new FileInfo(batPath);
                pro.StartInfo.WorkingDirectory = file.Directory.FullName;
                pro.StartInfo.FileName = batPath;
                pro.StartInfo.CreateNoWindow = true;
                pro.StartInfo.RedirectStandardOutput = true;
                pro.StartInfo.RedirectStandardError = true;
                pro.StartInfo.StandardOutputEncoding = Encoding.GetEncoding("gb2312");
                pro.StartInfo.StandardErrorEncoding = Encoding.GetEncoding("gb2312");
                pro.StartInfo.UseShellExecute = false;

                pro.Start();
                t1.Start(pro);
                t2.Start(pro);

                pro.WaitForExit();
                if (pro.HasExited)
                {
                    pro.Close();
                }
                 
                return batExcuteResult;
            }
        }

        /// <summary>
        /// 读取错误输出流
        /// </summary>
        /// <param name="data"></param>
        private static void ReadError(object data)
        {
            Process temp = data as Process;
            if (temp == null)
                return;

            StringBuilder errorLog = new StringBuilder();
            string outputStr = string.Empty;
            while ((outputStr = temp.StandardError.ReadLine()) != null)
            {
                if (!string.IsNullOrEmpty(outputStr.Trim()))
                    batExcuteResult = false;

                errorLog.Append("e: " + outputStr + "\n");
            }

            FileOp.ATFileOp.WriteToFile(errorLogPath, errorLog.ToString(),FileMode.Append);
        }

        /// <summary>
        /// 读取标准输出流
        /// </summary>
        /// <param name="data"></param>
        private static void ReadOutput(object data)
        {
            Process temp = data as Process;
            if (temp == null)
                return;

            StringBuilder infoLog = new StringBuilder();
            string standardOutputStr = string.Empty;
            while ((standardOutputStr = temp.StandardOutput.ReadLine()) != null)
            {
                if (standardOutputStr.Contains("errorlevel="))
                    batExcuteResult = "0".Equals(standardOutputStr.Substring(standardOutputStr.IndexOf("=") + 1, 1));

                infoLog.Append("d: " + standardOutputStr + "\n");
            }

            FileOp.ATFileOp.WriteToFile(infoLogPath, infoLog.ToString(), FileMode.Append);
        }
    }

}
