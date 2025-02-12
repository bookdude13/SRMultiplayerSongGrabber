using HarmonyLib;
using HarmonyLib.Tools;
using Il2Cpp;
using Il2CppExitGames.Client.Photon;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppInterop.Runtime.Runtime;
using Il2CppSynth.Multiplayer;
using Il2CppSynth.Retro;
using Il2CppSynth.SongSelection;
using Il2CppUtil.View;
using SRModCore;
using static Il2CppSystem.Globalization.CultureInfo;

namespace SRMultiplayerSongGrabber.Harmony
{
    [HarmonyPatch(typeof(MultiplayerEvents), nameof(MultiplayerEvents.OnEvent))]
    public class Patch_MultiplayerEvents_OnEvent
    {
        public static string LastRequestedSongHash = "";

        /// <summary>
        /// Helper method used to log the value of a generic Il2CppSystem.Object.
        /// TODO move into SRModCore
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="obj"></param>
        private static void TryLogObj(SRLogger logger, Il2CppSystem.Object obj)
        {
            if (obj == null)
            {
                logger.Msg("Null obj");
                return;
            }

            var cpptype = obj.GetIl2CppType();
            var actType = Type.GetType(cpptype.AssemblyQualifiedName);

            //logger.Msg($"Type info is {cpptype} {cpptype.AssemblyQualifiedName} {actType}");

            if (actType == typeof(bool))
            {
                var asBool = obj.Unbox<bool>();
                logger.Msg("As bool: " + asBool);
                return;
            }

            if (actType == typeof(System.Int32))
            {
                var asInt = obj.Unbox<int>();
                logger.Msg("As int: " + asInt);
                return;
            }

            var asStr = obj.ToString();
            if (asStr != null)
            {
                logger.Msg("String! " + asStr);
            }

            logger.Msg("No successful cast");
        }

        public static void Postfix(MultiplayerEvents __instance, EventData photonEvent)
        {
            // TODO figure out which is used when the track changes

            var logger = SRMultiplayerSongGrabber.Instance.Logger;

            if (photonEvent.Code == MultiplayerEvents.SetSelectedVersusTrack)
            {
                logger.Msg("Setting selected vs track");

                var data = photonEvent.customData;

                // Shouldn't happen, but might as well be safe
                if (data == null)
                {
                    logger.Error("Null data provided!");
                    return;
                }

                // Data is expected to be an array (and was, as of Feb 2025)
                var dataArray = data.TryCast<Il2CppSystem.Array>();
                if (dataArray == null)
                {
                    logger.Error("Data not array as expected!");
                    return;
                }

                // This is left over from reversing what was in the array
                //foreach (var elem in dataArray)
                //{
                //    TryLogObj(logger, elem);
                //}

                string hash = "";

                // The hash is present in the data as a 64 character string. That is unique enough to pick it out.
                foreach (var obj in dataArray)
                {
                    var cppType = obj.GetIl2CppType();
                    var actType = Type.GetType(cppType.AssemblyQualifiedName);

                    // Only looking for the hash string
                    if (actType != typeof(string))
                        continue;

                    var asStr = obj.ToString();
                    if (asStr == null)
                        continue;

                    if (asStr.Length != 64)
                    {
                        logger.Msg($"Ignoring non-hash string '{asStr}'");
                        continue;
                    }

                    hash = asStr;
                    logger.Msg("Requested song hash is " + hash);
                    break;
                }

                LastRequestedSongHash = hash;

                SRMultiplayerSongGrabber.Instance.OnMultiplayerTrackUpdated();
            }
        }
    }
}
