using PowerControlHubApp.Internal;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using static PowerControlHubApp.Internal.Constants;
using static PowerControlHubApp.Resources.Localization.AppResources;

namespace PowerControlHubApp.ViewModels
{
    public sealed class RelayViewModel : INotifyPropertyChanged
    {
        private int _state;
        private int _buttonImage;
        private string _shortName;
        private string _longName;
        private int _pin;
        private int _defaultState;
        private int _actionType;
        private int _linkedIndex;

        public int Index { get; set; }

        public string ShortName
        {
            get => _shortName;
            set
            {
                if (_shortName == value)
                    return;

                _shortName = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShortName));
            }
        }

        public string LongName
        {
            get => _longName;
            set
            {
                if (_longName == value)
                    return;

                _longName = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LongName));
            }
        }

        public int Pin
        {
            get => _pin;
            set
            {
                if (_pin == value)
                    return;

                _pin = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Pin));
            }
        }

        public int ButtonImage
        {
            get => _buttonImage;

            set
            {
                if (_buttonImage == value)
                    return;

                _buttonImage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ButtonImage));
            }
        }

        public int DefaultState
        {
            get => _defaultState;
            set
            {
                if (_defaultState == value)
                    return;

                _defaultState = value;
                OnPropertyChanged();
            }
        }

        public int ActionType
        {
            get => _actionType;
            set
            {
                if (_actionType == value)
                    return;

                _actionType = value;
                OnPropertyChanged();
            }
        }

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
                OnPropertyChanged(nameof(State));
            }
        }

        public int LinkedIndex
        {
            get => _linkedIndex;
            internal set
            {
                if (_linkedIndex == value)
                    return;

                _linkedIndex = value;
                OnPropertyChanged();
            }
        }

        public bool IsOn => State == 1;

        public bool IsEnabled => Pin < Constants.UnconfiguredPin;

        /// <summary>
        /// Display name shown in the UI. Returns <see cref="LongName"/> if present,
        /// otherwise falls back to <see cref="ShortName"/>.
        /// </summary>
        public string DisplayName => !string.IsNullOrWhiteSpace(LongName) ? LongName : ShortName;

        /// <summary>
        /// Pin summary for the list sub-label. Falls back to the localized
        /// "Not configured" text when the relay has no pin assigned.
        /// </summary>
        public string PinSummary => IsEnabled ? string.Format(PinFormat, Pin) : SensorNotConfigured;

        /// <summary>
        /// Translates <see cref="ButtonImage"/> into the corresponding panel accent colour.
        /// Accepts both 0-based indices (0-5) and raw Nextion picture IDs (2-7) to
        /// handle values loaded directly from the device before any save normalisation.
        /// Returns <c>Colors.Transparent</c> when unconfigured (255) or unknown.
        /// </summary>
        public Color PanelColor => ButtonImage switch
        {
            RelayColorBlue or NextionImageIdBlue => Color.FromArgb(ColorRelayPanelBlue),
            RelayColorGreen or NextionImageIdGreen => Color.FromArgb(ColorRelayPanelGreen),
            RelayColorGrey or NextionImageIdGrey => Color.FromArgb(ColorRelayPanelGrey),
            RelayColorOrange or NextionImageIdOrange => Color.FromArgb(ColorRelayPanelOrange),
            RelayColorRed or NextionImageIdRed => Color.FromArgb(ColorRelayPanelRed),
            RelayColorYellow or NextionImageIdYellow => Color.FromArgb(ColorRelayPanelYellow),
            _ => Colors.Transparent,
        };

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
