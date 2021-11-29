using System;
using System.Reflection;
using BOB.MessageBox;


namespace BOB
{
    /// <summary>
    /// "What's new" message box.  Based on macsergey's code in Intersection Marking Tool (Node Markup) mod.
    /// </summary>
    internal static class WhatsNew
    {
        // List of versions and associated update message lines (as translation keys).
        private readonly static WhatsNewMessage[] WhatsNewMessages = new WhatsNewMessage[]
        {
            new WhatsNewMessage
            {
                version = new Version("0.8.6.0"),
                versionHeader = "",
                messageKeys = true,
                messages = new string[]
                {
                    "BOB_UPD_086_0"
                }
            },new WhatsNewMessage
            {
                version = new Version("0.8.0.0"),
                versionHeader = "",
                messageKeys = true,
                messages = new string[]
                {
                    "BOB_UPD_080_0",
                    "BOB_UPD_080_1"
                }
            },
            new WhatsNewMessage
            {
                version = new Version("0.6.2.0"),
                versionHeader = "",
                messageKeys = true,
                messages = new string[]
                {
                    "BOB_UPD_062_0",
                    "BOB_UPD_062_1",
                    "BOB_UPD_062_2"
                }
            },
            new WhatsNewMessage
            {
                version = new Version("0.6.1.0"),
                versionHeader = "",
                messageKeys = true,
                messages = new string[]
                {
                    "BOB_UPD_061_0"
                }
            },
            new WhatsNewMessage
            {
                version = new Version("0.6.0.0"),
                versionHeader = "",
                messageKeys = true,
                messages = new string[]
                {
                    "BOB_UPD_060_0",
                    "BOB_UPD_060_1",
                    "BOB_UPD_060_2",
                    "BOB_UPD_060_3",
                    "BOB_UPD_060_4"
                }
            },
            new WhatsNewMessage
            {
                version = new Version("0.5.1.0"),
                versionHeader = "",
                messageKeys = true,
                messages = new string[]
                {
                    "BOB_UPD_051_0"
                }
            },
            new WhatsNewMessage
            {
                version = new Version("0.5.0.0"),
                versionHeader = "",
                messageKeys = true,
                messages = new string[]
                {
                    "BOB_UPD_050_0",
                    "BOB_UPD_050_1",
                    "BOB_UPD_050_2"
                }
            },
            new WhatsNewMessage
            {
                version = new Version("0.4.3.0"),
                versionHeader = "",
                messageKeys = true,
                messages = new string[]
                {
                    "BOB_UPD_043_0",
                    "BOB_UPD_043_1",
                    "BOB_UPD_043_2"
                }
            },
            new WhatsNewMessage
            {
                version = new Version("0.4.2.0"),
                versionHeader = "",
                messageKeys = true,
                messages = new string[]
                {
                    "BOB_UPD_042_0",
                    "BOB_UPD_042_1"
                }
            },
            new WhatsNewMessage
            {
                version = new Version("0.4.1.0"),
                versionHeader = "",
                messageKeys = true,
                messages = new string[]
                {
                    "BOB_UPD_041_0"
                }
            },
            new WhatsNewMessage
            {
                version = new Version("0.4.0.0"),
                versionHeader = "",
                messageKeys = true,
                messages = new string[]
                {
                    "BOB_UPD_040_0"
                }
            }
        };


        /// <summary>
        /// Close button action.
        /// </summary>
        /// <returns>True (always)</returns>
        internal static bool Confirm() => true;

        /// <summary>
        /// 'Don't show again' button action.
        /// </summary>
        /// <returns>True (always)</returns>
        internal static bool DontShowAgain()
        {
            // Save current version to settings file.
            ModSettings.whatsNewVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            // Save current version header as beta.
            ModSettings.whatsNewBetaVersion = WhatsNewMessages[0].betaVersion;
            ModSettings.Save();

            return true;
        }


        /// <summary>
        /// Check if there's been an update since the last notification, and if so, show the update.
        /// </summary>
        internal static void ShowWhatsNew()
        {
            // Get last notified version and current mod version.
            Version whatsNewVersion = new Version(ModSettings.whatsNewVersion);
            WhatsNewMessage latestMessage = WhatsNewMessages[0];

            // Don't show notification if we're already up to (or ahead of) the first what's new message.
            if (whatsNewVersion < latestMessage.version)
            {
                Logging.Message("displaying what's new message");

                // Show messagebox.
                WhatsNewMessageBox messageBox = MessageBoxBase.ShowModal<WhatsNewMessageBox>();
                messageBox.Title = BOBMod.ModName + " " + BOBMod.Version;
                messageBox.DSAButton.eventClicked += (component, clickEvent) => DontShowAgain();
                messageBox.SetMessages(whatsNewVersion, WhatsNewMessages);
            }
        }
    }


    /// <summary>
    /// Version message struct.
    /// </summary>
    public struct WhatsNewMessage
    {
        public Version version;
        public string versionHeader;
        public int betaVersion;
        public bool messageKeys;
        public string[] messages;
    }
}