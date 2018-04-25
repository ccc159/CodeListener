using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CodeListener
{
    class FeedbackSender
    {
        private NetworkStream stream;

        public FeedbackSender(NetworkStream istream)
        {
            stream = istream;
        }

        public void OnGotCodeFeedBack(object source, CodeListenerCommand.GotCodeFeedbackEventArgs e)
        {
            // Process the data sent by the client.
            byte[] errMsgBytes = Encoding.ASCII.GetBytes(e.Message);
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
