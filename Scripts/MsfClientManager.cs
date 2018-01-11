using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Barebones.Logging;
using Barebones.MasterServer;

public class MsfClientManager : MonoBehaviour
{
    public static MsfClientManager Singleton { get; protected set; }

    [System.Serializable]
    public class StringUnityEvent : UnityEvent<string> { }
    [Header("Connection to Master Server")]
    [Tooltip("Address to the server")]
    public string masterIp = "127.0.0.1";
    [Tooltip("Port of the server")]
    public int masterPort = 5000;
    [Tooltip("If true, ip and port will be read from cmd args")]
    public bool readMasterServerAddressFromCmd = true;
    [Tooltip("If true, will try to connect on the Start()")]
    public bool connectToMasterOnStart = true;
    [Tooltip("If true, will try to connect on failed")]
    public bool reconnectToMasterOnFailed = true;
    public float connectToMasterTimeout = 10f;

    [Header("Activities events")]
    public UnityEvent onMasterServerConnected;
    public UnityEvent onMasterServerDisconnected;
    public StringUnityEvent onMasterServerConnectFailed;
    public StringUnityEvent onLoggedIn;
    public StringUnityEvent onLoginFailed;
    public UnityEvent onRegistered;
    public StringUnityEvent onRegisterFailed;

    public string Status { get; protected set; }
    protected BmLogger Logger = Msf.Create.Logger(typeof(MsfClientManager).Name);
    protected IEnumerator backgroundCoroutine;

    private void Awake()
    {
        if (Singleton != null)
        {
            Destroy(gameObject);
            return;
        }

        Singleton = this;

        // In case this object is not at the root level of hierarchy
        // move it there, so that it won't be destroyed
        if (transform.parent != null)
            transform.SetParent(null, false);

        DontDestroyOnLoad(gameObject);

        if (readMasterServerAddressFromCmd)
        {
            // If master IP is provided via cmd arguments
            if (Msf.Args.IsProvided(Msf.Args.Names.MasterIp))
                masterIp = Msf.Args.MasterIp;

            // If master port is provided via cmd arguments
            if (Msf.Args.IsProvided(Msf.Args.Names.MasterPort))
                masterPort = Msf.Args.MasterPort;
        }
    }

    private void Start()
    {
        if (connectToMasterOnStart)
            ConnectToMasterServer();
    }

    protected void StopBackgroundCoroutine()
    {
        if (backgroundCoroutine != null)
        {
            StopCoroutine(backgroundCoroutine);
            backgroundCoroutine = null;
        }
    }

    public void ConnectToMasterServer()
    {
        Status = string.Empty;

        // connect to the master server
        Msf.Connection.Connected += OnMasterServerConnected;
        Msf.Connection.Disconnected += OnMasterServerDisconnected;

        backgroundCoroutine = CoroutineUtils.StartWaiting(connectToMasterTimeout,
            () => { OnMasterServerConnectionFailed("Timed out"); },
            1f,
            (time) => { Status = string.Format("Trying to connect {0}s", time); });
        StartCoroutine(backgroundCoroutine);

        Logger.Info(string.Format("Connecting to master on {0}:{1}", masterIp, masterPort));
        Msf.Connection.Connect(masterIp, masterPort);
    }

    protected virtual void OnMasterServerConnected()
    {
        StopBackgroundCoroutine();
        Status = string.Empty;

        if (onMasterServerConnected != null)
            onMasterServerConnected.Invoke();
    }

    protected virtual void OnMasterServerDisconnected()
    {
        StopBackgroundCoroutine();
        Status = string.Empty;

        if (onMasterServerDisconnected != null)
            onMasterServerDisconnected.Invoke();
    }

    private void OnMasterServerConnectionFailed(string errorMsg)
    {
        Status = string.Format("Master server connection failed: {0}", errorMsg);

        Msf.Connection.Connected -= OnMasterServerConnected;
        Msf.Connection.Disconnected -= OnMasterServerDisconnected;

        if (onMasterServerConnectFailed != null)
            onMasterServerConnectFailed.Invoke(errorMsg);

        if (reconnectToMasterOnFailed)
            ConnectToMasterServer();
    }

    public void LogInAsGuest()
    {
        if (!Msf.Connection.IsConnected)
        {
            OnLoginFailed("Master server not connected");
            return;
        }

        Msf.Client.Auth.LogInAsGuest((accountInfo, errorMsg) =>
        {
            if (accountInfo == null)
                OnLoginFailed(errorMsg);
            else
                OnLoggedIn(Msf.Client.Auth.AccountInfo.Username);
        });
    }

    public void Login(string username, string password)
    {
        if (!Msf.Connection.IsConnected)
        {
            OnLoginFailed("Master server not connected");
            return;
        }

        Msf.Client.Auth.LogIn(username, password, (accountInfo, errorMsg) =>
        {
            if (accountInfo == null)
                OnLoginFailed(errorMsg);
            else
                OnLoggedIn(Msf.Client.Auth.AccountInfo.Username);
        });
    }

    protected virtual void OnLoggedIn(string username)
    {
        Status = string.Format("Logged in: {0}", username);

        if (onLoggedIn != null)
            onLoggedIn.Invoke(username);
    }

    protected virtual void OnLoginFailed(string errorMsg)
    {
        Status = string.Format("Login failed: {0}", errorMsg);

        if (onLoginFailed != null)
            onLoginFailed.Invoke(errorMsg);
    }

    public void Register(string username, string password, string email)
    {
        if (!Msf.Connection.IsConnected)
        {
            OnRegisterFailed("Master server not connected");
            return;
        }

        var registerData = new Dictionary<string, string>
        {
            { "username", username },
            { "password", password },
            { "email", email }
        };

        Msf.Client.Auth.Register(registerData, (success, errorMsg) =>
        {
            if (!success)
                OnRegisterFailed(errorMsg);
            else
                OnRegistered();
        });
    }

    protected virtual void OnRegistered()
    {
        Status = string.Format("Registered");

        if (onRegistered != null)
            onRegistered.Invoke();
    }

    protected virtual void OnRegisterFailed(string errorMsg)
    {
        Status = string.Format("Register failed: {0}", errorMsg);

        if (onRegisterFailed != null)
            onRegisterFailed.Invoke(errorMsg);
    }
}
