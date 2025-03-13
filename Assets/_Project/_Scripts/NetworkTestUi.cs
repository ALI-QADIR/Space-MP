using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace CosmicClash
{
    public class NetworkTestUi : MonoBehaviour
    {
        [SerializeField] private Button m_hostButton;
        [SerializeField] private Button m_serverButton;
        [SerializeField] private Button m_clientButton;

        private void Start()
        {
            m_hostButton.onClick.AddListener(Host);
            m_serverButton.onClick.AddListener(Server);
            m_clientButton.onClick.AddListener(Client);
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
}