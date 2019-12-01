﻿using AutoCSer.Extension;
using System;

namespace AutoCSer.CacheServer.Cache.Lock.QueueTaskThread
{
    /// <summary>
    /// 锁操作队列线程
    /// </summary>
    internal sealed class Thread : AutoCSer.Threading.QueueTaskThread<Node>
    {
        /// <summary>
        /// 添加任务
        /// </summary>
        /// <param name="value"></param>
        internal void Add(Node value)
        {
            while (System.Threading.Interlocked.CompareExchange(ref queueLock, 1, 0) != 0) AutoCSer.Threading.ThreadYield.YieldOnly();
            if (head == null)
            {
                end = value;
                head = value;
                System.Threading.Interlocked.Exchange(ref queueLock, 0);
                WaitHandle.Set();
            }
            else
            {
                end.LinkNext = value;
                end = value;
                System.Threading.Interlocked.Exchange(ref queueLock, 0);
            }
        }
        /// <summary>
        /// 锁操作任务处理
        /// </summary>
        protected override void run()
        {
            do
            {
                WaitHandle.Wait();
                while (System.Threading.Interlocked.CompareExchange(ref queueLock, 1, 0) != 0) AutoCSer.Threading.ThreadYield.YieldOnly();
                Node value = head;
                end = null;
                head = null;
                System.Threading.Interlocked.Exchange(ref queueLock, 0);
                do
                {
                    try
                    {
                        do
                        {
                            value.RunTask(ref value);
                        }
                        while (value != null);
                        break;
                    }
                    catch (Exception error)
                    {
                        AutoCSer.Log.Pub.Log.Add(Log.LogType.Error, error);
                    }
                }
                while (value != null);
            }
            while (true);
        }

        /// <summary>
        /// 锁操作队列线程
        /// </summary>
        internal static readonly Thread Default = new Thread();
    }
}
