using System.Windows.Input;
using static PowerControlHubApp.Internal.Constants;

namespace PowerControlHubApp.Views;

public partial class TopBarView : ContentView
{
    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(TopBarView), string.Empty);

    public static readonly BindableProperty ShowBackButtonProperty =
        BindableProperty.Create(nameof(ShowBackButton), typeof(bool), typeof(TopBarView), false);

    public static readonly BindableProperty BackCommandProperty =
        BindableProperty.Create(nameof(BackCommand), typeof(ICommand), typeof(TopBarView));

    public static readonly BindableProperty RightButtonTextProperty =
        BindableProperty.Create(nameof(RightButtonText), typeof(string), typeof(TopBarView), string.Empty);

    public static readonly BindableProperty RightButtonCommandProperty =
        BindableProperty.Create(nameof(RightButtonCommand), typeof(ICommand), typeof(TopBarView));

    public static readonly BindableProperty RightButtonIsEnabledProperty =
        BindableProperty.Create(nameof(RightButtonIsEnabled), typeof(bool), typeof(TopBarView), true);

    private static readonly BindablePropertyKey HasRightButtonPropertyKey =
        BindableProperty.CreateReadOnly(nameof(HasRightButton), typeof(bool), typeof(TopBarView), false);

    public static readonly BindableProperty HasRightButtonProperty = HasRightButtonPropertyKey.BindableProperty;

    public event EventHandler RightButtonClicked;

    public TopBarView()
    {
        InitializeComponent();
        UpdateHasRightButton();
    }

    protected override void OnPropertyChanged(string propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName == nameof(RightButtonText) || propertyName == nameof(RightButtonCommand))
        {
            UpdateHasRightButton();
        }
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public bool ShowBackButton
    {
        get => (bool)GetValue(ShowBackButtonProperty);
        set => SetValue(ShowBackButtonProperty, value);
    }

    public ICommand BackCommand
    {
        get => (ICommand)GetValue(BackCommandProperty);
        set => SetValue(BackCommandProperty, value);
    }

    public string RightButtonText
    {
        get => (string)GetValue(RightButtonTextProperty);
        set => SetValue(RightButtonTextProperty, value);
    }

    public ICommand RightButtonCommand
    {
        get => (ICommand)GetValue(RightButtonCommandProperty);
        set => SetValue(RightButtonCommandProperty, value);
    }

    public bool RightButtonIsEnabled
    {
        get => (bool)GetValue(RightButtonIsEnabledProperty);
        set => SetValue(RightButtonIsEnabledProperty, value);
    }

    public bool HasRightButton
    {
        get => (bool)GetValue(HasRightButtonProperty);
        private set => SetValue(HasRightButtonPropertyKey, value);
    }

    private void UpdateHasRightButton()
    {
        HasRightButton = !string.IsNullOrEmpty(RightButtonText) || RightButtonCommand != null;
    }

    internal async void OnBackClicked(object sender, EventArgs e)
    {
        if (BackCommand?.CanExecute(null) == true)
        {
            BackCommand.Execute(null);
        }
        else
        {
            await Shell.Current.GoToAsync(NavBack);
        }
    }

    internal void OnRightButtonClicked(object sender, EventArgs e)
    {
        RightButtonClicked?.Invoke(this, EventArgs.Empty);
    }
}
