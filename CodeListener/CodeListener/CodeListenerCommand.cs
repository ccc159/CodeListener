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


namespace CodeListener
{
    public class CodeListenerCommand : Command
    {
        private static BackgroundWorker _tcpServerWorker;
        private Application _app;

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

        void TcpServerWorkerRunTcpServerWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            RhinoApp.WriteLine("VS Code Listener stopped.", EnglishName);
        }

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
                    myScript.Output = PrintToVSCode;
                    FeedbackSender feedbackSender = new FeedbackSender(nwStream);
                    this.GotCodeFeekBack += feedbackSender.OnGotCodeFeedBack;

                    try
                    {
                        myScript.ExecuteFile(path);
                    }
                    catch (Microsoft.Scripting.SyntaxErrorException exception)
                    {
                        FieldInfo sorceLineInfo = exception.GetType()
                            .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                            .Single(f => f.Name == "_sourceLine");
                        string sourceLine = sorceLineInfo.GetValue(exception) as string;

                        string message =
                            $"SyntaxError: {exception.Message}\n Line {exception.Line}, \"{exception.SourcePath}\"\n{sourceLine}";
                        PrintToVSCode(message);
                    }
                    // known two exceptions
                    catch (Exception exception) when ( exception is IronPython.Runtime.UnboundNameException || exception is IronPython.Runtime.Exceptions.ImportException )
                    {
                        // parse the exception into messages
                        var values = exception.Data.Values;
                        var valuesarr = new object[values.Count];
                        values.CopyTo(valuesarr, 0);
                        string errMsg = $"Message: {exception.Message}\n";
                        string errTraces = "Traceback:\n";

                        var infos = (Microsoft.Scripting.Interpreter.InterpretedFrameInfo[]) (valuesarr[0]);
                        for (int j = 0; j < infos.Length; j++)
                        {
                            var debugInfo = infos[j].DebugInfo;
                            errTraces +=
                                $"    Line {debugInfo.StartLine} - {debugInfo.EndLine}, \"{debugInfo.FileName}\"\n";
                        }

                        // the error msg
                        string message = errMsg + errTraces;

                        // send error msg back to client
                        PrintToVSCode(message);

                    }
                    catch (Exception exception)
                    {
                        try
                        {
                            // parse the exception into messages
                            var values = exception.Data.Values;
                            var valuesarr = new object[values.Count];
                            values.CopyTo(valuesarr, 0);
                            string errMsg = $"Message: {exception.Message}\n";
                            string errTraces = "Traceback:\n";

                            var infos = (Microsoft.Scripting.Interpreter.InterpretedFrameInfo[])(valuesarr[0]);
                            for (int j = 0; j < infos.Length; j++)
                            {
                                var debugInfo = infos[j].DebugInfo;
                                errTraces +=
                                    $"    Line {debugInfo.StartLine} - {debugInfo.EndLine}, \"{debugInfo.FileName}\"\n";
                            }

                            // the error msg
                            string message = errMsg + errTraces;

                            // send error msg back to client
                            PrintToVSCode(message);
                        }
                        catch (Exception e1)
                        {
                            string message = "Unhandeled exception:\n" + exception.Message;
                            // send error msg back to client
                            PrintToVSCode(message);
                        }
                    }
                    
                });
            }
        }

        public class GotCodeFeedbackEventArgs : EventArgs
        {
            public string Message { get; set; }
        }

        public delegate void CodeFeedBackEventHandler(object source, GotCodeFeedbackEventArgs e);

        public event CodeFeedBackEventHandler GotCodeFeekBack;

        protected virtual void OnGotCodeFeedBack(GotCodeFeedbackEventArgs e)
        {
            GotCodeFeekBack?.Invoke(this, e);
        }

        protected void PrintToVSCode(string m)
        {
            GotCodeFeedbackEventArgs arg = new GotCodeFeedbackEventArgs { Message = m };
            OnGotCodeFeedBack(arg);
        }

    }
}
