using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System.Threading;
using System.Windows;
using Rhino.Runtime;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;


namespace CodeListener
{
    public class CodeListenerCommand : Command
    {
        private static BackgroundWorker _tcpServerWorker;
        private Application _app;
        private string _output;

        public CodeListenerCommand()
        {
            // Rhino only creates one instance of each command class defined in a
            // plug-in, so it is safe to store a refence in a static property.
            
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static CodeListenerCommand Instance
        {
            get; private set;
        }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName
        {
            get { return "CodeListener"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // Start WPF UI Dispatcher if not running.
            if (_app == null)
            {
                _app = new Application();
            }

            // set up the listenner
            if (_tcpServerWorker != null && _tcpServerWorker.IsBusy)
            {
                RhinoApp.WriteLine("VS Code Listener is running.", EnglishName);
                return Result.Cancel;
            };

            // Start the worker thread
            _tcpServerWorker = new BackgroundWorker();
            _tcpServerWorker.DoWork += TcpServerWorkerListening;
            _tcpServerWorker.RunWorkerCompleted += TcpServerWorkerRunTcpServerWorkerCompleted;
            _tcpServerWorker.RunWorkerAsync();

            return Result.Success;
        }

        // fire this function when the background worker has stopped
        void TcpServerWorkerRunTcpServerWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            RhinoApp.WriteLine("VS Code Listener stopped.", EnglishName);
        }

        // the main listener function
        void TcpServerWorkerListening(object sender, DoWorkEventArgs e)
        {
            //---listen at the specified IP and port no.---
            const int portNo = 614;
            IPAddress serverIp = IPAddress.Parse("127.0.0.1");
            TcpListener server = new TcpListener(serverIp, portNo);
            
            try
            {
                server.Start();
                RhinoApp.WriteLine("VS Code Listener Started...");
            }
            catch (Exception err)
            {
                RhinoApp.WriteLine(err.ToString());
            }

            while (true)
            {
                // incoming client connected
                TcpClient client = server.AcceptTcpClient();

                // get the incoming data through a network stream
                NetworkStream nwStream = client.GetStream();
                byte[] buffer = new byte[client.ReceiveBufferSize];

                // read incoming stream
                int bytesRead = nwStream.Read(buffer, 0, client.ReceiveBufferSize);

                // convert the data received into a string
                StringBuilder msg = new StringBuilder();

                // parse the buffer into msg
                foreach (var b in buffer)
                {
                    if (b.Equals(00)) break;
                    msg.Append(Convert.ToChar(b).ToString());
                }

                string path = @msg.ToString();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    // create python script runner
                    PythonScript myScript = PythonScript.Create();

                    _output = "";
                    myScript.Output = PrintToVSCode;

                    //// create a new python scriptengine with trace and frame enabled
                    //Dictionary<string, object> options = new Dictionary<string, object>();
                    //options["Debug"] = true;
                    //options["Tracing"] = true;
                    //options["Frames"] = true;
                    //options["FullFrames"] = true;

                    //ScriptEngine myScriptEngine = Python.CreateEngine(options);

                    //// create a output redirector
                    //MemoryStream ms = new MemoryStream();
                    //myScriptEngine.Runtime.IO.SetOutput(ms, new StreamWriter(ms));

                    //// load rhino assembly
                    //myScriptEngine.Runtime.LoadAssembly(Assembly.LoadFrom(@"C:\Program Files\Rhinoceros 5 (64-bit)\System\RhinoCommon.dll"));

                    //// load extra python libraries
                    //ICollection<string> paths = myScriptEngine.GetSearchPaths();
                    //List<string> newpaths = new List<string>()
                    //{
                    //    @"C:\Program Files (x86)\IronPython 2.7\Lib",
                    //    @"C:\Program Files\Rhinoceros 5 (64-bit)\Plug-ins\IronPython\Lib",
                    //    @"C:\Users\jch\AppData\Roaming\McNeel\Rhinoceros\5.0\Plug-ins\IronPython (814d908a-e25c-493d-97e9-ee3861957f49)\settings\lib",
                    //    @"C:\Users\jch\AppData\Roaming\McNeel\Rhinoceros\5.0\scripts"
                    //};
                    //foreach (string newpath in newpaths)
                    //{
                    //    paths.Add(newpath);
                    //}
                    //myScriptEngine.SetSearchPaths(paths);
                    //ScriptScope scope = myScriptEngine.CreateScope();

                    try
                    {
                        // run self python script engine
                        //myScriptEngine.ExecuteFile(path, scope);
                        //string str = ReadFromStream(ms);
                        //RhinoApp.WriteLine(str);
                        //PrintToVSCode(str);

                        // run pythonscript
                        myScript.ExecuteFile(path);
                        SendFeedback(_output, nwStream);
                    }
                    catch (Exception ee)
                    {
                        var error = myScript.GetStackTraceFromException(ee);
                        string message = ee.Message + "\n" + error;
                        // send error msg back to client
                        SendFeedback(message, nwStream);
                    }
                });
            }
        }

        private static string ReadFromStream(MemoryStream ms)
        {
            int length = (int)ms.Length;
            Byte[] bytes = new Byte[length];
            ms.Seek(0, SeekOrigin.Begin);
            ms.Read(bytes, 0, (int)ms.Length);
            return Encoding.GetEncoding("utf-8").GetString(bytes, 0, (int)ms.Length);
        }

        protected void PrintToVSCode(string m)
        {
            RhinoApp.Write(m);
            _output += m;
        }

        protected void SendFeedback(string msg, NetworkStream stream)
        {
            // Process the data sent by the client.
            byte[] errMsgBytes = Encoding.ASCII.GetBytes(msg);
            // Send back a response.
            try
            {
                stream.Write(errMsgBytes, 0, errMsgBytes.Length);
                stream.Close();
            }
            catch (Exception exception)
            {
                stream.Close();
            }
        }
    }
}
