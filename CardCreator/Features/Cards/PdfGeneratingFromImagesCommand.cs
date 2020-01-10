﻿using CardCreator.Features.System;
using CardCreator.View;
using MediatR;
using Microsoft.Win32;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CardCreator.Features.Cards
{
    public class PdfGeneratingFromImagesCommand : IRequest<int>
    {
        public string[] FilePaths { get; set; }
        public int Dpi { get; set; }
        public CancellationTokenSource Cts { get; set; }

        public PdfGeneratingFromImagesCommand(string[] filePaths, int dpi, CancellationTokenSource cts)
        {
            FilePaths = filePaths;
            Dpi = dpi;
            Cts = cts;
        }
    }

    public class PdfGeneratingFromImagesHandler : IRequestHandler<PdfGeneratingFromImagesCommand, int>
    {
        private readonly ProcessWindow ProcessWindow;

        public PdfGeneratingFromImagesHandler(ProcessWindow processWindow)
        {
            ProcessWindow = processWindow;
        }

        public async Task<int> Handle(PdfGeneratingFromImagesCommand request, CancellationToken cancellationToken)
        {
            ProcessWindow.RegisterCancelationToken(request.Cts);
            ProcessWindow.Show();

            var firstFile = new FileInfo(request.FilePaths.First());
            if (!firstFile.Exists)
            {
                ProcessWindow.LogMessage($"File {firstFile.Name} not exists, so action cannot be processed.");
                return 0;
            }

            ProcessWindow.LogMessage("Creating document ...");

            using var firstImage = Image.FromFile(request.FilePaths.First());
            using var pdf = new PdfDocument();
            PdfPage pdfPage = null;

            var cardWidth = firstImage.Width * PdfConsts.PointsInInch / request.Dpi;
            var cardHeight = firstImage.Height * PdfConsts.PointsInInch / request.Dpi;
            var cardsInRow = Math.Max(1, (int)((PdfConsts.A4WidthInch * PdfConsts.PointsInInch - 2 * PdfConsts.PageMarginPts) / (cardWidth + PdfConsts.CardsMarginPts)));
            var cardsInCol = Math.Max(1, (int)((PdfConsts.A4HeightInch * PdfConsts.PointsInInch - 2 * PdfConsts.PageMarginPts) / (cardHeight + PdfConsts.CardsMarginPts)));
            var cardsPerPage = cardsInRow * cardsInCol;

            var i = 0;
            var nCard = 0;
            var n = request.FilePaths.Count();
            foreach (var filePath in request.FilePaths)
            {
                try
                {
                    var file = new FileInfo(filePath);
                    using var xImage = XImage.FromFile(filePath);
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

                    ProcessWindow.LogMessage($"{(i + 1).ToOrdinal()} card added to file: {file.Name}.");
                }
                catch (Exception ex)
                {
                    ProcessWindow.LogMessage($"An error occured while processing {(i + 1).ToOrdinal()} card: {ex}");
                }
                ++i;
                ProcessWindow.SetProgress(100 * i / (n + 1));
            }
            try
            {
                var file = SaveFile(pdf, firstFile.DirectoryName);
                if (file != null)
                {
                    ProcessWindow.SetProgress(100);
                    ProcessWindow.LogMessage($"The document {file.Name} saved.");
                }
                else
                {
                    ProcessWindow.LogMessage($"The document cannot be saved.");
                }
            }
            catch (Exception ex)
            {
                ProcessWindow.LogMessage($"An error occured while saving document: {ex}");
                return 0;
            }

            ProcessWindow.LogMessage($"... done.");

            return await Task.FromResult(nCard);
        }

        private FileInfo SaveFile(PdfDocument pdf, string initialDirectory)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter =
                    "Documents (*.pdf)|*.pdf|" +
                    "All files (*.*)|*.*",
                Title = Properties.Resources.ResourceManager.GetString("SavePdf"),
                InitialDirectory = initialDirectory
            };
            if (saveFileDialog.ShowDialog() ?? false)
            {
                var fileName = $"{saveFileDialog.FileName}{(saveFileDialog.FileName.ToLower().EndsWith(".pdf") ? "" : ".pdf")}";
                pdf.Save(fileName);

                return new FileInfo(fileName);
            }

            return null;
        }
    }
}
