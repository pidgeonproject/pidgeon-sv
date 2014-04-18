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
                lock (tp)
                {
                    return new List<Thread>(tp);
                }
            }
        }

        public static void KillThread(Thread thread)
        {
            if (thread == null)
            {
                return;
            }
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
            if (thread.Name == "")
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
                {
                    tp.Remove(thread);
                }
            }
        }
    }
}

