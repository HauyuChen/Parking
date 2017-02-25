using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.IO;

namespace ParkingServer
{
    class User
    {
        public TcpClient client { get; private set; }
        public BinaryReader br { get; private set; }
        public BinaryWriter bw { get; private set; }
        public string userName { get; set; }
        public NetworkStream networkStream;


        public User(TcpClient client)
        {
            try
            {
                this.client = client;
                networkStream = client.GetStream();
                br = new BinaryReader(networkStream);
                bw = new BinaryWriter(networkStream);
            }
            catch (Exception){ 
            
            }
        }

        public void Close()
        {
            br.Close();
            bw.Close();
            client.Close();
        }

    }
}
