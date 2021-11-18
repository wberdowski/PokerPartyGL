using System;
using System.Collections.Generic;
using System.IO;

namespace PokerParty.Client
{
    public static class CardDeck
    {
        public static Dictionary<PlayingCard, int> cards = new Dictionary<PlayingCard, int>();
        public static Texture3D Texture { get; private set; }

        public static void Load()
        {
            var colors = Enum.GetNames(typeof(PlayingCard.CardColor));
            var colorsV = Enum.GetValues(typeof(PlayingCard.CardColor));
            var values = Enum.GetNames(typeof(PlayingCard.CardValue));
            var valuesV = Enum.GetValues(typeof(PlayingCard.CardValue));

            Texture = new Texture3D(500, 726, 52);

            int idx = 0;

            for (int i = 0; i < colors.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    var file = (values[j].Replace("Num", "") + "_of_" + colors[i] + ".png").ToLower();
                    var filepath = Path.Combine("models/card/textures", file);

                    if (File.Exists(filepath))
                    {
                        Texture.LoadAndAdd(idx, filepath);

                        cards.Add(new PlayingCard(
                            (PlayingCard.CardColor)colorsV.GetValue(i),
                            (PlayingCard.CardValue)valuesV.GetValue(j)
                            ), idx);


                        Console.WriteLine(file + " " + idx);

                        idx++;
                    }
                    else
                    {
                        throw new IOException($"Cannot load file {filepath}.");
                    }
                }
            }
        }
    }
}
