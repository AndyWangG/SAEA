﻿/****************************************************************************
*Copyright (c) 2018 Microsoft All Rights Reserved.
*CLR版本： 4.0.30319.42000
*机器名称：WENLI-PC
*公司名称：Microsoft
*命名空间：SAEA.WebAPI.Http
*文件名： HttpServer
*版本号： V1.0.0.0
*唯一标识：914acb72-d4c4-4fa1-8e80-ce2f83bd06f0
*当前的用户域：WENLI-PC
*创建人： yswenli
*电子邮箱：wenguoli_520@qq.com
*创建时间：2018/4/10 13:51:50
*描述：
*
*=====================================================================
*修改标记
*修改时间：2018/4/10 13:51:50
*修改人： yswenli
*版本号： V1.0.0.0
*描述：
*
*****************************************************************************/
using SAEA.Sockets.Interface;
using SAEA.WebAPI.Common;
using SAEA.WebAPI.Http.Net;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace SAEA.WebAPI.Http
{
    /// <summary>
    /// web api httpServer
    /// </summary>
    class HttpServer
    {
        ServerSocket _serverSocket;

        ConcurrentDictionary<string, HttpContext> _dic;

        ConcurrentQueue<HttpContext> _queue;


        /// <summary>
        /// web api httpServer
        /// </summary>
        /// <param name="bufferSize"></param>
        /// <param name="count"></param>
        public HttpServer(int bufferSize = 1024 * 100, int count = 10000)
        {
            _serverSocket = new ServerSocket(bufferSize, count);

            _serverSocket.OnDisconnected += _serverSocket_OnDisconnected;

            _serverSocket.OnRequested += _serverSocket_OnRequested;

            _serverSocket.OnError += _serverSocket_OnError;

            _dic = new ConcurrentDictionary<string, HttpContext>();

            _queue = new ConcurrentQueue<HttpContext>();

            for (int i = 0; i < count; i++)
            {
                _queue.Enqueue(new HttpContext());
            }
        }



        public void Start(int port = 39654)
        {
            _serverSocket.Start(port);
        }


        private void _serverSocket_OnRequested(IUserToken userToken, string htmlStr)
        {
            while (true)
            {
                if (_queue.TryDequeue(out HttpContext httpContext))
                {
                    try
                    {
                        httpContext.Init(this, userToken, htmlStr);

                        _dic.TryAdd(userToken.ID, httpContext);

                        httpContext.InvokeAction();
                    }
                    catch (Exception ex)
                    {
                        _serverSocket.Disconnect(userToken, ex);
                    }
                    break;
                }
                else
                {
                    Thread.Sleep(1);
                }
            }
        }

        internal void Reponse(IUserToken userToken, byte[] data)
        {
            _serverSocket.Reply(userToken, data);
        }

        internal void Close(IUserToken userToken)
        {
            _serverSocket.Disconnect(userToken);
        }

        private void _serverSocket_OnDisconnected(string ID, System.Exception ex)
        {
            HttpContext httpContext;
            if (_dic.TryRemove(ID, out httpContext))
            {
                httpContext.Free();
                _queue.Enqueue(httpContext);
            }
            if (ex != null)
            {
                LogHelper.WriteError("_serverSocket_OnDisconnected 断开连接", ex);
            }

        }
        private void _serverSocket_OnError(string ID, Exception ex)
        {
            LogHelper.WriteError(ID, ex);
        }

    }
}
