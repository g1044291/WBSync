namespace WBSync
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            const int width = 1200;
            const int height = 650;

            var info = DeviceDisplay.Current.MainDisplayInfo;
            var screenW = info.Width / info.Density;
            var screenH = info.Height / info.Density;

            return new Window(new MainPage())
            {
                Title = "WBSync",
                Width = width,
                Height = height,
                X = (screenW - width) / 2,
                Y = (screenH - height) / 2,
            };
        }
    }
}
