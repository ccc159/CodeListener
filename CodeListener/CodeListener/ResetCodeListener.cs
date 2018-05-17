using System;
using Rhino;
using Rhino.Commands;

namespace CodeListener
{
    [System.Runtime.InteropServices.Guid("4e5821bd-858e-4183-9107-6c3f4be1783e")]
    public class ResetCodeListener : Command
    {
        static ResetCodeListener _instance;
        public ResetCodeListener()
        {
            _instance = this;
        }

        ///<summary>The only instance of the ResetCodeListener command.</summary>
        public static ResetCodeListener Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "ResetCodeListener"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // TODO: complete command.
            return Result.Success;
        }
    }
}
