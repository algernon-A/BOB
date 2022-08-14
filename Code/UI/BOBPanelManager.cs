// <copyright file="BOBPanelManager.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace BOB
{
    using System;
    using AlgernonCommons;
    using AlgernonCommons.Notifications;
    using ColossalFramework.UI;
    using UnityEngine;

    /// <summary>
    /// Static class to manage the BOB panels.
    /// </summary>
    internal static class BOBPanelManager
    {
        // Instance references.
        private static GameObject s_gameObject;
        private static BOBInfoPanelBase s_panel;

        // Previous state.
        private static float s_previousX;
        private static float s_previousY;

        // Exception flaggin.
        private static bool s_wasException = false;
        private static bool s_displayingException = false;

        /// <summary>
        /// Gets or sets the message from any exception.
        /// </summary>
        internal static string ExceptionMessage { get; set; }

        /// <summary>
        /// Gets the active panel instance.
        /// </summary>
        internal static BOBInfoPanelBase Panel => s_panel;

        /// <summary>
        /// Gets the last saved panel X position.
        /// </summary>
        internal static float PreviousX => s_previousX;

        /// <summary>
        /// Gets the last saved panel Y position.
        /// </summary>
        internal static float PreviousY => s_previousY;

        /// <summary>
        /// Sets the BOB target parent prefab to the selected prefab, creating the relevant info window if necessary.
        /// </summary>
        /// <param name="selectedPrefab">Target parent prefab.</param>
        internal static void SetTargetParent(PrefabInfo selectedPrefab)
        {
            // If no existing panel, create it.
            if (Panel == null)
            {
                Create(selectedPrefab);
            }
            else
            {
                // Otherwise, check for panel and prefab type match; if they match, update existing panel, otherwise close the existing panel (retaining BOB tool) and create new one with the new selection.
                if (selectedPrefab is BuildingInfo)
                {
                    // Building.
                    if (Panel is BOBBuildingPanel)
                    {
                        Panel.SetTargetParent(selectedPrefab);
                    }
                    else
                    {
                        Close(false);
                        Create(selectedPrefab);
                    }
                }
                else if (selectedPrefab is NetInfo)
                {
                    // Network.
                    if (Panel is BOBNetPanel)
                    {
                        Panel.SetTargetParent(selectedPrefab);
                    }
                    else
                    {
                        Close(false);
                        Create(selectedPrefab);
                    }
                }
                else if (selectedPrefab is TreeInfo || selectedPrefab is PropInfo)
                {
                    // Standalone tree/prop.
                    if (Panel is BOBMapPanel)
                    {
                        Panel.SetTargetParent(selectedPrefab);
                    }
                    else
                    {
                        Close(false);
                        Create(selectedPrefab);
                    }
                }
            }
        }

        /// <summary>
        /// Closes the panel by destroying the object (removing any ongoing UI overhead).
        /// </summary>
        /// <param name="resetTool">True to reset to default tool; false to leave current tool untouched (default true).</param>
        internal static void Close(bool resetTool = true)
        {
            // Check for null, just in case - this is also called by pressing Esc when BOB tool is active.
            if (s_panel != null)
            {
                // Perform any panel actions on close.
                s_panel.Close();

                // Stop highlighting.
                s_panel.SelectedTargetItem = null;
                RenderOverlays.Building = null;

                // Revert overlay patches.
                Patcher.Instance.PatchBuildingOverlays(false);
                Patcher.Instance.PatchNetworkOverlays(false);
                Patcher.Instance.PatchMapOverlays(false);

                // Clear tool lane overlay list.
                BOBTool.Instance.LaneOverlays.Clear();

                // Store previous position.
                s_previousX = Panel.relativePosition.x;
                s_previousY = Panel.relativePosition.y;

                // Destroy game objects.
                GameObject.Destroy(Panel);
                GameObject.Destroy(s_gameObject);

                // Let the garbage collector do its work (and also let us know that we've closed the object).
                s_panel = null;
                s_gameObject = null;

                // Restore default tool if needed.
                if (resetTool)
                {
                    ToolsModifierControl.SetTool<DefaultTool>();
                }
            }
        }

        /// <summary>
        /// Exception occured event handler.
        /// </summary>
        /// <param name="exceptionMessage">Exception message.</param>
        internal static void RecordException(string exceptionMessage)
        {
            s_wasException = true;
            ExceptionMessage = exceptionMessage;
        }

        /// <summary>
        /// Checks to see if an exception has occured and, and if so displays it (if we aren't already).
        /// </summary>
        internal static void CheckException()
        {
            // Display exception message if an exception occured and we're not already displaying one.
            if (s_wasException && !s_displayingException)
            {
                // Set displaying flag and show message.
                s_displayingException = true;
                NotificationBase.ShowNotification<ExceptionNotification>();
            }
        }

        /// <summary>
        /// Clears any exception data (e.g. once the notification has been displayed).
        /// </summary>
        internal static void ClearException()
        {
            s_wasException = false;
            s_displayingException = false;
        }

        /// <summary>
        /// Refreshes random prop/tree lists on close of random panel.
        /// </summary>
        internal static void RefreshRandom()
        {
            if (Panel is BOBInfoPanel infoPanel)
            {
                infoPanel.RefreshRandom();
            }
        }

        /// <summary>
        /// Creates the panel object in-game and displays it.
        /// </summary>
        private static void Create(PrefabInfo selectedPrefab)
        {
            try
            {
                // If no instance already set, create one.
                if (s_gameObject == null)
                {
                    if (selectedPrefab is BuildingInfo)
                    {
                        // A building prefab is selected; create a BuildingInfo panel.
                        // Give it a unique name for easy finding with ModTools.
                        s_gameObject = new GameObject("BOBBuildingPanel");
                        s_gameObject.transform.parent = UIView.GetAView().transform;

                        s_panel = s_gameObject.AddComponent<BOBBuildingPanel>();
                    }
                    else if (selectedPrefab is NetInfo)
                    {
                        // A network prefab is selected; create a NetInfo panel.
                        // Give it a unique name for easy finding with ModTools.
                        s_gameObject = new GameObject("BOBNetPanel");
                        s_gameObject.transform.parent = UIView.GetAView().transform;

                        s_panel = s_gameObject.AddComponent<BOBNetPanel>();
                    }
                    else if (selectedPrefab is TreeInfo || selectedPrefab is PropInfo)
                    {
                        // A tree prefab is selected; create a TreeInfo panel.
                        // Give it a unique name for easy finding with ModTools.
                        s_gameObject = new GameObject("BOBMapPanel");
                        s_gameObject.transform.parent = UIView.GetAView().transform;
                        s_panel = s_gameObject.AddComponent<BOBMapPanel>();
                    }
                    else
                    {
                        Logging.Message("unsupported prefab type ", selectedPrefab);
                        return;
                    }

                    // Set up panel with selected prefab.
                    Panel.SetTargetParent(selectedPrefab);
                }
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception creating InfoPanel");

                // Destroy the GameObjects rather than have a half-functional (at best) panel that confuses players.
                if (s_panel != null)
                {
                    GameObject.Destroy(s_panel);
                    s_panel = null;
                }

                if (s_gameObject != null)
                {
                    GameObject.Destroy(s_gameObject);
                    s_gameObject = null;
                }
            }
        }
    }
}
