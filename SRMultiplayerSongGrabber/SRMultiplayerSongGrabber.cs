using MelonLoader;
using SRModCore;
using UnityEngine;

namespace SRMultiplayerSongGrabber
{
    /// <summary>
    /// Provides a button in the multiplayer view to download custom songs that are missing locally
    /// </summary>
    public class SRMultiplayerSongGrabber : MelonMod
    {
        public static SRMultiplayerSongGrabber Instance { get; private set; }
        public SRLogger Logger { get; private set; }

        private DownloadButton? _downloadButton = null;

        public override void OnInitializeMelon()
        {
            base.OnInitializeMelon();

            Logger = new MelonLoggerWrapper(LoggerInstance);
            Instance = this;
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            base.OnSceneWasInitialized(buildIndex, sceneName);

            var scene = new SRScene(sceneName);
            if (scene.SceneType == SRScene.SRSceneType.MAIN_MENU)
            {
                SetupDownloadButton();
            }
        }

        /// <summary>
        /// Makes sure the download button is created for the multiplayer view
        /// </summary>
        private void SetupDownloadButton()
        {
            if (_downloadButton != null)
            {
                // Already set up, so just needs a refresh
                _downloadButton.Refresh();
                return;
            }

            Logger.Msg("Setting up download button");
            var mpRoomPanel = GameObject.Find("Main Stage Prefab/Z-Wrap/Multiplayer/RoomPanel/Scale Wrap/MultiplayerRoomPanel");
            _downloadButton = new DownloadButton(Logger);
            _downloadButton.Init(mpRoomPanel);
        }

        /// <summary>
        /// Called whenever our multiplayer track changes, for either host or client.
        /// </summary>
        public void OnMultiplayerTrackUpdated()
        {
            Logger.Msg("Track updated");
            SetupDownloadButton();
        }

        public void OnOpenMultiplayerRoomMenu(Il2CppSynth.Versus.Room room)
        {
            Logger.Msg("OpenMultiRoom");
            SetupDownloadButton();
        }
    }
}
