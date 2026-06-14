using PowerControlHubApp.Internal;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using static PowerControlHubApp.Internal.Constants;

namespace PowerControlHubApp.ViewModels
{
    public sealed class RelayViewModel : INotifyPropertyChanged
    {
        private int _state;
        private int _buttonImage;

        public int Index { get; set; }

        public string ShortName { get; set; }

        public string LongName { get; set; }

        public int Pin { get; set; }

        public int ButtonImage
        {
            get => _buttonImage;

            set
            {
                if (_buttonImage == value)
                    return;

                _buttonImage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PanelColor));
            }
        }

        public int DefaultState { get; set; }

        public int ActionType { get; set; }

        public int State
        {
            get => _state;

            set
            {
                if (_state == value)
                    return;

                _state = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsOn));
            }
        }

        public int LinkedIndex { get; internal set; }

        public bool IsOn => State == 1;

        public bool IsEnabled => Pin < Constants.UnconfiguredPin;

        /// <summary>
        /// Display name shown in the UI. Returns <see cref="LongName"/> if present,
        /// otherwise falls back to <see cref="ShortName"/>.
        /// </summary>
        public string DisplayName => !string.IsNullOrWhiteSpace(LongName) ? LongName : ShortName;

        /// <summary>
        /// Translates <see cref="ButtonImage"/> (0-5, 255) into the corresponding
        /// panel accent colour. Returns <c>Colors.Transparent</c> when unconfigured (255).
        /// </summary>
        public Color PanelColor => ButtonImage switch
        {
            RelayColorBlue => Color.FromArgb(ColorRelayPanelBlue),
            RelayColorGreen => Color.FromArgb(ColorRelayPanelGreen),
            RelayColorOrange => Color.FromArgb(ColorRelayPanelOrange),
            RelayColorPurple => Color.FromArgb(ColorRelayPanelPurple),
            RelayColorRed => Color.FromArgb(ColorRelayPanelRed),
            RelayColorYellow => Color.FromArgb(ColorRelayPanelYellow),
            _ => Colors.Transparent,
        };

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
