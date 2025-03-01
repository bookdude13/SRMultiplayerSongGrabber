using Il2CppSynth.SongSelection;
using MelonLoader;
using SRCustomLib;
using SRModCore;
using SRTimestampLib.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace SRMultiplayerSongGrabber
{
    /// <summary>
    /// Handles the actual download of a custom map in Unity
    /// </summary>
    public class CustomDownloaderUnity
    {
        private static CustomMapRepoTorrent? _repoTorrent = null;

        private const string apiRootZ = "synthriderz.com/api";
        private const string apiRootSyn = "synplicity.live/api";

        /// <summary>
        /// Downloads the given song to the CustomSongs folder using various fallback methods
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="hash"></param>
        /// <returns></returns>
        public static IEnumerator TryGetSongWithHash(SRLogger logger, string hash, Action onSuccess, Action onFail)
        {
            string downloadUrlZ = apiRootZ + "/beatmaps/hash/download/" + hash;

            var isSuccess = false;
            void OnSuccessZ()
            {
                isSuccess = true;
                onSuccess?.Invoke();
            }

            void OnFailZ()
            {
                isSuccess = false;
                // Don't trigger fail case yet; fallback still to come
            }

            yield return GetSongWithHash(logger, downloadUrlZ, OnSuccessZ, OnFailZ);

            // If it worked; we done!
            if (isSuccess)
                yield break;


            // Z download failed; fallback on synplicity
            // Note - could get the song data and use the download url from there, but this is so much simpler
            string downloadUrlSyn = apiRootSyn + "/downloads/" + hash;

            var isSuccessSyn = false;
            void OnSuccessSyn()
            {
                isSuccessSyn = true;
                onSuccess?.Invoke();
            }
            void OnFailSyn()
            {
                isSuccessSyn = false;
                // Don't trigger fail case yet; can still try another fallback
            }

            yield return GetSongWithHash(logger, downloadUrlSyn, OnSuccessSyn, OnFailSyn);

            // If it worked; we done!
            if (isSuccessSyn)
                yield break;

            // Synplicity download failed; fallback on torrent I guess
            if (_repoTorrent == null)
            {
                _repoTorrent = new CustomMapRepoTorrent(new SRTimestampLib.SRLogHandler());
                yield return _repoTorrent.Initialize();
            }

            // Get file info from the hash, so we get the file name
            yield return GetSongInfoFromHash(logger, hash, async (mapItem) =>
            {
                var fileName = mapItem.filename;
                if (string.IsNullOrEmpty(fileName))
                {
                    logger.Error($"Null or empty filename! '{fileName}'");
                    onFail?.Invoke();
                    return;
                }
                var downloadedPath = await _repoTorrent.DownloadMapFromFilename(fileName);
                if (string.IsNullOrEmpty(downloadedPath))
                {
                    logger.Error($"Failed to download {fileName}");
                    onFail?.Invoke();
                    return;
                }
            }, onFail);
        }

        public static IEnumerator GetSongInfoFromHash(SRLogger logger, string hash, Action<MapItem> onSuccess, Action onFail)
        {
            // Get from synplicity, since that also gets it from Z
            var url = apiRootSyn + "/beatmaps/" + hash;
            var request = UnityWebRequest.Get(url);

            // Don't hang forever if the site happens to be down
            request.SetTimeoutMsec(5000);

            yield return request.SendWebRequest();

            if (request.isNetworkError || request.isHttpError)
            {
                logger.Error("Failed web request: " + request.error);
                onFail?.Invoke();
                yield break;
            }

            // Try to parse
            MapItem? mapItem = JsonSerializer.Deserialize<MapItem>(request.downloadHandler.text, options: new JsonSerializerOptions());
            if (mapItem == null)
            {
                logger.Error("Failed to parse map info!");
                onFail?.Invoke();
                yield break;
            }

            onSuccess?.Invoke(mapItem);
        }

        /// <summary>
        /// Downloads the given song from the Z site to the CustomSongs folder
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        private static IEnumerator GetSongWithHash(SRLogger logger, string url, Action onSuccess, Action onFail)
        {
            var ssmInstance = SongSelectionManager.GetInstance;

            // Download to a temp file
            logger.Msg($"Downloading from '{url}'");
            var songRequest = UnityWebRequest.Get(url);

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
