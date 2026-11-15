using System.Globalization;
using System.IO;
using System.Text;
using HeadTracked.Display;
using Swing.Core;
using Swing.Game;
using UnityEngine;

namespace Swing.HeadTracking
{
    /// <summary>
    /// Runs after the head-tracking package each frame and redraws the view from a smoother eye
    /// position. The package only moves the eye when a webcam report arrives (about 30 times a
    /// second), which looks like small jumps on a 60 Hz screen; <see cref="EyeSmoother"/> fills in
    /// between reports. This also records eye positions to a file when asked.
    ///
    /// The package itself is not changed: this takes its result and replaces the camera's position
    /// and view for the frame.
    /// </summary>
    [DefaultExecutionOrder(10000)]   // after HeadTrackedDisplay.LateUpdate
    public sealed class WindowSmoother : MonoBehaviour
    {
        private const float RecordSeconds = 20f;

        public HeadTrackedDisplay Display;
        public Camera Camera;
        public bool Smoothing = true;

        private readonly EyeSmoother smoother = new EyeSmoother();
        private double lastReportTime = double.NegativeInfinity;
        private StringBuilder recording;
        private string recordingPath;
        private float recordingEnds;

        public bool Recording { get { return recording != null; } }

        public string StartRecording()
        {
            recordingPath = Path.Combine(Application.persistentDataPath,
                "headtracking_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv");
            recording = new StringBuilder("time,face_found,report_age_ms,tracker_fps," +
                "measured_x,measured_y,measured_z,package_x,package_y,package_z,drawn_x,drawn_y,drawn_z\n");
            recordingEnds = Time.unscaledTime + RecordSeconds;
            return recordingPath;
        }

        private void LateUpdate()
        {
            if (Display == null || !Display.enabled) return;

            Vector3 packageEye = Display.EyePositionMeters;   // what the package drew this frame
            Vector3 drawn = packageEye;
            float deltaTime = Time.unscaledDeltaTime;

            if (Display.IsTracking && Display.HasObservation)
            {
                HeadObservation report = Display.LatestObservation;
                if (report.receivedAtSeconds != lastReportTime)
                {
                    lastReportTime = report.receivedAtSeconds;
                    // The time the webcam saw the face: when the report arrived, less its age.
                    smoother.AddSample(packageEye.ToCore(), report.receivedAtSeconds - report.frameAgeMs * 0.001);
                }
                drawn = smoother.Update(Time.realtimeSinceStartupAsDouble, deltaTime).ToUnity();
            }
            else
            {
                drawn = smoother.Glide(packageEye.ToCore(), deltaTime).ToUnity();
            }

            if (Smoothing) Redraw(drawn);
            if (recording != null) Record(packageEye, Smoothing ? drawn : packageEye);
        }

        /// <summary>Places the camera at the eye and skews its view to the monitor's edges.</summary>
        private void Redraw(Vector3 eye)
        {
            // The eye must stay in front of the screen for the view to be valid.
            float nearest = -(Camera.nearClipPlane + 0.05f);
            if (eye.z > nearest) eye.z = nearest;

            Transform screen = Display.ScreenPlane;
            Camera.transform.SetPositionAndRotation(screen.TransformPoint(eye), screen.rotation);
            Camera.projectionMatrix = OffAxisProjection.Calculate(eye, Display.ProjectionScreenRectMeters,
                Camera.nearClipPlane, Camera.farClipPlane);
        }

        private void Record(Vector3 packageEye, Vector3 drawn)
        {
            Vector3 measured = Display.EstimatedEyePositionMeters;   // before any smoothing
            HeadObservation report = Display.LatestObservation;
            recording.AppendFormat(CultureInfo.InvariantCulture,
                "{0:0.0000},{1},{2:0.0},{3:0.0},{4:0.00000},{5:0.00000},{6:0.00000},{7:0.00000},{8:0.00000},{9:0.00000},{10:0.00000},{11:0.00000},{12:0.00000}\n",
                Time.realtimeSinceStartupAsDouble, Display.IsTracking ? 1 : 0, Display.ResultAgeMilliseconds, report.trackerFps,
                measured.x, measured.y, measured.z, packageEye.x, packageEye.y, packageEye.z, drawn.x, drawn.y, drawn.z);

            if (Time.unscaledTime < recordingEnds) return;
            File.WriteAllText(recordingPath, recording.ToString());
            Debug.Log("Head tracking recording saved: " + recordingPath);
            recording = null;
        }
    }
}
