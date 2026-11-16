using UnityEngine;

namespace ManhattanSwing.Game
{
    /// <summary>
    /// The flat-colour shader for rings, the web and the head-tracking frame. "Swing/Unlit" is
    /// written so it runs in both the built-in pipeline (this repository) and URP (the original
    /// repository). If it is missing or the graphics card refuses it, Unity's own unlit shaders are
    /// tried instead. Colours set with <c>material.color</c> work with all of them.
    /// </summary>
    public static class UnlitShader
    {
        public static Shader Find()
        {
            Shader shader = Resources.Load<Shader>("Shaders/SwingUnlit");
            if (Usable(shader)) return shader;
            shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (Usable(shader)) return shader;
            shader = Shader.Find("Unlit/Color");
            if (Usable(shader)) return shader;
            Debug.LogWarning("Manhattan Swing: no unlit shader found; rings and the web may look wrong.");
            return Shader.Find("Sprites/Default");
        }

        private static bool Usable(Shader shader)
        {
            return shader != null && shader.isSupported;
        }
    }
}
