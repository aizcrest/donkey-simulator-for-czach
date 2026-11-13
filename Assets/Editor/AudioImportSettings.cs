using UnityEditor;
using UnityEngine;
namespace DonkeyForest.Editor
{
    public sealed class AudioImportSettings:AssetPostprocessor
    {
        void OnPreprocessAudio()
        {
            if(!assetPath.StartsWith("Assets/Resources/Audio/"))return;
            var importer=(AudioImporter)assetImporter;bool loop=assetPath.Contains("ambience-")||assetPath.Contains("music.wav");
            importer.forceToMono=true;
            var settings=importer.defaultSampleSettings;
            settings.preloadAudioData=!loop;
            settings.loadType=loop?AudioClipLoadType.Streaming:AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.45f;
            settings.sampleRateSetting=AudioSampleRateSetting.OverrideSampleRate;settings.sampleRateOverride=22050;
            importer.defaultSampleSettings=settings;
        }
    }
}
