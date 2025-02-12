using Il2CppSynth.SongSelection;
using MelonLoader;
using SRModCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace SRMultiplayerSongGrabber
{
    public class ZDownloader
    {
        /// <summary>
        /// Downloads the given song from the Z site to the CustomSongs folder
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="hash"></param>
        /// <returns></returns>
        public static IEnumerator GetSongWithHash(SRLogger logger, string hash, Action onSuccess, Action onFail)
        {
            var ssmInstance = SongSelectionManager.GetInstance;

            // Download to a temp file
            string download_url = "synthriderz.com" + "/api/beatmaps/hash/download/" + hash;
            logger.Msg($"Downloading from '{download_url}'");
            var songRequest = UnityWebRequest.Get(download_url);

            // Don't hang forever if Z happens to be down
            songRequest.SetTimeoutMsec(5000);

            //logger.Msg("Data path is " + Application.dataPath);
            string customsPath = Application.dataPath + "/../SynthRidersUC/CustomSongs/";
            // Quest standalone has customs on the SD card
            if (Application.platform == RuntimePlatform.Android)
            {
                customsPath = "/sdcard/SynthRidersUC/CustomSongs";
            }
            logger.Msg("Using customs path " + customsPath);

            // Try to create folder structure if it doesn't exist yet
            Directory.CreateDirectory(customsPath);

            songRequest.downloadHandler = new DownloadHandlerFile(customsPath + "dump.synth", false);
            
            yield return songRequest.SendWebRequest();
            
            if (songRequest.isNetworkError)
            {
                logger.Error("GetSong error: " + songRequest.error);
                onFail?.Invoke();
            }
            else
            {
                logger.Msg("Download successful");
                
                //rename file
                if (File.Exists(customsPath + "dump.synth"))
                {
                    var contentDisposition = songRequest.GetResponseHeader("content-disposition").Split('"');
                    foreach (var elem in contentDisposition)
                    {
                        logger.Msg("content-disposition element is " + elem);
                    }
                    string fileName = contentDisposition[1];
                    logger.Msg("Using file name " + fileName);
                    File.Move(customsPath + "dump.synth", customsPath + fileName);

                    // TODO update file timestamp?
                }

                // Force reload
                ssmInstance.RefreshSongList(false);
                logger.Msg("Updated song list");

                onSuccess?.Invoke();
            }
        }

    }
}
