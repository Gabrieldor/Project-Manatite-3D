using System.Diagnostics;
using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Manatite.Core
{
    /// <summary>Editor-only capture tool: dumps a PNG sequence of the whole play session,
    /// surviving scene loads, and compiles it into an mp4 via ffmpeg once Play mode stops.
    /// Fully stripped from non-Editor builds.</summary>
    public sealed class PlaySessionRecorder : MonoBehaviour
    {
        [Tooltip("Uncheck to leave normal play-testing unaffected — no capture happens when false.")]
        [SerializeField] private bool recordingEnabled = true;

        [Tooltip("If true, the game runs at normal real-time speed and a frame is sampled every 1/captureFrameRate real seconds (stop-motion style — play normally, capture is sparse). If false, Time.captureFramerate locks simulated time to exact steps — perfectly deterministic output, but the game visibly fast-forwards while playing since simulated time no longer matches real time.")]
        [SerializeField] private bool realTimeSampling = true;

        [Tooltip("How often a frame is sampled — either real seconds (realTimeSampling on) or simulated seconds (off). Fractional values (e.g. 0.125 = one frame every 8s) only take effect with realTimeSampling on.")]
        [SerializeField] private float captureFrameRate = 24f;

        [Tooltip("Playback frame rate baked into the compiled mp4 — independent of captureFrameRate. Sparse captures at a high output rate is what gives the stop-motion/time-lapse look.")]
        [SerializeField] private int outputFrameRate = 60;

        [Tooltip("ffmpeg executable used to compile the frames into an mp4. Bare name relies on PATH.")]
        [SerializeField] private string ffmpegExecutable = "ffmpeg";

        [Tooltip("Subfolder under Render/ that frames and the compiled video are written to.")]
        [SerializeField] private string outputFolderName = "Session";

#if UNITY_EDITOR
        private string _framesFolder;
        private string _outputFolder;
        private string _resolvedFolderName;
        private int _frameIndex;
        private bool _finished;
        private float _captureInterval;
        private float _timeSinceLastCapture;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (!recordingEnabled)
            {
                return;
            }

            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string renderRoot = Path.Combine(projectRoot, "Render");

            _resolvedFolderName = outputFolderName;
            int suffix = 1;
            while (Directory.Exists(Path.Combine(renderRoot, _resolvedFolderName)))
            {
                _resolvedFolderName = outputFolderName + suffix;
                suffix++;
            }

            _outputFolder = Path.Combine(renderRoot, _resolvedFolderName);
            _framesFolder = Path.Combine(_outputFolder, "Frames");
            Directory.CreateDirectory(_framesFolder);

            if (realTimeSampling)
            {
                _captureInterval = 1f / captureFrameRate;
            }
            else
            {
                Time.captureFramerate = Mathf.Max(1, Mathf.RoundToInt(captureFrameRate));
            }
        }

        private void LateUpdate()
        {
            if (!recordingEnabled || _finished)
            {
                return;
            }

            if (realTimeSampling)
            {
                _timeSinceLastCapture += Time.unscaledDeltaTime;
                if (_timeSinceLastCapture < _captureInterval)
                {
                    return;
                }

                _timeSinceLastCapture -= _captureInterval;
            }

            ScreenCapture.CaptureScreenshot(Path.Combine(_framesFolder, $"{_frameIndex:D05}.png"));
            _frameIndex++;
        }

        private void OnDestroy()
        {
            // Play mode stopping destroys this object — that's the natural "stop recording" signal.
            if (!recordingEnabled || _finished)
            {
                return;
            }

            _finished = true;
            Time.captureFramerate = 0;
            CompileVideo();
        }

        private void CompileVideo()
        {
            if (_frameIndex == 0)
            {
                return;
            }

            string outputVideo = Path.Combine(_outputFolder, _resolvedFolderName + ".mp4");
            // libx264 requires even width/height; the Game view resolution rarely is.
            string args = $"-y -framerate {outputFrameRate} -i \"{_framesFolder}/%05d.png\" -vf \"scale=trunc(iw/2)*2:trunc(ih/2)*2\" -c:v libx264 -pix_fmt yuv420p \"{outputVideo}\"";

            var startInfo = new ProcessStartInfo(ffmpegExecutable, args)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            try
            {
                using var process = Process.Start(startInfo);
                process.WaitForExit();
                if (process.ExitCode == 0)
                {
                    Debug.Log($"[PlaySessionRecorder] Compiled {_frameIndex} frames to {outputVideo}");
                }
                else
                {
                    Debug.LogError($"[PlaySessionRecorder] ffmpeg exited with code {process.ExitCode}. Frames are still at {_framesFolder}.");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PlaySessionRecorder] Could not run ffmpeg ({e.Message}). Frames are still at {_framesFolder}; compile them manually.");
            }
        }
#endif
    }
}
