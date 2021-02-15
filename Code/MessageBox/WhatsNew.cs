using System;
using System.Reflection;
using System.Collections.Generic;
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
                new Version("0.5"),
                new List<string>
                {
                    "BOB_UPD_050_0",
                    "BOB_UPD_050_1",
                    "BOB_UPD_050_2"
                }
            },
            {
                new Version("0.4.3"),
                new List<string>
                {
                    "BOB_UPD_043_0",
                    "BOB_UPD_043_1",
                    "BOB_UPD_043_2"
                }
            },
            {
                new Version("0.4.2"),
                new List<string>
                {
                    "BOB_UPD_042_0",
                    "BOB_UPD_042_1"
                }
            },
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
            SettingsUtils.SaveSettings();

            return true;
        }


        /// <summary>
        /// Check if there's been an update since the last notification, and if so, show the update.
        /// </summary>
        internal static void ShowWhatsNew()
        {
            // Get last notified version and current mod version.
            Version whatsNewVersion = new Version(ModSettings.whatsNewVersion);
            Version modVersion = Assembly.GetExecutingAssembly().GetName().Version;

            // Don't show notification if we're already up to (or ahead of) this version.
            if (whatsNewVersion >= modVersion)
            {
                return;
            }

            // Show messagebox.
            WhatsNewMessageBox messageBox = MessageBoxBase.ShowModal<WhatsNewMessageBox>();
            messageBox.Title = BOBMod.ModName + " " + BOBMod.Version;
            messageBox.DSAButton.eventClicked += (component, clickEvent) => DontShowAgain();
            messageBox.SetMessages(whatsNewVersion, Versions);
        }
    }
}