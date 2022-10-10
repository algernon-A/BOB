// <copyright file="PreviewPanel.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace BOB
{
    using ColossalFramework.UI;
    using UnityEngine;

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
        private readonly UITextureSprite _previewSprite;
        private readonly UISprite _noPreviewSprite;
        private readonly UISprite _thumbnailSprite;
        private readonly PreviewRenderer _renderer;
        private readonly UIPanel _renderPanel;

        // Currently selected prefab.
        private PrefabInfo _renderPrefab;

        /// <summary>
        /// Initializes a new instance of the <see cref="PreviewPanel"/> class.
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
            dragHandle.width = this.width;
            dragHandle.height = this.height;
            dragHandle.relativePosition = Vector3.zero;
            dragHandle.target = this;

            // Preview render panel.
            _renderPanel = AddUIComponent<UIPanel>();
            _renderPanel.backgroundSprite = "AssetEditorItemBackgroundDisabled";
            _renderPanel.height = RenderHeight;
            _renderPanel.width = RenderWidth;
            _renderPanel.relativePosition = new Vector2(Margin, Margin);

            _previewSprite = _renderPanel.AddUIComponent<UITextureSprite>();
            _previewSprite.size = _renderPanel.size;
            _previewSprite.relativePosition = Vector3.zero;

            _noPreviewSprite = _renderPanel.AddUIComponent<UISprite>();
            _noPreviewSprite.size = _renderPanel.size;
            _noPreviewSprite.relativePosition = Vector3.zero;

            _thumbnailSprite = _renderPanel.AddUIComponent<UISprite>();
            _thumbnailSprite.size = _renderPanel.size;
            _thumbnailSprite.relativePosition = Vector3.zero;

            // Initialise renderer; use double size for anti-aliasing.
            _renderer = gameObject.AddComponent<PreviewRenderer>();
            _renderer.Size = _previewSprite.size * 2;

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
                _renderer.Zoom -= Mathf.Sign(mouseEvent.wheelDelta) * 0.25f;

                // Render updated image.
                RenderPreview();
            };
        }

        /// <summary>
        /// Sets the prefab to render.
        /// </summary>
        /// <param name="prefab">Prefab to render.</param>
        public void SetTarget(PrefabInfo prefab)
        {
            // Update current selection to the new prefab.
            _renderPrefab = prefab;

            // Show the updated render.
            RenderPreview();
        }

        /// <summary>
        /// Render and show a preview of a building.
        /// </summary>
        public void RenderPreview()
        {
            bool validRender = false;

            if (_renderPrefab is PropInfo prop)
            {
                if (prop.m_isDecal)
                {
                    // Special case for decals - just use main texture directly.
                    _previewSprite.texture = prop.m_material.mainTexture?.MakeReadable();

                    // Change width of previw sprite for non-square decals and ensure preview is centred in window; use double multiplied by 100 to avoid bizarre rounding errors.
                    double widthRatio = 100d, heightRatio = 100d;
                    if (prop.m_material.mainTexture.width > prop.m_material.mainTexture.height)
                    {
                        // Wider than high.
                        heightRatio = (prop.m_material.mainTexture.height * 100d) / prop.m_material.mainTexture.width;
                    }

                    if (prop.m_material.mainTexture.width < prop.m_material.mainTexture.height)
                    {
                        // Higher than wide.
                        widthRatio = (prop.m_material.mainTexture.width * 100d) / prop.m_material.mainTexture.height;
                    }

                    // Set preview sprite size and centre in renderPanel.
                    _previewSprite.width = (float)(_renderPanel.width * widthRatio / 100d);
                    _previewSprite.height = (float)(_renderPanel.height * heightRatio / 100d);
                    _previewSprite.relativePosition = new Vector2((_renderPanel.width - _previewSprite.width) / 2f, (_renderPanel.height - _previewSprite.height) / 2f);

                    // Treat this as a valid render.
                    validRender = true;
                }
                else
                {
                    // Not a decal - don't render anything without a mesh or material.
                    if (prop?.m_mesh != null && prop.m_material != null && !prop.m_mesh.name.Equals("none"))
                    {
                        // Set mesh and material for render.
                        _renderer.Mesh = prop.m_mesh;
                        _renderer.Material = prop.m_material;

                        if (prop.m_material?.mainTexture == null)
                        {
                            if (prop.m_variations != null && prop.m_variations.Length > 0 && prop.m_variations[0].m_finalProp != null)
                            {
                                _renderer.Mesh = prop.m_variations[0].m_finalProp.m_mesh;
                                _renderer.Material = prop.m_variations[0].m_finalProp.m_material;
                            }
                        }

                        // If the selected prop has colour variations, temporarily set the colour to the default for rendering.
                        if (prop.m_useColorVariations)
                        {
                            Color originalColor = prop.m_material.color;
                            prop.m_material.color = prop.m_color0;
                            _renderer.Render();
                            prop.m_material.color = originalColor;
                        }
                        else
                        {
                            // No temporary colour change needed.
                            _renderer.Render();
                        }

                        // We got a valid render; ensure preview sprite is square (decal previews can change width), set display texture, and set status flag.
                        _previewSprite.relativePosition = Vector3.zero;
                        _previewSprite.size = _renderPanel.size;
                        _previewSprite.texture = _renderer.Texture;
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
            else if (_renderPrefab is TreeInfo tree)
            {
                // Don't render anything without a mesh or material.
                if (tree?.m_mesh != null && tree.m_material != null)
                {
                    // Set mesh and material for render.
                    _renderer.Mesh = tree.m_mesh;
                    _renderer.Material = tree.m_material;

                    // Render.
                    _renderer.Render();

                    // We got a valid render; ensure preview sprite is square (decal previews can change width), set display texture, and set status flag.
                    _previewSprite.relativePosition = Vector3.zero;
                    _previewSprite.size = _renderPanel.size;
                    _previewSprite.texture = _renderer.Texture;
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
                _previewSprite.Hide();
                _thumbnailSprite.Hide();
                _noPreviewSprite.Show();
                return;
            }

            // If we got here, we should have a render; show it.
            _noPreviewSprite.Hide();
            _thumbnailSprite.Hide();
            _previewSprite.Show();
        }

        /// <summary>
        /// Rotates the preview camera (model rotation) in accordance with mouse movement.
        /// </summary>
        /// <param name="c">Calling component.</param>
        /// <param name="p">Mouse event pareameter.</param>
        private void RotateCamera(UIComponent c, UIMouseEventParameter p)
        {
            // Change rotation.
            _renderer.CameraRotation -= p.moveDelta.x / _previewSprite.width * 360f;

            // Render updated image.
            RenderPreview();
        }

        /// <summary>
        /// Displays a prefab's UI thumbnail (instead of a render or blank panel).
        /// </summary>
        /// <param name="atlas">Thumbnail atlas.</param>
        /// <param name="thumbnail">Thumbnail sprite name.</param>
        private void ShowThumbnail(UITextureAtlas atlas, string thumbnail)
        {
            // Set thumbnail.
            _thumbnailSprite.atlas = atlas;
            _thumbnailSprite.spriteName = thumbnail;

            // Show thumbnail sprite and hide others.
            _noPreviewSprite.Hide();
            _previewSprite.Hide();
            _thumbnailSprite.Show();
        }
    }
}