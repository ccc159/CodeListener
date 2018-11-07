using System;
using Rhino;
using Rhino.Commands;

namespace CodeListener
{
    [System.Runtime.InteropServices.Guid("4e5821bd-858e-4183-9107-6c3f4be1783e")]
    public class StopCodeListener : Command
    {
        static StopCodeListener _instance;
        public StopCodeListener()
        {
            _instance = this;
        }

        ///<summary>The only instance of the ResetCodeListener command.</summary>
        public static StopCodeListener Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "StopCodeListener"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            if (CodeListenerCommand._server != null)
            {
                CodeListenerCommand._server.Stop();
                CodeListenerCommand._server = null;
                return Result.Success;
            }

            return Result.Cancel;
        }
    }
}
