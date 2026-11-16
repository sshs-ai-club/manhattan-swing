using HeadTracked.Display;
using ManhattanSwing.Game;
using UnityEngine;

namespace ManhattanSwing.HeadTracking
{
    /// <summary>
    /// The window effect: a webcam tracker reports where the player's eyes are, and the camera is
    /// moved and its view skewed so the monitor behaves like a window into the city.
    ///
    /// All of the hard work is in the head-tracked-display package. This file only sets it up for
    /// this game and stores the monitor's measurements on this laptop.
    ///
    /// NOT TESTED: written from the package's source and guide, never run with a webcam.
    /// </summary>
    public sealed class HeadTracking : IHeadTracking
    {
        private const string WidthKey = "screenWidthCm", HeightKey = "screenHeightCm", DistanceKey = "eyeDistanceCm";
        private const string OnKey = "headTrackingOn", SmoothKey = "headSmoothing";
        private const string TurnKey = "headTurn", FlipKey = "headTurnFlip";

        private readonly Camera camera;
        private readonly Transform screen;            // the physical monitor, placed in front of the eyes
        private readonly HeadTrackedDisplay display;
        private readonly PythonBridgeSource source;   // listens for the webcam tracker on this laptop
        private readonly ScreenWindowFrame frame;
        private readonly WindowSmoother smoother;    // our own smoothing, run after the package each frame

        public float ScreenWidthCm { get; set; }
        public float ScreenHeightCm { get; set; }
        public float EyeDistanceCm { get; set; }

        /// <summary>The game calls this when it starts, if this assembly was compiled.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            HeadTrackingHook.Create = (camera, eyes, frameMaterial) => new HeadTracking(camera, eyes, frameMaterial);
        }

        private HeadTracking(Camera camera, Transform eyes, Material frameMaterial)
        {
            this.camera = camera;
            ScreenWidthCm = PlayerPrefs.GetFloat(WidthKey, 53f);
            ScreenHeightCm = PlayerPrefs.GetFloat(HeightKey, 30f);
            EyeDistanceCm = PlayerPrefs.GetFloat(DistanceKey, 60f);

            screen = new GameObject("Screen plane").transform;
            screen.SetParent(eyes, false);

            // Start both parts switched off; turning tracking on enables them.
            var link = new GameObject("Tracker link");
            link.SetActive(false);
            source = link.AddComponent<PythonBridgeSource>();
            source.enabled = false;
            link.SetActive(true);

            display = camera.gameObject.AddComponent<HeadTrackedDisplay>();
            display.enabled = false;
            display.Configure(screen, source);

            frame = screen.gameObject.AddComponent<ScreenWindowFrame>();
            frame.FrameMaterial = frameMaterial;
            frame.Configure(display);
            frame.EffectEnabled = false;

            smoother = camera.gameObject.AddComponent<WindowSmoother>();
            smoother.Display = display;
            smoother.Camera = camera;
            Smoothing = PlayerPrefs.GetInt(SmoothKey, 1) == 1;
            TurnWithHead = PlayerPrefs.GetInt(TurnKey, 0) == 1;
            FlipHeadTurn = PlayerPrefs.GetInt(FlipKey, 0) == 1;

            Apply();
            On = PlayerPrefs.GetInt(OnKey, 0) == 1;
        }

        public bool On
        {
            get { return display.enabled; }
            set
            {
                source.enabled = value;
                display.enabled = value;
                frame.EffectEnabled = value;
                if (!value)
                {
                    // Back to an ordinary camera sitting at the eyes.
                    camera.ResetProjectionMatrix();
                    camera.transform.localPosition = Vector3.zero;
                    camera.transform.localRotation = Quaternion.identity;
                }
                PlayerPrefs.SetInt(OnKey, value ? 1 : 0);
            }
        }

        public bool FaceFound { get { return display.enabled && display.IsTracking; } }

        public bool Smoothing
        {
            get { return smoother.Smoothing; }
            set
            {
                smoother.Smoothing = value;
                // With our smoothing on, the package's own filter is set steadier when the head is
                // still and quicker when it moves; the values come from the simulation in the tests.
                // With it off, the package's own values are restored.
                display.FilterMinimumCutoffHz = value ? 1f : 2f;
                display.FilterSpeedCoefficient = value ? 30f : 15f;
                PlayerPrefs.SetInt(SmoothKey, value ? 1 : 0);
            }
        }

        public string Diagnostics
        {
            get
            {
                if (!display.enabled || !display.HasObservation) return "Tracker: no reports yet";
                return string.Format("Tracker: {0:0} reports a second, picture {1:0} ms old",
                    display.LatestObservation.trackerFps, display.ResultAgeMilliseconds);
            }
        }

        private bool turnWithHead, flipHeadTurn;

        public bool TurnWithHead
        {
            get { return turnWithHead; }
            set { turnWithHead = value; PlayerPrefs.SetInt(TurnKey, value ? 1 : 0); }
        }

        public bool FlipHeadTurn
        {
            get { return flipHeadTurn; }
            set { flipHeadTurn = value; PlayerPrefs.SetInt(FlipKey, value ? 1 : 0); }
        }

        public float HeadYawDegrees
        {
            get
            {
                if (!FaceFound || !display.LatestObservation.poseValid) return 0;
                float yaw = display.LatestObservation.headEulerDegrees.y;
                return flipHeadTurn ? -yaw : yaw;
            }
        }

        public bool Recording { get { return smoother.Recording; } }

        public string StartRecording()
        {
            return smoother.StartRecording();
        }

        public string Status
        {
            get
            {
                if (!display.enabled) return "Head tracking off (H turns it on)";
                return display.IsTracking ? "Head tracking: face found" : "Head tracking: no face (" + display.SourceStatus + ")";
            }
        }

        public void Apply()
        {
            DisplayCalibration calibration = display.Calibration;
            calibration.screenWidth = ScreenWidthCm / 100f;
            calibration.screenHeight = ScreenHeightCm / 100f;
            calibration.referenceEyeDistanceFromScreen = EyeDistanceCm / 100f;
            // Assumes the webcam sits on top of the monitor, in the middle.
            calibration.webcamPosition = new Vector3(0, calibration.screenHeight / 2 + 0.01f, -0.025f);

            // The monitor hangs in front of the eyes at the sitting distance, so a player sitting
            // normally sees exactly what the ordinary camera would show.
            screen.localPosition = new Vector3(0, 0, calibration.referenceEyeDistanceFromScreen);
            frame.Refresh();

            PlayerPrefs.SetFloat(WidthKey, ScreenWidthCm);
            PlayerPrefs.SetFloat(HeightKey, ScreenHeightCm);
            PlayerPrefs.SetFloat(DistanceKey, EyeDistanceCm);
            PlayerPrefs.Save();
        }

        public bool CaptureFaceReference()
        {
            return display.enabled && display.CaptureReference();
        }

        public string TrackerCommand
        {
            get
            {
                string file = System.IO.Path.Combine(PythonBridgeSource.RuntimeCalibrationDirectory, "tracker_runtime_calibration.json");
                return "python tracker.py --camera 0 --port " + source.Port + " --calibration-file \"" + file + "\"";
            }
        }
    }
}
