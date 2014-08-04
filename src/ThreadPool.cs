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
using System.Text;
using System.Collections.Generic;
using System.Threading;

namespace pidgeon_sv
{
    public class ThreadPool
    {
        private static List<Thread> tp = new List<Thread>();

        public static List<Thread> Threads
        {
            get
            {
                List<Thread> result = new List<Thread>();
                lock (tp)
                    result.AddRange(tp);
                result.AddRange(libirc.ThreadManager.ThreadList);
                return result;
            }
        }

        public static void UnregisterThis()
        {
            UnregisterThread(Thread.CurrentThread);
        }

        public static void KillThread(Thread thread)
        {
            if (thread == null)
                return;
            if (thread != Thread.CurrentThread)
            {
                if (thread.ThreadState == ThreadState.Running || 
                    thread.ThreadState == ThreadState.WaitSleepJoin ||
                    thread.ThreadState == ThreadState.Background)
                {
                    thread.Abort();
                    SystemLog.DebugLog("Killed thread " + thread.Name);
                }
                else
                {
                    SystemLog.DebugLog("Ignored request to abort thread in " + thread.ThreadState.ToString() + " " + thread.Name);
                }
            }
            else
            {
                SystemLog.DebugLog("Ignored request to abort thread from within the same thread " + thread.Name);
            }
            UnregisterThread(thread);
        }

        public static void RegisterThread(Thread thread)
        {
            if (string.IsNullOrEmpty(thread.Name))
            {
                SystemLog.DebugLog("No thread name provided for: " + thread.ManagedThreadId.ToString());
            }
            lock (tp)
            {
                if (!tp.Contains(thread))
                {
                    tp.Add(thread);
                }
            }
        }

        public static void UnregisterThread(Thread thread)
        {
            lock (tp)
            {
                if (tp.Contains(thread))
                    tp.Remove(thread);

            }
        }
    }
}

