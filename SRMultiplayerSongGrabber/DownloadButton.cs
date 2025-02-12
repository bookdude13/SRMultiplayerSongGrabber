using Il2Cpp;
using Il2CppSynth.Multiplayer;
using Il2CppSynth.Retro;
using MelonLoader;
using SRModCore;
using SRMultiplayerSongGrabber.Harmony;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static Il2Cpp.ViveportDemo_IAP;

namespace SRMultiplayerSongGrabber
{
    public class DownloadButton
    {
        private SRLogger logger;

        private Transform bottomPanel;
        private GameObject noSongFoundObj;

        private GameObject gameObject;
        private SynthUIButton synthUIButton;
        private object _currentRefreshCo;
        private string? _currentDownloadHash;

        public DownloadButton(SRLogger logger)
        {
            this.logger = logger;
        }
        
        public void Init(GameObject mpRoomPanel)
        {
            // Get a button from the bottom and clone it for downloading
            bottomPanel = mpRoomPanel.transform.Find("MainPanel/BottomPanel");
            noSongFoundObj = bottomPanel.Find("NoSongFound").gameObject;

            var songInfoWrap = bottomPanel.Find("Song Info Wrap");
            var favoriteWrap = songInfoWrap.transform.Find("Favorite Wrap");
            var volumeUp = mpRoomPanel.transform.Find("MainPanel/BottomPanel/VolumeControl/Arrow Up");
            //UnityUtil.LogComponentsRecursive(logger, volumeUp);

            logger.Msg("Creating download button");

            // Use the button from Arrow Up since it's set up cleaner, but
            // use the size and relative position of the favorites button.
            gameObject = GameObject.Instantiate(volumeUp, favoriteWrap.parent).gameObject;
            gameObject.transform.position = favoriteWrap.position + new Vector3(-0.8f, -0.25f, 0f);

            synthUIButton = gameObject.GetComponent<SynthUIButton>();

            var bg = gameObject.transform.Find("bg").GetComponent<Image>();

            // BG should match with favorites shape (basic hex)
            //var favBackground = favoriteWrap.Find("BG").GetComponent<Image>();
            //bg.sprite = favBackground.sprite;

            // Remove outline and icon for now. TODO replace icon w/ dl icon
            //GameObject.Destroy(gameObject.transform.Find("outline").gameObject);
            //GameObject.Destroy(gameObject.transform.Find("icon").gameObject);
            var outline = gameObject.transform.Find("outline").GetComponent<Image>();
            outline.enabled = false;

            var icon = gameObject.transform.Find("icon").GetComponent<Image>();
            var downloadSprite = UnityUtil.CreateSpriteFromAssemblyResource(logger, Assembly.GetExecutingAssembly(), "SRMultiplayerSongGrabber.Resources.document-download-icon.png");
            icon.sprite = downloadSprite;
            //icon.enabled = false;

            // Stop text from being changed from localization running
            var l8ns = gameObject.GetComponentsInChildren<Il2CppSynth.Utils.LocalizationHelper>();
            if (l8ns != null)
            {
                foreach (var l8n in l8ns)
                {
                    l8n.enabled = false;
                }
            }

            synthUIButton.SetText("DL");

            // TODO do proper localization or fake it
            synthUIButton.showTooltip = false;
            synthUIButton.TooltipLocalizationKey = "";

            synthUIButton.hideTooltipOnClick = true;
            synthUIButton.stayHoveredwhenClicked = false;

            // Clear out events from the original button
            synthUIButton.WhenClicked = new UnityEvent();

            // Add our logic
            synthUIButton.WhenClicked.AddListener((UnityAction)OnDownloadClicked);

            // After the button is created, make sure it can be clicked by putting it within the "not found" text
            gameObject.transform.SetParent(noSongFoundObj.transform, true);

            // Set position explicitly, to make it consistent and easy to change
            gameObject.transform.position = new Vector3(0f, 0.31f, 16.41f); // 2.65

            Refresh();
        }

