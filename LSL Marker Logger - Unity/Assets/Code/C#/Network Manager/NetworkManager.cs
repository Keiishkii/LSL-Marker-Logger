using UnityEngine;

namespace MINE
{
    public class NetworkManager : MonoBehaviour
    {
        #region [ Instance ]
        private static NetworkManager _instance;
        public static NetworkManager Instance => _instance ? _instance : _instance = FindFirstObjectByType<NetworkManager>();
        #endregion

        #region [ Unserialised Fields ]
        public readonly Inlet Inlet = new ();
        public readonly Outlet Outlet = new ();
        #endregion


        private void Awake()
        {
            Inlet.Initialise();
            Outlet.Initialise();
        }

        private void Update() => Inlet.ProcessStreamInlets();
    }   
}