﻿using System.IO;
using System.Windows;
using Microsoft.Win32;
using CardCreator.Features.Cards;
using CardCreator.Features.Drawing;
using CardCreator.Features.Fonts;
using CardCreator.Features.Images;
using MediatR;
using System;
using CardCreator.View;
using System.ComponentModel;
using System.Threading;
using System.Configuration;
using CardCreator.Settings;
using Microsoft.Extensions.Options;
using System.Collections.Generic;

namespace CardCreator
{
    // https://www.codeproject.com/Articles/299436/WPF-Localization-for-Dummies
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly AppSettings settings;
        private readonly IMediator mediator;
        private readonly IFontProvider fontProvider;
        private readonly IImageProvider imageProvider;
        private readonly IPainter painter;
        private readonly ICardBuilder cardBuilder;

        private OpenFileDialog ChooseFileDialog { get; }

        public MainWindow(IOptions<AppSettings> settings, IMediator mediator, IFontProvider fontProvider, IImageProvider imageProvider, IPainter painter, ICardBuilder cardBuilder)
        {
            this.settings = settings.Value;
            this.mediator = mediator;
            this.fontProvider = fontProvider;
            this.imageProvider = imageProvider;
            this.painter = painter;
            this.cardBuilder = cardBuilder;

            InitializeComponent();

            ChooseFileDialog = InitializeChooseFileDialog();
            InitializeFonts();
            InitializeControls();
            InitializeButtons();
        }

        private OpenFileDialog InitializeChooseFileDialog()
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Excel files (*.xls;*xlsx)|*.xls;*xlsx";
#if DEBUG
            openFileDialog.InitialDirectory = Path.GetFullPath(Directory.GetCurrentDirectory() + "../../../../../AppData");
#else
            openFileDialog.InitialDirectory = Path.GetFullPath(Directory.GetCurrentDirectory());
#endif
            return openFileDialog;
        }

        private void InitializeFonts()
        {
            fontProvider.Register(Properties.Resources.Akvaléir_Normal_v2007);
            fontProvider.Register(Properties.Resources.colonna_mt);
            fontProvider.Register(Properties.Resources.runic);
            fontProvider.Register(Properties.Resources.runic_altno);
            fontProvider.Register(Properties.Resources.trebuc);
            fontProvider.Register(Properties.Resources.trebucbd);
            fontProvider.Register(Properties.Resources.trebucbi);
            fontProvider.Register(Properties.Resources.trebucit);
        }

        private void InitializeControls()
        {
            GenerateCards_Button.IsEnabled = !string.IsNullOrEmpty(ChooseFileDialog.FileName);
            PreparePdf_Button.IsEnabled = !string.IsNullOrEmpty(ChooseFileDialog.FileName);
        }

        private void InitializeButtons()
        {
            var currentRow = MainGrid.RowDefinitions.Count - 1;
            ISet<string> actionsInRow = new HashSet<string>();
            foreach (var button in settings.Buttons)
            {

            }
        }

        private void ChooseFile_Button_Click(object sender, RoutedEventArgs e)
        {
            if (ChooseFileDialog.ShowDialog() == true)
            {
                var fileInfo = new FileInfo(ChooseFileDialog.FileName);
                Directory_Label.Content = fileInfo.Name;
                ChooseFileDialog.InitialDirectory = fileInfo.Directory.FullName;

                GenerateCards_Button.IsEnabled = !string.IsNullOrEmpty(ChooseFileDialog.FileName);
                PreparePdf_Button.IsEnabled = !string.IsNullOrEmpty(ChooseFileDialog.FileName);
            }
        }

        private void GenerateCards_Button_Click(object sender, RoutedEventArgs e)
        {
            GenerateCard(ChooseFileDialog.FileName);
        }

        private void PreparePdf_Button_Click(object sender, RoutedEventArgs e)
        {
            PreparePdf(ChooseFileDialog.FileName);
        }

        private void GenerateCard(string fileName)
        {
            var cts = new CancellationTokenSource();
            var result = mediator.Send(new CardGeneratingCommand(fileName, cts), cts.Token).GetAwaiter().GetResult();
            Console.WriteLine(result);
        }

        private void PreparePdf(string fileName)
        {
            var cts = new CancellationTokenSource();
            var result = mediator.Send(new PdfPreparingCommand(fileName, cts), cts.Token).GetAwaiter().GetResult();
            Console.WriteLine(result);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            Environment.Exit(0);
        }
    }
}
