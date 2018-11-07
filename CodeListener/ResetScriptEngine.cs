using System;
using System.IO;
using System.Text;
using Rhino;
using Rhino.Commands;

namespace CodeListener
{
    [System.Runtime.InteropServices.Guid("78d32084-0d9a-41b4-99f6-83740f67fb48"),
     Rhino.Commands.CommandStyle(Rhino.Commands.Style.ScriptRunner)]
    public class ResetScriptEngine : Command
    {
        static ResetScriptEngine _instance;
        public ResetScriptEngine()
        {
            _instance = this;
        }

        ///<summary>The only instance of the Reset command.</summary>
        public static ResetScriptEngine Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "ResetScriptEngine"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            try
            {
                ResetEngine();
                return Result.Success;
            }
            catch (Exception exception)
            {
                return Result.Failure;
            }
        }

        public static void ResetEngine()
        {
            // shut down host
            RhinoPython.Host.ShutDown();

            // create a new resetscriptengine.py python file
            string tempPath = Path.GetTempPath();
            string filepath = tempPath + "resetscriptengine.py";
            try
            {
                if (!File.Exists(filepath))
                {
                    // Create the file.
                    using (FileStream fs = File.Create(filepath))
                    {
                        Byte[] info = new UTF8Encoding(true).GetBytes("print \"Python script engine has been reset.\"");
                        fs.Write(info, 0, info.Length);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            // run this file through RhinoApp Command to initialize a new python script engine.
            var script = "-_EditPythonScript Debugging=Off \n(\n" + filepath + "\n)\n";
            RhinoApp.RunScript(script, true);
        }
    }
}
