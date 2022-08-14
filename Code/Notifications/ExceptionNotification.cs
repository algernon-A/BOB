// <copyright file="ExceptionNotification.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace BOB
{
    using AlgernonCommons.Notifications;

    /// <summary>
    /// Message box to show exception messages.
    /// </summary>
    public class ExceptionNotification : ListNotification
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionNotification"/> class.
        /// </summary>
        public ExceptionNotification()
        {
            // Add event handler to close button to clear exception display flags.
            CloseButton.eventClicked += (c, p) => BOBPanelManager.ClearException();

            // Display the exception along with accompanying text.
            AddParas("Whoops, an exception occured in BOB, the tree and prop replacer.", "Please send a copy of your output log to algernon so the problem can be fixed!", "The exeption was: ", BOBPanelManager.ExceptionMessage);
        }
    }
}