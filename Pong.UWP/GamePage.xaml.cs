﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Pong.UWP
{
    public sealed partial class GamePage : Page
    {
        readonly Game.Game1 _game;

        public GamePage()
        {
            this.InitializeComponent();

            // Disable Xbox overscan (https://learn.microsoft.com/en-us/windows/uwp/xbox-apps/turn-off-overscan)
            Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().SetDesiredBoundsMode(Windows.UI.ViewManagement.ApplicationViewBoundsMode.UseCoreWindow);

            // Create the game.
            var launchArguments = string.Empty;
            _game = MonoGame.Framework.XamlGame<Game.Game1>.Create(launchArguments, Window.Current.CoreWindow, swapChainPanel);
        }
    }
}
