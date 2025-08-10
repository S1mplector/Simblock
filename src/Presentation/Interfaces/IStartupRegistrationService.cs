using System;

namespace SimBlock.Presentation.Interfaces
{
    /// <summary>
    /// Abstraction for registering/unregistering the application to start with Windows
    /// and querying the current startup state.
    /// </summary>
    public interface IStartupRegistrationService
    {
        /// <summary>
        /// Returns true if the application is configured to start with Windows for the current user.
        /// </summary>
        bool IsApplicationInStartup();

        /// <summary>
        /// Enables or disables starting the application with Windows for the current user.
        /// </summary>
        /// <param name="enable">True to enable, false to disable.</param>
        void SetStartWithWindows(bool enable);
    }
}
