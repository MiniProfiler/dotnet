namespace Sample.WebForms.Account
{
    using System;
    using System.Web.Security;

    /// <summary>
    /// register a new user.
    /// </summary>
    public partial class Register : System.Web.UI.Page
    {
        /// <summary>
        /// The page load.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="eventArguments">The event arguments.</param>
        protected void Page_Load(object sender, EventArgs eventArguments)
        {
            this.RegisterUser.ContinueDestinationPageUrl = Request.QueryString["ReturnUrl"];
        }

        /// <summary>
        /// register the created user.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="eventArguments">The event arguments.</param>
        protected void RegisterUser_CreatedUser(object sender, EventArgs eventArguments)
        {
            FormsAuthentication.SetAuthCookie(this.RegisterUser.UserName, false /* createPersistentCookie */);

            string continueUrl = this.RegisterUser.ContinueDestinationPageUrl;
            if (string.IsNullOrEmpty(continueUrl))
            {
                continueUrl = "~/";
            }
            Response.Redirect(continueUrl);
        }

    }
}
