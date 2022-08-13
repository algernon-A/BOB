﻿namespace BOB
{
    using AlgernonCommons.Notifications;

    /// <summary>
    /// Message box to show exception messages.
    /// </summary>
    public class ExceptionNotification : ListNotification
    {
        /// <summary>
        /// Constructor - does everything we need here.
        /// </summary>
        public ExceptionNotification()
        {
            // Add event handler to close button to clear exception display flags.
            CloseButton.eventClicked += (control, clickEvent) => { InfoPanelManager.wasException = false; InfoPanelManager.displayingException = false; };

            /// Display the exception along with accompanying text.
            AddParas("Whoops, an exception occured in BOB, the tree and prop replacer.", "Please send a copy of your output log to algernon so the problem can be fixed!", "The exeption was: ", InfoPanelManager.exceptionMessage);
        }
    }
}