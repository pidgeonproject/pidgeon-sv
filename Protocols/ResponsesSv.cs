//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or   
//  (at your option) version 3.                                         

//  This program is distributed in the hope that it will be useful,     
//  but WITHOUT ANY WARRANTY; without even the implied warranty of      
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the       
//  GNU General Public License for more details.                        

//  You should have received a copy of the GNU General Public License   
//  along with this program; if not, write to the                       
//  Free Software Foundation, Inc.,                                     
//  51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

using System;
using System.Xml;
using System.Threading;
using System.Collections.Generic;
using System.Text;

namespace pidgeon_sv
{
    public partial class ProtocolSv
    {
        private static class ResponsesSv
        {
            /// <summary>
            /// Load
            /// </summary>
            /// <param name="curr">Node</param>
            /// <param name="protocol">Protocol</param>
            public static void sLoad(XmlNode curr, ProtocolSv protocol)
            {
                SystemLog.WriteLine(curr.InnerText);
            }

            /// <summary>
            /// Status
            /// </summary>
            /// <param name="curr">XmlNode</param>
            /// <param name="protocol">Protocol which owns this request</param>
            public static void sStatus(XmlNode curr, ProtocolSv protocol)
            {
                switch (curr.InnerText)
                {
                    case "Connected":
                        protocol.ConnectionStatus = Status.Connected;
                        break;
                    case "WaitingPW":
                        protocol.ConnectionStatus = Status.WaitingPW;
                        break;
                }
            }

            /// <summary>
            /// Error
            /// </summary>
            /// <param name="curr">XmlNode</param>
            /// <param name="protocol">Protocol which owns this request</param>
            public static void sError(XmlNode curr, ProtocolSv protocol)
            {
                string error = "Error ocurred on services: ";
                string code = "unknown";
                string description = "this is an unknown error";
                if (curr.Attributes != null)
                {
                    foreach (XmlAttribute xx in curr.Attributes)
                    {
                        switch (xx.Name.ToLower())
                        {
                            case "description":
                                description = xx.Value;
                                break;
                            case "code":
                                code = xx.Value;
                                break;
                        }
                    }
                }

                error += "code (" + code + ") description: " + description;
                SystemLog.Warning(error);
            }

            /// <summary>
            /// Auth
            /// </summary>
            /// <param name="curr">XmlNode</param>
            /// <param name="protocol">Protocol which owns this request</param>
            public static void sAuth(XmlNode curr, ProtocolSv protocol)
            {
                if (curr.InnerText == "INVALID")
                {
                    SystemLog.WriteLine("You have supplied wrong password, connection closed");
                    protocol.Disconnect();
                }
                if (curr.InnerText == "OK")
                {
                    protocol.ConnectionStatus = Status.Connected;
                    SystemLog.WriteLine("You are now logged in to pidgeon bnc");
                    SystemLog.WriteLine(curr.Attributes[0].Value);
                }
                return;
            }
        }
    }
}
