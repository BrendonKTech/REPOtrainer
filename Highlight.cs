using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace REPOItemESP
{
    public partial class Plugin
    {
        private struct HighlightPart
        {
            public Renderer Renderer;
            public int SubMeshes;
        }

        private CommandBuffer highlightBuffer;
        private Camera highlightCamera;
        private Shader highlightShader;
        private bool highlightFailed;
        private readonly Dictionary<Color, Material> highlightMaterials = new Dictionary<Color, Material>();
        private readonly Dictionary<Component, HighlightPart[]> rendererCache = new Dictionary<Component, HighlightPart[]>();

        private void BeginHighlights()
        {
            if (highlightFailed)
            {
                return;
            }

            if (highlightBuffer == null)
            {
                // built in unity shader, lets us set ztest
                highlightShader = Shader.Find("Hidden/Internal-Colored");
                if (highlightShader == null)
                {
                    Logger.LogWarning("Highlight shader not found, highlighting is disabled.");
                    highlightFailed = true;
                    return;
                }
                highlightBuffer = new CommandBuffer { name = "REPO ESP highlight" };
            }

            highlightBuffer.Clear();

            Camera cam = Camera.main;
            if (cam != highlightCamera)
            {
                if (highlightCamera != null)
                {
                    highlightCamera.RemoveCommandBuffer(CameraEvent.AfterForwardAlpha, highlightBuffer);
                }
                if (cam != null)
                {
                    cam.AddCommandBuffer(CameraEvent.AfterForwardAlpha, highlightBuffer);
                }
                highlightCamera = cam;
            }
        }

        private void Highlight(Camera cam, Component root, Vector3 pos, Color color)
        {
            if (highlightBuffer == null || Vector3.Distance(cam.transform.position, pos) > maxDistance.Value)
            {
                return;
            }

            Material material = HighlightMaterial(color);
            foreach (HighlightPart part in GetParts(root))
            {
                Renderer renderer = part.Renderer;
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                {
                    continue;
                }
                for (int i = 0; i < part.SubMeshes; i++)
                {
                    highlightBuffer.DrawRenderer(renderer, material, i);
                }
            }
        }

        // cleared every second in Update
        private HighlightPart[] GetParts(Component root)
        {
            if (rendererCache.TryGetValue(root, out HighlightPart[] parts))
            {
                return parts;
            }

            List<HighlightPart> list = new List<HighlightPart>();
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
            {
                Mesh mesh = null;
                if (renderer is SkinnedMeshRenderer skinned)
                {
                    mesh = skinned.sharedMesh;
                }
                else if (renderer is MeshRenderer)
                {
                    MeshFilter filter = renderer.GetComponent<MeshFilter>();
                    mesh = filter != null ? filter.sharedMesh : null;
                }

                // skip particles etc
                if (mesh != null)
                {
                    list.Add(new HighlightPart { Renderer = renderer, SubMeshes = mesh.subMeshCount });
                }
            }

            parts = list.ToArray();
            rendererCache[root] = parts;
            return parts;
        }

        private Material HighlightMaterial(Color color)
        {
            if (highlightMaterials.TryGetValue(color, out Material material))
            {
                return material;
            }

            material = new Material(highlightShader) { hideFlags = HideFlags.HideAndDontSave };
            material.color = new Color(color.r, color.g, color.b, 0.5f);
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            // Greater = only the parts behind walls. dont change or it draws over everything
            // cull back or it tints itself
            material.SetInt("_ZTest", (int)CompareFunction.Greater);
            material.SetInt("_Cull", (int)CullMode.Back);

            highlightMaterials[color] = material;
            return material;
        }
    }
}
