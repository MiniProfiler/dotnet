namespace Sample.WebForms.Account
{
    using System;
    using System.Web;

    /// <summary>
    /// The login page.
    /// </summary>
    public partial class Login : System.Web.UI.Page
    {
        /// <summary>
        /// The page_ load.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="eventArguments">
        /// The event arguments.
        /// </param>
        protected void Page_Load(object sender, EventArgs eventArguments)
        {
            this.RegisterHyperLink.NavigateUrl = "Register.aspx?ReturnUrl=" + HttpUtility.UrlEncode(Request.QueryString["ReturnUrl"]);
        }
    }
}
