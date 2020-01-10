﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CardCreator.Features.Drawing;
using CardCreator.Features.Images;

namespace CardCreator.Features.Cards.Model
{
    public class Card : List<Element>, IDrawable
    {
        public CardSchema CardSchema { get; }
        public string Name => this.FirstOrDefault(element => IsNameElement(element))?.Content;
        public int Repetitions { get; set; }
        private Image Image { get; set; }

        public Card(IImageProvider imageProvider, CardSchema cardSchema, IEnumerable<string> cardElements, string directory) :
            base(cardSchema.Zip(cardElements, (elementSchema, content) =>
                elementSchema.Background == null && string.IsNullOrEmpty(content) ? null :
                    new Element(imageProvider, content, elementSchema, directory))
                .Where(element => element != null))
        {
            CardSchema = cardSchema;

            MergeElementsByName();
        }

        private void MergeElementsByName()
        {
            foreach (var elementsGroup in this.GroupBy(element => element.ElementSchema.Name))
            {
                var all = elementsGroup.Count();
                if (all == 1) continue;

                var position = 0;
                foreach (var element in elementsGroup)
                {
                    element.SetPosition(position++, all);
                }
            }
        }

        public void Draw(Graphics graphics)
        {
            if (CardSchema.Background != null)
                graphics.DrawImage(CardSchema.Background, new Rectangle(0, 0, CardSchema.WidthPx, CardSchema.HeightPx));

            ForEach(element => element.Draw(graphics));
        }

        public Image GetImage()
        {
            if (Image != null) return Image;

            Image = new Bitmap(CardSchema.WidthPx, CardSchema.HeightPx);

            using var graphics = Graphics.FromImage(Image);
            graphics.FillRectangle(Brushes.White, 0, 0, CardSchema.WidthPx, CardSchema.HeightPx);
            graphics.DrawRectangle(Pens.Black, 0, 0, CardSchema.WidthPx - 1, CardSchema.HeightPx - 1);
            Draw(graphics);

            return Image;
        }

        private bool IsNameElement(Element element)
        {
            var name = element.ElementSchema.Name.ToLower();
            return name == "name" || name == "nazwa";
        }
    }
}
