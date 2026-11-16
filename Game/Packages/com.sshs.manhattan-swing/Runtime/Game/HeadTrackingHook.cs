using System;
using UnityEngine;

namespace ManhattanSwing.Game
{
    /// <summary>
    /// What the game needs from head tracking. The real thing lives in Runtime/HeadTracking and is
    /// only compiled when the head-tracked-display package is installed, so the game still runs
    /// (with the mouse, as a normal screen) when the package or a webcam is missing.
    /// </summary>
    public interface IHeadTracking
    {
        /// <summary>Turns the window effect on or off. Off gives an ordinary camera.</summary>
        bool On { get; set; }
        /// <summary>True while the tracker is sending a usable face position.</summary>
        bool FaceFound { get; }
        /// <summary>One line for the screen.</summary>
        string Status { get; }

        // The physical monitor, measured with a ruler, in centimetres.
        float ScreenWidthCm { get; set; }
        float ScreenHeightCm { get; set; }
        /// <summary>Eyes to screen when sitting normally.</summary>
        float EyeDistanceCm { get; set; }
        /// <summary>Call after changing the three sizes: applies and saves them on this laptop.</summary>
        void Apply();

        /// <summary>With the player sitting at <see cref="EyeDistanceCm"/>, remembers how their face looks to the webcam.</summary>
        bool CaptureFaceReference();
        /// <summary>The command that starts the webcam tracker for this game.</summary>
        string TrackerCommand { get; }

        /// <summary>
        /// Extra smoothing on top of the package: fills in between tracker reports and makes up for
        /// their delay. Switch it off to compare with the package on its own.
        /// </summary>
        bool Smoothing { get; set; }
        /// <summary>How fast the tracker is and how old its picture is, for the setup panel.</summary>
        string Diagnostics { get; }

        /// <summary>Turn the view by turning the head (see HeadTurn). Off unless switched on.</summary>
        bool TurnWithHead { get; set; }
        /// <summary>Set if turning the head right turns the view left.</summary>
        bool FlipHeadTurn { get; set; }
        /// <summary>How far the head is turned, in degrees, right positive after the flip. 0 when no face is found.</summary>
        float HeadYawDegrees { get; }

        /// <summary>True while eye positions are being written to a file.</summary>
        bool Recording { get; }
        /// <summary>Starts a 20-second recording of eye positions and returns the file's path.</summary>
        string StartRecording();
    }

    public static class HeadTrackingHook
    {
        /// <summary>
        /// Set by the head-tracking assembly when it is present. Arguments: the camera, the "eyes"
        /// object the camera hangs under, and a material for the window frame.
        /// </summary>
        public static Func<Camera, Transform, Material, IHeadTracking> Create;
    }
}
