using System;

namespace osu.Game.Overlays.Settings.Sections.RemoteAudio
{
    public partial class RemoteAudioLoginButton : SettingsButton
    {
        string applicationName; // used to identify what the login button if for

        LoginButtonState state;

        public RemoteAudioLoginButton(string applicationName)
        {
            this.applicationName = applicationName;
        }

        public void SetState(LoginButtonState state, Action action)
        {
            Schedule(() => {
                this.state = state;
                Action = action;
                switch (state)
                {
                    case LoginButtonState.Login:
                        Text = "Login";
                        TooltipText = $"Sign in to your {applicationName} Account";
                        break;
                    case LoginButtonState.Cancel:
                        Text = "Cancel";
                        TooltipText = $"Cancel {applicationName} Login";
                        break;
                    case LoginButtonState.Logout:
                        Text = "Logout";
                        TooltipText = $"Sign out of your {applicationName} Account";
                        break;
                }
            });
        }
    }

    public enum LoginButtonState
    {
        Login,
        Cancel,
        Logout
    }
}
