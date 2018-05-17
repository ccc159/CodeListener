using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Text;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using System.Runtime.Serialization.Json;
using System.Text.RegularExpressions;
using System.Windows;
using Rhino.Runtime;


namespace CodeListener
{
    public class CodeListenerCommand : Command
    {
        internal static BackgroundWorker _tcpServerWorker;
        private RhinoDoc _idoc;
        private TcpListener _server;
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
            _idoc = doc;
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
                RhinoApp.WriteLine(err.ToString());
            }

            while (true)
            {
                // incoming client connected
                TcpClient client = _server.AcceptTcpClient();

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
                Application.Current.Dispatcher.Invoke(() =>
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
                        try
                        {
                            uint sn = _idoc.BeginUndoRecord("VS Code execution");
                            myScript.ExecuteFile(msgObj.filename);
                            _idoc.EndUndoRecord(sn);
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
