﻿using CardCreator.Features.Cards.Model;
using CardCreator.Features.Fonts;
using CardCreator.Features.Images;
using CardCreator.Features.System;
using CardCreator.Settings;
using CardCreator.View;
using MediatR;
using Microsoft.Extensions.Options;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CardCreator.Features.Cards
{
    public class PdfGeneratingCommand : IRequest<int>
    {
        public string FilePath { get; set; }
        public CancellationTokenSource Cts { get; set; }

        public PdfGeneratingCommand(string filePath, CancellationTokenSource cts)
        {
            FilePath = filePath;
            Cts = cts;
        }
    }

    public class PdfGeneratingHandler : CardGeneratingBaseHandler, IRequestHandler<PdfGeneratingCommand, int>
    {
        public PdfGeneratingHandler(IOptions<AppSettings> settings, IMediator mediator, IFontProvider fontProvider, IImageProvider imageProvider, ProcessWindow processWindow) :
            base(mediator, fontProvider, imageProvider, processWindow)
        { }

        public async Task<int> Handle(PdfGeneratingCommand request, CancellationToken cancellationToken)
        {
            ProcessWindow.RegisterCancelationToken(request.Cts);
            ProcessWindow.Show();

            var file = new FileInfo(request.FilePath);
            if (!file.Exists)
            {
                ProcessWindow.LogMessage($"File {file.Name} not exists, so action cannot be processed.");
                return 0;
            }

            ProcessWindow.LogMessage($"Reading {file.Name} ...");
            var readCardFile = await ReadCardFile(file);
            if (readCardFile == null) return 0;
            ProcessWindow.LogMessage($"... done.");
            ProcessWindow.SetProgress(100.0 / (1.0 + Math.Max(CardSchema.ParamsNumber, ElementSchema.ParamsNumber) + readCardFile.CardsElements.Count));

            ProcessWindow.LogMessage($"Initializing card schemas ...");
            var cardSchema = await GetCardSchema(readCardFile, file.DirectoryName);
            if (cardSchema == null) return 0;
            ProcessWindow.LogMessage($"... done.");
            ProcessWindow.SetProgress(GetProgress(0, readCardFile.CardsElements.Count));

            ProcessWindow.LogMessage("Creating document ...");
            var successes = await GeneratePdf(readCardFile, cardSchema, file);
            ProcessWindow.LogMessage($"... done.");

            return successes;
        }

        private async Task<int> GeneratePdf(ReadCardFileResults readCardFile, CardSchema cardSchema, FileInfo file)
        {
            using var pdf = new PdfDocument();
            PdfPage pdfPage = null;

            var cardWidth = cardSchema.WidthInch * PdfConsts.PointsInInch;
            var cardHeight = cardSchema.HeightInch * PdfConsts.PointsInInch;
            var cardsInRow = Math.Max(1, (int)((PdfConsts.A4WidthInch * PdfConsts.PointsInInch - 2 * PdfConsts.PageMarginPts) / (cardWidth + PdfConsts.CardsMarginPts)));
            var cardsInCol = Math.Max(1, (int)((PdfConsts.A4HeightInch * PdfConsts.PointsInInch - 2 * PdfConsts.PageMarginPts) / (cardHeight + PdfConsts.CardsMarginPts)));
            var cardsPerPage = cardsInRow * cardsInCol;

            var i = 0;
            var successes = 0;
            var nCard = 0;
            foreach (var cardElements in readCardFile.CardsElements)
            {
                try
                {
                    var card = new Card(ImageProvider, cardSchema, cardElements, file.DirectoryName);
                    try
                    {
                        var n = readCardFile.CardsRepetitions[i];
                        if (n > 0)
                        {
                            using var xImage = XImage.FromGdiPlusImage(card.GetImage());
                            for (var j = 0; j < n; ++j)
                            {
                                if (nCard % cardsPerPage == 0)
                                    pdfPage = pdf.AddPage();
                                using var xGraphics = XGraphics.FromPdfPage(pdfPage);
                                xGraphics.DrawImage(
                                    xImage,
                                    nCard % cardsInRow * (cardWidth + PdfConsts.CardsMarginPts) + PdfConsts.PageMarginPts,
                                    nCard % cardsPerPage / cardsInRow * (cardHeight + PdfConsts.CardsMarginPts) + PdfConsts.PageMarginPts,
                                    cardWidth,
                                    cardHeight);
                                ++nCard;
                            }
                            ++successes;
                            ProcessWindow.LogMessage($"{(i + 1).ToOrdinal()} card added to file {n} times: {card.Name}.");
                        }
                    }
                    catch (Exception ex)
                    {
                        ProcessWindow.LogMessage($"An error occured while processing {(i + 1).ToOrdinal()} card {card.Name}: {ex}");
                    }
                }
                catch (Exception ex)
                {
                    ProcessWindow.LogMessage($"An error occured while processing {(i + 1).ToOrdinal()} card: {ex}");
                }
                ++i;
                ProcessWindow.SetProgress(GetProgress(i, readCardFile.CardsElements.Count + 1));
            }
            try
            {
                var fileName = $"{file.Name.Substring(0, file.Name.LastIndexOf("."))}.pdf";
                pdf.Save(Path.Combine(file.DirectoryName, fileName));
                ProcessWindow.SetProgress(GetProgress(i + 1, readCardFile.CardsElements.Count + 1));
                ProcessWindow.LogMessage($"The document {fileName} saved.");
            }
            catch (Exception ex)
            {
                ProcessWindow.LogMessage($"An error occured while saving document: {ex}");
                return 0;
            }

            return await Task.FromResult(successes);
        }
    }
}
