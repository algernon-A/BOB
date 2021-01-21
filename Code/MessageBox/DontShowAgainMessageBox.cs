using ColossalFramework.UI;


namespace BOB.MessageBox
{
    /// <summary>
    /// Message box with separate pargaraphs and/or lists of dot points, with 'close' and 'dont show again' buttons.
    /// </summary>
    public class DontShowAgainMessageBox : ListMessageBox
    {
        // Don't Show Again button.
        private UIButton dsaButton;

        // Accessor.
        public UIButton DSAButton => dsaButton;

        /// <summary>
        /// Adds buttons to the message box.
        /// </summary>
        public override void AddButtons()
        {
            // Add close button.
            closeButton = AddButton(1, 2, Close);
            closeButton.text = Translations.Translate("MES_CLS");

            // Add don't show again button.
            dsaButton = AddButton(2, 2, Close);
            dsaButton.text = Translations.Translate("MES_DSA");
        }
    }
}