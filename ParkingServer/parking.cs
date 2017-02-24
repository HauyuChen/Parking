using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace ParkingServer
{
    public partial class SocketServer : Form
    {
        Socket socketWatch;
        Socket socketSend;
        Dictionary<string, Socket> dicSocket = new Dictionary<string, Socket>();
        string ZigBee="";  //标识：ZigBee网络地址
        string WeChat="";  //标识：WeChat网络地址
        string WinPC="";   //标识：WinPC网络地址

        public SocketServer()
        {
            InitializeComponent();
        }

        #region SocketServer_Load
        private void SocketServer_Load(object sender, EventArgs e)
        {
            //取消跨线程检查
            Control.CheckForIllegalCrossThreadCalls = false;
        }
        #endregion


        /******************************
         ** 按钮：启动服务器
         */
        #region btnStartBind_Click
        private void btnStartBind_Click(object sender, EventArgs e)
        {
            try
            {
                socketWatch = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint point = new IPEndPoint(IPAddress.Parse("127.0.0.1"), Convert.ToInt32("10086"));
                socketWatch.Bind(point);
                Logger("启动服务器成功。");
                socketWatch.Listen(10);

                //启动线程
                Thread th = new Thread(Accept);
                th.IsBackground = true;
                th.Start(socketWatch);

                //禁用StartBind button
                btnStartBind.Enabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        #endregion


        /******************************
         ** 按钮：发送数据
         */
        #region btnSendMsg_Click
        private void btnSendMsg_Click(object sender, EventArgs e)
        {
            try
            {
                string strMsg = txtMsg.Text.Trim();
                byte[] buffer = Encoding.UTF8.GetBytes(strMsg);
                string selectedIP = cboUserList.SelectedItem.ToString();
                dicSocket[selectedIP].Send(buffer);

                ShowMsgLocal(string.Format("{0}--{1}:\r\n{2}", "LocalHost", DateTime.Now.ToString(), strMsg));
                //清除文本框中的内容
                txtMsg.Clear();
            }
            catch (Exception)
            {
            }
        }
        #endregion


        /******************************
         ** Accept：接收连接请求
         */
        #region Accept
        private void Accept(object socketObj)
        {
            try
            {
                Socket socketWatch = socketObj as Socket;
                while (true)
                {
                    socketSend = socketWatch.Accept();
                    Logger(socketSend.RemoteEndPoint.ToString() + "连接成功");
                    dicSocket.Add(socketSend.RemoteEndPoint.ToString(), socketSend);
                    cboUserList.Items.Add(socketSend.RemoteEndPoint.ToString());

                    //设置Combobox的默认值
                    if (cboUserList.Items.Count > 0)
                    {
                        cboUserList.SelectedIndex = 0;
                    }

                    //启动线程
                    Thread th = new Thread(Receive);
                    th.IsBackground = true;
                    th.Start();
                }
            }
            catch (Exception)
            {
            }
        }
        #endregion


        /******************************
         ** Receive：接收数据
         */
        #region Receive
        private void Receive()
        {
            try
            {
                while (true)
                {
                    byte[] buffer = new byte[1024 * 1024 * 5];
                    int r = socketSend.Receive(buffer);
                    if (r == 0)
                    {
                        break;
                    }
                    string strMsg = Encoding.UTF8.GetString(buffer, 0, r);

                    switch(strMsg.Substring(0,2)){

                        /* ZigBee端消息处理 */ 
                        case "ZB":
                            if (ZigBee == "") {
                                ZigBee = socketSend.RemoteEndPoint.ToString();  //保存ZigBee网络地址
                            }
                            
                            doZigBee(strMsg);                               //ZigBee端消息处理
                            break;

                        /* WeChat端消息处理 */ 
                        case "WC":  
                            WeChat = socketSend.RemoteEndPoint.ToString();  //保存WeChat网络地址
                            doWeChat(strMsg);                               //WeChat端消息处理
                            break;

                        /* WinPC端消息处理 */ 
                        case "PC":
                            if (WinPC == "") {
                                WinPC = socketSend.RemoteEndPoint.ToString();   //保存WinPC网络地址
                            }
                            
                            doWinPC(strMsg);                                //WinPC端消息处理
                            break;
                    }         
                }
            }
            catch (Exception)
            {
            }

        }
        #endregion



        //private void MsgSend(string msg,EndPoint SS) {
        //    try
        //    {
        //        //string strMsg = txtMsg.Text.Trim();
        //        byte[] buffer = Encoding.UTF8.GetBytes(msg);
        //        //string selectedIP = cboUserList.SelectedItem.ToString();
        //        socketSend.SendTo(buffer,SS);
        //        Logger(SS.ToString());
               
        //        //清除文本框中的内容
        //        //txtMsg.Clear();
        //    }
        //    catch (Exception)
        //    {
        //    }
        //}


        #region Handle
        /******************************
         ** doZigBee：ZigBee端消息处理
         */
        private void doZigBee(string str) 
        {
            string sqlcmd;
            //string newstr = str.Remove(0, 2);
            Logger("ZigBee向WinPC转发数据：" + str);
            Logger(WinPC);
            SendData(str,WinPC);
            
            if(str.Substring(2,1)=="A"){    //数据：ZBA...........
                for (int i = 4; i <= str.Length; i++)
                {
                    if (i < 13)
                    {
                        sqlcmd = "UPDATE parking SET Pstatus=" + str.Substring(i - 1, 1) + " WHERE Pid='A00" + (i - 3).ToString() + "'";
                    }
                    else
                    {
                        sqlcmd = "UPDATE parking SET Pstatus=" + str.Substring(i - 1, 1) + " WHERE Pid='A0" + (i - 3).ToString() + "'";
                    }
                    MySqlHelper.ExecuteNonQuery(MySqlHelper.Conn, CommandType.Text, sqlcmd, null);     //更新数据库
                    Logger("更新数据库：" + sqlcmd);
                }
                    
            }
            else if (str.Substring(2, 1) == "B")    //数据：ZBB...........
            {
                for (int i = 4; i <= str.Length; i++)
                {
                    if (i < 13)
                    {
                        sqlcmd = "UPDATE parking SET Pstatus=" + str.Substring(i - 1, 1) + " WHERE Pid='B00" + (i - 3).ToString() + "'";
                    }
                    else
                    {
                        sqlcmd = "UPDATE parking SET Pstatus=" + str.Substring(i - 1, 1) + " WHERE Pid='B0" + (i - 3).ToString() + "'";
                    }
                    MySqlHelper.ExecuteNonQuery(MySqlHelper.Conn, CommandType.Text, sqlcmd, null);   //更新数据库
                    Logger("更新数据库：" + sqlcmd);
                }
            }
            else if (str.Substring(2, 5) == "LIGHT")    //数据：ZBLIGHT...........
            {
                sqlcmd = "UPDATE light SET lx='" + str.Substring(7, 5) + "',state=" + str.Substring(22, 1);
                MySqlHelper.ExecuteNonQuery(MySqlHelper.Conn, CommandType.Text, sqlcmd, null);      //更新数据库
                Logger("更新数据库：" + sqlcmd);
            }
            
        }


        /******************************
         ** doWeChat：WeChat端消息处理
         */
        private void doWeChat(string str)
        {
            //DataTable table = MySqlHelper.GetDataSet(MySqlHelper.Conn, CommandType.Text, "select * from parking", null).Tables[0];
            //DataRowCollection rows = table.Rows;
            //for (int i = 0; i < rows.Count; i++)
            //{
            //    DataRow row = rows[i];
            //    string msg = (string)row["Pid"];
            //}
        }


        /******************************
         ** doWinPC：WinPC端消息处理
         */
        private void doWinPC(string str)
        {
            string newstr = str.Remove(0, 2);
            switch (newstr)
            {
                case "LIGHTON":
                    Logger("WinPC向ZigBee转发数据：" + newstr);
                    Logger(ZigBee);
                    SendData(newstr, ZigBee);
                    
                    break;
                case "LIGHTOFF":
                    SendData(newstr, ZigBee);
                    Logger("WinPC向ZigBee转发数据：" + newstr);
                    break;
                case "MANUAL":
                    SendData(newstr, ZigBee);
                    Logger("WinPC向ZigBee转发数据：" + newstr);
                    break;
                case "SELFCONTROL":
                    SendData(newstr, ZigBee);
                    Logger("WinPC向ZigBee转发数据：" + newstr);
                    break;
                default: 
                    break;
            }

        }
        #endregion

        /******************************
         ** SendData：发送数据给指定的端口
         */
        private void SendData(string msg,string endpoint) {
            try
            {
                byte[] buffer = Encoding.UTF8.GetBytes(msg);
                dicSocket[endpoint].Send(buffer);
            }
            catch (Exception)
            {

            }
        }

        /******************************
         ** Logger：窗口显示发送的数据内容
         */
        #region Logger
        private void Logger(string strMsg)
        {
            txtLog.AppendText("<--服务日志-->" + strMsg + "\r\n");
        }
        #endregion

        #region ShowMsgLocal
        private void ShowMsgLocal(string strMsg)
        {
            txtLog.AppendText("<--服务日志-->" + strMsg + "\r\n");
        }
        #endregion

        private void SocketServer_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                socketSend.Close();
            }
            catch (Exception)
            {
            }
        }

    }
}
