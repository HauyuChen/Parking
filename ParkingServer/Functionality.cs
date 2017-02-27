using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace ParkingServer
{
    class Functionality
    {
        /* 返回空闲车位数 */
        public string searchFree() {
            //string sqltext = "SELECT count(Pstatus=0) FROM `parking` WHERE Pid like 'A%%%'";
            string sqltext = "SELECT count(Pstatus=0) FROM `parking`";
            string msg = MySqlHelper.ExecuteScalar(MySqlHelper.Conn, CommandType.Text, sqltext, null).ToString();
            return msg;
            //SELECT count(Pstatus=0) FROM `parking` WHERE Pid like 'B%%%';
        }

        public void addUser(string str)
        {
            //string[] values = str.Split('\n');

        }

        public void updateInfo()
        {

        }

        public void positionUsed()
        {

        }

        public void positionFree()
        {

        }

        public void userCheck()
        {

        }
    }
}
