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
        private readonly UITextureSprite previewSprite;
        private readonly UISprite noPreviewSprite, thumbnailSprite;
        private readonly PreviewRenderer renderer;
        private readonly UIPanel renderPanel;

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
            bool validRender = false;

            if (renderPrefab is PropInfo prop)
            {
                if (prop.m_isDecal)
                {
                    // Special case for decals - just use main texture directly.
                    previewSprite.texture = prop.m_material.mainTexture?.MakeReadable();

                    // Change width of previw sprite for non-square decals and ensure preview is centred in window; use double multiplied by 100 to avoid bizarre rounding errors.
                    double widthRatio = 100d, heightRatio = 100d;
                    if (prop.m_material.mainTexture.width > prop.m_material.mainTexture.height)
                    {
                        // Wider than high.
                        heightRatio = ((prop.m_material.mainTexture.height * 100d) / prop.m_material.mainTexture.width);
                    }
                    if (prop.m_material.mainTexture.width < prop.m_material.mainTexture.height)
                    {
                        // Higher than wide.
                        widthRatio = ((prop.m_material.mainTexture.width * 100d) / prop.m_material.mainTexture.height);
                    }

                    // Set preview sprite size and centre in renderPanel.
                    previewSprite.width = (float)((renderPanel.width * widthRatio) / 100d);
                    previewSprite.height = (float)((renderPanel.height * heightRatio) / 100d);
                    previewSprite.relativePosition = new Vector2((renderPanel.width - previewSprite.width) / 2f, (renderPanel.height - previewSprite.height) / 2f);

                    // Treat this as a valid render.
                    validRender = true;
                }
                else
                {
                    // Not a decal - don't render anything without a mesh or material.
                    if (prop?.m_mesh != null && prop.m_material != null && !prop.m_mesh.name.Equals("none"))
                    {
                        // Set mesh and material for render.
                        renderer.Mesh = prop.m_mesh;
                        renderer.Material = prop.m_material;

                        if (prop.m_material?.mainTexture == null)
                        {
                            if (prop.m_variations != null && prop.m_variations.Length > 0 && prop.m_variations[0].m_prop != null)
                            {
                                renderer.Mesh = prop.m_variations[0].m_prop.m_mesh;
                                renderer.Material = prop.m_variations[0].m_prop.m_material;
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

                        // We got a valid render; ensure preview sprite is square (decal previews can change width), set display texture, and set status flag.
                        previewSprite.relativePosition = Vector3.zero;
                        previewSprite.size = renderPanel.size;
                        previewSprite.texture = renderer.Texture;
                        validRender = true;
                    }
                    // If not a valid render, try to show thumbnail instead.
                    else if (prop.m_Atlas != null && !string.IsNullOrEmpty(prop.m_Thumbnail))
                    {
                        // Show thumbnail.
                        ShowThumbnail(prop.m_Atlas, prop.m_Thumbnail);

                        // All done here.
                        return;
                    }
                }
            }
            else if (renderPrefab is TreeInfo tree)
            {
                // Don't render anything without a mesh or material.
                if (tree?.m_mesh != null && tree.m_material != null)
                {
                    // Set mesh and material for render.
                    renderer.Mesh = tree.m_mesh;
                    renderer.Material = tree.m_material;

                    // Render.
                    renderer.Render();

                    // We got a valid render; ensure preview sprite is square (decal previews can change width), set display texture, and set status flag.
                    previewSprite.relativePosition = Vector3.zero;
                    previewSprite.size = renderPanel.size;
                    previewSprite.texture = renderer.Texture;
                    validRender = true;
                }
                // If not a valid render, try to show thumbnail instead.
                else if (tree.m_Atlas != null && !string.IsNullOrEmpty(tree.m_Thumbnail))
                {
                    // Show thumbnail.
                    ShowThumbnail(tree.m_Atlas, tree.m_Thumbnail);

                    // All done here.
                    return;
                }
            }

            // Reset background if we didn't get a valid render.
            if (!validRender)
            {
                previewSprite.Hide();
                thumbnailSprite.Hide();
                noPreviewSprite.Show();
                return;
            }

            // If we got here, we should have a render; show it.
            noPreviewSprite.Hide();
            thumbnailSprite.Hide();
            previewSprite.Show();
        }


        /// <summary>
        /// Constructor.
        /// </summary>
        internal PreviewPanel()
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
            renderPanel.backgroundSprite = "AssetEditorItemBackgroundDisabled";
            renderPanel.height = RenderHeight;
            renderPanel.width = RenderWidth;
            renderPanel.relativePosition = new Vector2(Margin, Margin);

            previewSprite = renderPanel.AddUIComponent<UITextureSprite>();
            previewSprite.size = renderPanel.size;
            previewSprite.relativePosition = Vector3.zero;

            noPreviewSprite = renderPanel.AddUIComponent<UISprite>();
            noPreviewSprite.size = renderPanel.size;
            noPreviewSprite.relativePosition = Vector3.zero;

            thumbnailSprite = renderPanel.AddUIComponent<UISprite>();
            thumbnailSprite.size = renderPanel.size;
            thumbnailSprite.relativePosition = Vector3.zero;

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


        /// <summary>
        /// Displays a prefab's UI thumbnail (instead of a render or blank panel).
        /// </summary>
        /// <param name="atlas"></param>
        /// <param name="thumbnail"></param>
        private void ShowThumbnail(UITextureAtlas atlas, string thumbnail)
        {
            // Set thumbnail.
            thumbnailSprite.atlas = atlas;
            thumbnailSprite.spriteName = thumbnail;

            // Show thumbnail sprite and hide others.
            noPreviewSprite.Hide();
            previewSprite.Hide();
            thumbnailSprite.Show();
        }
    }
}