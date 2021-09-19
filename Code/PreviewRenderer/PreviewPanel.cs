using ColossalFramework.UI;
using UnityEngine;


namespace BOB
{
    /// <summary>
    /// Panel that contains the building preview image.
    /// </summary>
    public class PreviewPanel : UIPanel
    {
        // Layout constants.
        private const float Margin = 5f;
        private const float RenderHeight = 150f;
        private const float RenderWidth = RenderHeight;


        // Panel components.
        private UITextureSprite previewSprite;
        private UISprite noPreviewSprite;
        private PreviewRenderer renderer;
        private UIPanel renderPanel;

        // Currently selected prefab.
        private PrefabInfo renderPrefab;


        public void SetTarget(PrefabInfo prefab)
        {
            // Update current selection to the new prefab.
            renderPrefab = prefab;

            // Show the updated render.
            RenderPreview();
        }


        /// <summary>
        /// Render and show a preview of a building.
        /// </summary>
        /// <param name="building">The building to render</param>
        public void RenderPreview()
        {
            if (renderPrefab is PropInfo prop)
            {
                // Safety first!
                if (prop?.m_mesh == null || prop.m_material == null)
                {
                    return;
                }

                // Set mesh and material for render.
                renderer.Mesh = prop.m_mesh;
                renderer.Material = prop.m_material;

                // props with prop fence shader need to be handled differently or they are not viewable
                renderer.IsPropFenceShader = false;
                if (!prop.m_isCustomContent)
                {
                    if (prop.m_material.shader == renderer.PropFenceShader)
                    {
                        renderer.IsPropFenceShader = true;
                    }
                }

                // If the selected prop has colour variations, temporarily set the colour to the default for rendering.
                if (prop.m_useColorVariations)
                {
                    Color originalColor = prop.m_material.color;
                    prop.m_material.color = prop.m_color0;
                    renderer.Render();
                    prop.m_material.color = originalColor;
                }
                else
                {
                    // No temporary colour change needed.
                    renderer.Render();
                }
            }
            else if (renderPrefab is TreeInfo tree)
            {
                // Safety first!
                if (tree?.m_mesh == null || tree.m_material == null)
                {
                    return;
                }

                // Set mesh and material for render.
                renderer.Mesh = tree.m_mesh;
                renderer.Material = tree.m_material;
                renderer.IsPropFenceShader = false;

                // Render.
                renderer.Render();
            }
            else
            {
                // No valid current selection with a mesh; reset background.
                previewSprite.texture = null;
                noPreviewSprite.isVisible = true;
                return;
            }

            // If we got here, we should have a render; show it.
            previewSprite.texture = renderer.Texture;
            noPreviewSprite.isVisible = false;
        }


        /// <summary>
        /// Constructor.
        /// </summary>
        internal void Setup()
        {
            // Size and position.
            width = RenderWidth + (Margin * 2f);
            height = RenderHeight + (Margin * 2f);

            // Appearance.
            backgroundSprite = "UnlockingPanel2";
            opacity = 1.0f;

            // Drag bar.
            UIDragHandle dragHandle = AddUIComponent<UIDragHandle>();
            dragHandle.width = this.width ;
            dragHandle.height = this.height;
            dragHandle.relativePosition = Vector3.zero;
            dragHandle.target = this;

            // Preview render panel.
            renderPanel = AddUIComponent<UIPanel>();
            renderPanel.backgroundSprite = "UnlockingItemBackground";
            renderPanel.height = RenderHeight;
            renderPanel.width = RenderWidth;
            renderPanel.relativePosition = new Vector2(Margin, Margin);

            previewSprite = renderPanel.AddUIComponent<UITextureSprite>();
            previewSprite.size = renderPanel.size;
            previewSprite.relativePosition = Vector3.zero;

            noPreviewSprite = AddUIComponent<UISprite>();
            noPreviewSprite.size = renderPanel.size;
            noPreviewSprite.relativePosition = Vector3.zero;

            // Initialise renderer; use double size for anti-aliasing.
            renderer = gameObject.AddComponent<PreviewRenderer>();
            renderer.Size = previewSprite.size * 2;

            // Click-and-drag rotation.
            eventMouseDown += (component, mouseEvent) =>
            {
                eventMouseMove += RotateCamera;
            };

            eventMouseUp += (component, mouseEvent) =>
            {
                eventMouseMove -= RotateCamera;
            };

            // Zoom with mouse wheel.
            eventMouseWheel += (component, mouseEvent) =>
            {
                renderer.Zoom -= Mathf.Sign(mouseEvent.wheelDelta) * 0.25f;

                // Render updated image.
                RenderPreview();
            };
        }


        /// <summary>
        /// Rotates the preview camera (model rotation) in accordance with mouse movement.
        /// </summary>
        /// <param name="c">Not used</param>
        /// <param name="p">Mouse event</param>
        private void RotateCamera(UIComponent c, UIMouseEventParameter p)
        {
            // Change rotation.
            renderer.CameraRotation -= p.moveDelta.x / previewSprite.width * 360f;

            // Render updated image.
            RenderPreview();
        }
    }
}