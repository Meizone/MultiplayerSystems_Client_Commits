using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class NetworkedClient : MonoBehaviour
{

    int connectionID;
    int maxConnections = 1000;
    int reliableChannelID;
    int unreliableChannelID;
    int hostID;
    int socketPort = 3389;
    byte error;
    bool isConnected = false;
    int ourClientID;



    //List<IDName> idlist;

    GameObject gameSystemManager;


    // Start is called before the first frame update
    void Start()
    {
        //idlist = new List<IDName>();

        GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();

        foreach (GameObject go in allObjects)
        {
            if (go.name == "GameManager")
                gameSystemManager = go;

        }

        Connect();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateNetworkConnection();
    }

    private void UpdateNetworkConnection()
    {
        if (isConnected)
        {
            int recHostID;
            int recConnectionID;
            int recChannelID;
            byte[] recBuffer = new byte[1024];
            int bufferSize = 1024;
            int dataSize;
            NetworkEventType recNetworkEvent = NetworkTransport.Receive(out recHostID, out recConnectionID, out recChannelID, recBuffer, bufferSize, out dataSize, out error);

            switch (recNetworkEvent)
            {
                case NetworkEventType.ConnectEvent:
                    Debug.Log("connected.  " + recConnectionID);
                    ourClientID = recConnectionID;
                    StartCoroutine(LoadGame());
                    break;
                case NetworkEventType.DataEvent:
                    string msg = Encoding.Unicode.GetString(recBuffer, 0, dataSize);
                    ProcessRecievedMsg(msg, recConnectionID);
                    break;
                case NetworkEventType.DisconnectEvent:
                    isConnected = false;
                    Debug.Log("disconnected.  " + recConnectionID);
                    break;
            }
        }
    }
    
    private void Connect()
    {
        if (!isConnected)
        {
            Debug.Log("Attempting to create connection");

            NetworkTransport.Init();

            ConnectionConfig config = new ConnectionConfig();
            reliableChannelID = config.AddChannel(QosType.Reliable);
            unreliableChannelID = config.AddChannel(QosType.Unreliable);
            HostTopology topology = new HostTopology(config, maxConnections);
            hostID = NetworkTransport.AddHost(topology, 0);
            Debug.Log("Socket open.  Host ID = " + hostID);

            connectionID = NetworkTransport.Connect(hostID, "192.168.50.210", socketPort, 0, out error); // server is local on network

            if (error == 0)
            {
                isConnected = true;

                Debug.Log("Connected, id = " + connectionID);

            }

        }
    }
    
    public void Disconnect()
    {
        NetworkTransport.Disconnect(hostID, connectionID, out error);
    }
    
    public void SendMessageToHost(string msg)
    {
        byte[] buffer = Encoding.Unicode.GetBytes(msg);
        NetworkTransport.Send(hostID, connectionID, reliableChannelID, buffer, msg.Length * sizeof(char), out error);
    }

    private void ProcessRecievedMsg(string msg, int id)
    {
        Debug.Log("msg recieved = " + msg + ".  connection id = " + id);
        string[] csv = msg.Split(',');

        int signifier = int.Parse(csv[0]);

        Debug.Log(signifier);

        if(signifier == ServerToClientSignifiers.LoginResponse)
        {
            int loginResultSignifier = int.Parse(csv[1]);

            if (loginResultSignifier == LoginResponses.Success)
            {
                gameSystemManager.GetComponent<GameSystemManager>().ChangeGameState(GameStates.MainMenu);
                FindObjectOfType<AudioController>().Play("Success");
            }
            else if (loginResultSignifier == LoginResponses.FailureNameInUse)
            {
                gameSystemManager.GetComponent<GameSystemManager>().usernameExists();
                FindObjectOfType<AudioController>().Play("Error");
            }
            else if (loginResultSignifier == LoginResponses.FailureNameNotFound)
            {
                gameSystemManager.GetComponent<GameSystemManager>().invalidUsername();
                FindObjectOfType<AudioController>().Play("Error");
            }
            else if (loginResultSignifier == LoginResponses.FailureIncorrectPassword)
            {
                gameSystemManager.GetComponent<GameSystemManager>().invalidPassword();
                FindObjectOfType<AudioController>().Play("Error");
            }
            else if (loginResultSignifier == LoginResponses.AccountCreated)
            {
                gameSystemManager.GetComponent<GameSystemManager>().accountCreate();
                FindObjectOfType<AudioController>().Play("Error");
            }

            if(loginResultSignifier == LoginResponses.SendUsername)
            {
                string n = csv[2];
                gameSystemManager.GetComponent<GameSystemManager>().name = n;
            }
            
        }

        else if (signifier == ChatStates.ServerToClient)
        {
            string name = csv[1];
            string message = csv[2];
            FindObjectOfType<ChatBehaviour>().AddTextToChat(name + ": " + message);
        }




        // Match
        if (signifier == ServerToClientSignifiers.MatchResponse)
        {
                
            int MatchSignifier = int.Parse(csv[1]);
            if (MatchSignifier == GameSignifiers.AddToGameSession)
            {
                gameSystemManager.GetComponent<GameSystemManager>().ChangeGameState(GameStates.ToGame);
            }

            else if (MatchSignifier == GameSignifiers.SendMoveToClients)
            {
                int move = int.Parse(csv[2]);
                FindObjectOfType<Board>().HitBox(FindObjectOfType<Board>().FindBox(move), Mark.X);
                FindObjectOfType<Board>().canPlay = true;
            }
        }



        //ChangeGameState(GameStates.MainMenu);
    }

    public bool IsConnected()
    {
        return isConnected;
    }







    IEnumerator LoadGame()
    {
        yield return new WaitForSeconds(3);
        FindObjectOfType<AudioController>().Play("Success"); 
        gameSystemManager.GetComponent<GameSystemManager>().ChangeGameState(GameStates.Login);
    }


}