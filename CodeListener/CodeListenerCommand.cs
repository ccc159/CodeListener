using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Text;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using System.Runtime.Serialization.Json;
using System.Text.RegularExpressions;
using System.Windows;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Runtime;
using Command = Rhino.Commands.Command;
using MessageBox = System.Windows.MessageBox;
#if RHINO6
using Eto.Forms;
#endif


namespace CodeListener
{
    public class CodeListenerCommand : Command
    {
        internal static BackgroundWorker _tcpServerWorker;
        private RhinoDoc _idoc;
        internal static TcpListener _server;

        #if RHINO5
        private Application _app;
        #endif


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
            _idoc = doc;
            CheckLatestVersion();
            #if RHINO5
            // Start WPF UI Dispatcher if not running.
            if (_app == null)
            {
                _app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };     
            }
            #endif
            // set up the listenner
            if (_tcpServerWorker != null && _tcpServerWorker.IsBusy)
            {
                RhinoApp.WriteLine("VS Code Listener is running.", EnglishName);
                return Result.Cancel;
            };

            // Start the worker thread
            _tcpServerWorker = new BackgroundWorker();
            _tcpServerWorker.WorkerSupportsCancellation = true;
            _tcpServerWorker.DoWork += TcpServerWorkerListening;
            _tcpServerWorker.RunWorkerCompleted += TcpServerWorkerRunTcpServerWorkerCompleted;
            _tcpServerWorker.RunWorkerAsync();
            

