using System;
using UnityEngine;

namespace Swing.Game
{
    /// <summary>
    /// What the game needs from head tracking. The real thing lives in Scripts/HeadTracking and is
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
