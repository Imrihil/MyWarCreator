﻿using CardCreator.Features.Drawing;
using CardCreator.Features.Images;
using MyWarCreator.Extensions;
using System;
using System.Drawing;
using System.IO;

namespace CardCreator.Features.Cards.Model
{
    public class Element : IDrawable
    {
        public string Content { get; }
        public ElementSchema ElementSchema { get; private set; }
        private Image Image { get; }

        public Element(IImageProvider imageProvider, string content, ElementSchema elementSchema, string directory)
        {
            Content = content;
            Image = imageProvider.TryGet(Path.Combine(directory, content));
            ElementSchema = elementSchema;
        }

        public void Draw(Graphics graphics)
        {
            if (ElementSchema.Background != null)
                graphics.DrawImage(ElementSchema.Background, ElementSchema.Area);

            if (Image != null)
                graphics.DrawImage(Image, ElementSchema.Area, ElementSchema.StringFormat, ElementSchema.StretchImage);
            else
                graphics.DrawAdjustedString(Content, ElementSchema.Font, ElementSchema.Color, ElementSchema.Area, ElementSchema.MaxSize, ElementSchema.StringFormat, ElementSchema.MinSize, true, ElementSchema.Wrap);
        }

        internal void SetPosition(int position, int all)
        {
            if (ElementSchema.JoinDirection == JoinDirection.None)
                return;

            var shift = ElementSchema.JoinDirection == JoinDirection.Horizontally ?
                ElementSchema.Area.Width / all :
                ElementSchema.Area.Height / all;

            var area = ElementSchema.JoinDirection == JoinDirection.Horizontally ?
                new Rectangle(ElementSchema.Area.X + position * shift, ElementSchema.Area.Y, shift, ElementSchema.Area.Height) :
                new Rectangle(ElementSchema.Area.X, ElementSchema.Area.Y + position * shift, ElementSchema.Area.Width, shift);

            ElementSchema = new ElementSchema(ElementSchema.Name, ElementSchema.Background, area, ElementSchema.Color, ElementSchema.Font,
                ElementSchema.MaxSize, ElementSchema.StringFormat, ElementSchema.Wrap, ElementSchema.StretchImage, ElementSchema.JoinDirection);
        }
    }
}
