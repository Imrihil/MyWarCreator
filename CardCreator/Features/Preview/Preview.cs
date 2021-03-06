﻿using CardCreator.Features.Cards;
using CardCreator.Features.Cards.Model;
using CardCreator.Features.Drawing;
using CardCreator.Features.Fonts;
using CardCreator.Settings;
using MediatR;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace CardCreator.Features.Preview
{
    public sealed class Preview : IPreview, IDisposable
    {
        private FileInfo File { get; }

        private int CurrentPosition { get; set; }
        private int MaxPosition => CardsElements?.Count ?? 0;
        public bool GenerateImages { get; private set; }

        private CardSchema CardSchema { get; set; }
        private List<List<string>> CardsElements { get; set; }
        private Dictionary<CardImagesKey, Image> CardImages { get; }

        private readonly TextSettings textSettings;
        private readonly IMediator mediator;
        private readonly IFontProvider fontProvider;
        private readonly IImageProvider imageProvider;
        private readonly IIconProvider iconProvider;

        private readonly Color gridColor;
        private readonly Font gridFont;

        private bool disposed = false;

        public Preview(TextSettings settings, IMediator mediator, IFontProvider fontProvider, IImageProvider imageProvider, IIconProvider iconProvider, string filePath, bool generateImages)
        {
            textSettings = settings;
            this.mediator = mediator;
            this.fontProvider = fontProvider;
            this.imageProvider = imageProvider;
            this.iconProvider = iconProvider;

            gridColor = Color.FromArgb(128, 255, 0, 0);
            gridFont = new Font(fontProvider.TryGet(string.Empty), 12, FontStyle.Bold, GraphicsUnit.Pixel);

            CardImages = new Dictionary<CardImagesKey, Image>();
            File = new FileInfo(filePath);
            GenerateImages = generateImages;
        }

        public async Task<BitmapImage> GetImage(int gridWidth, int gridHeight) =>
           (await GetImage(CurrentPosition, gridWidth, gridHeight)).ToBitmapImage();

        public async Task<BitmapImage> Next(int gridWidth, int gridHeight) =>
            (await GetImage((CurrentPosition + 1) % MaxPosition, gridWidth, gridHeight)).ToBitmapImage();

        public async Task<BitmapImage> Previous(int gridWidth, int gridHeight) =>
            (await GetImage(CurrentPosition > 0 ? CurrentPosition - 1 : MaxPosition - 1, gridWidth, gridHeight)).ToBitmapImage();

        private async Task<Image> GetImage(int position, int gridWidth, int gridHeight)
        {
            CurrentPosition = position;
            if (CardSchema == null)
                await Refresh(GenerateImages);

            var key = new CardImagesKey(CurrentPosition, gridWidth, gridHeight);
            if (!CardImages.TryGetValue(key, out var cardImage))
            {
                using var card = new Card(textSettings, imageProvider, iconProvider, CardSchema, CardsElements[CurrentPosition], File.DirectoryName, GenerateImages);
                cardImage = card.Image.GetNewBitmap();
                CardImages.Add(key, cardImage);

                using var graphics = Graphics.FromImage(cardImage);
                graphics.DrawGrid(gridWidth, gridHeight, cardImage.Width, cardImage.Height, gridColor, gridFont);
            }

            return cardImage;
        }

        public async Task Refresh(bool generateImages)
        {
            GenerateImages = generateImages;
            ClearCache();

            var readCardFile = await mediator.Send(new ReadCardFileCommand(null, File));

            CardsElements = readCardFile.CardsRepetitions
                .Zip(readCardFile.CardsElements, (cardRepetition, cardElements) => new KeyValuePair<List<string>, int>(cardElements, cardRepetition))
                .Where(kv => kv.Value > 0).Select(kv => kv.Key).ToList();
            CardSchema = new CardSchema(null, fontProvider, imageProvider, readCardFile.CardSchemaParams, readCardFile.ElementSchemasParams, File.DirectoryName, GenerateImages);
        }

        private void ClearCache()
        {
            foreach (var image in CardImages.Values)
                image.Dispose();
            CardImages.Clear();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                CardSchema.Dispose();
                foreach (var image in CardImages.Values)
                    image.Dispose();
            }

            disposed = true;
        }

        private struct CardImagesKey
        {
            public CardImagesKey(int position, int gridWidth, int gridHeight)
            {
                Position = position;
                GridWidth = gridWidth;
                GridHeight = gridHeight;
            }

            private readonly int Position;
            private readonly int GridWidth;
            private readonly int GridHeight;
        }
    }
}
