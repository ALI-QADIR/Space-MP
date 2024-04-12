using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;

public class NetworkTestUi : MonoBehaviour
{
    [SerializeField] private Button _hostButton;
    [SerializeField] private Button _serverButton;
    [SerializeField] private Button _clientButton;

    private void Start()
    {
        _hostButton.onClick.AddListener(Host);
        _serverButton.onClick.AddListener(Server);
        _clientButton.onClick.AddListener(Client);
    }

    private void Host()
    {
        NetworkManager.Singleton.StartHost();
        Hide();
    }

    private void Server()
    {
        NetworkManager.Singleton.StartServer();
        Hide();
    }

    private void Client()
    {
        NetworkManager.Singleton.StartClient();
        Hide();
    }

    private void Hide() => gameObject.SetActive(false);
}