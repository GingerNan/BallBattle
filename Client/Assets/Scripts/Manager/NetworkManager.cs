using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;

public class NetworkManager : MonoSingleton<NetworkManager>
{
    public Socket clientSocket;
    
    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        Connect();
    }

    private void Connect()
    {
        try
        {
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            clientSocket.Connect(IPInfo.IP, IPInfo.Port);
            
            Debug.Log("已连接至:" + clientSocket.RemoteEndPoint.ToString());
            Send("Hello World");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private void Send(string msg)
    {
        try
        {
            byte[] buffer = Encoding.UTF8.GetBytes(msg);
            clientSocket.Send(buffer);
        }
        catch (Exception e)
        {
            System.Console.WriteLine(e);
            throw;
        }
    }
}