            return Result.Success;
        }

        // fire this function when the background worker has stopped
        protected void TcpServerWorkerRunTcpServerWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            _tcpServerWorker.Dispose();
            _tcpServerWorker = null;
            RhinoApp.WriteLine("VS Code Listener stopped. Please run CodeListener again.");
        }

        // the main listener function
        protected void TcpServerWorkerListening(object sender, DoWorkEventArgs e)
        {
            //---listen at the specified IP and port no.---
            const int portNo = 614;
            IPAddress serverIp = IPAddress.Parse("127.0.0.1");
            if (_server == null) _server = new TcpListener(serverIp, portNo);  
            try
            {
                _server.Start();
                RhinoApp.WriteLine("VS Code Listener Started...");
            }
            catch (Exception err)
            {
                RhinoApp.WriteLine("Error start Code Listener. Is other Rhino instance occupying?");
                RhinoApp.WriteLine(err.ToString());
            }

            while (true)
            {
                // incoming client connected
                TcpClient client;
                try
                {
                    client = _server.AcceptTcpClient();
                }
                catch (Exception serverException)
                {
                    return;
                }

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

                // parse the received message into C# Object
                string msgString = msg.ToString();
                msgString = Regex.Split(msgString, "}")[0] + "}";
                MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(msgString));
                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(msgObject));
                msgObject msgObj;
                try
                {
                    msgObj = ser.ReadObject(ms) as msgObject;
                }
                catch (Exception ex)
                {
                    RhinoApp.WriteLine("Received invalid data, please try again.");
                    return;
                }
                
                ms.Close();

                // invoke the main task in the main thread
                #if RHINO5
                _app.Dispatcher.Invoke(() =>
                #else
                Eto.Forms.Application.Instance.Invoke(() =>
                #endif
                {
                    // create python script runner
                    PythonScript myScript = PythonScript.Create();
                    // redirect output to _output field
                    myScript.Output = PrintToVSCode;
                    FeedbackSender feedbackSender = new FeedbackSender(nwStream);
                    GotCodeFeekBack += feedbackSender.OnGotCodeFeedBack;

                    // if flagged reset, then reset the script engine.
                    if (msgObj.reset)
                    {
                        ResetScriptEngine.ResetEngine();
                    }

                    // if it is not a temp folder, add the folder to python library path
                    if (!msgObj.temp)
                    {
                        string pythonFilePath = Path.GetDirectoryName(msgObj.filename);
                        string code = string.Format("import sys\nimport os\nif \"{0}\" not in sys.path: sys.path.append(\"{0}\")", pythonFilePath);
                        try
                        {
                            myScript.ExecuteScript(code);
                        }
                        catch (Exception exception)
                        {
                            PrintToVSCode(exception.Message);
                        }
                    }

                    // determines if run actual script
                    if (msgObj.run)
                    {
                        uint sn = _idoc.BeginUndoRecord("VS Code execution");
                        var sn_start = RhinoObject.NextRuntimeSerialNumber;
                        try
                        {
                            myScript.ExecuteFile(msgObj.filename);
                        }
                        catch (Exception ex)
                        {
                            // get the exception message
                            var error = myScript.GetStackTraceFromException(ex);
                            string message = ex.Message + "\n" + error;
                            // send exception msg back to VS Code
                            PrintToVSCode(message);
                        }
                        finally
                        {
                            CloseConnection(nwStream);
                            _idoc.EndUndoRecord(sn);
                            // fix the rs.Prompt bug
                            RhinoApp.SetCommandPrompt("Command");

                            // select created objects
                            var sn_end = RhinoObject.NextRuntimeSerialNumber;
                            if (sn_end > sn_start)
                            {
                                for (var i = sn_start; i < sn_end; i++)
                                {
                                    var obj = _idoc.Objects.Find(i);
                                    if (null != obj)
                                    {
                                        obj.Select(true);
                                    }
                                }
                            }
                            // enable the view
                            _idoc.Views.RedrawEnabled = true;
                        }
                    }
                    else
                    {
                        CloseConnection(nwStream);
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

        // add the action to redirect output to VS Code
        protected void PrintToVSCode(string m)
        {
            RhinoApp.Write(m);
            GotCodeFeedbackEventArgs arg = new GotCodeFeedbackEventArgs { Message = m };
            OnGotCodeFeedBack(arg);
        }

        // close connection
        protected void CloseConnection(NetworkStream stream)
        {
            stream.Close();
        }

        // check plugin versions
        protected async void CheckLatestVersion()
        {
            // hardcoded github latest releases
            string sURL = "https://api.github.com/repos/ccc159/CodeListener/releases/latest";
            string pageURL = "https://github.com/ccc159/CodeListener/releases/latest";
            var client = new HttpClient();
            ServicePointManager.SecurityProtocol =
                SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            client.DefaultRequestHeaders.Add("User-Agent", "CodeListener");
            try
            {
                var uri = new Uri(sURL);
                Stream respStream = await client.GetStreamAsync(uri);
                StreamReader reader = new StreamReader(respStream);
                string responseFromServer = reader.ReadToEnd();
                var index = responseFromServer.IndexOf("tag_name", StringComparison.Ordinal);
                var version = (responseFromServer.Substring(index + 11, 10).Split('\"')[0]).Split('.');
                // version Major.Minor.Patch -> 0.1.5
                int major = Int32.Parse(version[0]);
                int minor = Int32.Parse(version[1]);
                int patch = Int32.Parse(version[2]);
                if (CodeListenerVersion.MAJOR < major || CodeListenerVersion.MINOR < minor ||
                    CodeListenerVersion.PATCH < patch)
                {
                    var msg = $"CodeListener has new a version {major}.{minor}.{patch}! Go to download page?";
                    var result = MessageBox.Show(msg, "New Version", MessageBoxButton.OKCancel,
                        MessageBoxImage.Information);
                    if (result == MessageBoxResult.OK) System.Diagnostics.Process.Start(pageURL);
                }
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine("Check new version failed.");
            }
        }
    }

    // define the message object structure that received from VS Code
    [DataContract]
    public class msgObject
    {
        [DataMember]
        internal bool run;
        [DataMember]
        internal bool temp;
        [DataMember]
        internal bool reset;
        [DataMember]
        internal string filename;
    }
}