        private IEnumerator WaitForSelectedTrack()
        {
            var ssmInstance = Il2CppSynth.SongSelection.SongSelectionManager.GetInstance;
            while (ssmInstance == null || ssmInstance.SelectedGameTrack == null)
            {
                logger.Msg("Waiting for selected track");
                yield return null;
            }
        }

        /// <summary>
        /// The last requested song hash from multiplayer events. This _may_ match with CurrentSongHash if we have the song
        /// locally. Otherwise it'll be the hash of the unavailable song.
        /// </summary>
        private static string? LastRequestedHash => Patch_MultiplayerEvents_OnEvent.LastRequestedSongHash;

        private Il2CppSynth.Retro.Game_Track_Retro? GetSelectedTrack()
        {
            return Il2CppSynth.SongSelection.SongSelectionManager.GetInstance?.SelectedGameTrack;
        }

        public void Refresh()
        {
            //if (_currentRefreshCo != null)
            //{
            //    MelonCoroutines.Stop(_currentRefreshCo);
            //}
            //_currentRefreshCo = MelonCoroutines.Start(RefreshCo());
            MelonCoroutines.Start(RefreshCo());
        }

        public void RefreshDelayed(float delaySec)
        {
            MelonCoroutines.Start(RefreshDelayedCo(delaySec));
        }

        private IEnumerator RefreshDelayedCo(float delaySec)
        {
            yield return new WaitForSeconds(delaySec);
            logger.Msg($"Refreshing after {delaySec}sec delay");
            yield return RefreshCo();
        }

        private bool IsSongMissingAndDownloadable(out string songHash)
        {
            songHash = "";

            var currentSong = GetSelectedTrack();
            if (currentSong == null)
            {
                // Can't download a null song
                return false;
            }

            // If we have a hash for the current selected song, that means it exists locally.
            // If we go from an available song to an unavailable one, the current song is still tied to the last one,
            // so there'll be a mismatch if we need to download
            if (!string.IsNullOrEmpty(currentSong.LeaderboardHash) && currentSong.LeaderboardHash == LastRequestedHash)
            {
                logger.Msg($"Local hash is set; not downloadable");
                return false;
            }

            // No hash locally. Do we have one set that we can download?
            logger.Msg("No local hash. Last requested is " + LastRequestedHash);
            if (string.IsNullOrEmpty(LastRequestedHash))
            {
                logger.Msg("No requested hash, cannot set up (may be initializing)");
                return false;
            }

            songHash = LastRequestedHash;
            return true;
        }

        private IEnumerator RefreshCo()
        {
            yield return WaitForSelectedTrack();

            if (!IsSongMissingAndDownloadable(out string songHash))
            {
                synthUIButton.SetButtonDisabled(true);
                synthUIButton.UpdateVisualState();
                yield break;
            }

            // If our song changed, allow downloading again
            if (songHash != _currentDownloadHash)
            {
                ResetState();
            }

            logger.Msg("Valid; showing");
            synthUIButton.SetButtonDisabled(false);
            synthUIButton.UpdateVisualState();
        }

        private void OnDownloadClicked()
        {
            if (!IsSongMissingAndDownloadable(out string songHash))
            {
                logger.Error("Trying to download when song is not downloadable!");
                return;
            }

            if (_currentDownloadHash == songHash)
            {
                logger.Msg($"Already downloading song with hash '{_currentDownloadHash}'; ignoring click");
                return;
            }

            _currentDownloadHash = songHash;
            logger.Msg($"Download clicked. Hash is {songHash}");
            MelonCoroutines.Start(ZDownloader.GetSongWithHash(logger, songHash, OnDownloadSuccess, OnDownloadFail));
        }

        private void OnDownloadSuccess()
        {
            ResetState();

            // Note - it's unnecessary to disable the button at this point, since the successful download refreshes the songs
            // and causes the Refresh logic to run again, hiding the button
        }

        private void OnDownloadFail()
        {
            ResetState();

            // TODO - disable button when pressed, re-enable here to allow retry
        }

        private void ResetState()
        {
            _currentDownloadHash = null;
        }
    }
}
