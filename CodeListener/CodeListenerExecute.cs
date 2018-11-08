using System;
using System.Diagnostics;
using System.IO;
using Rhino;
using Rhino.Commands;
using Rhino.Input;

namespace CodeListener
{
    [System.Runtime.InteropServices.Guid("a6dbd0d2-3ef9-44e7-8867-e0b359b57f38")]
    public class CodeListenerExecute : Command
    {
        static CodeListenerExecute _instance;
        public CodeListenerExecute()
        {
            _instance = this;
        }

        ///<summary>The only instance of the CodeListenerExecute command.</summary>
        public static CodeListenerExecute Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "CodeListenerExecute"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // get the path
            string path = string.Empty;
            RhinoGet.GetString("Absolute path of the exexutable", true, ref path);
            if (string.IsNullOrEmpty(path))
            {
                RhinoApp.WriteLine("Cancelled.");
                return Result.Cancel;
            }
            // valdiate path
            if (!File.Exists(path))
            {
                RhinoApp.WriteLine("File path is not valid.");
                return Result.Cancel;
            }
            // run the file
            try
            {
                Process.Start(path);
            }
            catch (Exception e)
            {
                RhinoApp.WriteLine(e.Message);
            }
            
            return Result.Success;
        }
    }
}
