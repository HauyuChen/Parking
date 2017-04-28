/******************************
** Author：Hauyu.Chen
** Email：mrchenhy@Gmail.com
*/

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
using System.IO;

namespace ParkingServer
{
    public partial class SocketServer : Form
    {
        private List<User> userList = new List<User>();
        private TcpListener myListener;

        Dictionary<string, Socket> dicSocket = new Dictionary<string, Socket>();

        string ZigBee=null;  //标识：ZigBee网络地址
        string WeChat=null;  //标识：WeChat网络地址
        string WinPC=null;   //标识：WinPC网络地址

        bool isNormalExit = false;


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
                myListener = new TcpListener(IPAddress.Parse("127.0.0.1"), Convert.ToInt32("6000"));
                myListener.Start();
                Logger("  服务器启动成功！");

                //启动线程
                Thread th = new Thread(Accept);
                th.IsBackground = true;
                th.Start();

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
        private void Accept()
        {
            TcpClient newClient = null;
            while (true)
            {

                try
                {
                    newClient = myListener.AcceptTcpClient();
                }
                catch(Exception)
                {
                    break;
                }
                User user = new User(newClient);
                Thread threadReceive = new Thread(Receive);
                threadReceive.Start(user);
                userList.Add(user);
            }
        }
        #endregion

        /******************************
         ** Receive：接收数据
         */
        #region Receive
        private void Receive(object userState)
        {
            User user = (User)userState;
            TcpClient client = user.client;
            
            while (isNormalExit == false) {
                string receiveString = null;
                try
                {
                    //从网络流中读出字符串，此方法会自动判断字符串长度前缀
                    byte[] buffer = new byte[100];
                    int count = user.br.Read(buffer, 0, 100);
                    receiveString = Encoding.Default.GetString(buffer).Substring(0, count);

                    string target = receiveString.Substring(0, 2);
                    user.userName = user.client.Client.RemoteEndPoint.ToString();
                    switch (target)
                    {


                        /* ZigBee端消息处理 */
                        case "ZB":
                            ZigBee = user.client.Client.RemoteEndPoint.ToString();
                            Logger("  ZigBee端：" + ZigBee);

                            doZigBee(receiveString);                               //ZigBee端消息处理

                            break;

                        /* WeChat端消息处理 */
                        case "WC":
                            WeChat = user.client.Client.RemoteEndPoint.ToString();
                            Logger("  WeChat端：" + WeChat);

                            doWeChat(receiveString);                               //WeChat端消息处理

                            break;

                        /* WinPC端消息处理 */
                        case "PC":
                            WinPC = user.client.Client.RemoteEndPoint.ToString();
                            Logger("  WinPC端：" + WinPC);

                            doWinPC(receiveString);                                //WinPC端消息处理

                            break;
                        default:
                            //Logger("Hi");
                            break;
                    }         
                }
                catch (Exception)
                {
                    if (isNormalExit == false)
                    {
                        RemoveUser(user);
                    }
                    break;
                }

                

            }

            

        }
        #endregion

        /******************************
         ** SendMsg：向特定的客户端发送数据
         */
        private void SendMsg(string obj,string msg) {
            for (int i = 0; i < userList.Count; i++)
            {
                if (userList[i].userName == obj)
                {
                    SendToClient(userList[i], msg);
                }
            }
        }

