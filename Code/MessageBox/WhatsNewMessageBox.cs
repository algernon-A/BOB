using ColossalFramework.UI;
using System;
using System.Collections.Generic;
using UnityEngine;


namespace BOB.MessageBox
{
    /// <summary>
    /// 'What's new' message box.
    /// </summary>
    public class WhatsNewMessageBox : DontShowAgainMessageBox
    {
        /// <summary>
        /// Sets the 'what's new' messages to display.
        /// </summary>
        /// <param name="messages">Version update messages to display, in order (newest versions first), with a list of items (as translation keys) for each version</param>
        /// <param name="lastNotifiedVersion">Last notified version (version messages equal to or earlier than this will be minimized</param>
        public void SetMessages(Version lastNotifiedVersion, Dictionary<Version, List<string>> messages)
        {
            // Iterate through each provided version and add it to the messagebox.
            foreach (KeyValuePair<Version, List<string>> message in messages)
            {
                VersionMessage versionMessage = ScrollableContent.AddUIComponent<VersionMessage>();
                versionMessage.width = ScrollableContent.width;
                versionMessage.SetText(message.Key, message.Value);
                // Add spacer below.
                AddSpacer();

                // Hide version messages that have already been notified.
                if (message.Key <= lastNotifiedVersion)
                {
                    versionMessage.IsCollapsed = true;
                }
            }
        }


        /// <summary>
        /// Update message for a given version.
        /// </summary>
        public class VersionMessage : UIPanel
        {
            // Components.
            private readonly UIButton minimizeButton;
            public List<ListItem> listItems;

            // Version title.
            private string versionTitle;

            // Visibility state.
            private bool isExpanded;


            /// <summary>
            /// Sets message expanded/collapsed state.
            /// </summary>
            public bool IsCollapsed { set { isExpanded = value; ToggleExpanded(); } }


            /// <summary>
            /// Constructor - performs basic setup.
            /// </summary>
            public VersionMessage()
            {
                // Init list before we do anything else.
                listItems = new List<ListItem>();

                // Basic setup.
                autoLayout = true;
                autoLayoutDirection = LayoutDirection.Vertical;
                autoFitChildrenVertically = true;
                autoLayoutPadding = new RectOffset(0, 0, 2, 2);

                // Add minimize button (which will also be the version label).
                minimizeButton = AddUIComponent<UIButton>();
                minimizeButton.height = 20f;
                minimizeButton.horizontalAlignment = UIHorizontalAlignment.Left;
                minimizeButton.color = Color.white;
                minimizeButton.textHorizontalAlignment = UIHorizontalAlignment.Left;

                // Toggle visible (minimized) state when clicked.
                minimizeButton.eventClick += (component, eventParam) => ToggleExpanded();
            }


            /// <summary>
            /// Sets version message text.
            /// </summary>
            /// <param name="version">Version</param>
            /// <param name="messageKeys">Message text as list of translation keys for individual points</param>
            public void SetText(Version version, List<string> messageKeys)
            {
                // Set version header and message text.
                versionTitle = BOBMod.ModName + " " + version.ToString();

                // Add messages as separate list items.
                foreach (string messageKey in messageKeys)
                {
                    ListItem newMessageLabel = AddUIComponent<ListItem>();
                    listItems.Add(newMessageLabel);
                    newMessageLabel.Text = Translations.Translate(messageKey);

                    // Make sure initial width is set properly.
                    newMessageLabel.width = width;
                }

                // Always start maximized.
                isExpanded = true;

                // Set state indictor.
                UpdateState();
            }


            /// <summary>
            /// Handles size changed events, for e.g. when visibility changes.  Called by game as needed.
            /// </summary>
            protected override void OnSizeChanged()
            {
                base.OnSizeChanged();

                // Set width of button and label to match new width of list item (whose width has been set by the MessageBox).
                if (minimizeButton != null)
                {
                    minimizeButton.width = width;
                };

                // Set width of each item label.
                if (listItems != null)
                {
                    foreach (ListItem listItem in listItems)
                    {
                        listItem.width = width;
                    }
                };
            }


            /// <summary>
            /// Toggles expanded/collapsed state of the update messages.
            /// </summary>
            private void ToggleExpanded()
            {
                // Toggle state and update state indicator.
                isExpanded = !isExpanded;
                UpdateState();

                // Show/hide each list item according to state.
                foreach (ListItem listItem in listItems)
                {
                    listItem.isVisible = isExpanded;
                }
            }


            /// <summary>
            /// Sets expaned/collapsed state indicator.
            /// </summary>
            private void UpdateState() => minimizeButton.text = (isExpanded ? "▼ " : "► ") + versionTitle;
        }
    }
}