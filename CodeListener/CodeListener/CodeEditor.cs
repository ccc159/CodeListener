using System;
using System.Collections.Generic;
using System.Drawing;
using Rhino;
using Rhino.Commands;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using Rhino.Runtime;
using Point = System.Drawing.Point;

namespace CodeListener
{
    [System.Runtime.InteropServices.Guid("cbb77122-9c28-4762-8344-fcce6bbed87c")]
    
    public class CodeEditor : Command
    {
        private Form myForm;
        private PythonScript myScript;
        private Control control;
        static CodeEditor _instance;
        public CodeEditor()
        {
            _instance = this;
        }

        ///<summary>The only instance of the CodeEditor command.</summary>
        public static CodeEditor Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "CodeEditor"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            myForm = new Form();
            // create python script runner
            myScript = PythonScript.Create();
            control = myScript.CreateTextEditorControl("", ShowHelp);

            Button runButton = new Button();
            runButton.Location = new Point(10, 10);
            control.Location = new Point(20, 20);
            runButton.Click += RunButtonOnClick;
            // Set the caption bar text of the form.   
            myForm.Text = "Code Editor";
            myForm.Controls.Add(control);
            myForm.Controls.Add(runButton);
            myForm.Show();
            return Result.Success;
        }

     
        private void RunButtonOnClick(object sender, EventArgs eventArgs)
        {
            myScript.ExecuteScript(control.Text);
        }

        protected void ShowHelp(string help)
        {
            RhinoApp.WriteLine(help);
            
        }

    }
}
