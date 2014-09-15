/***************************************************************************
 *   This program is free software; you can redistribute it and/or modify  *
 *   it under the terms of the GNU General Public License as published by  *
 *   the Free Software Foundation; either version 2 of the License, or     *
 *   (at your option) version 3.                                           *
 *                                                                         *
 *   This program is distributed in the hope that it will be useful,       *
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of        *
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the         *
 *   GNU General Public License for more details.                          *
 *                                                                         *
 *   You should have received a copy of the GNU General Public License     *
 *   along with this program; if not, write to the                         *
 *   Free Software Foundation, Inc.,                                       *
 *   51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.         *
 ***************************************************************************/

using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Threading;
using System.IO;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Xml;

namespace pidgeon_sv
{
    public partial class ProtocolIrc : libirc.Protocols.ProtocolIrc
    {
        public Network _network = null;
        public Thread BufferTh = null;
        public Buffer buffer = null;
        public SystemUser systemUser = null;
        
        protected override void DisconnectExec (string reason, Exception ex)
        {
            ProtocolMain.Datagram dt = new ProtocolMain.Datagram("CONNECTION", "PROBLEM");
            dt.Parameters.Add("network", Server);
            dt.Parameters.Add("info", ex.Message);
            systemUser.FailSafeDeliver(dt);
            Core.handleException(ex);
        }
        
        public override void DebugLog (string Text, int Verbosity)
        {
            SystemLog.DebugLog(Text, Verbosity);
        }
        
        protected override void HandleException (Exception fail)
        {
            Core.handleException(fail);
        }
        
        public void ClearBuffers()
        {
            SystemLog.DebugLog("Removing all buffers for " + Server);
            lock (buffer)
            {
                systemUser.DatabaseEngine.DeleteCache(Server);
                buffer.oldmessages.Clear();
                buffer.messages.Clear();
            }
            lock (systemUser.Messages)
            {
                List<ProtocolMain.SelfData> delete = new List<ProtocolMain.SelfData>();
                foreach (ProtocolMain.SelfData ms in systemUser.Messages)
                {
                    if (ms.Network == _network)
                        delete.Add(ms);
                }
                foreach (ProtocolMain.SelfData ms in delete)
                    systemUser.Messages.Remove(ms);
            }
        }
        
        public override void Exit ()
        {
            ClearBuffers();
            base.Exit();
        }

        public override Thread Open()
        {
            buffer.protocol = this;
            BufferTh = new Thread(buffer.Run);
            BufferTh.Name = "BufferThread";
            ThreadPool.RegisterThread(BufferTh);
            BufferTh.Start();
            return base.Open();
        }
    }
}
