﻿using MyWarCreator.Extensions;
using MyWarCreator.Helpers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyWarCreator.Models
{
    class Skill : Card
    {
        public List<int> PTs { get; set; } = new List<int>();
        public string PTMain { get { return PTs.Any() ? ("PT: " + PTs.FirstOrDefault().ToString()) : ""; } }
        public Rectangle PTMainArea { get; set; } = new Rectangle(29, 331, 53, 20);
        public string PTRune { get { return PTs.Count > 1 ? ("/" + string.Join("/", PTs.Skip(1).Take(3).Select(x => x.ToString()))) : ""; } }
        public Rectangle PTRuneArea { get; set; } = new Rectangle(79, 333, 43, 20);
        public string Attribute { get; set; }
        public Rectangle AttributeArea { get; set; } = new Rectangle(99, 303, 163, 18);
        public string Critical { get; set; }
        public Rectangle CriticalArea { get; set; } = new Rectangle(122, 331, 207, 20);
        public override string FileName { get { return $"{Type} - {Attribute} {Rune} - {Name}"; } }
        public Skill(IList<string> row, string dirPath) : base(row, dirPath)
        {
            RunesArea = new Rectangle(25, 35, 45, 70);
            DescriptionArea = new Rectangle(29, 353, 303, 97);
            MainImageArea = new Rectangle(72, 72, 217, 173);

            if (row.Count < 5)
                throw new ArgumentException("W wierszu znajduje się za mało kolumn by utworzyć kartę!");
            int value;

            Rune = row[0];
            Type = row[1];
            Attribute = row[2];
            Name = row[3];
            Dmg = row[4];
            int.TryParse(row[6], out value);
            if (value != 0) PTs.Add(value);
            if (Rune == "NULL") Rune = null;
            Description = row[8];
            Critical = string.IsNullOrEmpty(row[9]) ? "" : $"[{row[9]}]";
            if (PTs.Any())
            {
                for (int i = 10; i < 14; ++i)
                {
                    int.TryParse(row[i], out value);
                    if (value != 0 && value != PTs.Last()) PTs.Add(value);
                }
            }

            string defaultBackgroudPath = dirPath + "/background.png";
            string backgroundPath = dirPath + $"/background" + (Description.Any() ? "_" + new string(Description.Split().FirstOrDefault().Where(Char.IsLetter).ToArray()).ToLower() : "") + ".png";
            string mainImageFramePath = dirPath + $"/frame" + (Description.Any() ? "_" + new string(Description.Split().FirstOrDefault().Where(Char.IsLetter).ToArray()).ToLower() : "") + ".png";
            if (File.Exists(backgroundPath))
                BackgroundImage = Image.FromFile(backgroundPath);
            else if (File.Exists(defaultBackgroudPath))
                BackgroundImage = Image.FromFile(defaultBackgroudPath);
            if (File.Exists(mainImageFramePath))
                MainImageFrame = Image.FromFile(mainImageFramePath);
            string mainImagePath = dirPath + "/" + Name + ".png";
            if (File.Exists(mainImagePath))
                MainImage = Image.FromFile(mainImagePath);
            if (!string.IsNullOrEmpty(Dmg))
                LoadDiceImage();
        }
        public override void DrawCard(Graphics graphics)
        {
            base.DrawCard(graphics);
            using (Font font = new Font(FontsHelper.pfc.Families.FirstOrDefault(x => x.Name.Contains("Trebuchet MS")), 18, FontStyle.Bold, GraphicsUnit.Pixel))
                graphics.DrawAdjustedString(PTMain, font, Brushes.Black, PTMainArea, FontsHelper.StringFormatLeft, 6);
            using (Font font = new Font(FontsHelper.pfc.Families.FirstOrDefault(x => x.Name.Contains("Trebuchet MS")), 10, FontStyle.Bold, GraphicsUnit.Pixel))
                graphics.DrawAdjustedString(PTRune, font, Brushes.Black, PTRuneArea, FontsHelper.StringFormatLeft, 6);
            using (Font font = new Font(FontsHelper.pfc.Families.FirstOrDefault(x => x.Name.Contains("Trebuchet MS")), 10, FontStyle.Regular, GraphicsUnit.Pixel))
                graphics.DrawAdjustedString(Critical, font, Brushes.Black, CriticalArea, FontsHelper.StringFormatRight, 6);
            using (Font font = new Font(FontsHelper.pfc.Families.FirstOrDefault(x => x.Name.Contains("Akvaleir")), 12, FontStyle.Bold, GraphicsUnit.Pixel))
                graphics.DrawAdjustedString(Attribute, font, Brushes.Black, AttributeArea, FontsHelper.StringFormatCentered, 6);
        }
    }
}