        #region Handle
        /******************************
         ** doZigBee：ZigBee端消息处理
         */
        private void doZigBee(string str) 
        {
            string sqlcmd;
            string data;
            int idle=0;

            /* 数据转发：将ZigBee端发送过来的数据转发给WinPC端 */
            if (WinPC != null)
            {
                data = str.Remove(0, 2);
                Logger("  数据转发>>>ZigBee向WinPC转发数据：" + data);
                SendMsg(WinPC, data);
            }

            /* 数据转发：将ZigBee端发送过来的数据转发给WeChat端 */
            if (WeChat != null)
            {
                data = str.Remove(0, 2);
                Logger("  数据转发>>>ZigBee向WeChat转发数据：" + data);
                SendMsg(WeChat, data);
            }



            /* 更新数据库 */
            if (str.Substring(2,1) != null && str.Substring(2,1) == "A")  //数据：ZBA...........
            {    
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
                    if (str.Substring(i - 1, 1) == "0")
                    {
                        idle++; //空闲车位计数
                    }
                    MySqlHelper.ExecuteNonQuery(MySqlHelper.Conn, CommandType.Text, sqlcmd, null);     //更新数据库
                }
                Logger("  数据库操作>>>成功更新A区车位信息");
                /* 数据回传：将计算后的空闲车位数回传给ZigBee端 */
                if (idle < 10)
                {
                    SendMsg(ZigBee, "PA0" + idle.ToString());
                    Logger("  A区空闲车位数：" + idle.ToString());
                }
                else {
                    SendMsg(ZigBee, "PA" + idle.ToString());
                    Logger("  A区空闲车位数：" + idle.ToString());
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
                    if (str.Substring(i - 1, 1) == "0")
                    {
                        idle++; //空闲车位计数
                    }
                    MySqlHelper.ExecuteNonQuery(MySqlHelper.Conn, CommandType.Text, sqlcmd, null);   //更新数据库
                }
                Logger("  数据库操作>>>成功更新B区车位信息");
                /* 数据回传：将计算后的空闲车位数回传给ZigBee端 */
                if (idle < 10)
                {
                    SendMsg(ZigBee, "PB0" + idle.ToString());
                    Logger("  B区空闲车位数：" + idle.ToString());
                }
                else
                {
                    SendMsg(ZigBee, "PB" + idle.ToString());
                    Logger("  B区空闲车位数：" + idle.ToString());
                }
            }
            else if (str.Substring(2, 5) == "LIGHT")    //数据：ZBLIGHT00123lx,status=1
            {
                sqlcmd = "UPDATE light SET lx='" + str.Substring(7, 5) + "',state=" + str.Substring(22, 1);
                MySqlHelper.ExecuteNonQuery(MySqlHelper.Conn, CommandType.Text, sqlcmd, null);      //更新数据库
                Logger("  数据库操作>>>成功更新照明信息");
            }
            
        }


        /******************************
         ** doWeChat：WeChat端消息处理
         */
        private void doWeChat(string str)
        {
            Functionality func = new Functionality();
            try
            {
                if (str.Substring(2, 6) == "search")    //Done
                {
                    string msg = func.searchFree();
                    Logger("停车场空闲车位数：" + msg);
                }
                else if (str.Substring(0, 6) == "insert")
                {
                    //func.addUser(str);
                }
                else if (str.Substring(0, 6) == "update")
                {
                    //func.updateInfo();
                }
                else if (str.Substring(0, 3) == "use")
                {
                    //func.positionUsed();
                }
                else if (str.Substring(0, 4) == "free")
                {
                    //func.positionFree();
                }
                else if (str.Substring(2, 2) == "Li")   //Done
                {
                    SendMsg(ZigBee,str.Remove(0,2));
                }
                else if (str.Substring(0, 4) == "look")
                {
                    //func.userCheck();
                }
                else if (str.Substring(0, 5) == "admin")
                {

                }
                else if (str.Substring(0, 7) == "unadmin")
                {

                }
            }
            catch(Exception){
                
            }

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
            string data;
            /* 数据转发：将WinPC端发送过来的数据转发给ZigBee端 */
            if (ZigBee != null)
            {
                data = str.Remove(0, 2);
                Logger("  数据转发>>>WinPC向ZigBee转发数据：" + data);
                SendMsg(ZigBee, data);
            }
        }
        #endregion

        /******************************
         ** SendData：发送数据给指定的端口
         */
        //private void SendData(string msg,string endpoint) {
        //    try
        //    {
        //        byte[] buffer = Encoding.UTF8.GetBytes(msg);
        //        dicSocket[endpoint].Send(buffer);
        //    }
        //    catch (Exception)
        //    {

        //    }
        //}

        /******************************
         ** Logger：窗口显示发送的数据内容
         */
        #region Logger
        public void Logger(string strMsg)
        {
            txtLog.AppendText("<--服务日志--> " + DateTime.Now + "\r\n" + strMsg + "\r\n");
        }
        #endregion

        /******************************
         ** SendToClient：将数据写入特定客户端的网络流
         */
        private void SendToClient(User user, string message)
        {
            try
            {
                //将字符串写入网络流，此方法会自动附加字符串长度前缀
                user.bw.Write(message);
                user.bw.Flush();
            }
            catch
            {
                
            }
        }

        /******************************
         ** RemoveUser：删除用户
         */
        private void RemoveUser(User user)
        {
            userList.Remove(user);
            user.Close();
        }


        /******************************
         ** MainForm_FormClosing：关闭本程序时的处理
         */
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (myListener != null) {
                isNormalExit = true;
                for (int i = userList.Count - 1; i >= 0; i--)
                {
                    RemoveUser(userList[i]);
                }
                //通过停止监听让 myListener.AcceptTcpClient() 产生异常退出监听线程
                myListener.Stop();
            }
        }
    }
}
