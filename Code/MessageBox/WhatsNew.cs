using System;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using BOB.MessageBox;



namespace BOB
{
    /// <summary>
    /// "What's new" message box.  Based on macsergey's code in Intersection Marking Tool (Node Markup) mod.
    /// </summary>
    internal static class WhatsNew
    {
        // List of versions and associated update message lines (as translation keys).
        private static Dictionary<Version, List<string>> Versions => new Dictionary<Version, List<String>>
        {
            {
                new Version("0.4.1"),
                new List<string>
                {
                    "BOB_UPD_041_0"
                }
            },
            {
                new Version("0.4"),
                new List<string>
                {
                    "BOB_UPD_040_0"
                }
            }
        };


        /// <summary>
        /// Close button action.
        /// </summary>
        /// <returns>True (always)</returns>
        public static bool Confirm() => true;

        /// <summary>
        /// 'Don't show again' button action.
        /// </summary>
        /// <returns>True (always)</returns>
        public static bool DontShowAgain()
        {
            // Save current version to settings file.
            ModSettings.whatsNewVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            //ModSettings.whatsNewBeta = PloppableRICOMod.Beta;
            SettingsUtils.SaveSettings();

            return true;
        }


        /// <summary>
        /// Check if there's been an update since the last notification, and if so, show the update.
        /// </summary>
        internal static void ShowWhatsNew()
        {
            // Get last notified version and current mod version.
            Version whatNewVersion = new Version(ModSettings.whatsNewVersion);
            Version modVersion = Assembly.GetExecutingAssembly().GetName().Version;

            // Don't show notification if we're already up to (or ahead of) this version AND there hasn't been a beta update.
            if (whatNewVersion >= modVersion)// && ModSettings.whatsNewBeta.Equals(PloppableRICOMod.Beta))
            {
                return;
            }

            // Get version update messages.
            Dictionary<Version, string> messages = GetWhatsNewMessages(whatNewVersion, modVersion);

            // Don't do anything if no version messages to display. 
            if (!messages.Any())
            {
                return;
            }

            // Show messagebox
            WhatsNewMessageBox messageBox = MessageBoxBase.ShowModal<WhatsNewMessageBox>();
            messageBox.Title = BOBMod.ModName + " " + BOBMod.Version;
            messageBox.OnButton1Click = Confirm;
            messageBox.OnButton2Click = DontShowAgain;
            messageBox.Init(messages);
        }


        /// <summary>
        /// Builds a dictionary of versions and associated what's new messages.
        /// </summary>
        /// <param name="lastNotifiedVersion">Most recently notified version</param>
        /// <param name="modVersion">Current mod version</param>
        /// <returns>New dictionary of version and associated what's new strings</returns>
        private static Dictionary<Version, string> GetWhatsNewMessages(Version lastNotifiedVersion, Version modVersion)
        {
            Dictionary<Version, string> messages = new Dictionary<Version, string>();

            // Iterate through each version 
            foreach (var version in Versions.Keys)
            {
                // Skip this version message if it's newer than the current mod version, or older than the last notified version AND there hasn't been a beta update.
                if (version > modVersion || (version <= lastNotifiedVersion)) // && ModSettings.whatsNewBeta.Equals(PloppableRICOMod.Beta)))
                {
                    continue;
                }

                // Convert the message list for this version into a single string, and append it to the dictionary of messages to display.
                StringBuilder message = new StringBuilder();
                foreach (string line in Versions[version])
                {
                    message.AppendLine(Translations.Translate(line));
                }
                messages.Add(version, message.ToString());
            }

            return messages;
        }
    }
}